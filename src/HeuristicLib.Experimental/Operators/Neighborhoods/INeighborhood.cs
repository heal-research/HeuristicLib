using HEAL.HeuristicLib.Operators.MoveAppliers;
using HEAL.HeuristicLib.Operators.MoveCreators;
using HEAL.HeuristicLib.Operators.MoveEvaluators;

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
    IMoveCreator<TCandidate, TMove> MoveCreator { get; }
    IMoveApplier<TCandidate, TMove> MoveApplier { get; }
    IMoveEvaluator<TCandidate, TMove> MoveEvaluator { get; }
}
#endregion
