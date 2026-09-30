# 20. Measure speed without losing the contract

A billing component can become faster by accidentally doing less work or dropping validation. This workshop designs performance experiments that retain correctness oracles. It uses the tier rater, usage store and subscription repository as three different workloads rather than treating one elapsed-time number as a measure of the whole application.

## Define the operation

For tier rating, an operation is a call with a known quantity and schedule, followed by validation of total and allocation. For usage ingestion, distinguish a new-key insertion, an equivalent replay and a conflicting replay. For subscription mutation, distinguish independent aggregates from competing calls on one expected version. Those workloads exercise different branches and synchronization behavior.

Record input sizes and distributions. A tier schedule with two entries has different sorting cost from a large schedule. A usage benchmark with every call using a new key measures growth as well as insertion. A replay-heavy workload repeatedly touches existing keys. A same-key conflict wave may spend much of its time constructing and handling exceptions. Name the mixture rather than calling all of them ingestion throughput.

## Keep the oracle outside the timed loop where appropriate

Validate fixture expectations before measurement. During measurement, collect enough results to detect incorrect behavior without allowing assertion overhead to dominate every tiny arithmetic operation. Afterward, verify aggregate counts, distinct identities, totals and error categories. Explain which checks occur inside and outside the timed region.

Do not discard correctness checks completely. A mutation that skips payload comparison could make conflicting replay faster while breaking the contract. A benchmark result is meaningful only for an implementation that still satisfies the selected behavior. Run the relevant functional suite before interpreting speed differences.

## Warmup and environment

Record runtime version, build configuration, process architecture and machine conditions relevant to the experiment. Separate startup, compilation and warmup from steady-state timing if that is your intended question. Conversely, if cold-start latency matters, measure it explicitly rather than hiding it in an average of unrelated runs.

Avoid assigning universal performance claims to a developer laptop. The result describes a workload and environment. It can help compare two local implementations under controlled conditions, but production capacity also depends on databases, networks, providers and deployment topology absent from these local classes.

## Contention and lock granularity

The in-memory subscription repository uses one shared mutation lock. Test independent subscriptions and one hot subscription separately. A slow callback can block unrelated mutations within that repository instance. Measure queueing effects only with callbacks representative of the proposed workload, and retain a deliberately blocked callback experiment as a structural demonstration rather than a normal throughput estimate.

If proposing per-key synchronization, test correctness around key creation, removal and multiple repository instances. A faster benchmark does not prove that a new locking design coordinates all writers. Shared mutable references returned by GetAsync remain an ownership concern even if lock granularity improves.

## Memory and lifetime

An in-memory store that retains every usage event can grow with the number of distinct keys. A short throughput run may look healthy while a long-lived workload accumulates memory. Record unique-key count and retained object count where measurable. Do not infer a retention policy that the inspected store does not implement.

Propose retention only with replay semantics in view. Removing an idempotency record can cause a later retry to appear new. The design must decide whether replay keys expire, how clients learn that boundary and whether durable receipts remain elsewhere. Memory optimization can change correctness if identity history is part of the contract.

## Exercise

Write a benchmark plan with three workload families, correctness checks, environment fields and a comparison decision. Include one optimization that you would reject despite an apparent speed improvement because it weakens identity or validation. Mark the plan unexecuted until a controlled measurement artifact exists; this chapter supplies a design method, not performance results.

[Studio index](README.md) · [Review](reviews/05-experimental-design.md)
