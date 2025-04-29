
namespace HEAL.HeuristicLib.Problems;

public interface IEncodedProblem<TSolution, in TGenotype, out TSearchSpace> : IProblem<TSolution>, IOptimizable<TGenotype, TSearchSpace>
  where TSearchSpace : ISearchSpace<TGenotype> {
  TSolution Decode(TGenotype genotype);
}

public abstract class EncodedProblemBase<TSolution, TGenotype, TSearchSpace> : ProblemBase<TSolution>, IEncodedProblem<TSolution, TGenotype, TSearchSpace>
  where TSearchSpace : ISearchSpace<TGenotype>
{
  public TSearchSpace SearchSpace { get; }
  
  public abstract TSolution Decode(TGenotype genotype);
  
  Fitness IOptimizable<TGenotype, TSearchSpace>.Evaluate(TGenotype genotype) {
    var phenotype = Decode(genotype);
    return Evaluate(phenotype);
  }
  protected EncodedProblemBase(Objective objective, TSearchSpace searchSpace) : base(objective) {
    SearchSpace = searchSpace;
  }
}

public class EncodedProblem<TSolution, TGenotype, TSearchSpace> : IEncodedProblem<TSolution, TGenotype, TSearchSpace>
  where TSearchSpace : ISearchSpace<TGenotype> 
{
  public IProblem<TSolution> Problem { get; }
  public Objective Objective => Problem.Objective;
  public TSearchSpace SearchSpace { get; }
  public IDecoder<TGenotype, TSolution> Decoder { get; }
  
  public EncodedProblem(IProblem<TSolution> problem, TSearchSpace searchSpace, IDecoder<TGenotype, TSolution> decoder) {
    Problem = problem;
    SearchSpace = searchSpace;
    Decoder = decoder;
  }

  public Fitness Evaluate(TSolution solution) {
    return Problem.Evaluate(solution);
  }
  
  Fitness IOptimizable<TGenotype, TSearchSpace>.Evaluate(TGenotype genotype) {
    var phenotype = Decode(genotype);
    return Problem.Evaluate(phenotype);
  }
  
  public TSolution Decode(TGenotype genotype) {
    return Decoder.Decode(genotype);
  }
}
