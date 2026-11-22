// Adapted from modelcontextprotocol/python-sdk, .github/scripts/pr_intake_gate.js (MIT).
// Policy: CONTRIBUTING.md#opening-a-pull-request. Run only from the trusted base repository.
/*
MIT License

Copyright (c) 2024 Anthropic, PBC

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
'use strict';

const LABEL = 'missing-issue-link'; // marks PRs the gate has closed
const BYPASS_LABEL = 'bypass-issue-check'; // sticky maintainer override
// Keep in sync with the leadership table in the FSIG charter.
const FACILITATORS = new Set(['sambhav', 'pederhp']);
const MARKER = '<!-- require-linked-issue -->';
const BOT_LOGIN = 'github-actions[bot]';
module.exports = async function run({ github, context, core }) {
  const { owner, repo } = context.repo;
  const enforce = process.env.ENFORCE === 'true';
  const contributingUrl = `https://github.com/${owner}/${repo}/blob/main/CONTRIBUTING.md#opening-a-pull-request`;

  // ── Entry points ─────────────────────────────────────────────────────────

  if (context.eventName === 'issues') {
    const issue = context.payload.issue;
    if (issue.pull_request) return;
    const issueNumber = issue.number;
    const recheckClosed = ['assigned', 'reopened'].includes(context.payload.action);
    const candidates = await github.paginate(github.rest.issues.listForRepo, {
      owner, repo, state: recheckClosed ? 'closed' : 'open', per_page: 100,
      ...(recheckClosed ? { labels: LABEL } : {}),
      ...(context.payload.assignee ? { creator: context.payload.assignee.login } : {}),
    });
    const prs = candidates.filter((item) => item.pull_request && closingRefs(item.body).includes(issueNumber));
    // Evaluate each independently so one transient failure doesn't strand
    // the rest (this event won't fire again for the same assignment).
    const failures = [];
    for (const pr of prs) {
      try {
        await evaluate(pr.number, context.payload.action, context.payload.sender?.login, issueNumber);
      } catch (e) {
        failures.push(`#${pr.number}: ${e.message}`);
      }
    }
    if (failures.length) throw new Error(`Could not re-evaluate ${failures.join('; ')}`);
    return;
  }

  if (context.eventName === 'workflow_dispatch') {
    const n = parseInt(process.env.PR_NUMBER_INPUT, 10);
    if (!Number.isInteger(n) || n <= 0) throw new Error(`Bad pr_number input: ${process.env.PR_NUMBER_INPUT}`);
    await evaluate(n, 'dispatch', context.payload.sender?.login);
    return;
  }

  await evaluate(context.payload.pull_request.number, context.payload.action, context.payload.sender?.login);

  // ── The rules ────────────────────────────────────────────────────────────

  async function evaluate(prNumber, action, sender, hintIssue = null) {
    // Always read the PR live; the event payload can be stale by the time a
    // queued run starts.
    const { data: pr } = await github.rest.pulls.get({ owner, repo, pull_number: prNumber });
    const labels = pr.labels.map((l) => l.name);
    const gated = labels.includes(LABEL);
    console.log(`PR #${prNumber} by ${pr.user.login} (${pr.state}${pr.draft ? ', draft' : ''}) — ${action} by ${sender ?? '-'}, enforce=${enforce}`);

    // 0. Scope: open PRs, plus closed PRs the gate closed itself. Merged PRs
    //    and PRs someone closed for other reasons are left alone.
    if (pr.merged_at) return log('merged — nothing to do');
    if (pr.state === 'closed' && !gated) return log('closed by someone else — not ours');

    if (labels.includes(BYPASS_LABEL)) return pass('explicit PR label override');
    if (FACILITATORS.has(pr.user.login.toLowerCase())) return pass('FSIG facilitator');
    if (await isMaintainer(pr.user.login)) return pass('repository maintainer');

    const author = pr.user.login.toLowerCase();
    const refs = closingRefs(pr.body);
    if (hintIssue && refs.includes(hintIssue)) refs.unshift(...refs.splice(refs.indexOf(hintIssue), 1));
    const linked = [];
    for (const num of refs) {
      const issue = await getIssue(num);
      if (!issue) continue; // missing, a PR, closed, or transferred away
      linked.push(num);
      if (issue.assignees.some((a) => a.login.toLowerCase() === author)) return pass(`author is assigned to #${num}`);
    }
    return fail(linked);

    // ── Outcomes ─────────────────────────────────────────────────────────

    async function pass(reason) {
      console.log(`PASS: ${reason}`);
      if (pr.state === 'closed' && !(await reopen(pr, reason))) return;
      if (gated) {
        await removeLabel(prNumber, LABEL);
        await deleteGateComment(prNumber);
      }
    }

    async function fail(linkedIssues) {
      console.log(`FAIL: ${linkedIssues.length ? `not assigned to ${linkedIssues.map((n) => `#${n}`).join(', ')}` : 'no usable issue link'}`);
      await mutate('prepare override label', () => ensureLabelExists(BYPASS_LABEL));
      await addLabel(prNumber, LABEL);
      await upsertGateComment(prNumber, closedComment());
      if (pr.state === 'open') {
        await mutate(`close PR #${prNumber}`, () => github.rest.pulls.update({ owner, repo, pull_number: prNumber, state: 'closed' }));
      }
    }

    function log(msg) {
      console.log(msg);
    }
  }

  // ── Comment text ─────────────────────────────────────────────────────────

  function closedComment() {
    return [
      MARKER,
      'This PR was closed automatically. Start with an open FSIG issue describing the financial-services problem and the MCP capabilities you tried.',
      'After triage, a facilitator or maintainer records the agreed scope and assigns the issue to the contributor. Link it in the PR description with `Fixes #123` (using your issue number). Assignment or editing the link triggers another check and can reopen this PR.',
      'For feedback or help finding the right group, ask in [#financial-services-ig](https://discord.gg/6CSzBmMkjX).',
      `See the [contribution guide](${contributingUrl}). Repository triagers can apply \`${BYPASS_LABEL}\` to this PR for an explicit exception.`,
    ].join('\\n\\n');
  }

  function cannotReopenComment(pr, reason) {
    return [
      MARKER,
      `This PR now passes the intake check (${reason}), but GitHub won't let it be reopened — usually because the branch was force-pushed or deleted while the PR was closed, or because another open PR uses the same branch.`,
      '',
      `If you have another open PR from this branch, please continue there. Otherwise, either push the branch back to \`${pr.head.sha.slice(0, 7)}\` and edit this PR's description to retry, or open a new PR that links the same issue (if a maintainer had waved this one through, mention that so they can do the same there).`,
    ].join('\n');
  }

  // ── Helpers ──────────────────────────────────────────────────────────────

  async function mutate(description, fn) {
    if (!enforce) {
      console.log(`[dry-run] would ${description}`);
      return undefined;
    }
    return fn();
  }

  // A permission lookup failure must not close a maintainer's PR.
  async function isMaintainer(username) {
    try {
      const { data } = await github.rest.repos.getCollaboratorPermissionLevel({ owner, repo, username });
      const p = data.user?.permissions;
      if (!p) throw new Error(`permission response for ${username} has no capability flags`);
      const trusted = Boolean(p.maintain || p.admin);
      console.log(`  ${username}: ${trusted ? 'trusted' : 'not trusted'} (role ${data.role_name || '-'})`);
      return trusted;
    } catch (e) {
      if (e.status === 404) return false;
      throw new Error(`Permission check failed for ${username} (HTTP ${e.status ?? '?'}): ${e.message}`);
    }
  }

  // Issue numbers referenced with a closing keyword, in the forms GitHub itself
  // honors: `Fixes #1`, `closes owner/repo#1`, `Resolved https://github.com/owner/repo/issues/1`.
  function closingRefs(body) {
    const repoRef = `${owner}/${repo}`.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    const re = new RegExp(
      `\\b(?:close[sd]?|fix(?:e[sd])?|resolve[sd]?)\\s*:?\\s*(?:${repoRef}#|#|https?://github\\.com/${repoRef}/issues/)(\\d+)`,
      'gi',
    );
    return [...new Set([...(body || '').matchAll(re)].map((m) => parseInt(m[1], 10)))];
  }

  // The linked issue, or null if it doesn't exist, is actually a PR, isn't
  // open, or has been transferred to another repository.
  async function getIssue(num) {
    let issue;
    try {
      ({ data: issue } = await github.rest.issues.get({ owner, repo, issue_number: num }));
    } catch (e) {
      if (e.status === 404 || e.status === 410) return null;
      throw new Error(`Cannot fetch issue #${num} (HTTP ${e.status ?? '?'}): ${e.message}`);
    }
    if (issue.pull_request || issue.state !== 'open') return null;
    if (!issue.repository_url?.endsWith(`/${owner}/${repo}`)) return null;
    return issue;
  }

  // Reopen a gate-closed PR. GitHub refuses (422) if the branch was rewritten
  // or deleted while closed, or another open PR uses it. Explain that in the
  // comment and make sure the control label is (still) on, so the PR stays
  // gate-managed and a later edit or override retries the reopen.
  async function reopen(pr, reason) {
    try {
      await mutate(`reopen PR #${pr.number}`, () => github.rest.pulls.update({ owner, repo, pull_number: pr.number, state: 'open' }));
      return true;
    } catch (e) {
      if (e.status !== 422) throw e;
      core.warning(`GitHub refused to reopen PR #${pr.number}: ${e.message}`);
      await addLabel(pr.number, LABEL);
      await upsertGateComment(pr.number, cannotReopenComment(pr, reason));
      return false;
    }
  }

  async function addLabel(prNumber, name) {
    await mutate(`add "${name}" to PR #${prNumber}`, async () => {
      await ensureLabelExists(name);
      await github.rest.issues.addLabels({ owner, repo, issue_number: prNumber, labels: [name] });
    });
  }

  async function removeLabel(prNumber, name) {
    await mutate(`remove "${name}" from PR #${prNumber}`, async () => {
      try {
        await github.rest.issues.removeLabel({ owner, repo, issue_number: prNumber, name });
      } catch (e) {
        if (e.status !== 404) throw e;
      }
    });
  }

  async function ensureLabelExists(name) {
    const meta = {
      [LABEL]: ['b76e79', 'Auto-closed: PR needs a linked issue assigned to its author (see CONTRIBUTING.md)'],
      [BYPASS_LABEL]: ['0e8a16', 'Maintainer override for the linked-issue intake gate'],
    }[name];
    try {
      await github.rest.issues.getLabel({ owner, repo, name });
    } catch (e) {
      if (e.status !== 404) throw e;
      try {
        await github.rest.issues.createLabel({ owner, repo, name, color: meta[0], description: meta[1] });
      } catch (createErr) {
        if (createErr.status !== 422) throw createErr; // created concurrently
      }
    }
  }

  // The gate keeps at most one comment per PR: authored by the Actions bot and
  // carrying MARKER. It's created or updated on failure and deleted on pass.
  async function findGateComment(prNumber) {
    const comments = await github.paginate(github.rest.issues.listComments, { owner, repo, issue_number: prNumber, per_page: 100 });
    return comments.find((c) => c.user?.login === BOT_LOGIN && c.body?.includes(MARKER));
  }

  async function upsertGateComment(prNumber, body) {
    const existing = await findGateComment(prNumber);
    if (!existing) {
      await mutate(`comment on PR #${prNumber}`, () => github.rest.issues.createComment({ owner, repo, issue_number: prNumber, body }));
    } else if (existing.body !== body) {
      await mutate(`update the gate comment on PR #${prNumber}`, () => github.rest.issues.updateComment({ owner, repo, comment_id: existing.id, body }));
    }
  }

  async function deleteGateComment(prNumber) {
    const existing = await findGateComment(prNumber);
    if (!existing) return;
    await mutate(`delete the gate comment on PR #${prNumber}`, async () => {
      try {
        await github.rest.issues.deleteComment({ owner, repo, comment_id: existing.id });
      } catch (e) {
        if (e.status !== 404) throw e; // already deleted by a concurrent run
      }
    });
  }
};
