using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators;

public interface IInterceptor<TGenotype, TIterationResult, in TEncoding, in TProblem>
  where TIterationResult : IIterationResult<TGenotype>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding>
{
  TIterationResult Transform(TIterationResult currentIterationResult, TIterationResult? previousIterationResult, TEncoding encoding, TProblem problem);
}

public abstract class Interceptor<TGenotype, TIterationResult, TEncoding, TProblem> : IInterceptor<TGenotype, TIterationResult, TEncoding, TProblem>
  where TIterationResult : IIterationResult<TGenotype>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding>
{
  public abstract TIterationResult Transform(TIterationResult currentIterationResult, TIterationResult? previousIterationResult, TEncoding encoding, TProblem problem);
}

public abstract class Interceptor<TGenotype, TIterationResult, TEncoding> : IInterceptor<TGenotype, TIterationResult, TEncoding, IProblem<TGenotype, TEncoding>>
  where TIterationResult : IIterationResult<TGenotype>
  where TEncoding : class, IEncoding<TGenotype>
{
  public abstract TIterationResult Transform(TIterationResult currentIterationResult, TIterationResult? previousIterationResult, TEncoding encoding);

  TIterationResult IInterceptor<TGenotype, TIterationResult, TEncoding, IProblem<TGenotype, TEncoding>>.Transform(TIterationResult currentIterationResult, TIterationResult? previousIterationResult, TEncoding encoding, IProblem<TGenotype, TEncoding> problem) {
    return Transform(currentIterationResult, previousIterationResult, encoding);
  }
}

public abstract class Interceptor<TGenotype, TIterationResult> : IInterceptor<TGenotype, TIterationResult, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>
  where TIterationResult : IIterationResult<TGenotype>
{
  public abstract TIterationResult Transform(TIterationResult currentIterationResult, TIterationResult? previousIterationResult);

  TIterationResult IInterceptor<TGenotype, TIterationResult, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.Transform(TIterationResult currentIterationResult, TIterationResult? previousIterationResult, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    return Transform(currentIterationResult, previousIterationResult);
  }
}

public abstract class Interceptor<TGenotype> : IInterceptor<TGenotype, IIterationResult<TGenotype>, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>
{
  public abstract IIterationResult<TGenotype> Transform(IIterationResult<TGenotype> currentIterationResult, IIterationResult<TGenotype>? previousIterationResult);

  IIterationResult<TGenotype> IInterceptor<TGenotype, IIterationResult<TGenotype>, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.Transform(IIterationResult<TGenotype> currentIterationResult, IIterationResult<TGenotype>? previousIterationResult, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    return Transform(currentIterationResult, previousIterationResult);
  }
}


// public class IdentityInterceptor<TGenotype> : Interceptor<TGenotype>
// {
//   public override IIterationResult<TGenotype> Transform(IIterationResult<TGenotype> currentIterationResult, IIterationResult<TGenotype>? previousIterationResult) {
//     return currentIterationResult;
//   }
// }




public interface IPopulationIterationResult<TGenotype, out TSelf> : IIterationResult<TGenotype>
  where TSelf : IPopulationIterationResult<TGenotype, TSelf>
{
  Population<TGenotype> Solutions { get; }
  TSelf WithSolutions(Population<TGenotype> solutions);
}

public class RemoveDuplicatesInterceptor<TGenotype, TIterationResult> : Interceptor<TGenotype, TIterationResult>
  where TIterationResult : IPopulationIterationResult<TGenotype, TIterationResult>
{
  private readonly IEqualityComparer<TGenotype> comparer;
  public RemoveDuplicatesInterceptor(IEqualityComparer<TGenotype> comparer) {
    this.comparer = comparer;
  }
  
  public override TIterationResult Transform(TIterationResult currentIterationResult, TIterationResult? previousIterationResult) {
    var newSolutions = currentIterationResult.Solutions.DistinctBy(s => s.Genotype, comparer);
    return currentIterationResult.WithSolutions(
      new Population<TGenotype>(new ImmutableList<Solution<TGenotype>>(newSolutions))
    );
    // return currentIterationResult with {
    //   Solutions = new ImmutableList<Solution<TGenotype>>(newSolutions)
    // };
  }
}
