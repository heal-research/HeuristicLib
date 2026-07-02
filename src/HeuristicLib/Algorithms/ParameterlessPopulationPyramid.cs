//using System.Diagnostics;
//using System.Runtime.InteropServices;
//using HEAL.HeuristicLib.Algorithms.Evolutionary;
//using HEAL.HeuristicLib.Algorithms.LocalSearch;
//using HEAL.HeuristicLib.Execution;
//using HEAL.HeuristicLib.Genotypes.Vectors;
//using HEAL.HeuristicLib.Optimization;
//using HEAL.HeuristicLib.Problems;
//using HEAL.HeuristicLib.Random;
//using HEAL.HeuristicLib.SearchSpaces;
//using HEAL.HeuristicLib.SearchSpaces.Vectors;
//using HEAL.HeuristicLib.States;

//namespace HEAL.HeuristicLib.Algorithms;

//public record ParameterlessPopulationPyramid<TP>
//  : IterativeAlgorithm<BoolVector, BoolVectorSearchSpace, TP, ParameterlessPopulationPyramidResultState, ParameterlessPopulationPyramid<TP>.State>
//  where TP : class, IProblem<BoolVector, BoolVectorSearchSpace>
//{

//  public class State : ExecutionState
//  {
//    public readonly HashSet<BoolVector> Seen = new(new EnumerableBoolEqualityComparer());
//    public readonly List<Population> Pyramid = [];

//    private void AddIfUnique(BoolVector solution, int level)
//    {
//      // Don't add things you have seen
//      if (Seen.Contains(solution))
//        return;
//      if (level == pyramid.Count) {
//        pyramid.Add(new Population(tracker.Length, random));
//      }

//      var copied = (BinaryVector)solution.Clone();
//      pyramid[level].Add(copied);
//      Seen.Add(copied);
//    }
//  }

//  protected override State CreateInitialExecutionState(IExecutionInstanceResolver resolver) => new () {
//    Evaluator = resolver.Resolve(Evaluator)
//  };

//  protected override ParameterlessPopulationPyramidResultState ExecuteStep(ParameterlessPopulationPyramidResultState? previousState, State executionState, TP problem, IRandomNumberGenerator random)
//  {
//    var solution = new BoolVector(random.NextBools(tracker.Length));
//    var fitness = tracker.Evaluate(solution, random);
//    fitness = HillClimber.ImproveToLocalOptimum(tracker, solution, fitness, random);
//    AddIfUnique(solution, 0);

//    for (var level = 0; level < pyramid.Count; level++) {
//      var current = pyramid[level];
//      var newFitness = LinkageCrossover.ImproveUsingTree(current.Tree, current.EvaluatedCandidates, solution, fitness, tracker, random);
//      // add it to the next level if its a strict fitness improvement
//      if (!tracker.IsBetter(newFitness, fitness)) {
//        continue;
//      }

//      fitness = newFitness;
//      AddIfUnique(solution, level + 1);
//    }
//  }
//}

//public class Population
//{
//  public List<BoolVector> Solutions { get; } = [];
//  public LinkageTree Tree { get; }
//  public Population(int length, IRandomNumberGenerator rand)
//  {
//    Solutions = [];
//    Tree = new LinkageTree(length, rand);
//  }
//  public void Add(BoolVector solution)
//  {
//    Solutions.Add(solution);
//    Tree.Add(solution);
//  }
//}

//public class LinkageTree
//{
//  private readonly int[][][] occurrences;
//  private readonly List<int>[] clusters;
//  private List<int> clusterOrdering;
//  private readonly int length;
//  private readonly IRandomNumberGenerator rand;
//  private bool rebuildRequired;

//  public LinkageTree(int length, IRandomNumberGenerator rand)
//  {
//    this.length = length;
//    this.rand = rand;
//    occurrences = new int[length][][];

//    // Create a lower triangular matrix without the diagonal
//    for (var i = 1; i < length; i++) {
//      occurrences[i] = new int[i][];
//      for (var j = 0; j < i; j++) {
//        occurrences[i][j] = new int[4];
//      }
//    }

