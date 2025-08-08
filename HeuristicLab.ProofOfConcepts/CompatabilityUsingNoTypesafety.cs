
using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TravelingSalesman;

namespace HEAL.HeuristicLib._ProofOfConcept.CompatabilityUsingNoTypesafety;


public class IterationContext
{
  public required IEncoding Encoding { get; init; }
  public required IProblem Problem { get; init; }
  public required IAlgorithm Algorithm { get; init; }
  public required IIterationState State { get; init; }
}

public interface IEncodingDependentOperator<TEncoding>
  where TEncoding : IEncoding
{
  TEncoding Encoding { get; set; }
}

public interface ICrossover
{
  object Cross(object parent1, object parent2);
}

public abstract class Crossover : ICrossover
{
  public object Cross(object parent1, object parent2) {
    return Cross<object>(parent1, parent2);
  }
  
  public abstract TGenotype Cross<TGenotype>(TGenotype parent1, TGenotype parent2)
    where TGenotype : class;
}

public abstract class Crossover<TGenotype, TEncoding> : ICrossover, IEncodingDependentOperator<TEncoding>
  where TGenotype : class
  where TEncoding : IEncoding<TGenotype>
{
  public TEncoding? Encoding { get; set; }

  public abstract TGenotype Cross(TGenotype parent1, TGenotype parent2, TEncoding encoding);
  
  public object Cross(object parent1, object parent2) {
    if (parent1 is not TGenotype p1 || parent2 is not TGenotype p2) throw new ArgumentException("Parent genotypes do not match the expected type.");
    if (Encoding is null) throw new InvalidOperationException("Encoding must be set before crossing.");
    if (Encoding is not TEncoding encoding) throw new ArgumentException("Encoding does not match the expected type.");
    
    return Cross(p1, p2, encoding);
  }
}

public abstract class Crossover<TGenotype, TEncoding, TProblem> : ICrossover
  where TGenotype : class
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  public TEncoding? Encoding { get; set; }
  public TProblem? Problem { get; set; }

  public abstract TGenotype Cross(TGenotype parent1, TGenotype parent2, TEncoding encoding, TProblem problem);

  public object Cross(object parent1, object parent2) {
    if (parent1 is not TGenotype p1 || parent2 is not TGenotype p2)
      throw new ArgumentException("Parent genotypes do not match the expected type.");
    if (Encoding is null)
      throw new InvalidOperationException("Encoding must be set before crossing.");
    if (Encoding is not TEncoding encoding)
      throw new ArgumentException("Encoding does not match the expected type.");
    if (Problem is null)
      throw new InvalidOperationException("Problem must be set before crossing.");
    if (Problem is not TProblem problem)
      throw new ArgumentException("Problem does not match the expected type.");

    return Cross(p1, p2, encoding, problem);
  }
}


public class IndependentCrossover : Crossover
{
  public override TGenotype Cross<TGenotype>(TGenotype parent1, TGenotype parent2) => throw new NotImplementedException();
}

public class PermutationSpecificCrossover : Crossover<Permutation, PermutationEncoding>
{
  public override Permutation Cross(Permutation parent1, Permutation parent2, PermutationEncoding encoding) => throw new NotImplementedException();
}

public class TspSpecificCrossover : Crossover<Permutation, PermutationEncoding, TravelingSalesmanProblem>
{
  public override Permutation Cross(Permutation parent1, Permutation parent2, PermutationEncoding encoding, TravelingSalesmanProblem problem) => throw new NotImplementedException();
}

public class RealVectorSpecificCrossover : Crossover<RealVector, RealVectorEncoding>
{
  public override RealVector Cross(RealVector parent1, RealVector parent2, RealVectorEncoding encoding) => throw new NotImplementedException();
}

public class TestFunctionProblemSpecificCrossover : Crossover<RealVector, RealVectorEncoding, TestFunctionProblem>
{
  public override RealVector Cross(RealVector parent1, RealVector parent2, RealVectorEncoding encoding, TestFunctionProblem problem) => throw new NotImplementedException();
}

public interface IIterationState
{
  
}

public interface IIterationState<out TGenotype> : IIterationState
{
  IReadOnlyList<TGenotype> Population { get; }
}

public class IterationState<TGenotype> : IIterationState<TGenotype>
{
  public required IReadOnlyList<TGenotype> Population { get; init; }
}

public interface IAlgorithm
{
  ICrossover Crossover { get; set; }
  
  IIterationState Run(IProblem problem, IIterationState? initialState = null);
}


public class EncodingIndependentAlgorithm<TGenotype, TEncoding> : IAlgorithm
  where TGenotype : class
  where TEncoding : IEncoding<TGenotype> 
{
  public ICrossover Crossover {
    get { return Crossover2; }
    set {
      if (value is not Crossover<TGenotype, TEncoding> crossover) throw new ArgumentException($"Crossover must be of type Crossover<{typeof(TGenotype).Name}, {typeof(TEncoding).Name}>.");
      Crossover2 = crossover;
    }
  }
  public Crossover<TGenotype, TEncoding> Crossover2 { get; set; }
  
  // public Crossover<TGenotype, TEncoding> Crossover { get; set; }
  //
  // ICrossover IAlgorithm.Crossover {
  //   get => Crossover;
  //   set {
  //     if (value is not Crossover<TGenotype, TEncoding> crossover) throw new ArgumentException($"Crossover must be of type Crossover<{typeof(TGenotype).Name}, {typeof(TEncoding).Name}>.");
  //     Crossover = crossover;
  //   }
  // }
  
  public IIterationState<TGenotype> Run(IProblem problem, IIterationState<TGenotype>? initialState = null) {
    
    throw new NotImplementedException();
  }

  IIterationState IAlgorithm.Run(IProblem problem, IIterationState? initialState) {
    if (problem is not IProblem<TGenotype, TEncoding> specificProblem) throw new ArgumentException($"Problem must be of type IProblem<{typeof(TGenotype).Name}, {typeof(TEncoding).Name}>.");
    if (initialState is not null && initialState is not IIterationState<TGenotype>) throw new ArgumentException($"Initial state must be of type IterationState<{typeof(TGenotype).Name}> or null.");
    return Run(specificProblem, initialState as IterationState<TGenotype>);
  }
}

