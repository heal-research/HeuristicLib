using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TravelingSalesman;

namespace HEAL.HeuristicLib._ProofOfConcept.CompatabilityUsingGenericConstraints;

public interface IContext;

public interface IEncodingContext<out TEncoding> : IContext
 where TEncoding : IEncoding
{
  TEncoding Encoding { get; }
}

public interface IProblemContext<out TProblem> : IContext
  where TProblem : IProblem
{
  TProblem Problem { get; }
}

public interface IAlgorithmContext<out TAlgorithm> : IContext
  where TAlgorithm : IAlgorithm
{
  TAlgorithm Algorithm { get; }
}

public interface IDuringIterationContext<out TIterationState> : IContext
  where TIterationState : IIterationResult
{
  TIterationState? PreviousState { get; }
}

public interface IAfterIterationContext<out TIterationState> : IContext
  where TIterationState : IIterationResult
{
  TIterationState CurrentState { get; }
}


public record class AlgorithmInitializationContext<TEncoding, TProblem, TAlgorithm>
  : IEncodingContext<TEncoding>, IProblemContext<TProblem>, IAlgorithmContext<TAlgorithm>
  where TEncoding : IEncoding
  where TProblem : IProblem
  where TAlgorithm : IAlgorithm
{
  public required TEncoding Encoding { get; init; }
  public required TProblem Problem { get; init; }
  public required TAlgorithm Algorithm { get; init; }
}

public record AlgorithmExecutionContext<TEncoding, TProblem, TAlgorithm>
  : IEncodingContext<TEncoding>, IProblemContext<TProblem>, IAlgorithmContext<TAlgorithm>
  where TEncoding : IEncoding
  where TProblem : IProblem
  where TAlgorithm : IAlgorithm
{
  public required TEncoding Encoding { get; init; }
  public required TProblem Problem { get; init; }
  public required TAlgorithm Algorithm { get; init; }
  // AlgorithmState?
}

public record IterativeAlgorithmIterationExecutionContext<TEncoding, TProblem, TAlgorithm, TIterationState>
  : IEncodingContext<TEncoding>, IProblemContext<TProblem>, IAlgorithmContext<TAlgorithm>, IDuringIterationContext<TIterationState>
  where TEncoding : IEncoding
  where TProblem : IProblem
  where TAlgorithm : IAlgorithm
  where TIterationState : IIterationResult
{
  public required TEncoding Encoding { get; init; }
  public required TProblem Problem { get; init; }
  public required TAlgorithm Algorithm { get; init; }
  public required TIterationState? PreviousState { get; init; }
}

public record IterativeAlgorithmAfterIterationContext<TEncoding, TProblem, TAlgorithm, TIterationState>
  : IterativeAlgorithmIterationExecutionContext<TEncoding, TProblem, TAlgorithm, TIterationState>, IAfterIterationContext<TIterationState>
  where TEncoding : IEncoding
  where TProblem : IProblem
  where TAlgorithm : IAlgorithm
  where TIterationState : IIterationResult
{
  public required TIterationState CurrentState { get; init; }
}



public interface IOperator
{
  
}

public interface IOperator<in TContext> : IOperator
  where TContext : IContext
{
  
}


public interface ICrossover<TGenotype, in TContext> : IOperator<TContext>
  where TContext : IContext
{
  TGenotype Cross(TGenotype parent1, TGenotype parent2, TContext context);
}

public abstract class BaseCrossover<TGenotype, TContext> : ICrossover<TGenotype, TContext>
  where TContext : IContext
{
  public abstract TGenotype Cross(TGenotype parent1, TGenotype parent2, TContext context);
}


public class IndependentCrossover<TGenotype, TContext> : BaseCrossover<TGenotype, TContext>
  where TContext : IContext
{
  public override TGenotype Cross(TGenotype parent1, TGenotype parent2, TContext context) => throw new NotImplementedException();
}

public class PermutationSpecificCrossover<TContext> : BaseCrossover<Permutation, TContext>
  where TContext : IEncodingContext<PermutationEncoding>
{
  public override Permutation Cross(Permutation parent1, Permutation parent2, TContext context) => throw new NotImplementedException();
}

public class TspSpecificCrossover<TContext> : BaseCrossover<Permutation, TContext>
  where TContext : IEncodingContext<PermutationEncoding>, IProblemContext<TravelingSalesmanProblem>
{
  public override Permutation Cross(Permutation parent1, Permutation parent2, TContext context) => throw new NotImplementedException();
}

public class RealVectorSpecificCrossover<TContext> : BaseCrossover<RealVector, TContext>
  where TContext : IEncodingContext<RealVectorEncoding>
{
  public override RealVector Cross(RealVector parent1, RealVector parent2, TContext context) => throw new NotImplementedException();
}

public class TestFunctionProblemSpecificCrossover<TContext> : BaseCrossover<RealVector, TContext>
  where TContext : IEncodingContext<RealVectorEncoding>, IProblemContext<TestFunctionProblem>
{
  public override RealVector Cross(RealVector parent1, RealVector parent2, TContext context) => throw new NotImplementedException();
}

