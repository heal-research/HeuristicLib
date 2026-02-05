// using HEAL.HeuristicLib.Execution;
// using HEAL.HeuristicLib.Problems;
// using HEAL.HeuristicLib.Random;
// using HEAL.HeuristicLib.SearchSpaces;
//
// namespace HEAL.HeuristicLib.Operators.StateTransformers;
//
// public interface IStateTransformer<TState, TGenotype, in TSearchSpace, in TProblem>
//   : IExecutable<IStateTransformerInstance<TState, TGenotype, TSearchSpace, TProblem>>
//   where TGenotype : class
//   where TSearchSpace : class, ISearchSpace<TGenotype>
//   where TProblem : class, IProblem<TGenotype, TSearchSpace>
// {
//   
// }
//
// public interface IStateTransformerInstance<TState, TGenotype, in TSearchSpace, in TProblem>
//   : IExecutionInstance
//   where TGenotype : class
//   where TSearchSpace : class, ISearchSpace<TGenotype>
//   where TProblem : class, IProblem<TGenotype, TSearchSpace>
// {
//   TState Transform(TState? state, IRandomNumberGenerator randomNumberGenerator, TSearchSpace searchSpace, TProblem problem);
// }
