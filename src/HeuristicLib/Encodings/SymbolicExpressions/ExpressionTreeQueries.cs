namespace HEAL.HeuristicLib.Encodings.SymbolicExpressions;

public static class ExpressionTreeQueries
{
    extension(ExpressionTree expression)
    {
        public IEnumerable<ExpressionNode> FindNodesOfSymbol<TSymbol>()
            where TSymbol : Symbol
        {
            return expression.TraversePreOrder().Where(node => node.Symbol is TSymbol);
        }

        public IEnumerable<ExpressionPoint> FindLocallyPerturbablePoints()
        {
            return expression.RootPoint.TraversePreOrder()
                .Where(point => point.Node.Symbol.SupportsLocalPerturbation && point.Node.Symbol.CanPerturb(point.Node));
        }
    }
}
