
using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib._ProofOfConcept.CompatabilityUsingFacories;

public interface IProblem
{
}

public interface IProblem<in TGenotype, out TEncoding> : IProblem
  where TEncoding : IEncoding<TGenotype>
{
  TEncoding Encoding { get; }
  double Evaluate(TGenotype genotype);
}

public class TravelingSalesmanProblem : IProblem<Permutation, PermutationEncoding>
{
  public PermutationEncoding Encoding { get; } = new PermutationEncoding(10);

  public double Evaluate(Permutation genotype) => throw new NotImplementedException();
}

public class TestFunctionProblem : IProblem<RealVector, RealVectorEncoding>
{
  public RealVectorEncoding Encoding { get; } = new RealVectorEncoding(10, new RealVector(10), new RealVector(10));

  public double Evaluate(RealVector genotype) => throw new NotImplementedException();
}

public interface IOperator<TGenotype, TEncoding, TProblem, TAlgorithm, TAlgorithmState>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
  where TAlgorithm : IAlgorithm<TGenotype, TEncoding, TProblem>
  where TAlgorithmState : IIterationState<TGenotype>

{
  void Initialize(IterationContext<TGenotype, TEncoding, TProblem, TAlgorithm, TAlgorithmState> context);
}


public class IterationContext<TGenotype, TEncoding, TProblem, TAlgorithm, TAlgorithmState>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
  where TAlgorithm : IAlgorithm<TGenotype, TEncoding, TProblem>
  where TAlgorithmState : IIterationState<TGenotype>
{
  public required TEncoding Encoding { get; init; }
  public required TProblem Problem { get; init; }
  public required TAlgorithm Algorithm { get; init; }
  public required TAlgorithmState State { get; init; }
}

public interface ICrossover
{
  TGenotype Cross<TGenotype>(TGenotype parent1, TGenotype parent2);
}

public abstract class BaseCrossover : ICrossover
{
  public abstract TGenotype Cross<TGenotype>(TGenotype parent1, TGenotype parent2);
  
  public class Adapter<TGenotype, TEncoding>(ICrossover crossover) : BaseCrossover<TGenotype, TEncoding>
    where TEncoding : IEncoding<TGenotype>
  {
    public override TGenotype Cross(TGenotype parent1, TGenotype parent2, TEncoding encoding) => crossover.Cross(parent1, parent2);
  }

  public class Adapter<TGenotype, TEncoding, TProblem>(ICrossover crossover) : BaseCrossover<TGenotype, TEncoding, TProblem>
    where TEncoding : IEncoding<TGenotype> 
    where TProblem : IProblem<TGenotype, TEncoding>
  {
    public override TGenotype Cross(TGenotype parent1, TGenotype parent2, TEncoding encoding, TProblem problem) => crossover.Cross(parent1, parent2);
  }
}

public interface ICrossover<TGenotype, in TEncoding>
  where TEncoding : IEncoding<TGenotype>
{
  TGenotype Cross(TGenotype parent1, TGenotype parent2, TEncoding encoding);
}

public abstract class BaseCrossover<TGenotype, TEncoding> : ICrossover<TGenotype, TEncoding>
  where TEncoding : IEncoding<TGenotype>
{
  public abstract TGenotype Cross(TGenotype parent1, TGenotype parent2, TEncoding encoding);
  
  public class Adapter<TProblem>(ICrossover<TGenotype, TEncoding> crossover) : BaseCrossover<TGenotype, TEncoding, TProblem>
    where TProblem : IProblem<TGenotype, TEncoding>
  {
    public override TGenotype Cross(TGenotype parent1, TGenotype parent2, TEncoding encoding, TProblem problem) => crossover.Cross(parent1, parent2, encoding);
  }
}

public interface ICrossover<TGenotype, in TEncoding, in TProblem>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  TGenotype Cross(TGenotype parent1, TGenotype parent2, TEncoding encoding, TProblem problem);
}