//    clusters = new List<int>[2 * length - 1];
//    for (var i = 0; i < clusters.Length; i++) {
//      clusters[i] = [];
//    }

//    clusterOrdering = [];

//    // first "length" clusters just contain a single gene
//    for (var i = 0; i < length; i++) {
//      clusters[i].Add(i);
//    }
//  }

//  public void Add(BoolVector solution)
//  {
//    if (solution.Count != length)
//      throw new ArgumentException("The individual has not the correct length.");
//    for (var i = 1; i < solution.Count; i++) {
//      for (var j = 0; j < i; j++) {
//        // Updates the entry of the 4 long array based on the two bits

//        var pattern = (Convert.ToByte(solution[j]) << 1) + Convert.ToByte(solution[i]);
//        occurrences[i][j][pattern]++;
//      }
//    }

//    rebuildRequired = true;
//  }

//  // While "total" always has an integer value, it is a double to reduce
//  // how often type casts are needed to prevent integer divison
//  // In the GECCO paper, calculates Equation 2
//  private static double NegativeEntropy(int[] counts, double total)
//  {
//    double sum = 0;
//    for (var i = 0; i < counts.Length; i++) {
//      if (counts[i] != 0) {
//        sum += ((counts[i] / total) * Math.Log(counts[i] / total));
//      }
//    }

//    return sum;
//  }

//  // Uses the frequency table to calcuate the entropy distance between two indices.
//  // In the GECCO paper, calculates Equation 1
//  private double EntropyDistance(int i, int j)
//  {
//    var bits = new int[4];
//    // This ensures you are using the lower triangular part of "occurances"
//    if (i < j) {
//      (i, j) = (j, i);
//    }

//    var entry = occurrences[i][j];
//    // extracts the occurrences of the individual bits
//    bits[0] = entry[0] + entry[2]; // i zero
//    bits[1] = entry[1] + entry[3]; // i one
//    bits[2] = entry[0] + entry[1]; // j zero
//    bits[3] = entry[2] + entry[3]; // j one
//    double total = bits[0] + bits[1];
//    // entropy of the two bits on their own
//    var separate = NegativeEntropy(bits, total);
//    // entropy of the two bits as a single unit
//    var together = NegativeEntropy(entry, total);
//    // If together there is 0 entropy, the distance is zero
//    if (together.IsAlmost(0)) {
//      return 0.0;
//    }

//    return 2 - (separate / together);
//  }

//  // Performs O(N^2) clustering based on the method described in:
//  // "Optimal implementations of UPGMA and other common clustering algorithms"
//  // by I. Gronau and S. Moran
//  // In the GECCO paper, Figure 2 is a simplified version of this algorithm.
//  private void Rebuild()
//  {
//    double[][] distances = null;
//    if (distances == null) {
//      distances = new double[clusters.Length * 2 - 1][];
//      for (var i = 0; i < distances.Length; i++)
//        distances[i] = new double[clusters.Length * 2 - 1];
//    }

//    // Keep track of which clusters have not been merged
//    var topLevel = new List<int>(length);
//    for (var i = 0; i < length; i++)
//      topLevel.Add(i);

//    var useful = new bool[clusters.Length];
//    for (var i = 0; i < useful.Length; i++)
//      useful[i] = true;

//    // Store the distances between all clusters
//    for (var i = 1; i < length; i++) {
//      for (var j = 0; j < i; j++) {
//        distances[i][j] = EntropyDistance(clusters[i][0], clusters[j][0]);
//        // make it symmetric
//        distances[j][i] = distances[i][j];
//      }
//    }

//    // Each iteration we add some amount to the path, and remove the last
//    // two elements.  This keeps track of how much of usable is in the path.
//    var end_of_path = 0;

//    // build all clusters of size greater than 1
//    for (var index = length; index < clusters.Length; index++) {
//      // Shuffle everything not yet in the path
//      topLevel.ShuffleInPlace(rand, end_of_path, topLevel.Count - 1);

