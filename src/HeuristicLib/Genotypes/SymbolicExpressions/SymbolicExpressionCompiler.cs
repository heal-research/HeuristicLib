namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public static class SymbolicExpressionCompiler
{
    public static CompiledSymbolicExpression Compile(SymbolicExpression expression, bool optimize = true)
    {
        var instructions = new List<ExpressionInstruction>(expression.Length);
        var numericLiterals = new List<NumericLiteral>();
        var variableReferences = new List<VariableReference>();
        var variableIndexByName = new Dictionary<string, int>(StringComparer.Ordinal);
        var compileStack = new SymbolCompilationContext.CompileNode[expression.Length];

        var context = new SymbolCompilationContext(expression, instructions, numericLiterals, variableReferences, variableIndexByName, compileStack, optimize);
        context.CompileSymbol(expression.RootLocation.InstructionIndex);

        return CreateWithCompactedPayloadTables(instructions, numericLiterals, variableReferences);
    }

    private static CompiledSymbolicExpression CreateWithCompactedPayloadTables(
        IReadOnlyList<ExpressionInstruction> sourceInstructions,
        IReadOnlyList<NumericLiteral> sourceNumericLiterals,
        IReadOnlyList<VariableReference> sourceVariableReferences)
    {
        var numericLiterals = new List<NumericLiteral>();
        var variableReferences = new List<VariableReference>();
        var variableIndexByName = new Dictionary<string, int>(StringComparer.Ordinal);
        var compactedInstructions = new ExpressionInstruction[sourceInstructions.Count];

        for (var i = 0; i < sourceInstructions.Count; i++)
        {
            var instruction = sourceInstructions[i];
            switch (SymbolicExpressionOpCodes.GetPayloadKind(instruction.OpCode))
            {
                case SymbolicExpressionPayloadKind.NumericLiteral:
                    var numericLiteral = sourceNumericLiterals[instruction.PayloadIndex];
                    var numericLiteralIndex = numericLiterals.IndexOf(numericLiteral);
                    if (numericLiteralIndex < 0)
                    {
                        numericLiteralIndex = numericLiterals.Count;
                        numericLiterals.Add(numericLiteral);
                    }

                    compactedInstructions[i] = instruction with { PayloadIndex = numericLiteralIndex };
                    break;
                case SymbolicExpressionPayloadKind.VariableReference:
                    var variableName = sourceVariableReferences[instruction.PayloadIndex].Name;
                    if (!variableIndexByName.TryGetValue(variableName, out var variableIndex))
                    {
                        variableIndex = variableReferences.Count;
                        variableIndexByName.Add(variableName, variableIndex);
                        variableReferences.Add(new VariableReference(variableName, variableIndex));
                    }

                    compactedInstructions[i] = instruction with { PayloadIndex = variableIndex };
                    break;
                default:
                    compactedInstructions[i] = instruction;
                    break;
            }
        }

        return CompiledSymbolicExpression.FromOwnedArrays(compactedInstructions, numericLiterals.ToArray(), variableReferences.ToArray());
    }
}
