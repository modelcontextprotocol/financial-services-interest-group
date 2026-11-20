# MCP Financial Services Interest Group (FSIG)

The **MCP Financial Services Interest Group (FSIG)** is a forum for financial-services participants to identify shared problems, exchange implementation experience, and connect those problems to existing [Model Context Protocol (MCP)](https://modelcontextprotocol.io) capabilities and community work. Its default output is a clearer problem statement or a useful referral—not a new protocol feature.

**Charter:** [modelcontextprotocol.io/community/interest-groups/financial-services](https://modelcontextprotocol.io/community/interest-groups/financial-services) — mission, scope, leadership, membership, operations, and discussion topics.
**Calendar:** [MCP Meetings — Finance](https://meet.modelcontextprotocol.io/tag/finance)
**Meeting notes & agendas:** [FSIG Meeting Notes](https://docs.google.com/document/d/19HwCw7i4k-omABGnRYGGuLdMJNXQc3rs_QRgJxPhQU8/edit)
**Discord:** [#financial-services-ig](https://discord.gg/6CSzBmMkjX)

## Mission

The FSIG brings financial-industry stakeholders together to:

- **Identify common problems:** Share cross-institution use cases, constraints and interoperability gaps, distinguishing common needs from institution-specific choices.
- **Share existing approaches:** Discuss publicly shareable implementation patterns, including how existing MCP primitives can be combined within the current specification.
- **Connect to existing work:** Refer unresolved protocol gaps and interested participants to an existing MCP Working Group (WG), rather than duplicating its work.
- **Build a case for new work when warranted:** For a substantial, well-evidenced gap with no relevant WG, gather interest across IGs and the wider community and help contributors propose a new WG through the established process.

This is FSIG's preferred operating model, not a prohibition on exploratory technical work. The [MCP group governance](https://modelcontextprotocol.io/community/working-interest-groups) defines IG outputs as problem statements, use cases, recommendations and input to WGs and SEPs; the [FSIG charter](https://modelcontextprotocol.io/community/interest-groups/financial-services) also permits extension development, which remains subject to the broader MCP governance and is not the default response to a problem.

See the [charter](https://modelcontextprotocol.io/community/interest-groups/financial-services) for the full scope, leadership, membership, and current discussion topics.

## Interest Groups vs Working Groups

The authoritative [MCP Working Groups and Interest Groups governance](https://modelcontextprotocol.io/community/working-interest-groups) (adopted through [SEP-2149](https://modelcontextprotocol.io/seps/2149)) distinguishes **IGs that identify and discuss problems** from **WGs that build scoped solutions**.

| | Interest Group (FSIG) | Working Group |
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

## Related Working Groups

Examples of existing MCP working groups and experimental work relevant to FSIG discussions. These are possible destinations for requirements and interested contributors, not FSIG-owned deliverables:

- **Interceptors**
  - [Working group](https://modelcontextprotocol.io/community/working-groups/interceptors)
  - [experimental-ext-interceptors](https://github.com/modelcontextprotocol/experimental-ext-interceptors)
- **Variants**
  - [experimental-ext-variants](https://github.com/modelcontextprotocol/experimental-ext-variants)
- **Skills over MCP**
  - [Working group](https://modelcontextprotocol.io/community/working-groups/skills-over-mcp)
  - [experimental-ext-skills](https://github.com/modelcontextprotocol/experimental-ext-skills)

## Meetings

**Proposed cadence:** Monthly, for up to 60 minutes initially. If recurring topics, participation or the volume of agenda proposals warrants it, Facilitators can reconsider the frequency. This is subject to the [official charter amendment](https://github.com/modelcontextprotocol/modelcontextprotocol/pull/3572); consult the [MCP finance calendar](https://meet.modelcontextprotocol.io/tag/finance) for the current schedule.

FSIG meetings are open, free-flowing industry discussions guided by a **lightweight agenda submitted in advance**, not fixed time slots. Recurring prompts may include introductions and ways to contribute to MCP, common financial-services problems, existing approaches with current MCP primitives, relevant WG/IG activity, and follow-ups. Anyone can propose additional topics ahead of a meeting. Facilitators help prioritize discussion and connect detailed technical work with the appropriate WG.

Meetings follow [MCP meeting requirements](https://modelcontextprotocol.io/community/working-interest-groups#meetings-and-channels): open attendance, calendar publication at least seven days ahead, a public agenda in GitHub Discussions before the meeting, and public notes within 48 hours. See the [meeting notes](https://docs.google.com/document/d/19HwCw7i4k-omABGnRYGGuLdMJNXQc3rs_QRgJxPhQU8/edit) and [antitrust policy](https://modelcontextprotocol.io/community/antitrust).

## Example Discussion Areas

These are areas for identifying shared problems and existing approaches, not commitments to design new extensions:

1. **Regulatory Audit & Attestation** — Audit requirements and current approaches to recording and verifying activity
2. **Data Lineage & Citation** — Provenance, consent metadata and source attribution needs
3. **Guardrails & Security** — Common trust, verification and security constraints
4. **Policy Enforcement** — How existing MCP primitives and surrounding systems support tool and data policies

## Repository Structure

```
/
├── README.md         # This file
├── CONTRIBUTING.md   # How to contribute
├── extensions/       # MCP extension specifications and examples
└── docs/             # Guides, whitepapers, and documentation
```

## How to Participate

Anyone can participate — no approval or membership is required:

- **Join** the [#financial-services-ig](https://discord.gg/6CSzBmMkjX) channel on the [MCP Discord](https://modelcontextprotocol.io/community/communication#discord)
- **Attend** the bi-weekly meetings
- **Contribute** by opening issues, submitting PRs, or providing feedback — see [CONTRIBUTING.md](./CONTRIBUTING.md)

Leadership and membership are listed in the [charter](https://modelcontextprotocol.io/community/interest-groups/financial-services#membership).

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
