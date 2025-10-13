using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public class ProportionalSelector<TGenotype>(bool windowing = true) : BatchSelector<TGenotype> { // ToDo: Probability-based selection base class (fitness -> probability, rank -> probability, etc.)
  public bool Windowing { get; set; } = windowing;

  public override IReadOnlyList<Solution<TGenotype>> Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random) {
    var singleObjective = objective.Directions.Length == 1 ? objective.Directions[0] : throw new InvalidOperationException("Proportional selection requires a single objective.");
    var fitnesses = population.Select(s => s.ObjectiveVector.Count == 1 ? s.ObjectiveVector[0] : throw new InvalidOperationException("Proportional selection requires a single objective.")).ToList();

    // prepare qualities
    double minQuality = double.MaxValue, maxQuality = double.MinValue;
    foreach (var val in fitnesses) {
      minQuality = Math.Min(minQuality, val);
      maxQuality = Math.Max(maxQuality, val);
    }

    var qualities = fitnesses.AsEnumerable();
    if (Math.Abs(minQuality - maxQuality) < double.Epsilon) {
      qualities = qualities.Select(_ => 1.0);
    } else {
      if (Windowing) {
        if (singleObjective == ObjectiveDirection.Maximize) {
          qualities = qualities.Select(q => q - minQuality);
        } else {
          qualities = qualities.Select(q => maxQuality - q);
        }
      } else {
        if (minQuality < 0.0)
          throw new InvalidOperationException("Proportional selection without windowing does not work with quality values < 0.");
        if (singleObjective == ObjectiveDirection.Minimize) {
          var limit = Math.Min(maxQuality * 2, double.MaxValue);
          qualities = qualities.Select(q => limit - q);
        }
      }
    }

    var list = qualities.ToArray();
    var qualitySum = list.Sum();

    var selected = new Solution<TGenotype>[count];
    for (var i = 0; i < selected.Length; i++) {
      var selectedQuality = random.Random() * qualitySum;
      var index = 0;
      var currentQuality = list[index];
      while (currentQuality < selectedQuality) {
        index++;
        currentQuality += list[index];
      }

      selected[i] = population[index];
    }

    return selected;
  }
}
