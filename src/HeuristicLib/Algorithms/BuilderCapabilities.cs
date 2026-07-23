using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public interface IBuilderWithEvaluator<TCandidate, TSearchSpace, TProblem> : IAlgorithmBuilder
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    IEvaluator<TCandidate, TSearchSpace, TProblem> Evaluator { get; set; }
}

public interface IBuilderWithCreator<TCandidate, TSearchSpace, TProblem> : IAlgorithmBuilder
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    ICreator<TCandidate, TSearchSpace, TProblem> Creator { get; set; }
}

public interface IBuilderWithSelector<TCandidate, TSearchSpace, TProblem> : IAlgorithmBuilder
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    ISelector<TCandidate, TSearchSpace, TProblem> Selector { get; set; }
}

public interface IBuilderWithCrossover<TCandidate, TSearchSpace, TProblem> : IAlgorithmBuilder
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    ICrossover<TCandidate, TSearchSpace, TProblem> Crossover { get; set; }
}

public interface IBuilderWithMutator<TCandidate, TSearchSpace, TProblem> : IAlgorithmBuilder
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    IMutator<TCandidate, TSearchSpace, TProblem> Mutator { get; set; }
}

public interface IBuilderWithReplacer<TCandidate, TSearchSpace, TProblem> : IAlgorithmBuilder
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    IReplacer<TCandidate, TSearchSpace, TProblem> Replacer { get; set; }
}

public interface IBuilderWithTerminator<TCandidate, TSearchSpace, TProblem, TSearchState> : IAlgorithmBuilder
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TSearchState : class, ISearchState
{
    ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> Terminator { get; set; }
}

public interface IBuilderWithInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> : IAlgorithmBuilder
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TSearchState : class, ISearchState
{
    IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>? Interceptor { get; set; }
}