//      // if nothing in the path, just add a random usable node
//      if (end_of_path == 0) {
//        end_of_path = 1;
//      }

//      while (end_of_path < topLevel.Count) {
//        // last node in the path
//        var final = topLevel[end_of_path - 1];

//        // best_index stores the location of the best thing in the top level
//        var best_index = end_of_path;
//        var min_dist = distances[final][topLevel[best_index]];
//        // check all options which might be closer to "final" than "topLevel[best_index]"
//        for (var option = end_of_path + 1; option < topLevel.Count; option++) {
//          if (distances[final][topLevel[option]] < min_dist) {
//            min_dist = distances[final][topLevel[option]];
//            best_index = option;
//          }
//        }

//        // If the current last two in the path are minimally distant
//        if (end_of_path > 1 && min_dist >= distances[final][topLevel[end_of_path - 2]]) {
//          break;
//        }

//        // move the best to the end of the path
//        topLevel.Swap(end_of_path, best_index);
//        end_of_path++;
//      }

//      // Last two elements in the path are the clusters to join
//      var first = topLevel[end_of_path - 2];
//      var second = topLevel[end_of_path - 1];

//      // Only keep a cluster if the distance between the joining clusters is > zero
//      var keep = !distances[first][second].IsAlmost(0.0);
//      useful[first] = keep;
//      useful[second] = keep;

//      // create the new cluster
//      clusters[index] = clusters[first].Concat(clusters[second]).ToList();
//      // Calculate distances from all clusters to the newly created cluster
//      var i = 0;
//      var end = topLevel.Count - 1;
//      while (i <= end) {
//        var x = topLevel[i];
//        // Moves 'first' and 'second' to after "end" in topLevel
//        if (x == first || x == second) {
//          topLevel.Swap(i, end);
//          end--;
//          continue;
//        }

//        // Use the previous distances to calculate the joined distance
//        var first_distance = distances[first][x];
//        first_distance *= clusters[first].Count;
//        var second_distance = distances[second][x];
//        second_distance *= clusters[second].Count;
//        distances[x][index] = ((first_distance + second_distance)
//                               / (clusters[first].Count + clusters[second].Count));
//        // make it symmetric
//        distances[index][x] = distances[x][index];
//        i++;
//      }

//      // Remove first and second from the path
//      end_of_path -= 2;
//      topLevel.RemoveAt(topLevel.Count - 1);
//      topLevel[topLevel.Count - 1] = index;
//    }

//    // Extract the useful clusters
//    clusterOrdering.Clear();
//    // Add all useful clusters. The last one is never useful.
//    for (var i = 0; i < useful.Length - 1; i++) {
//      if (useful[i])
//        clusterOrdering.Add(i);
//    }

//    // Shuffle before sort to ensure ties are broken randomly
//    clusterOrdering.ShuffleInPlace(rand);
//    clusterOrdering = clusterOrdering.OrderBy(i => clusters[i].Count).ToList();
//  }

//  public IEnumerable<List<int>> Clusters
//  {
//    get {
//      // Just in time rebuilding
//      if (rebuildRequired)
//        Rebuild();
//      foreach (var index in clusterOrdering) {
//        // Send out the clusters in the desired order
//        yield return clusters[index];
//      }
//    }
//  }
//}
//}

//public class EnumerableBoolEqualityComparer : IEqualityComparer<IEnumerable<bool>>
//{
//  public bool Equals(IEnumerable<bool>? first, IEnumerable<bool>? second)
//  {
//    return first!.SequenceEqual(second!);
//  }

//  public int GetHashCode(IEnumerable<bool> obj)
//  {
//    unchecked {
//      return obj.Aggregate(17, (current, bit) => current * 29 + (bit ? 1231 : 1237));
//    }
//  }
//}

//public record ParameterlessPopulationPyramidResultState : PopulationState<BoolVector>
//{

//}