public interface IAlgorithmResult
{
}

public interface IAlgorithmResult<out TGenotype> : IAlgorithmResult
{
}

public interface IIterationResult
{
}

public interface IIterationResult<out TGenotype> : IIterationResult
{
}

public interface IAlgorithm
{
}


public interface IAlgorithm<TGenotype, in TEncoding, in TProblem> : IAlgorithm/*, IEncodingContext<TEncoding>, IProblemContext<TProblem>*/
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  IAlgorithmResult<TGenotype> Execute(TProblem problem);
}

public interface IIterativeAlgorithm<TGenotype, in TEncoding, in TProblem, TIterationResult> : IAlgorithm<TGenotype, TEncoding, TProblem>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
  where TIterationResult : IIterationResult
{
  IAlgorithmResult<TGenotype> Execute(TProblem problem, TIterationResult? previousIterationResult);
  TIterationResult ExecuteStep(TProblem problem, TIterationResult? previousIterationResult = default);
}


// public interface IAlgorithm<TGenotype, in TEncoding, in TProblem> 
//   where TEncoding : IEncoding<TGenotype>
//   where TProblem : IProblem<TGenotype, TEncoding>
// {
//   //IterationState<TGenotype> Run(TProblem problem, IterationState<TGenotype>? initialState = null);
// }


//
// public interface IAlgorithm<TGenotype, in TEncoding, in TProblem, TIterationState> 
//   where TEncoding : IEncoding<TGenotype>
//   where TProblem : IProblem<TGenotype, TEncoding>
//   where TIterationState : IIterationState<TGenotype>
// {
//   TIterationState Run(TProblem problem, TIterationState? initialState = default);
// }



public class EncodingIndependentAlgorithm<TGenotype, TEncoding> : IAlgorithm<TGenotype, TEncoding>
  where TEncoding : IEncoding<TGenotype>
{
  public required ICrossover<TGenotype, TEncoding> Crossover { get; set; }
  public IIterationResult<TGenotype> Run<TProblem>(TProblem problem, IIterationResult<TGenotype>? initialState = null)
    where TProblem : IProblem<TGenotype, TEncoding> => throw new NotImplementedException();
}


public class EncodingIndependentAlgorithm<TGenotype, TEncoding, TProblem> : IAlgorithm<TGenotype, TEncoding, TProblem>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  public required ICrossover<TGenotype, TEncoding, TProblem> Crossover { get; set; }
  public IIterationResult<TGenotype> Run(TProblem problem, IIterationResult<TGenotype>? initialState = null) => throw new NotImplementedException();
}


public class PermutationEncodingSpecificAlgorithm : IAlgorithm<Permutation, PermutationEncoding>
{
  public required ICrossover<Permutation, PermutationEncoding> Crossover { get; set; }
  public IIterationResult<Permutation> Run<TProblem>(TProblem problem, IIterationResult<Permutation>? initialState = null) where TProblem : IProblem<Permutation, PermutationEncoding> => throw new NotImplementedException();
}



public class TravelingSalesmanProblemSpecificAlgorithm : IAlgorithm<Permutation, PermutationEncoding, TravelingSalesmanProblem>
{
  public required ICrossover<Permutation, PermutationEncoding, TravelingSalesmanProblem> Crossover { get; set; }
  public IIterationResult<Permutation> Run(TravelingSalesmanProblem problem, IIterationResult<Permutation>? initialState = null) => throw new NotImplementedException();
}



public class RealVectorEncodingSpecificAlgorithm : IAlgorithm<RealVector, RealVectorEncoding>
{
  public required ICrossover<RealVector, RealVectorEncoding> Crossover { get; set; }
  public IIterationResult<RealVector> Run<TProblem>(TProblem problem, IIterationResult<RealVector>? initialState = null) where TProblem : IProblem<RealVector, RealVectorEncoding> => throw new NotImplementedException();
}


public class TestFunctionProblemSpecificAlgorithm : IAlgorithm<RealVector, RealVectorEncoding, TestFunctionProblem>
{
  public required ICrossover<RealVector, RealVectorEncoding, TestFunctionProblem> Crossover { get; set; }
  public IIterationResult<RealVector> Run(TestFunctionProblem problem, IIterationResult<RealVector>? initialState = null) => throw new NotImplementedException();
}


