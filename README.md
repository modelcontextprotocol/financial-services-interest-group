# MCP Financial Services Interest Group (FSIG)

The **MCP Financial Services Interest Group (FSIG)** is a forum for financial-services participants to identify shared problems, exchange implementation experience, and connect those problems to existing [Model Context Protocol (MCP)](https://modelcontextprotocol.io) capabilities and community work. Its default output is a clearer problem statement or a useful referral—not a new protocol feature.

- **Charter:** [modelcontextprotocol.io/community/interest-groups/financial-services](https://modelcontextprotocol.io/community/interest-groups/financial-services) — mission, scope, leadership, membership, operations, and discussion topics.
- **Calendar:** [MCP Meetings — Finance](https://meet.modelcontextprotocol.io/tag/finance)
- Find agendas and meeting notes in [MCP GitHub Discussions · Meeting Notes - Other](https://github.com/modelcontextprotocol/modelcontextprotocol/discussions/categories/meeting-notes-other).
- **Discord:** [#financial-services-ig](https://discord.gg/6CSzBmMkjX)

## Mission

The FSIG brings financial-industry stakeholders together to:

- **Identify common problems:** Share cross-institution use cases, constraints and interoperability gaps, distinguishing common needs from institution-specific choices.
- **Share existing approaches:** Discuss publicly shareable implementation patterns, including how existing MCP primitives can be combined within the current specification.
- **Connect to existing work:** Refer unresolved protocol gaps and interested participants to an existing MCP Working Group (WG), rather than duplicating its work.
- **Build a case for new work when warranted:** For a substantial, well-evidenced gap with no relevant WG, gather interest across IGs and the wider community and help contributors propose a new WG through the established process.

FSIG can discuss possible solutions and collaborate with relevant WGs on prototypes, including exploratory finance-specific extensions.

See the [charter](https://modelcontextprotocol.io/community/interest-groups/financial-services) for the full scope, leadership, membership, and current discussion topics.

## Interest Groups vs Working Groups

The authoritative [MCP Working Groups and Interest Groups governance](https://modelcontextprotocol.io/community/working-interest-groups) (adopted through [SEP-2149](https://github.com/modelcontextprotocol/modelcontextprotocol/pull/2149)) distinguishes **IGs that identify and discuss problems** from **WGs that build scoped solutions**.

| Responsibility | Interest Group (FSIG) | Working Group |
| --- | --- | --- |
| Primary purpose | Identify and discuss common problems, exchange expertise, and route requirements | Build a concrete solution |
| Typical outputs | Problem statements, use cases, recommendations, input to WGs and SEPs | SEPs, reference implementations, or stewardship of endorsed projects |
| Decisions | Non-binding rough consensus (formal internal procedures are possible) | Binding within delegated scope, subject to escalation |
| Leadership | Facilitators | Leads |
| Duration | Ongoing while relevant | Until deliverables are complete |
| SEP sponsorship | Cannot sponsor as an IG | Can sponsor within scope following WG agreement |

### How FSIG handles a problem

1. **Understand the common need.** Collect cross-institution use cases, constraints and evidence; articulate the problem without assuming a particular technical solution.
2. **Find what works today.** Share existing industry patterns and ways to compose current MCP primitives. Document reusable examples and limitations before suggesting protocol changes.
3. **Connect to existing groups.** If a genuine gap remains, identify the relevant WG and pass along the problem statement, evidence and interested contributors. Related IGs can help refine requirements.
4. **Build momentum for a WG only when justified.** If no WG covers a widely acknowledged problem with a clear potential deliverable, gather cross-IG/community interest and prospective contributors, then follow [MCP's WG formation process](https://modelcontextprotocol.io/community/working-interest-groups#forming-a-group). Cross-IG interest is useful evidence, not a formal prerequisite or sufficient approval: the group and its leadership require the prescribed sponsorship and charter process.

This progression reflects the [governance document's usual IG-to-WG path](https://modelcontextprotocol.io/community/working-interest-groups#working-groups-and-interest-groups). It is an FSIG workflow preference, **not** an additional mandatory MCP-wide gate.

### SEP sponsorship boundary

FSIG participants can contribute problem statements, requirements, implementation evidence and draft proposals, but the IG itself does not sponsor SEPs. The [official SEP template](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/seps/TEMPLATE.md) requires the sponsor to be named on behalf of a WG or the Core Maintainers, or to be `None` until sponsorship is secured. Under the [SEP Guidelines](https://modelcontextprotocol.io/community/sep-guidelines#sponsors), a proposal may be developed as an unsponsored draft PR, but cannot enter the formal SEP process until a WG agrees to sponsor it or, exceptionally, the Core Maintainers do. Sponsorship is not approval.

## Related Working Groups and Experimental Extensions

Examples of existing MCP working groups and experimental work relevant to FSIG discussions. These are possible destinations for requirements and interested contributors, not FSIG-owned deliverables:

- [Filesystems WG](https://modelcontextprotocol.io/community/working-groups/filesystems) explores how clients discover, read, and update filesystem-shaped MCP resources. One financial-services use case is finding a filing in a collection and retrieving it by URI.
- [Core Primitives WG](https://github.com/modelcontextprotocol/core-primitives-wg) is the forum for follow-up on the [granular citations proposal](https://github.com/modelcontextprotocol/modelcontextprotocol/pull/3094). For example, a client could use a citation to show the exact table row in a filing that supports a reported figure.
- **Interceptors**
  - [Working group](https://modelcontextprotocol.io/community/working-groups/interceptors)
  - [experimental-ext-interceptors](https://github.com/modelcontextprotocol/experimental-ext-interceptors)
- **Variants**
  - [experimental-ext-variants](https://github.com/modelcontextprotocol/experimental-ext-variants)
- **Skills over MCP**
  - [Working group](https://modelcontextprotocol.io/community/working-groups/skills-over-mcp)
  - [experimental-ext-skills](https://github.com/modelcontextprotocol/experimental-ext-skills)

## Meetings

FSIG meets monthly for up to 60 minutes. Facilitators may revisit the cadence as interest and agenda volume change.

FSIG meetings are open, free-flowing industry discussions. Recurring prompts include introductions, common problems, existing MCP approaches, WG/IG activity and follow-ups.

Before each meeting, facilitators invite topic suggestions in [#financial-services-ig](https://discord.gg/6CSzBmMkjX). Anyone can reply with a question or use case. Facilitators then post the agenda or a link to it in [MCP GitHub Discussions · Meeting Notes - Other](https://github.com/modelcontextprotocol/modelcontextprotocol/discussions/categories/meeting-notes-other). Within 48 hours after the meeting, they publish the notes in that same discussion.

The [MCP finance calendar](https://meet.modelcontextprotocol.io/tag/finance) is the source for scheduled meetings. Meetings are open to everyone and appear on the calendar at least seven days in advance. The [Code of Conduct](https://github.com/modelcontextprotocol/.github/blob/main/CODE_OF_CONDUCT.md) and [Antitrust Policy](https://modelcontextprotocol.io/community/antitrust) apply.

## Example Discussion Areas

These questions illustrate topics participants can bring to FSIG.

| Topic                                    | Illustrative discussion question                                                                                             |
| ---------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------- |
| **Auditability and operational control** | If a connection drops after an order is submitted, what can the client establish about the outcome?                          |
| **Data lineage and citation**            | Can a reader trace a reported figure to the filing and reporting period it came from?                                        |
| **Data handling**                        | When a result combines licensed market data with a public filing, can the client tell which data the restriction applies to? |

## Repository Structure

```
/
├── README.md         # This file
├── CONTRIBUTING.md   # How to contribute
├── extensions/       # Approved experiments linked to triaged issues
└── docs/             # Shared use cases, implementation approaches, and gaps
```

## How to Participate

Anyone can participate — no approval or membership is required:

- **Join** the [#financial-services-ig](https://discord.gg/6CSzBmMkjX) channel on the [MCP Discord](https://modelcontextprotocol.io/community/communication#discord)
- **Attend** the [monthly meetings](#meetings)
- **Start with an [issue](https://github.com/modelcontextprotocol/financial-services-interest-group/issues)** describing the problem. Ask in Discord if you need feedback or help finding the right group. See [how FSIG triages issues before a PR](./CONTRIBUTING.md#opening-a-pull-request).

The charter lists the [facilitators](https://modelcontextprotocol.io/community/interest-groups/financial-services#leadership) and [participants](https://modelcontextprotocol.io/community/interest-groups/financial-services#membership).

## Contributing

We welcome contributions of all kinds. Please review:

- **[CONTRIBUTING.md](./CONTRIBUTING.md)** — Contribution guidelines
- **[MCP Code of Conduct](https://github.com/modelcontextprotocol/.github/blob/main/CODE_OF_CONDUCT.md)** — Community standards
- **[MCP Antitrust Policy](https://modelcontextprotocol.io/community/antitrust)** — Required compliance

## Related Resources

- [Model Context Protocol](https://modelcontextprotocol.io)
- [MCP Specification](https://modelcontextprotocol.io/specification)
- [MCP GitHub Organization](https://github.com/modelcontextprotocol)
- [MCP Discord Community](https://modelcontextprotocol.io/community/communication#discord)
