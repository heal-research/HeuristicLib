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
  private readonly IEvaluator<TGenotype, TSearchSpace, TProblem> evaluator;
  private readonly Func<TGenotype, TKey> keySelector;
  private readonly long? sizeLimit;
  
  public CachedEvaluator(IEvaluator<TGenotype, TSearchSpace, TProblem> evaluator, Func<TGenotype, TKey>? keySelector = null, long? sizeLimit = null)
  {
    this.evaluator = evaluator;
    this.keySelector = keySelector ?? (g => (TKey)(object)g);
    this.sizeLimit = sizeLimit;
  }

  public override Instance CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    var evaluatorInstance = instanceRegistry.GetOrAdd(evaluator, () => this.evaluator.CreateExecutionInstance(instanceRegistry));
    return new Instance(evaluatorInstance, keySelector, sizeLimit);
  }

  public class Instance
    : EvaluatorInstance<TGenotype, TSearchSpace, TProblem>
  {
    private readonly IEvaluatorInstance<TGenotype, TSearchSpace, TProblem> evaluator;
    private readonly Func<TGenotype, TKey> keySelector;
    private readonly MemoryCache cache;

    public Instance(IEvaluatorInstance<TGenotype, TSearchSpace, TProblem> evaluator, Func<TGenotype, TKey> keySelector, long? sizeLimit)
    {
      this.evaluator = evaluator;
      this.keySelector = keySelector;
      var cacheOptions = new MemoryCacheOptions { SizeLimit = sizeLimit };
      cache = new MemoryCache(cacheOptions);
      
    }
    
    public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
      var keys = genotypes.Select(keySelector).ToList();
      var results = new ObjectiveVector[genotypes.Count];
      
      var uncachedSolutions = new List<TGenotype>();
      var uncachedSolutionIndices = new List<int>();
      
      for (int i = 0; i < genotypes.Count; i++) {
        var genotype = genotypes[i];
        var key = keys[i];
        if (cache.TryGetValue(key, out ObjectiveVector? cachedObjective)) {
          results[i] = cachedObjective!;
        } else {
          uncachedSolutions.Add(genotype);
          uncachedSolutionIndices.Add(i);
        }
      }
      
      if (uncachedSolutions.Count > 0) {
        var newObjectives = evaluator.Evaluate(uncachedSolutions, random, searchSpace, problem);
        for (int i = 0; i < uncachedSolutions.Count; i++) {
          var objective = newObjectives[i];
          
          var originalIndex = uncachedSolutionIndices[i];
          var key = keys[originalIndex];
          
          cache.Set(key, objective, new MemoryCacheEntryOptions { Size = 1 });
          
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