public abstract class BaseCrossover<TGenotype, TEncoding, TProblem> : ICrossover<TGenotype, TEncoding, TProblem>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  public abstract TGenotype Cross(TGenotype parent1, TGenotype parent2, TEncoding encoding, TProblem problem);
}

public class IndependentCrossover : BaseCrossover
{
  public override TGenotype Cross<TGenotype>(TGenotype parent1, TGenotype parent2) => throw new NotImplementedException();
}

public class PermutationSpecificCrossover : BaseCrossover<Permutation, PermutationEncoding>
{
  public override Permutation Cross(Permutation parent1, Permutation parent2, PermutationEncoding encoding) => throw new NotImplementedException();
}

public class TspSpecificCrossover : BaseCrossover<Permutation, PermutationEncoding, TravelingSalesmanProblem>
{
  public override Permutation Cross(Permutation parent1, Permutation parent2, PermutationEncoding encoding, TravelingSalesmanProblem problem) => throw new NotImplementedException();
}

public class RealVectorSpecificCrossover : BaseCrossover<RealVector, RealVectorEncoding>
{
  public override RealVector Cross(RealVector parent1, RealVector parent2, RealVectorEncoding encoding) => throw new NotImplementedException();
}

public class TestFunctionProblemSpecificCrossover : BaseCrossover<RealVector, RealVectorEncoding, TestFunctionProblem>
{
  public override RealVector Cross(RealVector parent1, RealVector parent2, RealVectorEncoding encoding, TestFunctionProblem problem) => throw new NotImplementedException();
}

public interface IIterationState<out TGenotype>
{
  IReadOnlyList<TGenotype> Population { get; }
}

public class IterationState<TGenotype> : IIterationState<TGenotype>
{
  public required IReadOnlyList<TGenotype> Population { get; init; }
}

public interface IAlgorithm
{
}



public interface IAlgorithmBuilder<TSelf, out TAlgorithm>
  where TSelf : IAlgorithmBuilder<TSelf, TAlgorithm>
  where TAlgorithm : IAlgorithm
{
  TAlgorithm Create();
}

public interface IAlgorithm<TGenotype, in TEncoding> : IAlgorithm 
  where TEncoding : IEncoding<TGenotype>
{
  IterationState<TGenotype> Run<TProblem>(TProblem problem, IterationState<TGenotype>? initialState = null) where TProblem : IProblem<TGenotype, TEncoding>;
}

public interface IAlgorithmBuilder<TGenotype, TEncoding, out TAlgorithm, out TSelf>
  where TEncoding : IEncoding<TGenotype>
  where TSelf : IAlgorithmBuilder<TGenotype, TEncoding, TAlgorithm, TSelf>
{
  TAlgorithm Create();
  
  ICrossover<TGenotype, TEncoding>? Crossover { get; }
  
  TSelf WithCrossover(ICrossover crossover);
  TSelf WithCrossover(ICrossover<TGenotype, TEncoding> crossover);
  
  // for lifting builder to problem specific builder
  //IAlgorithmBuilder<TGenotype, TEncoding, TProblem> WithCrossover<TProblem>(ICrossover<TGenotype, TEncoding, TProblem> crossover) where TProblem : IProblem<TGenotype, TEncoding>;
}

public interface IAlgorithm<TGenotype, in TEncoding, in TProblem> 
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  //IterationState<TGenotype> Run(TProblem problem, IterationState<TGenotype>? initialState = null);
}

public interface IAlgorithmBuilder<TGenotype, TEncoding, TProblem, TAlgorithm, out TSelf>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
  where TAlgorithm : IAlgorithm<TGenotype, TEncoding, TProblem>
  where TSelf : IAlgorithmBuilder<TGenotype, TEncoding, TProblem, TAlgorithm, TSelf>
{
  TAlgorithm Create();
  
  ICrossover<TGenotype, TEncoding, TProblem>? Crossover { get; }
  
  TSelf WithCrossover(ICrossover crossover);
  TSelf WithCrossover(ICrossover<TGenotype, TEncoding> crossover);
  TSelf WithCrossover(ICrossover<TGenotype, TEncoding, TProblem> crossover);
}


