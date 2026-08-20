namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions.AutomaticDifferentiation;

internal sealed record VariableBindingFailure(string VariableName, VariableBindingFailureReason Reason);

internal enum VariableBindingFailureReason
{
    Missing,
    IncompatibleType
}
