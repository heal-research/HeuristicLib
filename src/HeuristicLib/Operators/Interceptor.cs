using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators;

public abstract class Interceptor<TGenotype, TIterationResult, TEncoding, TProblem> : IInterceptor<TGenotype, TIterationResult, TEncoding, TProblem>
  where TIterationResult : IIterationResult
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public abstract TIterationResult Transform(TIterationResult currentIterationResult, TIterationResult? previousIterationResult, TEncoding encoding, TProblem problem);
}

public abstract class Interceptor<TGenotype, TIterationResult, TEncoding> : IInterceptor<TGenotype, TIterationResult, TEncoding, IProblem<TGenotype, TEncoding>>
  where TIterationResult : IIterationResult
  where TEncoding : class, IEncoding<TGenotype> {
  public abstract TIterationResult Transform(TIterationResult currentIterationResult, TIterationResult? previousIterationResult, TEncoding encoding);

  TIterationResult IInterceptor<TGenotype, TIterationResult, TEncoding, IProblem<TGenotype, TEncoding>>.Transform(TIterationResult currentIterationResult, TIterationResult? previousIterationResult, TEncoding encoding, IProblem<TGenotype, TEncoding> problem) {
    return Transform(currentIterationResult, previousIterationResult, encoding);
  }
}

public abstract class Interceptor<TGenotype, TIterationResult> : IInterceptor<TGenotype, TIterationResult, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>
  where TIterationResult : IIterationResult {
  public abstract TIterationResult Transform(TIterationResult currentIterationResult, TIterationResult? previousIterationResult);

  TIterationResult IInterceptor<TGenotype, TIterationResult, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.Transform(TIterationResult currentIterationResult, TIterationResult? previousIterationResult, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    return Transform(currentIterationResult, previousIterationResult);
  }
}

public abstract class Interceptor<TGenotype> : IInterceptor<TGenotype, IIterationResult, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> {
  public abstract IIterationResult Transform(IIterationResult currentIterationResult, IIterationResult? previousIterationResult);

  IIterationResult IInterceptor<TGenotype, IIterationResult, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.Transform(IIterationResult currentIterationResult, IIterationResult? previousIterationResult, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    return Transform(currentIterationResult, previousIterationResult);
  }
}
