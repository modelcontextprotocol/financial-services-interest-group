# FSIG Charter

This document defines the mission, scope, and strategic approach of the MCP Financial Services Interest Group (FSIG).

> **For governance details, roles, and current members, see [GOVERNANCE.md](./GOVERNANCE.md)**

## Mission & Scope

### Mission

The FSIG coordinates financial-industry stakeholders to:

- **Define and build** finance-specific MCP extensions (compliance/auditability, data lineage & provenance, guardrails & attestation, etc.)
- **Advocate for spec enhancements** when extensions aren't sufficient (via SEPs with core maintainer sponsorship)
- **Drive common standards** enabling interoperability across regulated institutions
- **Provide a neutral forum** to address regulatory and risk constraints with open technical solutions

### Scope

**In Scope:**
- Technical standards and protocols for financial services
- MCP extensions addressing compliance, security, and regulatory requirements
- Appropriate implementations of extensions for testing and validation
- Interoperability patterns and conformance testing
- Documentation and reference implementations

**Out of Scope:**
- Competitively sensitive topics (pricing, customer lists, market strategies, etc.)
- Non-technical business discussions

> **Note:** FSIG adheres to the [MCP Antitrust Policy](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/ANTITRUST.md). All participants must comply with antitrust guidelines.

## Extensions vs. Core Spec

### Extensions

**Extensions** are domain-specific additions that can be developed and iterated on within the FSIG to address financial services requirements. They allow us to:
- Develop solutions for real-world financial services scenarios
- Gather implementation experience and feedback
- Move quickly on specialized domain requirements
- Provide production-ready capabilities for financial institutions

Extensions are fully supported specifications developed and maintained within the FSIG repository. They may evolve based on implementation experience and feedback from the community.

### Relationship to Core Spec

FSIG extensions complement the core MCP specification rather than fragment it. When an extension demonstrates **broad applicability beyond financial services**, it becomes a candidate for promotion to the core MCP specification through:

1. **SEP (Spec Enhancement Proposal)** — A formal proposal documenting the extension, its rationale, and implementation experience
2. **Core Maintainer Sponsorship** — Support from MCP core maintainers who shepherd the proposal
3. **Community Consensus** — Agreement from the broader MCP community through the SEP review process

This pathway ensures that domain-specific innovations can eventually benefit the entire MCP ecosystem when appropriate.

### Proactive Promotion

The FSIG actively identifies extensions with broad applicability and assesses appetite for promotion to the core spec. The group serves as:

- **Real-world validation** — Providing implementation experience and use cases from regulated environments
- **Collective advocacy** — A coalition of contributors who can speak to the value and necessity of capabilities
- **SEP champions** — Subject matter experts who can shepherd proposals through the enhancement process

When an extension shows promise for broader adoption, maintainers will:
1. **Assess applicability** — Evaluate whether the extension solves problems beyond financial services
2. **Gauge community interest** — Discuss with other MCP groups and the broader community
3. **Build consensus** — Work with FSIG members to align on the promotion strategy
4. **Coordinate with liaison** — Engage with [David Soria Parra](https://github.com/dsp-ant) to pursue SEP sponsorship

This proactive approach ensures that valuable innovations developed for financial services can benefit the entire MCP ecosystem.

**Core Maintainer Liaison:** [David Soria Parra](https://github.com/dsp-ant) serves as our liaison to the core MCP maintainers for SEP sponsorship.

## Amendments

This charter may be amended by:
1. Proposal via GitHub Issue or pull request
2. Discussion in meetings and/or GitHub
3. Consensus approval from maintainers
4. Documentation of rationale in the PR or issue

Significant changes should be announced in Discord and allow for community input before finalization.

## Questions?

For questions about this charter, please:
- Ask in the `#financial-services-ig` Discord channel
- Open a GitHub Issue
- Contact the maintainers (see [GOVERNANCE.md](./GOVERNANCE.md))
