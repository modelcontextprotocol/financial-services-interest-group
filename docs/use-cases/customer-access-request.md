# A bank customer asks when their data was accessed through an AI agent, and why

Status: draft for discussion, relates to #17

## Scenario

A bank gives its staff an AI assistant. The assistant reads customer records through MCP tools, for example a tool that returns a customer's account history. Months later, a customer asks the bank when their data was looked at and for what purpose.

## Why the bank has to answer

In Pankki S (CJEU, C-579/21, 22 June 2023), a customer of a Finnish bank asked who had consulted their data and why. The Court held that the right of access in GDPR Article 15 covers the dates and purposes of those consultations. It does not generally cover the names of the staff who did them. In Canada, PIPEDA (Schedule 1, principle 4.9.1) requires an organization to give an individual an account of the use made of their information.

When an AI agent does the consulting through a tool call, the record of that call is where the bank has to find the answer.

## What the record needs for this

For each tool call that reads a customer's data:

1. Whose data it read. A reference the bank can link back to the customer, without the customer's name or account number in the record itself.
2. The purpose of the access, given before the call.
3. When it happened, and whether it was allowed or refused.

As I read the outline in #17, it covers the third point but not the first two.

## Questions for the group

1. Should the purpose travel with the request in `_meta`? SEP-2817 proposes `invocationReason`, which is close, but it is free text set by the client and explicitly not authorization evidence.
2. Should the server check the purpose against its own policy before the tool runs, and record the result? An interceptor looks like the natural place.
3. Should the customer reference and the purpose be part of the core record, or an extension on top of it?
