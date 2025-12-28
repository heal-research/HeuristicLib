// using HEAL.HeuristicLib.Operators;
// using HEAL.HeuristicLib.Optimization;
// using HEAL.HeuristicLib.Problems;
//
// namespace HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
//
// public class GeneticAlgorithmBuilder<TGenotype, TSearchSpace, TProblem>
//   : IAlgorithmBuilder<TGenotype, TSearchSpace, TProblem, GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>>
//   where TSearchSpace : class, IEncoding<TGenotype>
//   where TProblem : class, IProblem<TGenotype, TSearchSpace>
// {
//   public GeneticAlgorithm<TGenotype, TSearchSpace, TProblem> GetEvaluator() {
//     if (!IsValid()) {
//       throw new InvalidOperationException("Genetic Algorithm is not valid. Please check the configuration.");
//     }
//
//     return new GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>(
//       populationSize!.Value,
//       creator!,
//       crossover!,
//       mutator!,
//       mutationRate!.Value,
//       selector!,
//       replacer!,
//       randomSeed!.Value,
//       terminator!,
//       interceptor!
//     );
//   }
//
//   public bool IsValid() {
//     return 
//       populationSize is not null &&
//       creator is not null &&
//       crossover is not null &&
//       mutator is not null &&
//       mutationRate >= 0 &&
//       selector is not null &&
//       replacer is not null &&
//       randomSeed >= 0 &&
//       terminator is not null;
//   }
//   
//   private int? populationSize;
//   private ICreator<TGenotype, TSearchSpace, TProblem, GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>>? creator;
//   private ICrossover<TGenotype, TSearchSpace, TProblem, GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>>? crossover;
//   private IMutator<TGenotype, TSearchSpace, TProblem, GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>>? mutator;
//   private double? mutationRate;
//   private ISelector<TGenotype, TSearchSpace, TProblem, GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>>? selector;
//   private IReplacer<TGenotype, TSearchSpace, TProblem, GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>>? replacer;
//   private int? randomSeed;
//   private ITerminator<PopulationIterationResult<TGenotype>, TGenotype, TSearchSpace, TProblem, GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>>? terminator;
//   private IInterceptor<PopulationIterationResult<TGenotype>, TGenotype, TSearchSpace, TProblem, GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>>? interceptor;
//
//   public GeneticAlgorithmBuilder<TGenotype, TSearchSpace, TProblem> WithPopulationSize(int populationSize) {
//     this.populationSize = populationSize;
//     return this;
//   }
//   
//   public GeneticAlgorithmBuilder<TGenotype, TSearchSpace, TProblem> WithCreator(ICreator creator) {
//     this.creator = new CreatorAdapter<TGenotype, TSearchSpace, TProblem, GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>>(creator);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TSearchSpace, TProblem> WithCreator(ICreator<TGenotype, TSearchSpace> creator) {
//     this.creator = new CreatorAdapter<TGenotype, TSearchSpace, TProblem, GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>>(creator);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TSearchSpace, TProblem> WithCreator(ICreator<TGenotype, TSearchSpace, TProblem> creator) {
//     this.creator = new CreatorAdapter<TGenotype, TSearchSpace, TProblem, GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>>(creator);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TSearchSpace, TProblem> WithCreator(ICreator<TGenotype, TSearchSpace, TProblem, GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>> creator) {
//     this.creator = creator;
//     return this;
//   }
//   
//   public GeneticAlgorithmBuilder<TGenotype, TSearchSpace, TProblem> WithCrossover(ICrossover crossover) {
//     this.crossover = new CrossoverAdapter<TGenotype, TSearchSpace, TProblem, GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>>(crossover);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TSearchSpace, TProblem> WithCrossover(ICrossover<TGenotype, TSearchSpace> crossover) {
//     this.crossover = new CrossoverAdapter<TGenotype, TSearchSpace, TProblem, GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>>(crossover);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TSearchSpace, TProblem> WithCrossover(ICrossover<TGenotype, TSearchSpace, TProblem> crossover) {
//     this.crossover = new CrossoverAdapter<TGenotype, TSearchSpace, TProblem, GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>>(crossover);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TSearchSpace, TProblem> WithCrossover(ICrossover<TGenotype, TSearchSpace, TProblem, GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>> crossover) {
//     this.crossover = crossover;
//     return this;
//   }
//   
//   public GeneticAlgorithmBuilder<TGenotype, TSearchSpace, TProblem> WithMutator(IMutator mutator) {
//     this.mutator = new MutatorAdapter<TGenotype, TSearchSpace, TProblem, GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>>(mutator);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TSearchSpace, TProblem> WithMutator(IMutator<TGenotype, TSearchSpace> mutator) {
//     this.mutator = new MutatorAdapter<TGenotype, TSearchSpace, TProblem, GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>>(mutator);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TSearchSpace, TProblem> WithMutator(IMutator<TGenotype, TSearchSpace, TProblem> mutator) {
//     this.mutator = new MutatorAdapter<TGenotype, TSearchSpace, TProblem, GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>>(mutator);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TSearchSpace, TProblem> WithMutator(IMutator<TGenotype, TSearchSpace, TProblem, GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>> mutator) {
//     this.mutator = mutator;
//     return this;
//   }
//   
//   public GeneticAlgorithmBuilder<TGenotype, TSearchSpace, TProblem> WithMutationRate(double rate) {
//     mutationRate = rate;
//     return this;
//   }
//   
//   public GeneticAlgorithmBuilder<TGenotype, TSearchSpace, TProblem> WithSelector(ISelector selector) {
//     this.selector = new SelectorAdapter<TGenotype, TSearchSpace, TProblem, GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>>(selector);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TSearchSpace, TProblem> WithSelector(ISelector<TGenotype, TSearchSpace> selector) {
//     this.selector = new SelectorAdapter<TGenotype, TSearchSpace, TProblem, GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>>(selector);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TSearchSpace, TProblem> WithSelector(ISelector<TGenotype, TSearchSpace, TProblem> selector) {
//     this.selector = new SelectorAdapter<TGenotype, TSearchSpace, TProblem, GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>>(selector);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TSearchSpace, TProblem> WithSelector(ISelector<TGenotype, TSearchSpace, TProblem, GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>> selector) {
//     this.selector = selector;
//     return this;
//   }
//   
//   public GeneticAlgorithmBuilder<TGenotype, TSearchSpace, TProblem> WithReplacer(IReplacer replacer) {
//     this.replacer = new ReplacerAdapter<TGenotype, TSearchSpace, TProblem, GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>>(replacer);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TSearchSpace, TProblem> WithReplacer(IReplacer<TGenotype, TSearchSpace> replacer) {
//     this.replacer = new ReplacerAdapter<TGenotype, TSearchSpace, TProblem, GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>>(replacer);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TSearchSpace, TProblem> WithReplacer(IReplacer<TGenotype, TSearchSpace, TProblem> replacer) {
//     this.replacer = new ReplacerAdapter<TGenotype, TSearchSpace, TProblem, GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>>(replacer);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TSearchSpace, TProblem> WithReplacer(IReplacer<TGenotype, TSearchSpace, TProblem, GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>> replacer) {
//     this.replacer = replacer;
//     return this;
//   }
//   
//   public GeneticAlgorithmBuilder<TGenotype, TSearchSpace, TProblem> WithRandomSeed(int seed) {
//     randomSeed = seed;
//     return this;
//   }
//   
//   public GeneticAlgorithmBuilder<TGenotype, TSearchSpace, TProblem> WithTerminator(ITerminator terminator) {
//     this.terminator = new TerminatorAdapter<PopulationIterationResult<TGenotype>, TGenotype, TSearchSpace, TProblem, GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>>(terminator);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TSearchSpace, TProblem> WithTerminator(ITerminator<PopulationIterationResult<TGenotype>> terminator) {
//     this.terminator = new TerminatorAdapter<PopulationIterationResult<TGenotype>, TGenotype, TSearchSpace, TProblem, GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>>(terminator);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TSearchSpace, TProblem> WithTerminator(ITerminator<PopulationIterationResult<TGenotype>, TGenotype, TSearchSpace> terminator) {
//     this.terminator = new TerminatorAdapter<PopulationIterationResult<TGenotype>, TGenotype, TSearchSpace, TProblem, GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>>(terminator);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TSearchSpace, TProblem> WithTerminator(ITerminator<PopulationIterationResult<TGenotype>, TGenotype, TSearchSpace, TProblem> terminator) {
//     this.terminator = new TerminatorAdapter<PopulationIterationResult<TGenotype>, TGenotype, TSearchSpace, TProblem, GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>>(terminator);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TSearchSpace, TProblem> WithTerminator(ITerminator<PopulationIterationResult<TGenotype>, TGenotype, TSearchSpace, TProblem, GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>> terminator) {
//     this.terminator = terminator;
//     return this;
//   }
//   
//   public GeneticAlgorithmBuilder<TGenotype, TSearchSpace, TProblem> WithInterceptor(IInterceptor interceptor) {
//     this.interceptor = new InterceptorAdapter<PopulationIterationResult<TGenotype>, TGenotype, TSearchSpace, TProblem, GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>>(interceptor);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TSearchSpace, TProblem> WithInterceptor(IInterceptor<PopulationIterationResult<TGenotype>> interceptor) {
//     this.interceptor = new InterceptorAdapter<PopulationIterationResult<TGenotype>, TGenotype, TSearchSpace, TProblem, GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>>(interceptor);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TSearchSpace, TProblem> WithInterceptor(IInterceptor<PopulationIterationResult<TGenotype>, TGenotype, TSearchSpace> interceptor) {
//     this.interceptor = new InterceptorAdapter<PopulationIterationResult<TGenotype>, TGenotype, TSearchSpace, TProblem, GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>>(interceptor);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TSearchSpace, TProblem> WithInterceptor(IInterceptor<PopulationIterationResult<TGenotype>, TGenotype, TSearchSpace, TProblem> interceptor) {
//     this.interceptor = new InterceptorAdapter<PopulationIterationResult<TGenotype>, TGenotype, TSearchSpace, TProblem, GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>>(interceptor);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TSearchSpace, TProblem> WithInterceptor(IInterceptor<PopulationIterationResult<TGenotype>, TGenotype, TSearchSpace, TProblem, GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>> interceptor) {
//     this.interceptor = interceptor;
//     return this;
//   }
//   
//   
//   
// }
