using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminator;

public interface ITerminator<TGenotype, in TIterationResult, in TEncoding> : ITerminator<TGenotype, TIterationResult, TEncoding, IProblem<TGenotype, TEncoding>>
  where TIterationResult : IIterationResult
  where TEncoding : class, IEncoding<TGenotype>;

public interface ITerminator<TGenotype, in TIterationResult> : ITerminator<TGenotype, TIterationResult, IEncoding<TGenotype>>
  where TIterationResult : IIterationResult;

public interface ITerminator<TGenotype> : ITerminator<TGenotype, IIterationResult>;
