using HEAL.HeuristicLib.DataAnalysis.Regression;
using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.SymbolicExpressions;

namespace HEAL.HeuristicLib.Operators.Refiners.SymbolicRegressionRefiners;

/// <summary>
/// Fits the numeric parameters of an expression to the problem's training data by nonlinear least squares, and returns
/// the expression rebuilt with the fitted values.
/// </summary>
/// <remarks>
/// <para>
/// The parameters are the occurrences of <see cref="EvolvableConstantSymbol"/> in the expression. They are constant
/// within one evaluation, which is why the genotype calls them constants, and free variables of the fitted model,
/// which is why the numerics call them parameters. PySR and HeuristicLab call this capability constant optimization.
/// </para>
/// <para>
/// The fit minimizes the mean squared error against the raw training targets, which need not be the problem's
/// configured objective. The fitted expression is therefore returned without being compared to the original. Wrap this
/// refiner in an <see cref="ImprovementCheckingRefiner{TCandidate, TSearchSpace, TProblem}"/> to keep the result only
/// where it improves the problem objective.
/// </para>
/// <para>
/// An expression the solver cannot fit is returned unchanged, as is one with no evolvable constants. An expression
/// using an operation that cannot be differentiated, or a variable the training data does not supply, throws instead:
/// both describe the configuration rather than the candidate, so every affected candidate would fail the same way.
/// </para>
/// <para>
/// Linear scaling stays an evaluation concern. The fit runs against raw targets even where the problem applies scaling
/// when it evaluates.
/// </para>
/// </remarks>
public sealed record NumericParameterFittingRefiner
    : SingleCandidateRefiner<ExpressionTree, ExpressionTreeSearchSpace, SymbolicRegressionProblem>
{
    /// <summary>
    /// The maximum number of Levenberg-Marquardt iterations spent on one candidate. Zero returns every candidate
    /// unchanged.
    /// </summary>
    public int MaximumIterations { get; init; } = 5;

    /// <summary>
    /// The data the parameters are fitted to. A <see langword="null"/> value fits to the problem's training data.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Set this to fit on a subset of the rows, which trades fitting accuracy for solver time. The subset is fixed for
    /// the lifetime of the configuration, so every candidate is fitted to the same rows. Drawing a fresh sample per
    /// candidate is deliberately not offered here: it would recompute a binding per candidate for a benefit that a
    /// fixed representative sample already provides.
    /// </para>
    /// <para>
    /// The problem still evaluates on its own training data, so fitting to a subset makes the refiner's objective
    /// differ from the problem's more sharply than it already does. Compose an
    /// <see cref="ImprovementCheckingRefiner{TCandidate, TSearchSpace, TProblem}"/> around it to reject a fit that
    /// does not carry over.
    /// </para>
    /// <para>
    /// Being a dataset, this setting takes part in configuration equality by reference rather than by value.
    /// </para>
    /// </remarks>
    public RegressionData? FittingData { get; init; }

    public override ExpressionTree RefineCandidate(ExpressionTree candidate, IRandomNumberGenerator random, ExpressionTreeSearchSpace searchSpace, SymbolicRegressionProblem problem)
    {
        if (NumericParameterFitter.TryFit(candidate, FittingData ?? problem.TrainingData, MaximumIterations, out var fittedExpression, out var failure))
        {
            return fittedExpression;
        }

        // A solve that does not converge is an ordinary outcome for one candidate among many, so that candidate is
        // simply left as it was. The other failures are not per-candidate: an undifferentiable operation or an unbound
        // variable follows from the search space and the training data, so every affected candidate fails identically
        // and skipping them silently would hide the misconfiguration for a whole run.
        return failure is NumericParameterFittingFailure.NumericalOptimization
            ? candidate
            : throw NumericParameterFitter.CreateException(failure, nameof(problem));
    }
}
