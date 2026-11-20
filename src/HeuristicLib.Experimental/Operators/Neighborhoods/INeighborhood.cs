namespace HEAL.HeuristicLib.Operators.Neighborhoods;
#region neighborhoods
/// <summary>
/// The three move operators that make up a neighborhood.
/// </summary>
/// <remarks>
/// What the neighborhood was written for is stated on
/// <see cref="Neighborhood{TCandidate,TSearchSpace,TProblem,TMove}"/>, which is where it is authored.
/// </remarks>
public interface INeighborhood<TCandidate, TMove>
{
    IOperator<MoveCreation<TMove>, TCandidate> MoveCreator { get; }
    IOperator<MoveApplication<TMove>, TCandidate> MoveApplier { get; }
    IOperator<MoveEvaluation<TMove>, TCandidate> MoveEvaluator { get; }
}
#endregion
