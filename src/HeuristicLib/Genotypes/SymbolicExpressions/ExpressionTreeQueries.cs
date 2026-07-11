namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public static class ExpressionTreeQueries
{
    extension(ExpressionTree expression)
    {
        public IEnumerable<ExpressionSubtree> FindNodesOfSymbol<TSymbol>()
            where TSymbol : Symbol
        {
            return expression.TraversePostOrder().Where(node => node.Symbol is TSymbol);
        }

        public IEnumerable<ExpressionLocation> FindLocallyPerturbableLocations()
        {
            return expression.TraversePostOrder()
                .Where(node => node.Symbol.SupportsLocalPerturbation && node.Symbol.CanPerturb(node.Node))
                .Select(node => node.Location);
        }
    }
}
