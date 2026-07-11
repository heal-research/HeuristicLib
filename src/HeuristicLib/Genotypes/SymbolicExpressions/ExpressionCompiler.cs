namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public static class ExpressionCompiler
{
    public static CompiledExpressionTree Compile(ExpressionTree expression, bool optimize = true)
    {
        return new ExpressionCompilation(expression, optimize).Compile();
    }
}