public interface IAlgorithm<TGenotype, in TEncoding, in TProblem, TIterationState> 
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
  where TIterationState : IIterationState<TGenotype>
{
  TIterationState Run(TProblem problem, TIterationState? initialState = default);
}



public class EncodingIndependentAlgorithm<TGenotype, TEncoding> : IAlgorithm<TGenotype, TEncoding>
  where TEncoding : IEncoding<TGenotype>
{
  public required ICrossover<TGenotype, TEncoding> Crossover { get; set; }
  public IterationState<TGenotype> Run<TProblem>(TProblem problem, IterationState<TGenotype>? initialState = null)
    where TProblem : IProblem<TGenotype, TEncoding> => throw new NotImplementedException();
}

public class EncodingIndependentAlgorithmBuilder<TGenotype, TEncoding> : IAlgorithmBuilder<TGenotype, TEncoding, EncodingIndependentAlgorithm<TGenotype, TEncoding>, EncodingIndependentAlgorithmBuilder<TGenotype, TEncoding>>
  where TEncoding : IEncoding<TGenotype>
{
  public ICrossover<TGenotype, TEncoding>? Crossover { get; private set; }

  public EncodingIndependentAlgorithm<TGenotype, TEncoding> Create() => new EncodingIndependentAlgorithm<TGenotype, TEncoding> { Crossover = Crossover };

  public EncodingIndependentAlgorithmBuilder<TGenotype, TEncoding> WithCrossover(ICrossover crossover) {
    Crossover = new BaseCrossover.Adapter<TGenotype, TEncoding>(crossover);
    return this;
  }

  public EncodingIndependentAlgorithmBuilder<TGenotype, TEncoding> WithCrossover(ICrossover<TGenotype, TEncoding> crossover) {
    Crossover = crossover;
    return this;
  }
}

public class EncodingIndependentAlgorithm<TGenotype, TEncoding, TProblem> : IAlgorithm<TGenotype, TEncoding, TProblem>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  public required ICrossover<TGenotype, TEncoding, TProblem> Crossover { get; set; }
  public IterationState<TGenotype> Run(TProblem problem, IterationState<TGenotype>? initialState = null) => throw new NotImplementedException();
}

public class EncodingIndependentAlgorithmBuilder<TGenotype, TEncoding, TProblem> : IAlgorithmBuilder<TGenotype, TEncoding, TProblem, EncodingIndependentAlgorithm<TGenotype, TEncoding, TProblem>, EncodingIndependentAlgorithmBuilder<TGenotype, TEncoding, TProblem>>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  public ICrossover<TGenotype, TEncoding, TProblem>? Crossover { get; private set; }

  public EncodingIndependentAlgorithm<TGenotype, TEncoding, TProblem> Create() => new EncodingIndependentAlgorithm<TGenotype, TEncoding, TProblem> { Crossover = Crossover };

  public EncodingIndependentAlgorithmBuilder<TGenotype, TEncoding, TProblem> WithCrossover(ICrossover crossover) {
    Crossover = new BaseCrossover.Adapter<TGenotype, TEncoding, TProblem>(crossover);
    return this;
  }

  public EncodingIndependentAlgorithmBuilder<TGenotype, TEncoding, TProblem> WithCrossover(ICrossover<TGenotype, TEncoding> crossover) {
    Crossover = new BaseCrossover<TGenotype, TEncoding>.Adapter<TProblem>(crossover);
    return this;
  }

  public EncodingIndependentAlgorithmBuilder<TGenotype, TEncoding, TProblem> WithCrossover(ICrossover<TGenotype, TEncoding, TProblem> crossover) {
    Crossover = crossover;
    return this;
  }
}

