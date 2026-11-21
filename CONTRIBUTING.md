# Contributing to FSIG

Thank you for your interest in contributing to the MCP Financial Services Interest Group! For the group's mission, scope, leadership, and current discussion topics, see the [charter](https://modelcontextprotocol.io/community/interest-groups/financial-services).

## How to Participate

Anyone can participate — no approval or membership is required. You can:

- Join the [#financial-services-ig](https://discord.gg/6CSzBmMkjX) channel on the [MCP Discord](https://modelcontextprotocol.io/community/communication#discord)
- Attend the monthly meetings (see [Meetings](#meetings))
- Browse and open [issues](https://github.com/modelcontextprotocol/financial-services-interest-group/issues)
- Describe a concrete problem in an issue. After triage, open a PR for the agreed work.

General contribution norms follow the [MCP Contributing Guide](https://github.com/modelcontextprotocol/.github/blob/main/CONTRIBUTING.md).

## Communication Channels

| Channel | Purpose |
| :--- | :--- |
| [Discord #financial-services-ig](https://discord.gg/6CSzBmMkjX) | Suggest agenda topics and ask questions |
| [GitHub Issues](https://github.com/modelcontextprotocol/financial-services-interest-group/issues) | Proposals, bugs, and structured discussions |
| [MCP GitHub Discussions · Meeting Notes - Other](https://github.com/modelcontextprotocol/modelcontextprotocol/discussions/categories/meeting-notes-other) | Read the agenda before the meeting and find the notes in the same thread afterward |

## Meetings

FSIG meets monthly for up to 60 minutes. See the [MCP finance calendar](https://meet.modelcontextprotocol.io/tag/finance) for upcoming meetings.

To suggest a topic, reply to the facilitators’ pre-meeting thread in [#financial-services-ig](https://discord.gg/6CSzBmMkjX). Find the agenda in [MCP GitHub Discussions · Meeting Notes - Other](https://github.com/modelcontextprotocol/modelcontextprotocol/discussions/categories/meeting-notes-other) before the meeting. Facilitators publish notes in the same discussion within 48 hours after the call.

## Decision-Making

The FSIG is an **Interest Group**: it identifies common problems, shares existing implementation approaches and makes non-binding recommendations and referrals through rough consensus. It may adopt formal procedures for internal decisions, but cannot make binding MCP specification decisions or formally sponsor SEPs. Working Groups make binding decisions within their delegated scope and may sponsor SEPs. See the [IG/WG comparison](./README.md#interest-groups-vs-working-groups), MCP [group governance](https://modelcontextprotocol.io/community/working-interest-groups), and the FSIG [charter](https://modelcontextprotocol.io/community/interest-groups/financial-services#scope).

## Antitrust Compliance

All contributions and discussions must comply with the [MCP Antitrust Policy](https://modelcontextprotocol.io/community/antitrust).

**Do not discuss:** pricing, costs, or margins; customer lists or strategies; market division or competitive coordination; confidential business information.

**Focus on:** technical standards and specifications, implementation approaches, interoperability patterns, documentation, and testing.

## Bringing a Problem to FSIG

Start by finding or opening a [GitHub issue](https://github.com/modelcontextprotocol/financial-services-interest-group/issues). Describe the financial-services workflow, the MCP interaction, and the difficulty you encountered. A small, publicly shareable example is enough. If you would like feedback or help choosing the next step, bring the issue to [#financial-services-ig](https://discord.gg/6CSzBmMkjX).

Useful questions for triage include

1. **Shared problem:** Who encounters it, and what evidence suggests it matters across institutions?
2. **What works today:** Which existing MCP primitives, combinations of capabilities or publicly shareable industry patterns address it?
3. **Remaining gap:** Is this a protocol limitation, or a documentation, discovery, integration or deployment problem?
4. **Related work:** Which WGs or other IGs already discuss the issue, and who is interested in contributing?

The preferred output is a documented answer or a clear problem statement and referral—not automatically an extension or SEP. This approach implements the IG's problem-identification role under [MCP group governance](https://modelcontextprotocol.io/community/working-interest-groups) while remaining within the published [FSIG charter](https://modelcontextprotocol.io/community/interest-groups/financial-services).

## Routing Unresolved Problems

- **Existing answer:** Document reusable MCP patterns, references and limitations.
- **Existing WG:** Take the shared requirements and interested participants to the WG responsible for that area.
- **No matching WG:** For a widely acknowledged gap with a clear potential deliverable, gather support across IGs and the wider MCP community, and help interested contributors propose a WG under the [formal formation rules](https://modelcontextprotocol.io/community/working-interest-groups#forming-a-group). Cross-IG support is useful, not mandatory; group and leadership sponsorship and a charter are still required.

FSIG participants can discuss possible solutions and collaborate with relevant WGs on prototypes, including exploratory finance-specific extensions. See the [charter](https://modelcontextprotocol.io/community/interest-groups/financial-services#scope).

## Opening a Pull Request

Start with an issue for any proposed change, including an extension. During triage, facilitators and contributors consider whether existing MCP capabilities address the problem, whether documenting a pattern would help, or whether a WG should take it forward. If an experiment would answer a remaining question, record that question, the scope, and the relevant WG on the issue. A facilitator or maintainer records the agreed next step and assigns the issue to the contributor when a PR is welcome.

Link that open FSIG issue in the PR description with a closing keyword, for example `Fixes #123`. Drafts follow the same rule. The intake workflow closes PRs without a qualifying link or assignment and explains how to proceed. Editing the description or receiving the assignment triggers another check, so you can use the same PR.

A repository triager can apply `bypass-issue-check` to a PR to keep it open or reopen it after automatic closure. Repository maintainers (Maintain or Admin permission) and the [charter’s named facilitators](https://modelcontextprotocol.io/community/interest-groups/financial-services#leadership) are exempt from automatic closure. Approval to experiment covers the scope recorded on the issue. Adoption of an extension or specification change follows the relevant WG and SEP process.

### SEP participation

FSIG participants can contribute requirements, evidence and draft proposals. **FSIG cannot formally sponsor a SEP.** The [SEP template](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/seps/TEMPLATE.md) specifies a sponsor acting for a WG or the Core Maintainers (or `None` while unsponsored). The [SEP Guidelines](https://modelcontextprotocol.io/community/sep-guidelines) allow an unsponsored draft PR, but require WG sponsorship or exceptional Core Maintainer sponsorship before the proposal enters the SEP process. The sponsor advances it through review; Core Maintainers decide acceptance.

## Getting Help

- Ask in the [#financial-services-ig](https://discord.gg/6CSzBmMkjX) Discord channel
- Open an [issue](https://github.com/modelcontextprotocol/financial-services-interest-group/issues)
- Attend a meeting
