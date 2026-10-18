# Checkpoint 2: prepared advice and successful-call counting

Date: 2026-10-16. Scope: general and typed advice in the isolated owner/view prototype. Production migration remains unstarted and Package 0 remains incomplete.

## Findings

One externally authored general counter now observes mutation, evaluation, termination, interception and an external scalar operation through their success adapters. The counter does not enumerate operations or execution signatures. Its typed counterpart uses the same hook implementation, with an operation-specific public preparation and binding surface. A mutation counter on a configuration implementing several operations applies only to its mutation view, even when its reference pointcut also matches the other views.

Advice state and wrapper construction have separate lifetimes. Each configuration owner weakly keys advice entries by advice reference. An entry contains one successfully prepared advice object and checked wrapper constructors keyed by the complete requested view. A general prepared object can therefore serve several operations and compatible state views, while typed prepared advice remains restricted to its declared candidate/operation pair. Successful preparation survives a failed later view; failed preparation and failed wrapper checks remain retryable.

Resolution preflights every applicable wrapper constructor before constructing the raw node or resolving its children. It then constructs the raw node, applies wrappers in the shipping application order and returns the complete bound chain. Unsupported `Advice` subclasses produce an `InvalidOperationException` naming their type, selected operation and supported bases. They do not fail through an unchecked cast. Typed advice for another operation view is skipped without preparing it.

The four existing built-in bindings and the external `Checksum` binding implement one success adapter each. The shared typed binding base constructs a wrapper around the named operation and forwards exactly the child's control provider. No invocation-time factory check, universal execution method or per-call closure is introduced. Allocation and latency have not been measured, so this is a structural finding rather than a performance result.

Success hooks run after the child returns. A failed or canceled child does not advance a success counter. A callback failure propagates unchanged when it is the only failure; already completed counter updates remain. Lifecycle hooks, cleanup/fault aggregation, duration and around policies are outside this checkpoint.

## Validation

Run the [runner](run.ps1) from the repository root:

```powershell
pwsh -NoProfile -File ./plans/prototypes/operator-role-integrated/run.ps1
```

Detailed run snapshots, including environment details and backend hashes, are retained locally and excluded from version control.

The combined runner passes the original 61 runtime assertions and 74 additional advice assertions, for 135 assertions in 17 groups. It also passes the positive consumer compilation and five expected compiler failures. The added negative case rejects mismatched typed advice/pointcut operation contracts with `CS0311`; the previous four diagnostics remain unchanged.

The new fixtures establish:

- Once-only preparation across mutation/evaluation/external/termination views, in both initial operation orders; independent state in independent roots; selected source-reference identity.
- Separate wrapper constructors for broad and two narrower termination state views, in both broad/narrow resolution orders, with names and general/typed advice. Incompatible interceptor views reject before advice binding or raw construction.
- Current-layer, aspect and pair application order; first-position advice deduplication, including an initial nonmatching pointcut; distinct advice instances sharing one result accumulator remain distinct.
- Live controls through stacked names and inspection nodes that do not implement the control interface; independent advice state at wrapper and child boundaries; unchanged earlier contexts after new advised children are bound.
- Advice ownership under Fresh/Retained/Shared activation, ancestor reuse, and separate owners for value-equal configuration records.
- Advice preparation/binding rejection before child preparation; malformed carrier rejection before construction or control exposure; reuse of successful advice bindings when a later view fails; retry after raw or wrapper construction failure.
- Unchanged child-failure and cancellation exceptions, success-callback failure semantics and continued usability after failure.
- Collection of discarded advice and prepared state while an owner lives, including a prepared-object-to-advice key/value cycle; collection of discarded fresh owners while advice/aspects live; retention of hook state by a live advised execution.

The generated backend still compiles current production sources with only the two partial-declaration substitutions described in the [README](README.md). The production resolver, its original scope/aspect/activation machinery and the Git index are not edited. Build-time analyzer warnings remain visible in generated logs; no whole-solution or analyzer-clean result is claimed. C# whitespace and changed Markdown are checked separately.

## Contract and cost boundaries

The advanced public surface now includes general and typed `OperatorAdvice`/`PreparedOperatorAdvice` pairs and the success-hook profile. The generic prepared advice base keeps its candidate and operation statically available to external authors while its run signature remains generic. Internally, general/typed prepared objects are erased only in the preparation cache and checked by their supported dispatch branch.

`SupportsCandidateWork` and successful work counts are supplied by the adapters, but no candidate metric, clock or budget is implemented here. Runtime rejection of unsupported candidate metrics remains a required later fixture. The external scalar operation supports call counting while declaring no candidate work.

The binding protocol still lacks lifecycle and policy profiles and multi-child composition. There is no retry, weighted choice/routing, complete nine-operation family, native host migration or real `EvolutionStrategy` adaptation yet. The public protocol remains provisional. Source/API/extension costs and the performance budgets must still earn acceptance before production migration.

## Next checkpoint

Measure the cheap success-counting path against an equivalent direct counter, then extend this same binding model with lifecycle hooks and actual duration measurement. Validate the specified entry, fault, success and cleanup failure contract before adding around policies or retry. The full Package 0 gate still requires all remaining correctness, integration, source-reduction and performance evidence.
