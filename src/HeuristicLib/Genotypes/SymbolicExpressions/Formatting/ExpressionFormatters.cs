namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public static class ExpressionFormatters
{
    public static InfixExpressionFormatter Infix { get; } = new();
    public static IExpressionFormatter CSharp { get; } = new CSharpExpressionFormatter();
    public static IExpressionFormatter Python { get; } = new PythonExpressionFormatter();
    public static IExpressionFormatter Latex { get; } = new LatexExpressionFormatter();
}
