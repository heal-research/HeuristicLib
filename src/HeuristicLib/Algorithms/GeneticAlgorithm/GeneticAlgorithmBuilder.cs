// using HEAL.HeuristicLib.Operators;
// using HEAL.HeuristicLib.Optimization;
// using HEAL.HeuristicLib.Problems;
//
// namespace HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
//
// public class GeneticAlgorithmBuilder<TGenotype, TEncoding, TProblem>
//   : IAlgorithmBuilder<TGenotype, TEncoding, TProblem, GeneticAlgorithm<TGenotype, TEncoding, TProblem>>
//   where TEncoding : class, IEncoding<TGenotype>
//   where TProblem : class, IProblem<TGenotype, TEncoding>
// {
//   public GeneticAlgorithm<TGenotype, TEncoding, TProblem> Create() {
//     if (!IsValid()) {
//       throw new InvalidOperationException("Genetic Algorithm is not valid. Please check the configuration.");
//     }
//
//     return new GeneticAlgorithm<TGenotype, TEncoding, TProblem>(
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
//   private ICreator<TGenotype, TEncoding, TProblem, GeneticAlgorithm<TGenotype, TEncoding, TProblem>>? creator;
//   private ICrossover<TGenotype, TEncoding, TProblem, GeneticAlgorithm<TGenotype, TEncoding, TProblem>>? crossover;
//   private IMutator<TGenotype, TEncoding, TProblem, GeneticAlgorithm<TGenotype, TEncoding, TProblem>>? mutator;
//   private double? mutationRate;
//   private ISelector<TGenotype, TEncoding, TProblem, GeneticAlgorithm<TGenotype, TEncoding, TProblem>>? selector;
//   private IReplacer<TGenotype, TEncoding, TProblem, GeneticAlgorithm<TGenotype, TEncoding, TProblem>>? replacer;
//   private int? randomSeed;
//   private ITerminator<GeneticAlgorithmIterationResult<TGenotype>, TGenotype, TEncoding, TProblem, GeneticAlgorithm<TGenotype, TEncoding, TProblem>>? terminator;
//   private IInterceptor<GeneticAlgorithmIterationResult<TGenotype>, TGenotype, TEncoding, TProblem, GeneticAlgorithm<TGenotype, TEncoding, TProblem>>? interceptor;
//
//   public GeneticAlgorithmBuilder<TGenotype, TEncoding, TProblem> WithPopulationSize(int populationSize) {
//     this.populationSize = populationSize;
//     return this;
//   }
//   
//   public GeneticAlgorithmBuilder<TGenotype, TEncoding, TProblem> WithCreator(ICreator creator) {
//     this.creator = new CreatorAdapter<TGenotype, TEncoding, TProblem, GeneticAlgorithm<TGenotype, TEncoding, TProblem>>(creator);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TEncoding, TProblem> WithCreator(ICreator<TGenotype, TEncoding> creator) {
//     this.creator = new CreatorAdapter<TGenotype, TEncoding, TProblem, GeneticAlgorithm<TGenotype, TEncoding, TProblem>>(creator);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TEncoding, TProblem> WithCreator(ICreator<TGenotype, TEncoding, TProblem> creator) {
//     this.creator = new CreatorAdapter<TGenotype, TEncoding, TProblem, GeneticAlgorithm<TGenotype, TEncoding, TProblem>>(creator);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TEncoding, TProblem> WithCreator(ICreator<TGenotype, TEncoding, TProblem, GeneticAlgorithm<TGenotype, TEncoding, TProblem>> creator) {
//     this.creator = creator;
//     return this;
//   }
//   
//   public GeneticAlgorithmBuilder<TGenotype, TEncoding, TProblem> WithCrossover(ICrossover crossover) {
//     this.crossover = new CrossoverAdapter<TGenotype, TEncoding, TProblem, GeneticAlgorithm<TGenotype, TEncoding, TProblem>>(crossover);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TEncoding, TProblem> WithCrossover(ICrossover<TGenotype, TEncoding> crossover) {
//     this.crossover = new CrossoverAdapter<TGenotype, TEncoding, TProblem, GeneticAlgorithm<TGenotype, TEncoding, TProblem>>(crossover);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TEncoding, TProblem> WithCrossover(ICrossover<TGenotype, TEncoding, TProblem> crossover) {
//     this.crossover = new CrossoverAdapter<TGenotype, TEncoding, TProblem, GeneticAlgorithm<TGenotype, TEncoding, TProblem>>(crossover);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TEncoding, TProblem> WithCrossover(ICrossover<TGenotype, TEncoding, TProblem, GeneticAlgorithm<TGenotype, TEncoding, TProblem>> crossover) {
//     this.crossover = crossover;
//     return this;
//   }
//   
//   public GeneticAlgorithmBuilder<TGenotype, TEncoding, TProblem> WithMutator(IMutator mutator) {
//     this.mutator = new MutatorAdapter<TGenotype, TEncoding, TProblem, GeneticAlgorithm<TGenotype, TEncoding, TProblem>>(mutator);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TEncoding, TProblem> WithMutator(IMutator<TGenotype, TEncoding> mutator) {
//     this.mutator = new MutatorAdapter<TGenotype, TEncoding, TProblem, GeneticAlgorithm<TGenotype, TEncoding, TProblem>>(mutator);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TEncoding, TProblem> WithMutator(IMutator<TGenotype, TEncoding, TProblem> mutator) {
//     this.mutator = new MutatorAdapter<TGenotype, TEncoding, TProblem, GeneticAlgorithm<TGenotype, TEncoding, TProblem>>(mutator);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TEncoding, TProblem> WithMutator(IMutator<TGenotype, TEncoding, TProblem, GeneticAlgorithm<TGenotype, TEncoding, TProblem>> mutator) {
//     this.mutator = mutator;
//     return this;
//   }
//   
//   public GeneticAlgorithmBuilder<TGenotype, TEncoding, TProblem> WithMutationRate(double rate) {
//     mutationRate = rate;
//     return this;
//   }
//   
//   public GeneticAlgorithmBuilder<TGenotype, TEncoding, TProblem> WithSelector(ISelector selector) {
//     this.selector = new SelectorAdapter<TGenotype, TEncoding, TProblem, GeneticAlgorithm<TGenotype, TEncoding, TProblem>>(selector);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TEncoding, TProblem> WithSelector(ISelector<TGenotype, TEncoding> selector) {
//     this.selector = new SelectorAdapter<TGenotype, TEncoding, TProblem, GeneticAlgorithm<TGenotype, TEncoding, TProblem>>(selector);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TEncoding, TProblem> WithSelector(ISelector<TGenotype, TEncoding, TProblem> selector) {
//     this.selector = new SelectorAdapter<TGenotype, TEncoding, TProblem, GeneticAlgorithm<TGenotype, TEncoding, TProblem>>(selector);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TEncoding, TProblem> WithSelector(ISelector<TGenotype, TEncoding, TProblem, GeneticAlgorithm<TGenotype, TEncoding, TProblem>> selector) {
//     this.selector = selector;
//     return this;
//   }
//   
//   public GeneticAlgorithmBuilder<TGenotype, TEncoding, TProblem> WithReplacer(IReplacer replacer) {
//     this.replacer = new ReplacerAdapter<TGenotype, TEncoding, TProblem, GeneticAlgorithm<TGenotype, TEncoding, TProblem>>(replacer);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TEncoding, TProblem> WithReplacer(IReplacer<TGenotype, TEncoding> replacer) {
//     this.replacer = new ReplacerAdapter<TGenotype, TEncoding, TProblem, GeneticAlgorithm<TGenotype, TEncoding, TProblem>>(replacer);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TEncoding, TProblem> WithReplacer(IReplacer<TGenotype, TEncoding, TProblem> replacer) {
//     this.replacer = new ReplacerAdapter<TGenotype, TEncoding, TProblem, GeneticAlgorithm<TGenotype, TEncoding, TProblem>>(replacer);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TEncoding, TProblem> WithReplacer(IReplacer<TGenotype, TEncoding, TProblem, GeneticAlgorithm<TGenotype, TEncoding, TProblem>> replacer) {
//     this.replacer = replacer;
//     return this;
//   }
//   
//   public GeneticAlgorithmBuilder<TGenotype, TEncoding, TProblem> WithRandomSeed(int seed) {
//     randomSeed = seed;
//     return this;
//   }
//   
//   public GeneticAlgorithmBuilder<TGenotype, TEncoding, TProblem> WithTerminator(ITerminator terminator) {
//     this.terminator = new TerminatorAdapter<GeneticAlgorithmIterationResult<TGenotype>, TGenotype, TEncoding, TProblem, GeneticAlgorithm<TGenotype, TEncoding, TProblem>>(terminator);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TEncoding, TProblem> WithTerminator(ITerminator<GeneticAlgorithmIterationResult<TGenotype>> terminator) {
//     this.terminator = new TerminatorAdapter<GeneticAlgorithmIterationResult<TGenotype>, TGenotype, TEncoding, TProblem, GeneticAlgorithm<TGenotype, TEncoding, TProblem>>(terminator);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TEncoding, TProblem> WithTerminator(ITerminator<GeneticAlgorithmIterationResult<TGenotype>, TGenotype, TEncoding> terminator) {
//     this.terminator = new TerminatorAdapter<GeneticAlgorithmIterationResult<TGenotype>, TGenotype, TEncoding, TProblem, GeneticAlgorithm<TGenotype, TEncoding, TProblem>>(terminator);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TEncoding, TProblem> WithTerminator(ITerminator<GeneticAlgorithmIterationResult<TGenotype>, TGenotype, TEncoding, TProblem> terminator) {
//     this.terminator = new TerminatorAdapter<GeneticAlgorithmIterationResult<TGenotype>, TGenotype, TEncoding, TProblem, GeneticAlgorithm<TGenotype, TEncoding, TProblem>>(terminator);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TEncoding, TProblem> WithTerminator(ITerminator<GeneticAlgorithmIterationResult<TGenotype>, TGenotype, TEncoding, TProblem, GeneticAlgorithm<TGenotype, TEncoding, TProblem>> terminator) {
//     this.terminator = terminator;
//     return this;
//   }
//   
//   public GeneticAlgorithmBuilder<TGenotype, TEncoding, TProblem> WithInterceptor(IInterceptor interceptor) {
//     this.interceptor = new InterceptorAdapter<GeneticAlgorithmIterationResult<TGenotype>, TGenotype, TEncoding, TProblem, GeneticAlgorithm<TGenotype, TEncoding, TProblem>>(interceptor);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TEncoding, TProblem> WithInterceptor(IInterceptor<GeneticAlgorithmIterationResult<TGenotype>> interceptor) {
//     this.interceptor = new InterceptorAdapter<GeneticAlgorithmIterationResult<TGenotype>, TGenotype, TEncoding, TProblem, GeneticAlgorithm<TGenotype, TEncoding, TProblem>>(interceptor);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TEncoding, TProblem> WithInterceptor(IInterceptor<GeneticAlgorithmIterationResult<TGenotype>, TGenotype, TEncoding> interceptor) {
//     this.interceptor = new InterceptorAdapter<GeneticAlgorithmIterationResult<TGenotype>, TGenotype, TEncoding, TProblem, GeneticAlgorithm<TGenotype, TEncoding, TProblem>>(interceptor);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TEncoding, TProblem> WithInterceptor(IInterceptor<GeneticAlgorithmIterationResult<TGenotype>, TGenotype, TEncoding, TProblem> interceptor) {
//     this.interceptor = new InterceptorAdapter<GeneticAlgorithmIterationResult<TGenotype>, TGenotype, TEncoding, TProblem, GeneticAlgorithm<TGenotype, TEncoding, TProblem>>(interceptor);
//     return this;
//   }
//   public GeneticAlgorithmBuilder<TGenotype, TEncoding, TProblem> WithInterceptor(IInterceptor<GeneticAlgorithmIterationResult<TGenotype>, TGenotype, TEncoding, TProblem, GeneticAlgorithm<TGenotype, TEncoding, TProblem>> interceptor) {
//     this.interceptor = interceptor;
//     return this;
//   }
//   
//   
//   
// }
