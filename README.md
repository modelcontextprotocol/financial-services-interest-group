# MCP Financial Services Interest Group (FSIG)

The **MCP Financial Services Interest Group (FSIG)** is a collaborative community advancing the [Model Context Protocol (MCP)](https://modelcontextprotocol.io) for use in regulated financial services environments.

**Charter:** [modelcontextprotocol.io/community/interest-groups/financial-services](https://modelcontextprotocol.io/community/interest-groups/financial-services) — mission, scope, leadership, membership, operations, and discussion topics.
**Calendar:** [MCP Meetings — Finance](https://meet.modelcontextprotocol.io/tag/finance)
**Meeting notes & agendas:** [FSIG Meeting Notes](https://docs.google.com/document/d/19HwCw7i4k-omABGnRYGGuLdMJNXQc3rs_QRgJxPhQU8/edit)
**Discord:** [#financial-services-ig](https://discord.gg/6CSzBmMkjX)

## Mission

The FSIG coordinates financial-industry stakeholders to:

- **Identify and document** finance-specific interoperability problems, use cases, requirements, and potential extensions (compliance/auditability, data lineage & provenance, guardrails & attestation, etc.)
- **Contribute to spec enhancements** by developing proposals and collaborating with a sponsoring MCP Working Group (or, exceptionally, the Core Maintainers)
- **Build consensus and recommend interoperable approaches** across regulated institutions
- **Provide a neutral forum** to address regulatory and risk constraints with open technical solutions

See the [charter](https://modelcontextprotocol.io/community/interest-groups/financial-services) for the full scope, leadership, membership, and current discussion topics.

## Interest Groups vs Working Groups

Under the [MCP Working and Interest Group governance](https://modelcontextprotocol.io/community/working-interest-groups), **Interest Groups (IGs) identify and discuss problems; Working Groups (WGs) develop concrete solutions**.

| | Interest Group (FSIG) | Working Group |
| --- | --- | --- |
| Purpose | Identify problems, gather requirements, exchange expertise and build rough consensus | Deliver a scoped technical solution |
| Typical outputs | Problem statements, use cases, recommendations, input to SEPs and WGs | SEPs, reference implementations, or stewardship of endorsed MCP projects |
| Decisions | Non-binding rough consensus; an IG may adopt formal procedures for internal decisions | Binding within delegated scope, subject to escalation and Core Maintainer authority |
| Leadership | Facilitators | Leads |
| Duration | Ongoing while relevant | Until agreed deliverables are complete |
| SEP sponsorship | **Cannot formally sponsor a SEP**; members may author, prototype and contribute | **Can sponsor SEPs** within the WG's scope, following its decision process |

**How FSIG work becomes a SEP:** FSIG can frame the problem, gather cross-industry requirements, develop proposals and prototypes, and contribute to SEP drafts. Before formal SEP review, an existing WG must agree to sponsor the proposal. If no WG covers the topic, FSIG participants can propose forming one. The Core Maintainers may sponsor a SEP directly as an **exception**, not the default path. Formal sponsorship does not itself approve a SEP; Core Maintainers make the acceptance decision.

An IG can also initiate an [experimental extension](https://blog.modelcontextprotocol.io/posts/mcp-roadmap/) under SEP-2133; experimental work is not the same as formal SEP sponsorship. See the authoritative [SEP Guidelines](https://modelcontextprotocol.io/community/sep-guidelines) for the current workflow and sponsor requirements. Group charters cannot override MCP-wide governance.

## Related Working Groups

FSIG members are actively contributing to these MCP working groups and experimental extensions:

- **Interceptors**
  - [Working group](https://modelcontextprotocol.io/community/working-groups/interceptors)
  - [experimental-ext-interceptors](https://github.com/modelcontextprotocol/experimental-ext-interceptors)
- **Variants**
  - [experimental-ext-variants](https://github.com/modelcontextprotocol/experimental-ext-variants)
- **Skills over MCP**
  - [Working group](https://modelcontextprotocol.io/community/working-groups/skills-over-mcp)
  - [experimental-ext-skills](https://github.com/modelcontextprotocol/experimental-ext-skills)

## Meetings

**Cadence:** Every two weeks · **Time:** 16:00–17:00 London (BST/GMT) · **Duration:** 60 minutes · **Format:** Virtual

See the [calendar](https://meet.modelcontextprotocol.io/tag/finance) for the next meeting and the [meeting notes doc](https://docs.google.com/document/d/19HwCw7i4k-omABGnRYGGuLdMJNXQc3rs_QRgJxPhQU8/edit) for agendas. Meeting requirements — open attendance, advance agendas, and public notes — follow MCP [group governance](https://modelcontextprotocol.io/community/working-interest-groups#meeting-requirements). Each meeting opens with an [antitrust](https://modelcontextprotocol.io/community/antitrust) reminder.

## Current Focus Areas

1. **Regulatory Audit & Attestation** — Portable, verifiable event/claim models
2. **Data Lineage & Citation** — Provenance, consent metadata, and source attribution
3. **Guardrails & Security** — Verification frameworks and cryptographic attestations
4. **Policy Enforcement** — Declarative policies for tool usage and data handling

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
