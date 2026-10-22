# Dynamic racing rebase reconciliation

Status: settled implementation decisions.

The racing changes used execution-instance registries that were removed by the execution-factory redesign. Preserve the requested independent mutation-response copies and retained winners using the current resolution model.

## Decisions

- Opt in through `ExecutionFactory.Forkable`, which retains a typed state copier alongside its binding delegate. A scope copies prepared state, never a live execution graph.
- Fork only local opted-in preparations into a peer state. Reuse ancestor state, prepare ordinary local factories afresh and keep configuration-reference identity.
- Install every snapshot before any child is resolved. This removes the old eager instance-loading dependency on registry enumeration order.
- Prepare advice afresh in the fork. Keep intentionally shared external analysis accumulators under their existing ownership.
- Retain the winning contender's child scope across race boundaries. Dispose each active iterator and reset race-local performance observation, while preserving the winner's prepared algorithm and operator state.
- Apply the current binding's observation context when restarting a retained contender. Previously bound nodes keep their original observations.
- Dynamic-rate mutation opts in by copying its source-problem reference, observed epoch and remaining response-call count. Child mutator executions bind through the forked scope. Other configurations remain unaffected unless their factories explicitly opt in.
- Keep deferred epoch updates and normalization under the current dynamic-problem contracts. Refresh regression reference losses after evaluating the batch, against its applied environment version; add no event-subscription lifecycle.

## Validation

Use the repository's Release build and test sequence. Cover scope snapshot isolation, nested forks, ordinary intermediate composites, inherited state, failed snapshots, observation rebinding, retained racing winners, dynamic-rate response and regression reference refresh. Exclude the long paper experiment-export batches from routine solution validation.
