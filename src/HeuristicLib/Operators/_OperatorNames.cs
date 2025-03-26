using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings;

namespace HEAL.HeuristicLib.Operators;

public abstract record OperatorName {
  
  // public static IOperatorFactory<TOperator> Fixed<TOperator>(TOperator @operator) where TOperator : IOperator {
  //   return new FixedOperatorFactory<TOperator>(@operator);
  // }
  // public static IEncodingOperatorFactory<TOperator, TEncodingParameter> Fixed<TOperator, TEncodingParameter>(TOperator @operator) where TOperator : IOperator where TEncodingParameter : IEncodingParameter {
  //   return new FixedEncodingOperatorFactory<TOperator, TEncodingParameter>(@operator);
  // }
}

public abstract record Creator : OperatorName;
public abstract record Creator<TGenotype, TEncodingParameter> : Creator, 
  IExecutableEncodingOperatorFactory<ICreatorOperator<TGenotype>, TEncodingParameter> where TEncodingParameter : IEncodingParameter<TGenotype> {
  public abstract ICreatorOperator<TGenotype> Create(EncodingOperatorCreationContext<TEncodingParameter> context);
};

public record RandomPermutationCreator : Creator<Permutation, PermutationEncodingParameter> {
  public override RandomPermutationCreatorOperator Create(EncodingOperatorCreationContext<PermutationEncodingParameter> context) =>
    new RandomPermutationCreatorOperator(context.EncodingParameter, context.RandomSource.CreateRandomNumberGenerator());
}
public record UniformRealVectorCreator(double[]? Minimum = null, double[]? Maximum = null) : Creator<RealVector, RealVectorEncodingParameter> {
  public override UniformDistributedCreatorOperator Create(EncodingOperatorCreationContext<RealVectorEncodingParameter> context) => 
    new UniformDistributedCreatorOperator(Minimum, Maximum, context.EncodingParameter, context.RandomSource.CreateRandomNumberGenerator());
}
public record NormalRealVectorCreator(double[]? Mean = null, double[]? StandardDeviation = null) : Creator<RealVector, RealVectorEncodingParameter> {
  public override NormalDistributedCreatorOperator Create(EncodingOperatorCreationContext<RealVectorEncodingParameter> context) => 
    new NormalDistributedCreatorOperator(Mean ?? [0.0], StandardDeviation ?? [1.0], context.EncodingParameter, context.RandomSource.CreateRandomNumberGenerator());
}

public abstract record Crossover : OperatorName;
public abstract record Crossover<TGenotype, TEncodingParameter> : Crossover, 
  IExecutableEncodingOperatorFactory<ICrossoverOperator<TGenotype>, TEncodingParameter> where TEncodingParameter : IEncodingParameter {
  public abstract ICrossoverOperator<TGenotype> Create(EncodingOperatorCreationContext<TEncodingParameter> context);
}
public record OrderCrossover : Crossover<Permutation, PermutationEncodingParameter> {
  public override OrderCrossoverOperator Create(EncodingOperatorCreationContext<PermutationEncodingParameter> context) => 
    new OrderCrossoverOperator(context.RandomSource.CreateRandomNumberGenerator());
}
public record PartiallyMatchedCrossover : Crossover<Permutation, PermutationEncodingParameter> {
  public override PartiallyMatchedCrossoverOperator Create(EncodingOperatorCreationContext<PermutationEncodingParameter> context) => 
    new PartiallyMatchedCrossoverOperator(context.RandomSource.CreateRandomNumberGenerator());
}
public record SinglePointRealVectorCrossover : Crossover<RealVector, RealVectorEncodingParameter> {
  public override SinglePointCrossoverOperator Create(EncodingOperatorCreationContext<RealVectorEncodingParameter> context) => 
    new SinglePointCrossoverOperator(context.RandomSource.CreateRandomNumberGenerator());
}
public record AlphaBetaBlendRealVectorCrossover(double? Alpha = null, double? Beta = null) : Crossover<RealVector, RealVectorEncodingParameter> {
  public override AlphaBetaBlendCrossoverOperator Create(EncodingOperatorCreationContext<RealVectorEncodingParameter> context) => 
    new AlphaBetaBlendCrossoverOperator(Alpha, Beta);
}

public abstract record Mutator : OperatorName;
public abstract record Mutator<TGenotype, TEncodingParameter> : Mutator, 
  IExecutableEncodingOperatorFactory<IMutatorOperator<TGenotype>, TEncodingParameter> where TEncodingParameter : IEncodingParameter {
  public abstract IMutatorOperator<TGenotype> Create(EncodingOperatorCreationContext<TEncodingParameter> context);
}
public record SwapMutator : Mutator<Permutation, PermutationEncodingParameter> {
  public override SwapMutatorOperator Create(EncodingOperatorCreationContext<PermutationEncodingParameter> context) => 
    new SwapMutatorOperator(context.RandomSource.CreateRandomNumberGenerator());
}
public record GaussianRealVectorMutator(double? Rate = null, double? Strength = null) : Mutator<RealVector, RealVectorEncodingParameter> {
  public override GaussianMutatorOperator Create(EncodingOperatorCreationContext<RealVectorEncodingParameter> context) => 
    new GaussianMutatorOperator(Rate ?? 0.1, Strength ?? 0.1, context.EncodingParameter, context.RandomSource.CreateRandomNumberGenerator());
}
public record InversionMutator : Mutator<Permutation, PermutationEncodingParameter> {
  public override InversionMutatorOperator Create(EncodingOperatorCreationContext<PermutationEncodingParameter> context) => 
    new InversionMutatorOperator(context.RandomSource.CreateRandomNumberGenerator());
}

public abstract record Selector : OperatorName,
  IExecutableOperatorFactory<ISelectorOperator> {
  public abstract ISelectorOperator Create(OperatorCreationContext context);
}
public record RandomSelector : Selector {
  public override RandomSelectorOperator Create(OperatorCreationContext context) => 
    new RandomSelectorOperator(context.RandomSource.CreateRandomNumberGenerator());
}
public record TournamentSelector(int? TournamentSize = null) : Selector {
  public override TournamentSelectorOperator Create(OperatorCreationContext context) => 
    new TournamentSelectorOperator(TournamentSize ?? 2, context.RandomSource.CreateRandomNumberGenerator());
}
public record ProportionalSelector(bool Windowing = true) : Selector {
  public override ProportionalSelectorOperator Create(OperatorCreationContext context) => 
    new ProportionalSelectorOperator(context.RandomSource.CreateRandomNumberGenerator(), Windowing);
}

public abstract record Replacer : OperatorName,
  IExecutableOperatorFactory<IReplacerOperator> {
  public abstract IReplacerOperator Create(OperatorCreationContext context);
}
public record ElitistReplacer(int? Elites = null) : Replacer {
  public override ElitismReplacerOperator Create(OperatorCreationContext context) => 
    new ElitismReplacerOperator(Elites ?? 1);
}
public record PlusSelectionReplacer : Replacer {
  public override PlusSelectionReplacerOperator Create(OperatorCreationContext context) => 
    new PlusSelectionReplacerOperator();
}
