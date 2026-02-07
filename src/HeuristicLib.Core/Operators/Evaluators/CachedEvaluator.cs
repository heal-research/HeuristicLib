using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using Microsoft.Extensions.Caching.Memory;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public class CachedEvaluator<TGenotype, TSearchSpace, TProblem, TKey>
  : Evaluator<TGenotype, TSearchSpace, TProblem>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TKey : notnull
{
  protected readonly IEvaluator<TGenotype, TSearchSpace, TProblem> Evaluator;
  protected readonly Func<TGenotype, TKey> KeySelector;
  protected readonly long? SizeLimit;
  
  public CachedEvaluator(IEvaluator<TGenotype, TSearchSpace, TProblem> evaluator, Func<TGenotype, TKey>? keySelector = null, long? sizeLimit = null)
  {
    this.Evaluator = evaluator;
    this.KeySelector = keySelector ?? (g => (TKey)(object)g);
    this.SizeLimit = sizeLimit;
  }

  public override Instance CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    var evaluatorInstance = instanceRegistry.GetOrCreate(Evaluator);
    return new Instance(evaluatorInstance, KeySelector, SizeLimit);
  }

  public class Instance
    : EvaluatorInstance<TGenotype, TSearchSpace, TProblem>
  {
    protected readonly IEvaluatorInstance<TGenotype, TSearchSpace, TProblem> Evaluator;
    protected readonly Func<TGenotype, TKey> KeySelector;
    protected readonly MemoryCache Cache;

    public Instance(IEvaluatorInstance<TGenotype, TSearchSpace, TProblem> evaluator, Func<TGenotype, TKey> keySelector, long? sizeLimit)
    {
      this.Evaluator = evaluator;
      this.KeySelector = keySelector;
      var cacheOptions = new MemoryCacheOptions { SizeLimit = sizeLimit };
      Cache = new MemoryCache(cacheOptions);
      
    }
    
    public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
      var keys = genotypes.Select(KeySelector).ToList();
      var results = new ObjectiveVector[genotypes.Count];
      
      var uncachedSolutions = new List<TGenotype>();
      var uncachedSolutionIndices = new List<int>();
      
      for (int i = 0; i < genotypes.Count; i++) {
        var genotype = genotypes[i];
        var key = keys[i];
        if (Cache.TryGetValue(key, out ObjectiveVector? cachedObjective)) {
          results[i] = cachedObjective!;
        } else {
          uncachedSolutions.Add(genotype);
          uncachedSolutionIndices.Add(i);
        }
      }
      
      if (uncachedSolutions.Count > 0) {
        var newObjectives = Evaluator.Evaluate(uncachedSolutions, random, searchSpace, problem);
        for (int i = 0; i < uncachedSolutions.Count; i++) {
          var objective = newObjectives[i];
          
          var originalIndex = uncachedSolutionIndices[i];
          var key = keys[originalIndex];
          
          Cache.Set(key, objective, new MemoryCacheEntryOptions { Size = 1 });
          
          results[originalIndex] = objective;
        }
      }

      return results;
    }
  }
}

public static class CachedEvaluatorExtensions
{
  extension<TGenotype, TSearchSpace, TProblem>(IEvaluator<TGenotype, TSearchSpace, TProblem> evaluator) 
    where TGenotype : class 
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
  {
    public CachedEvaluator<TGenotype, TSearchSpace, TProblem, TKey> WithCache<TKey>(Func<TGenotype, TKey>? keySelector = null, long? sizeLimit = null)
      where TKey : notnull
    {
      return new CachedEvaluator<TGenotype, TSearchSpace, TProblem, TKey>(evaluator, keySelector, sizeLimit);
    }
    
    public CachedEvaluator<TGenotype, TSearchSpace, TProblem, TGenotype> WithCache(long? sizeLimit = null)
    {
      return new CachedEvaluator<TGenotype, TSearchSpace, TProblem, TGenotype>(evaluator, sizeLimit: sizeLimit);
    }
  }
}
