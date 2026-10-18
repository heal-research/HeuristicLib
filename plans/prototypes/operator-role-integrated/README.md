# Integrated operator-role prototype

**Status:** standalone checkpoints stopped after checkpoint 3. Package 1 integrates the proven contracts and resolver ownership into the core library, with regular tests. Reuse the remaining examples during core migrations; the historical runner below expects the earlier source layout and is no longer a validation command for this branch. See the [current plan](../../operator-role-parameterization-rework.md) and [glossary](../../../docs/guide/glossary.md#executor) for scope and terminology.

The [first report](checkpoint-1.md) records the API and owner/view experiment; the [second report](checkpoint-2.md) records advice dispatch, ownership, controls and lifetime findings; the [third report](checkpoint-3.md) records lifecycle semantics and measured costs. The historical `operator-role-slice` and its reports remain unchanged.

## Historical runner

From the repository root:

```powershell
pwsh -NoProfile -File ./plans/prototypes/operator-role-integrated/run.ps1
```

The runner uses the SDK selected by `global.json`, restores the prototype dependencies and runs .NET commands sequentially. Windows sandbox execution may need access to the installed SDK and NuGet cache. No separate production build is required.

It creates generated projects and outputs under ignored `artifacts/operator-role-integrated`. Full command output, including analyzer warnings and environment details, stays local under that directory's `logs`. A failed assertion, unexpected compiler diagnostic or failed command fails the runner. Expected compiler failures are identified individually; their full logs contain intentional `Build FAILED` entries. Historical `results*.txt` snapshots and `evidence/` are also ignored. Commit the tooling and concise reports; keep detailed run evidence local.

Performance runs are separate. See the [third checkpoint's reproduction commands](checkpoint-3.md#reproduce) for the local three-process benchmark harness and its strict budget checks. This is plan-local feasibility tooling, not a permanent production benchmark project.

## How isolation works

The generated backend project mirrors the core project's settings, package references and analyzer references, and compiles the current production core sources. It replaces only `ResolutionScope.cs` with a generated copy in which the outer class and its nested `State` are marked `partial`. The runner checks both declarations before making those two substitutions. Production source files are never rewritten.

[ResolutionScope.Operators.cs](Backend/ResolutionScope.Operators.cs) adds the experimental owner/view path to those partial classes. It uses the shipping state's ancestry and shared construction guard, the shipping aspect enumeration, and the shipping `WithState` construction frame. `ChildScopeSelector` is compiled unchanged. No standalone replacement scope or global owner cache is used.

The experimental operator-owner map sits beside the shipping legacy factory map. This is sufficient to test the new path, legacy resolution availability and a mixed-graph cycle. It does **not** establish the full old/new adaptation contract required by Package 1.

The consumer executable references only the generated backend assembly and uses public APIs. Its descriptor, control, recommendation, generic forwarding topology and leaf examples live in a separate assembly. Prototype authoring and convenience extensions use `HEAL.HeuristicLib.OperatorRolePrototype` to coexist with shipping authoring bases. Consuming contracts and preparation carriers use the plan's intended concept namespaces inside the isolated backend.

## Source map

- [OperatorContracts.cs](Backend/OperatorContracts.cs): candidate-only consuming contract, operation declaration and four built-in descriptors.
- [Preparation.cs](Backend/Preparation.cs): once-only prepared owner, checked factories, covariant bound views and forwarding/success/lifecycle binding profiles.
- [OperatorAdvice.cs](Backend/OperatorAdvice.cs) and [PreparedOperatorAdvice.cs](Backend/PreparedOperatorAdvice.cs): general and typed advice preparation/binding contracts.
- [CallLifecycle.cs](Backend/CallLifecycle.cs): invocation-local tokens and callback/cleanup failure handling; [InspectionAdvice.cs](Backend/InspectionAdvice.cs): atomic counting and actual duration advice.
- [Authoring.cs](Backend/Authoring.cs): representative stateless, stateful and explicit mutation authoring, plus stateless evaluation.
- [Composition.cs](Backend/Composition.cs) and [TypedResolution.cs](Backend/TypedResolution.cs): generic naming and typed resolution, including optional, try and control-returning forms.
- [AuthoringExamples.cs](Fixtures/AuthoringExamples.cs) and [ExternalOperation.cs](Fixtures/ExternalOperation.cs): independently compiled authoring and extension stories.
- [CountingAdvice.cs](Fixtures/CountingAdvice.cs): one external general counter and its typed adapter, sharing a single success hook.
- [Program.cs](Fixtures/Program.cs), [AdviceCases.cs](Fixtures/AdviceCases.cs) and [LifecycleCases.cs](Fixtures/LifecycleCases.cs): focused runtime fixtures; [CompilerCases.cs](CompilerCases.cs): positive and expected-negative compilation fixtures.
- [InspectionBenchmarks.cs](Benchmarks/InspectionBenchmarks.cs), [run-benchmarks.ps1](run-benchmarks.ps1) and [summarize-benchmarks.ps1](summarize-benchmarks.ps1): matched direct/shared measurements and evidence validation.

This binding subset supports success and lifecycle hooks through prepared advice. Policy invocation adapters, candidate metrics, retry and choice remain unimplemented. A matching unsupported advice is rejected explicitly before raw construction. The public extension protocol and complete authoring ladders remain provisional until the full Package 0 gate passes.