public class PermutationEncodingSpecificAlgorithm : IAlgorithm
{
  public Crossover<Permutation, PermutationEncoding> Crossover { get; set; }

  ICrossover IAlgorithm.Crossover {
    get => Crossover;
    set {
      if (value is not Crossover<Permutation, PermutationEncoding> crossover) throw new ArgumentException("Crossover must be of type Crossover<Permutation, PermutationEncoding>.");
      Crossover = crossover;
    }
  }

  public IIterationState<Permutation> Run(PermutationProblem problem, IIterationState<Permutation>? initialState = null) {
    throw new NotImplementedException();
  }
  
  IIterationState IAlgorithm.Run(IProblem problem, IIterationState? initialState) {
    if (problem is not TravelingSalesmanProblem tspProblem) throw new ArgumentException("Problem must be of type TravelingSalesmanProblem.");
    if (initialState is not null && initialState is not IIterationState<Permutation>) throw new ArgumentException("Initial state must be of type IterationState<Permutation> or null.");
    return Run(tspProblem, initialState as IterationState<Permutation>);
  }
}

public class TravelingSalesmanProblemSpecificAlgorithm : IAlgorithm
{
  public Crossover<Permutation, PermutationEncoding, TravelingSalesmanProblem> Crossover { get; set; }
  ICrossover IAlgorithm.Crossover {
    get => Crossover;
    set {
      if (value is not Crossover<Permutation, PermutationEncoding, TravelingSalesmanProblem> crossover)
        throw new ArgumentException("Crossover must be of type Crossover<Permutation, PermutationEncoding, TravelingSalesmanProblem>.");
      Crossover = crossover;
    }
  }
  public IterationState<Permutation> Run(TravelingSalesmanProblem problem, IterationState<Permutation>? initialState = null) => throw new NotImplementedException();
  
  IIterationState IAlgorithm.Run(IProblem problem, IIterationState? initialState) {
    if (problem is not TravelingSalesmanProblem tspProblem) throw new ArgumentException("Problem must be of type TravelingSalesmanProblem.");
    if (initialState is not null && initialState is not IIterationState<Permutation>) throw new ArgumentException("Initial state must be of type IterationState<Permutation> or null.");
    return Run(tspProblem, initialState as IterationState<Permutation>);
  }
}

public class RealVectorEncodingSpecificAlgorithm : IAlgorithm
{
  public Crossover<RealVector, RealVectorEncoding> Crossover { get; set; }
  ICrossover IAlgorithm.Crossover {
    get => Crossover;
    set {
      if (value is not Crossover<RealVector, RealVectorEncoding> crossover) throw new ArgumentException("Crossover must be of type Crossover<RealVector, RealVectorEncoding>.");
      Crossover = crossover;
    }
  }
  
  public IIterationState<RealVector> Run(RealVectorProblem problem, IIterationState<RealVector>? initialState = null) => throw new NotImplementedException();
  
  IIterationState IAlgorithm.Run(IProblem problem, IIterationState? initialState) {
    if (problem is not RealVectorProblem rvProblem) throw new ArgumentException("Problem must be of type RealVectorProblem.");
    if (initialState is not null && initialState is not IIterationState<RealVector>) throw new ArgumentException("Initial state must be of type IterationState<RealVector> or null.");
    return Run(rvProblem, initialState as IterationState<RealVector>);
  }
  
}

public class TestFunctionProblemSpecificAlgorithm : IAlgorithm
{
  public Crossover<RealVector, RealVectorEncoding, TestFunctionProblem> Crossover { get; set; }
  ICrossover IAlgorithm.Crossover {
    get => Crossover;
    set {
      if (value is not Crossover<RealVector, RealVectorEncoding, TestFunctionProblem> crossover)
        throw new ArgumentException("Crossover must be of type Crossover<RealVector, RealVectorEncoding, TestFunctionProblem>.");
      Crossover = crossover;
    }
  }
  
  
  public IIterationState<RealVector> Run(TestFunctionProblem problem, IIterationState<RealVector>? initialState = null) => throw new NotImplementedException();
  
  IIterationState IAlgorithm.Run(IProblem problem, IIterationState? initialState) {
    if (problem is not TestFunctionProblem tfProblem) throw new ArgumentException("Problem must be of type TestFunctionProblem.");
    if (initialState is not null && initialState is not IIterationState<RealVector>) throw new ArgumentException("Initial state must be of type IterationState<RealVector> or null.");
    return Run(tfProblem, initialState as IterationState<RealVector>);
  }
}


public class Test
{
  static void TestCompatability() {
    
    var independentAlgorithmWithPermutation = new EncodingIndependentAlgorithm<Permutation, PermutationEncoding>();
    independentAlgorithmWithPermutation.Run(new TravelingSalesmanProblem()); //ok
    independentAlgorithmWithPermutation.Run(new TestFunctionProblem()); // error: incompatible encoding
    independentAlgorithmWithPermutation.Crossover = new IndependentCrossover(); // ok
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
