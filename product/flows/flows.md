# Operator flows

*Last updated: 2026-09-27*

```mermaid
flowchart LR
    Input[Product facts and reviewed media] --> Card[Preset and composition]
    Input --> Copy[Localized caption draft]
    Card --> PNG[Rendered immutable artwork]
    PNG --> Draft[Saved post revision]
    Copy --> Draft
    Draft --> Review[Inspect and approve]
    Review --> Manual[Manual ZIP package]
    Review --> Plan[Channel and date plan]
    Plan --> Ready[Explicit connection and execution readiness]
    Ready --> Receipt[Provider delivery attempt and receipt]
    Receipt --> Results[Observed metrics and enquiries]
```

A job finishing does not overwrite newer form edits. Generated output is selected explicitly. A downloaded ZIP does not establish publication. Missing source claims remain visible at review. The exact HTTP behavior belongs to the [contract](../../engineering/architecture/marketing-contract.md).
