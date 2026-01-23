using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Crossovers.PermutationCrossovers;

public class EdgeRecombinationCrossover : SingleSolutionCrossover<Permutation>
{
  public override Permutation Cross(IParents<Permutation> parents, IRandomNumberGenerator random)
  {
    if (parents.Parent1.Count != parents.Parent2.Count) {
      throw new ArgumentException("EdgeRecombinationCrossover: The parent permutations are of unequal length.");
    }

    var length = parents.Parent1.Count;
    var result = new int[length];
    var edgeList = new int[length, 4];
    var remainingNumbers = new bool[length];
    int index;

    for (var i = 0; i < length; i++) {
      // generate edge list for every number
      remainingNumbers[i] = true;

      index = 0;
      while (index < length && parents.Parent1[index] != i) {
        // search edges in parent1
        index++;
      }

      if (index == length) {
        throw new InvalidOperationException("Permutation doesn't contain number " + i + ".");
      }

      edgeList[i, 0] = parents.Parent1[(index - 1 + length) % length];
      edgeList[i, 1] = parents.Parent1[(index + 1) % length];
      index = 0;
      while (index < length && parents.Parent2[index] != i) {
        // search edges in parent2
        index++;
      }

      if (index == length) {
        throw new InvalidOperationException("Permutation doesn't contain number " + i + ".");
      }

      var currentEdge = parents.Parent2[(index - 1 + length) % length];
      edgeList[i, 2] = edgeList[i, 0] != currentEdge && edgeList[i, 1] != currentEdge ? currentEdge : -1; // new edge found ?

      currentEdge = parents.Parent2[(index + 1) % length];
      edgeList[i, 3] = edgeList[i, 0] != currentEdge && edgeList[i, 1] != currentEdge ? currentEdge : -1; // new edge found ?
    }

    var currentNumber = random.NextInt(length); // get number to start
    for (var i = 0; i < length; i++) {
      result[i] = currentNumber;
      remainingNumbers[currentNumber] = false;

      for (var j = 0; j < 4; j++) {
        // remove all edges to / from currentNumber
        if (edgeList[currentNumber, j] == -1) {
          continue;
        }

        for (var k = 0; k < 4; k++) {
          if (edgeList[edgeList[currentNumber, j], k] == currentNumber) {
            edgeList[edgeList[currentNumber, j], k] = -1;
          }
        }
      }

      var minEdgeCount = 5;
      var nextNumber = -1;
      for (var j = 0; j < 4; j++) {
        // find next number with least edges
        if (edgeList[currentNumber, j] == -1) {
          continue;
        }

        // next number found
        var currentEdgeCount = 0;
        for (var k = 0; k < 4; k++) {
          // count edges of next number
          if (edgeList[edgeList[currentNumber, j], k] != -1) {
            currentEdgeCount++;
          }
        }

        if (currentEdgeCount >= minEdgeCount &&
            (currentEdgeCount != minEdgeCount || random.NextBool())) {
          continue;
        }

        nextNumber = edgeList[currentNumber, j];
        minEdgeCount = currentEdgeCount;
      }

      currentNumber = nextNumber;
      if (currentNumber != -1) {
        continue;
      }

      // current number has no more edge
      index = 0;
      while (index < length && !remainingNumbers[index]) {
        // choose next remaining number
        index++;
      }

      if (index < length) {
        currentNumber = index;
      }
    }

    return new Permutation(result);
  }
}