public class PermutationEncodingSpecificAlgorithm : IAlgorithm<Permutation, PermutationEncoding>
{
  public required ICrossover<Permutation, PermutationEncoding> Crossover { get; set; }
  public IterationState<Permutation> Run<TProblem>(TProblem problem, IterationState<Permutation>? initialState = null) where TProblem : IProblem<Permutation, PermutationEncoding> => throw new NotImplementedException();
}

public class PermutationEncodingSpecificAlgorithmBuilder : IAlgorithmBuilder<Permutation, PermutationEncoding, PermutationEncodingSpecificAlgorithm, PermutationEncodingSpecificAlgorithmBuilder>
{
  public ICrossover<Permutation, PermutationEncoding>? Crossover { get; private set; }

  public PermutationEncodingSpecificAlgorithm Create() => new PermutationEncodingSpecificAlgorithm { Crossover = Crossover };

  public PermutationEncodingSpecificAlgorithmBuilder WithCrossover(ICrossover crossover) {
    Crossover = new BaseCrossover.Adapter<Permutation, PermutationEncoding>(crossover);
    return this;
  }

  public PermutationEncodingSpecificAlgorithmBuilder WithCrossover(ICrossover<Permutation, PermutationEncoding> crossover) {
    Crossover = crossover;
    return this;
  }
}

public class TravelingSalesmanProblemSpecificAlgorithm : IAlgorithm<Permutation, PermutationEncoding, TravelingSalesmanProblem>
{
  public required ICrossover<Permutation, PermutationEncoding, TravelingSalesmanProblem> Crossover { get; set; }
  public IterationState<Permutation> Run(TravelingSalesmanProblem problem, IterationState<Permutation>? initialState = null) => throw new NotImplementedException();
}

public class TravelingSalesmanProblemSpecificAlgorithmBuilder : IAlgorithmBuilder<Permutation, PermutationEncoding, TravelingSalesmanProblem, TravelingSalesmanProblemSpecificAlgorithm, TravelingSalesmanProblemSpecificAlgorithmBuilder>
{
  public ICrossover<Permutation, PermutationEncoding, TravelingSalesmanProblem>? Crossover { get; private set; }

  public TravelingSalesmanProblemSpecificAlgorithm Create() => new TravelingSalesmanProblemSpecificAlgorithm { Crossover = Crossover };

  public TravelingSalesmanProblemSpecificAlgorithmBuilder WithCrossover(ICrossover crossover) {
    Crossover = new BaseCrossover.Adapter<Permutation, PermutationEncoding, TravelingSalesmanProblem>(crossover);
    return this;
  }

  public TravelingSalesmanProblemSpecificAlgorithmBuilder WithCrossover(ICrossover<Permutation, PermutationEncoding> crossover) {
    Crossover = new BaseCrossover<Permutation, PermutationEncoding>.Adapter<TravelingSalesmanProblem>(crossover);
    return this;
  }

  public TravelingSalesmanProblemSpecificAlgorithmBuilder WithCrossover(ICrossover<Permutation, PermutationEncoding, TravelingSalesmanProblem> crossover) {
    Crossover = crossover;
    return this;
  }
}

public class RealVectorEncodingSpecificAlgorithm : IAlgorithm<RealVector, RealVectorEncoding>
{
  public required ICrossover<RealVector, RealVectorEncoding> Crossover { get; set; }
  public IterationState<RealVector> Run<TProblem>(TProblem problem, IterationState<RealVector>? initialState = null) where TProblem : IProblem<RealVector, RealVectorEncoding> => throw new NotImplementedException();
}

public class RealVectorEncodingSpecificAlgorithmBuilder : IAlgorithmBuilder<RealVector, RealVectorEncoding, RealVectorEncodingSpecificAlgorithm, RealVectorEncodingSpecificAlgorithmBuilder>
{
  public ICrossover<RealVector, RealVectorEncoding>? Crossover { get; private set; }

  public RealVectorEncodingSpecificAlgorithm Create() => new RealVectorEncodingSpecificAlgorithm { Crossover = Crossover };

  public RealVectorEncodingSpecificAlgorithmBuilder WithCrossover(ICrossover crossover) {
    Crossover = new BaseCrossover.Adapter<RealVector, RealVectorEncoding>(crossover);
    return this;
  }

