using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Problems.Partial;

public class PermutationState : StackedState<int>
{
  private readonly IPartialSolutionProblem<Permutation, PermutationSearchSpace, int> problem;
  private readonly RandomNumberGenerator rng;
  private Permutation solution = [];
  private readonly SortedSet<int> unused;

  public PermutationState(IPartialSolutionProblem<Permutation, PermutationSearchSpace, int> problem, RandomNumberGenerator rng)
  {
    this.problem = problem;
    this.rng = rng;
    unused = new SortedSet<int>(Enumerable.Range(0, problem.SearchSpace.Length));
  }

  private PermutationState(PermutationState original) : base(original)
  {
    problem = original.problem;
    rng = original.rng;
    solution = original.solution;
    unused = new SortedSet<int>(original.unused);
  }

  public override object Clone() => new PermutationState(this);

  protected override ObjectiveVectorQuality CalculateBound() => new(problem.Bound(solution, rng), problem.Objective);

  protected override ObjectiveVectorQuality? CalculateQuality()
  {
    var q = problem.EvaluatePartial(solution, rng);
    return q == null ? null : new ObjectiveVectorQuality(q, problem.Objective);
  }

  public override IEnumerable<int> GetChoices() => unused;

  protected override void ApplyChoice(int choice)
  {
    solution = new Permutation(solution.Append(choice));
    unused.Remove(choice);
  }

  protected override void UndoLastChoice(int choice)
  {
    solution = new Permutation(solution.Take(solution.Count - 1));
    unused.Add(choice);
  }
}
