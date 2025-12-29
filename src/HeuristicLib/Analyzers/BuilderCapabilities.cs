using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Analyzers;

public interface IHasMutator<TG, TS, TP, TR, out TA> : IAlgorithmBuilder<TG, TS, TP, TR, TA> 
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : IAlgorithmState
  where TA : IAlgorithm<TG, TS, TP, TR>
  where TG : class {
  IMutator<TG, TS, TP> Mutator { get; set; }
} 

public interface IHasCrossover<TG, TS, TP, TR, out TA> : IAlgorithmBuilder<TG, TS, TP, TR, TA> 
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : IAlgorithmState
  where TA : IAlgorithm<TG, TS, TP, TR>
  where TG : class {
  ICrossover<TG, TS, TP> CrossoverOperator { get; set; }
}

public interface IHasTerminator<TG, TS, TP, TR, out TA> : IAlgorithmBuilder<TG, TS, TP, TR, TA> 
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : AlgorithmState
  where TA : IAlgorithm<TG, TS, TP, TR>
  where TG : class {
  ITerminator<TG, TR, TS, TP> Terminator { get; set; }
}

public interface IHasEvaluator<TG, TS, TP, TR, out TA> : IAlgorithmBuilder<TG, TS, TP, TR, TA>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : IAlgorithmState
  where TA : IAlgorithm<TG, TS, TP, TR>
  where TG : class {
  IEvaluator<TG, TS, TP> Evaluator { get; set; }
}
