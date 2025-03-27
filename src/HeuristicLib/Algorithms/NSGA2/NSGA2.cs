// using HEAL.HeuristicLib.Core;
// using HEAL.HeuristicLib.Operators;
// using HEAL.HeuristicLib.Random;
//
// namespace HEAL.HeuristicLib.Algorithms.NSGA2;
//
// public class NSGA2<TGenotype> : AlgorithmBase<PopulationState<TGenotype>> {
//   
//   public int PopulationSize { get; }
//   public ICreator<,> Creator { get; }
//   public ICrossover<,> Crossover { get; }
//   public IMutator<,> Mutator { get; }
//   public double MutationRate { get; }
//   public IEvaluator<TGenotype> Evaluator { get; }
//   public Objective Goal { get; }
//   public ISelector<> Selector { get; }
//   public IReplacer Replacer { get; }
//   public IRandomSource RandomSource { get; }
//   public ITerminator<PopulationState<TGenotype>>? Terminator { get; }
//   public IInterceptor<PopulationState<TGenotype>> Interceptor { get; }
//   
//   public NSGA2(int populationSize, ICreator<,> creator, ICrossover<,> crossover, IMutator<,> mutator, double mutationRate,
//     IEvaluator<TGenotype> evaluator, Objective goal, ISelector<> selector, IReplacer replacer, IRandomSource randomSourceState,
//     ITerminator<PopulationState<TGenotype>>? terminator = null, IInterceptor<PopulationState<TGenotype>>? interceptor = null)
//   {
//     PopulationSize = populationSize;
//     Creator = creator;
//     Crossover = crossover;
//     Mutator = mutator;
//     MutationRate = mutationRate;
//     Evaluator = evaluator;
//     Goal = goal;
//     Selector = selector;
//     Replacer = replacer;
//     RandomSource = randomSourceState;
//     Terminator = terminator;
//     Interceptor = interceptor ?? new IdentityInterceptor<PopulationState<TGenotype>>();
//   }
//   public override ResultStream<PopulationState<TGenotype>> CreateExecutionStream(PopulationState<TGenotype>? initialState = null) {
//     return new ResultStream<PopulationState<TGenotype>>(InternalCreateExecutionStream(initialState));
//   }
//
//   private IEnumerable<PopulationState<TGenotype>> InternalCreateExecutionStream(PopulationState<TGenotype>? initialState) {
//     var random = RandomSource.CreateRandomNumberGenerator();
//     
//     PopulationState<TGenotype> currentState;
//     if (initialState is not null) {
//       currentState = initialState;
//     } else {
//       var initialPopulation = InitializePopulation();
//       var evaluatedPopulation = Evaluator.Evaluate(initialPopulation);
//       currentState = new PopulationState<TGenotype> { Population = evaluatedPopulation, Objective = Goal };
//       currentState = Interceptor.Transform(currentState);
//       yield return currentState;
//     }
//
//     int offspringCount = Replacer.GetOffspringCount(PopulationSize);
//     while (Terminator?.ShouldContinue(currentState) ?? true) {
//       var offsprings = EvolvePopulation(currentState.Population, offspringCount, random);
//       var evaluatedOffsprings = Evaluator.Evaluate(offsprings);
//
//       var newPopulation = Replacer.Replace(currentState.Population, evaluatedOffsprings, Goal);
//
//       currentState = currentState.Next() with { Population = newPopulation }; // increment durations and other counts
//       currentState = Interceptor.Transform(currentState);
//
//       yield return currentState;
//     }
//   }
//   
//   private TGenotype[] InitializePopulation() {
//     var population = new TGenotype[PopulationSize];
//     for (int i = 0; i < PopulationSize; i++) {
//       population[i] = Creator.Create();
//     }
//     return population;
//   }
//
//   private TGenotype[] EvolvePopulation(Solution<TGenotype>[] population, int offspringCount, IRandomNumberGenerator random) {
//     var newPopulation = new TGenotype[offspringCount];
//     var parents = Selector.Select(population, Goal, offspringCount * 2).Select(p => p.Phenotype).ToList();
//   
//     for (int i = 0; i < parents.Count; i += 2) {
//       var parent1 = parents[i];
//       var parent2 = parents[i + 1];
//       var offspring = Crossover.Cross(parent1, parent2);
//       if (random.Random() < MutationRate) {
//         offspring = Mutator.Mutate(offspring);
//       }
//       newPopulation[i / 2] = offspring;
//     }
//     return newPopulation;
//   }
// }
