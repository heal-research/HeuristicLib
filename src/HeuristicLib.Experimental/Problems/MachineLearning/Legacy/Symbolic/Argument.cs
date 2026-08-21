using HEAL.HeuristicLib.Encodings.LegacySymbolicExpressions;

namespace HEAL.HeuristicLib.Problems.MachineLearning.Legacy;

/// <summary>
///   Placeholder symbol Function Invocation and Arguments are not yet implemented
/// </summary>
internal class Argument(int minimumArity, int maximumArity) : Symbol(minimumArity, minimumArity, maximumArity);