  public RealVectorEncodingSpecificAlgorithmBuilder WithCrossover(ICrossover<RealVector, RealVectorEncoding> crossover) {
    Crossover = crossover;
    return this;
  }
}

public class TestFunctionProblemSpecificAlgorithm : IAlgorithm<RealVector, RealVectorEncoding, TestFunctionProblem>
{
  public required ICrossover<RealVector, RealVectorEncoding, TestFunctionProblem> Crossover { get; set; }
  public IterationState<RealVector> Run(TestFunctionProblem problem, IterationState<RealVector>? initialState = null) => throw new NotImplementedException();
}

public class TestFunctionProblemSpecificAlgorithmBuilder : IAlgorithmBuilder<RealVector, RealVectorEncoding, TestFunctionProblem, TestFunctionProblemSpecificAlgorithm, TestFunctionProblemSpecificAlgorithmBuilder>
{
  public ICrossover<RealVector, RealVectorEncoding, TestFunctionProblem>? Crossover { get; private set; }

  public TestFunctionProblemSpecificAlgorithm Create() => new TestFunctionProblemSpecificAlgorithm { Crossover = Crossover };

  public TestFunctionProblemSpecificAlgorithmBuilder WithCrossover(ICrossover crossover) {
    Crossover = new BaseCrossover.Adapter<RealVector, RealVectorEncoding, TestFunctionProblem>(crossover);
    return this;
  }

  public TestFunctionProblemSpecificAlgorithmBuilder WithCrossover(ICrossover<RealVector, RealVectorEncoding> crossover) {
    Crossover = new BaseCrossover<RealVector, RealVectorEncoding>.Adapter<TestFunctionProblem>(crossover);
    return this;
  }

  public TestFunctionProblemSpecificAlgorithmBuilder WithCrossover(ICrossover<RealVector, RealVectorEncoding, TestFunctionProblem> crossover) {
    Crossover = crossover;
    return this;
  }
}

