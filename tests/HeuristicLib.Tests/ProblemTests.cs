using HEAL.HeuristicLib.ProofOfConcept;

public class ProblemTests {
  [Fact]
  public Task TestMultipleEncodingsFromSingleProblem() {
    double[,] distances = new double[,] {
      { 0, 1, 2 },
      { 1, 0, 3 },
      { 2, 3, 0 }
    };
    var tsp = new TravelingSalesmanProblem(distances);

    var permutationEncoding = tsp.CreatePermutationEncoding();
    
    var fakeEncoding = tsp.CreateFakeEncoding();
    
    return Verify([permutationEncoding, fakeEncoding]);
  }
}
