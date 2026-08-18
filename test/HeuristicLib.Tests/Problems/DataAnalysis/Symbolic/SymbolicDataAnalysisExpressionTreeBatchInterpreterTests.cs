using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Problems.DataAnalysis;
using HEAL.HeuristicLib.Problems.DataAnalysis.Symbolic;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math;

namespace HEAL.HeuristicLib.Tests.Problems.DataAnalysis.Symbolic;

public class SymbolicDataAnalysisExpressionTreeBatchInterpreterTests
{
    // The batched interpreter evaluates whole batches of BatchSize rows and then a remainder. Datasets smaller than
    // one batch only ever reach the remainder path, which is why a defect in the batched path stayed hidden.
    [Theory]
    [InlineData(11)]
    [InlineData(BatchOperations.BatchSize)]
    [InlineData(BatchOperations.BatchSize + 1)]
    [InlineData((BatchOperations.BatchSize * 3) + 7)]
    public void BatchedAndScalarInterpretersAgreeAcrossBatchBoundaries(int rowCount)
    {
        var dataset = CreateDataset(rowCount);
        var rows = Enumerable.Range(0, rowCount).ToArray();
        var tree = CreateTree();

        var batched = new SymbolicDataAnalysisExpressionTreeBatchInterpreter()
            .GetSymbolicExpressionTreeValues(tree, dataset, rows).ToArray();
        var scalar = new SymbolicDataAnalysisExpressionTreeInterpreter()
            .GetSymbolicExpressionTreeValues(tree, dataset, rows).ToArray();

        batched.ShouldBe(scalar, tolerance: 1e-12);
    }

    // A constant is a single value rather than a dataset column, so evaluating one over rows beyond the batch size
    // used to read past the end of its buffer.
    [Fact]
    public void ABatchedConstantIsEvaluatedForEveryRow()
    {
        var rowCount = BatchOperations.BatchSize * 2;
        var dataset = CreateDataset(rowCount);
        var rows = Enumerable.Range(0, rowCount).ToArray();
        var tree = CreateTree(new SymbolicExpressionTreeNode(new Number().CreateTreeNode(3.5).Symbol));

        var values = new SymbolicDataAnalysisExpressionTreeBatchInterpreter()
            .GetSymbolicExpressionTreeValues(tree, dataset, rows).ToArray();

        values.Length.ShouldBe(rowCount);
    }

    private static ModifiableDataset CreateDataset(int rowCount)
    {
        var data = new double[rowCount, 2];
        for (var row = 0; row < rowCount; row++)
        {
            data[row, 0] = row * 0.25;
            data[row, 1] = row;
        }

        return new ModifiableDataset(["x", "y"], data);
    }

    // x * 2.5 + 1.5, so the result differs per row and depends on both a variable column and numeric values.
    private static SymbolicExpressionTree CreateTree(SymbolicExpressionTreeNode? root = null)
    {
        if (root is null)
        {
            var product = new SymbolicExpressionTreeNode(new Multiplication());
            product.AddSubtree(new Variable().CreateTreeNode("x", 1.0));
            product.AddSubtree(new Number().CreateTreeNode(2.5));

            var sum = new SymbolicExpressionTreeNode(new Addition());
            sum.AddSubtree(product);
            sum.AddSubtree(new Number().CreateTreeNode(1.5));
            root = sum;
        }

        var start = new SymbolicExpressionTreeNode(new StartSymbol());
        start.AddSubtree(root);
        var program = new SymbolicExpressionTreeNode(new ProgramRootSymbol());
        program.AddSubtree(start);
        return new SymbolicExpressionTree(program);
    }
}
