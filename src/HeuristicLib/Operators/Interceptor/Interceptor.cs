using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Interceptor;

public abstract class Interceptor<TGenotype, TIterationResult, TEncoding, TProblem> : IInterceptor<TGenotype, TIterationResult, TEncoding, TProblem>
  where TIterationResult : IIterationResult
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public abstract TIterationResult Transform(TIterationResult currentIterationResult, TIterationResult? previousIterationResult, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);
}

public abstract class Interceptor<TGenotype, TIterationResult, TEncoding> : IInterceptor<TGenotype, TIterationResult, TEncoding, IProblem<TGenotype, TEncoding>>
  where TIterationResult : IIterationResult
  where TEncoding : class, IEncoding<TGenotype> {
  public abstract TIterationResult Transform(TIterationResult currentIterationResult, TIterationResult? previousIterationResult, IRandomNumberGenerator random, TEncoding encoding);

  TIterationResult IInterceptor<TGenotype, TIterationResult, TEncoding, IProblem<TGenotype, TEncoding>>.Transform(TIterationResult currentIterationResult, TIterationResult? previousIterationResult, IRandomNumberGenerator random, TEncoding encoding, IProblem<TGenotype, TEncoding> problem) {
    return Transform(currentIterationResult, previousIterationResult, random, encoding);
  }
}

public abstract class Interceptor<TGenotype, TIterationResult> : IInterceptor<TGenotype, TIterationResult, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>
  where TIterationResult : IIterationResult {
  public abstract TIterationResult Transform(TIterationResult currentIterationResult, TIterationResult? previousIterationResult, IRandomNumberGenerator random);

  TIterationResult IInterceptor<TGenotype, TIterationResult, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.Transform(TIterationResult currentIterationResult, TIterationResult? previousIterationResult, IRandomNumberGenerator random, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    return Transform(currentIterationResult, previousIterationResult, random);
  }
}

public abstract class Interceptor<TGenotype> : IInterceptor<TGenotype, IIterationResult, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> {
  public abstract IIterationResult Transform(IIterationResult currentIterationResult, IIterationResult? previousIterationResult, IRandomNumberGenerator random);

  IIterationResult IInterceptor<TGenotype, IIterationResult, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.Transform(IIterationResult currentIterationResult, IIterationResult? previousIterationResult, IRandomNumberGenerator random, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    return Transform(currentIterationResult, previousIterationResult, random);
  }
}
