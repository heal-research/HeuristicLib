using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure.OperatorFoundation.Fixtures.Authoring;

public abstract record Mutator<TCandidate, TSearchSpace, TProblem> : HEAL.HeuristicLib.Operators.Mutators.Mutator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>;

public abstract record Mutator<TCandidate> : HEAL.HeuristicLib.Operators.Mutators.Mutator<TCandidate>;
public abstract record StatelessMutator<TCandidate> : HEAL.HeuristicLib.Operators.Mutators.StatelessMutator<TCandidate>;
public abstract record StatelessEvaluator<TCandidate> : HEAL.HeuristicLib.Operators.Evaluators.StatelessEvaluator<TCandidate>;
