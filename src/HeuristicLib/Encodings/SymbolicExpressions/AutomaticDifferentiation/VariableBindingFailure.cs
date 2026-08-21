namespace HEAL.HeuristicLib.Encodings.SymbolicExpressions;

internal sealed record VariableBindingFailure(string VariableName, VariableBindingFailureReason Reason);

internal enum VariableBindingFailureReason
{
    Missing,
    IncompatibleType
}
