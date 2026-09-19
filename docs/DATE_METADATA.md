# Resource date metadata

Created and modified dates are not universal fields in either cloud's generic inventory APIs.

## Azure

Cloud Explorer first reads the Azure Resource Manager `createdTime` and `changedTime` fields. When they are absent, it searches returned provider properties for a small allowlist of clearly named creation/modification fields. Many resource types expose neither value; those rows show `Not exposed`.

## AWS

Cloud Explorer searches AWS Resource Explorer properties for clearly named creation/modification fields. Resource support varies by service. Resource Explorer's `LastReportedAt` means the inventory last observed the resource; it does not mean the resource changed at that time. The model therefore stores it separately as `LastObservedAt` and never maps it to `LastModifiedAt`.

## Rule for future enrichers

Any service-specific date enricher must:

- identify its API and field in `CreatedAtSource` or `LastModifiedAtSource`;
- document API pricing and throttling;
- remain off by default if it can add charges;
- preserve `null` when semantics are uncertain;
- run only during an explicit refresh, never in the background.