public class Test
{
  static void TestCompatability() {
    
    var independentAlgorithmWithPermutation = new EncodingIndependentAlgorithmBuilder<Permutation, PermutationEncoding>();
    independentAlgorithmWithPermutation.WithCrossover(new IndependentCrossover()); // ok
    independentAlgorithmWithPermutation.WithCrossover(new PermutationSpecificCrossover()); // ok
    independentAlgorithmWithPermutation.WithCrossover(new TspSpecificCrossover()); // error: problem independent algorithm
    independentAlgorithmWithPermutation.WithCrossover(new RealVectorSpecificCrossover()); // error: incompatible encoding
    independentAlgorithmWithPermutation.WithCrossover(new TestFunctionProblemSpecificCrossover()); // error: incompatible encoding, problem independent algorithm
    
    var independentAlgorithmWithRealVector = new EncodingIndependentAlgorithmBuilder<RealVector, RealVectorEncoding>();
    independentAlgorithmWithRealVector.WithCrossover(new IndependentCrossover()); // ok
    independentAlgorithmWithRealVector.WithCrossover(new PermutationSpecificCrossover()); // error: incompatible encoding
    independentAlgorithmWithRealVector.WithCrossover(new TspSpecificCrossover()); // error: incompatible encoding, problem independent algorithm
    independentAlgorithmWithRealVector.WithCrossover(new RealVectorSpecificCrossover()); // ok
    independentAlgorithmWithRealVector.WithCrossover(new TestFunctionProblemSpecificCrossover()); // error: problem independent algorithm
    
    var independentAlgorithmWithTsp = new EncodingIndependentAlgorithmBuilder<Permutation, PermutationEncoding, TravelingSalesmanProblem>();
    independentAlgorithmWithTsp.WithCrossover(new IndependentCrossover()); // ok
    independentAlgorithmWithTsp.WithCrossover(new PermutationSpecificCrossover()); // ok
    independentAlgorithmWithTsp.WithCrossover(new TspSpecificCrossover()); // ok
    independentAlgorithmWithTsp.WithCrossover(new RealVectorSpecificCrossover()); // error: incompatible encoding
    independentAlgorithmWithTsp.WithCrossover(new TestFunctionProblemSpecificCrossover()); // error: incompatible encoding, incompatible problem
    
    var independentAlgorithmWithTestFunction = new EncodingIndependentAlgorithmBuilder<RealVector, RealVectorEncoding, TestFunctionProblem>();
    independentAlgorithmWithTestFunction.WithCrossover(new IndependentCrossover()); // ok
    independentAlgorithmWithTestFunction.WithCrossover(new PermutationSpecificCrossover()); // error: incompatible encoding
    independentAlgorithmWithTestFunction.WithCrossover(new TspSpecificCrossover()); // error: incompatible encoding, incompatible problem
    independentAlgorithmWithTestFunction.WithCrossover(new RealVectorSpecificCrossover()); // ok
    independentAlgorithmWithTestFunction.WithCrossover(new TestFunctionProblemSpecificCrossover()); // ok
    
    var permutationSpecificAlgorithm = new PermutationEncodingSpecificAlgorithmBuilder();
    permutationSpecificAlgorithm.WithCrossover(new IndependentCrossover()); // ok
    permutationSpecificAlgorithm.WithCrossover(new PermutationSpecificCrossover()); // ok
    permutationSpecificAlgorithm.WithCrossover(new TspSpecificCrossover()); // error: problem independent algorithm
    permutationSpecificAlgorithm.WithCrossover(new RealVectorSpecificCrossover()); // error: incompatible encoding
    permutationSpecificAlgorithm.WithCrossover(new TestFunctionProblemSpecificCrossover()); // error: incompatible encoding, problem independent algorithm
    
    var tspSpecificAlgorithm = new TravelingSalesmanProblemSpecificAlgorithmBuilder();
    tspSpecificAlgorithm.WithCrossover(new IndependentCrossover()); // ok
    tspSpecificAlgorithm.WithCrossover(new PermutationSpecificCrossover()); // ok
    tspSpecificAlgorithm.WithCrossover(new TspSpecificCrossover()); // ok
    tspSpecificAlgorithm.WithCrossover(new RealVectorSpecificCrossover()); // error: incompatible encoding
    tspSpecificAlgorithm.WithCrossover(new TestFunctionProblemSpecificCrossover()); // error: incompatible encoding, incompatible problem
    
    var realVectorSpecificAlgorithm = new RealVectorEncodingSpecificAlgorithmBuilder();
    realVectorSpecificAlgorithm.WithCrossover(new IndependentCrossover()); // ok
    realVectorSpecificAlgorithm.WithCrossover(new PermutationSpecificCrossover()); // error: incompatible encoding
    realVectorSpecificAlgorithm.WithCrossover(new TspSpecificCrossover()); // error: incompatible encoding, problem independent algorithm
    realVectorSpecificAlgorithm.WithCrossover(new RealVectorSpecificCrossover()); // ok
    realVectorSpecificAlgorithm.WithCrossover(new TestFunctionProblemSpecificCrossover()); // error: problem independent algorithm
    
    var testFunctionProblemSpecificAlgorithm = new TestFunctionProblemSpecificAlgorithmBuilder();
    testFunctionProblemSpecificAlgorithm.WithCrossover(new IndependentCrossover()); // ok
    testFunctionProblemSpecificAlgorithm.WithCrossover(new PermutationSpecificCrossover()); // error: incompatible encoding
    testFunctionProblemSpecificAlgorithm.WithCrossover(new TspSpecificCrossover()); // error: incompatible encoding, incompatible problem
    testFunctionProblemSpecificAlgorithm.WithCrossover(new RealVectorSpecificCrossover()); // ok
    testFunctionProblemSpecificAlgorithm.WithCrossover(new TestFunctionProblemSpecificCrossover()); // ok
  }
}