public class Test
{
  static void TestCompatability() {
    
     var independentAlgorithmWithPermutation = new EncodingIndependentAlgorithm<Permutation, PermutationEncoding>();
    independentAlgorithmWithPermutation.Run(new TravelingSalesmanProblem()); //ok
    independentAlgorithmWithPermutation.Run(new TestFunctionProblem()); // error: incompatible encoding
    independentAlgorithmWithPermutation.Crossover = new IndependentCrossover(); // ok
    independentAlgorithmWithPermutation.Crossover = new BaseCrossover.Adapter<Permutation, PermutationEncoding>(new IndependentCrossover()); // ok
    independentAlgorithmWithPermutation.Crossover = new PermutationSpecificCrossover(); // ok
    independentAlgorithmWithPermutation.Crossover = new TspSpecificCrossover(); // error: problem independent algorithm
    independentAlgorithmWithPermutation.Crossover = new RealVectorSpecificCrossover(); // error: incompatible encoding
    independentAlgorithmWithPermutation.Crossover = new TestFunctionProblemSpecificCrossover(); // error: incompatible encoding, problem independent algorithm
    
    var independentAlgorithmWithRealVector = new EncodingIndependentAlgorithm<RealVector, RealVectorEncoding>();
    independentAlgorithmWithRealVector.Run(new TravelingSalesmanProblem()); // error: incompatible encoding
    independentAlgorithmWithRealVector.Run(new TestFunctionProblem()); // ok
    independentAlgorithmWithRealVector.Crossover = new IndependentCrossover(); // ok
    independentAlgorithmWithRealVector.Crossover = new PermutationSpecificCrossover(); // error: incompatible encoding
    independentAlgorithmWithRealVector.Crossover = new TspSpecificCrossover(); // error: incompatible encoding, problem independent algorithm
    independentAlgorithmWithRealVector.Crossover = new RealVectorSpecificCrossover(); // ok
    independentAlgorithmWithRealVector.Crossover = new TestFunctionProblemSpecificCrossover(); // error: problem independent algorithm
    
    var permutationSpecificAlgorithm = new PermutationEncodingSpecificAlgorithm();
    permutationSpecificAlgorithm.Run(new TravelingSalesmanProblem()); // ok
    permutationSpecificAlgorithm.Run(new TestFunctionProblem()); // error: incompatible encoding
    permutationSpecificAlgorithm.Crossover = new IndependentCrossover(); // ok
    permutationSpecificAlgorithm.Crossover = new PermutationSpecificCrossover(); // ok
    permutationSpecificAlgorithm.Crossover = new TspSpecificCrossover(); // error: problem independent algorithm
    permutationSpecificAlgorithm.Crossover = new RealVectorSpecificCrossover(); // error: incompatible encoding
    permutationSpecificAlgorithm.Crossover = new TestFunctionProblemSpecificCrossover(); // error: incompatible encoding, problem independent algorithm
    
    var tspSpecificAlgorithm = new TravelingSalesmanProblemSpecificAlgorithm();
    tspSpecificAlgorithm.Run(new TravelingSalesmanProblem()); // ok
    tspSpecificAlgorithm.Run(new TestFunctionProblem()); // error: incompatible encoding, incompatible problem
    tspSpecificAlgorithm.Crossover = new IndependentCrossover(); // ok
    tspSpecificAlgorithm.Crossover = new BaseCrossover.Adapter<Permutation, PermutationEncoding, TravelingSalesmanProblem>(new IndependentCrossover()); // ok
    tspSpecificAlgorithm.Crossover = new BaseCrossover<Permutation, PermutationEncoding>.Adapter<TravelingSalesmanProblem>(new IndependentCrossover()); // ok
    tspSpecificAlgorithm.Crossover = new PermutationSpecificCrossover(); // ok
    tspSpecificAlgorithm.Crossover = new TspSpecificCrossover(); // ok
    tspSpecificAlgorithm.Crossover = new RealVectorSpecificCrossover(); // error: incompatible encoding
    tspSpecificAlgorithm.Crossover = new TestFunctionProblemSpecificCrossover(); // error: incompatible encoding, incompatible problem
    
    var realVectorSpecificAlgorithm = new RealVectorEncodingSpecificAlgorithm();
    realVectorSpecificAlgorithm.Run(new TravelingSalesmanProblem()); // error: incompatible encoding
    realVectorSpecificAlgorithm.Run(new TestFunctionProblem()); // ok
    realVectorSpecificAlgorithm.Crossover = new IndependentCrossover(); // ok
    realVectorSpecificAlgorithm.Crossover = new PermutationSpecificCrossover(); // error: incompatible encoding
    realVectorSpecificAlgorithm.Crossover = new TspSpecificCrossover(); // error: incompatible encoding, problem independent algorithm
    realVectorSpecificAlgorithm.Crossover = new RealVectorSpecificCrossover(); // ok
    realVectorSpecificAlgorithm.Crossover = new TestFunctionProblemSpecificCrossover(); // error: problem independent algorithm
    
    var testFunctionProblemSpecificAlgorithm = new TestFunctionProblemSpecificAlgorithm();
    testFunctionProblemSpecificAlgorithm.Run(new TravelingSalesmanProblem()); // error: incompatible encoding, incompatible problem
    testFunctionProblemSpecificAlgorithm.Run(new TestFunctionProblem()); // ok
    testFunctionProblemSpecificAlgorithm.Crossover = new IndependentCrossover(); // ok
    testFunctionProblemSpecificAlgorithm.Crossover = new PermutationSpecificCrossover(); // error: incompatible encoding
    testFunctionProblemSpecificAlgorithm.Crossover = new TspSpecificCrossover(); // error: incompatible encoding, incompatible problem
    testFunctionProblemSpecificAlgorithm.Crossover = new RealVectorSpecificCrossover(); // ok
    testFunctionProblemSpecificAlgorithm.Crossover = new TestFunctionProblemSpecificCrossover(); // ok
  }
}

