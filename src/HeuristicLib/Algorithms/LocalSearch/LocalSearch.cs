using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms.LocalSearch;
public class LocalSearch<TGenotype, TEncoding, TProblem> : 
  IterativeAlgorithm<TGenotype, TEncoding, TProblem, LocalSearchResult<TGenotype>, LocalSearchResultIterationResult<TGenotype>> 
  where TEncoding : class, IEncoding<TGenotype> where TProblem : class, IProblem<TGenotype, TEncoding> {
  public ICreator<TGenotype, TEncoding, TProblem> Creator { get; }
  public IMutator<TGenotype, TEncoding, TProblem> Mutator { get; }

  private TournamentSelector<TGenotype> selector = new(2);

  public LocalSearch(
    ITerminator<TGenotype, LocalSearchResultIterationResult<TGenotype>, TEncoding, TProblem> terminator, 
    IInterceptor<TGenotype, LocalSearchResultIterationResult<TGenotype>, TEncoding, TProblem>? interceptor, 
    ICreator<TGenotype, TEncoding, TProblem> creator,
    IMutator<TGenotype, TEncoding, TProblem> mutator) : base(terminator, interceptor) {
    Creator = creator;
    Mutator = mutator;
  }
  public override LocalSearchResultIterationResult<TGenotype> ExecuteStep(
    TProblem problem, 
    TEncoding? searchSpace = default, 
    LocalSearchResultIterationResult<TGenotype>? previousIterationResult = default, 
    IRandomNumberGenerator? random = null) {


    return null;

  }

  protected override LocalSearchResult<TGenotype> FinalizeResult(LocalSearchResultIterationResult<TGenotype> iterationResult, TProblem problem) {
    return null;
  }
}

public record LocalSearchResultIterationResult<T> : IIterationResult<T> {
  private T Solution;
  private 
}

public record LocalSearchResult<T> : IAlgorithmResult<T> { }
