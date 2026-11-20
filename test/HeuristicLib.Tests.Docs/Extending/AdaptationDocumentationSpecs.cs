using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Tests.Docs.Extending;

public class AdaptationDocumentationSpecs
{
    [Fact]
    public async Task StrategyAndWrappedMutatorUseOneAdaptationLink()
    {
        var problem = new TestFunctionProblem(new SphereFunction(2));
        var creator = new UniformDistributedCreator(problem.SearchSpace);
        var duration = new Duration();

        #region adaptation-wiring
        var adaptation = new ScaleAdaptation();
        var gaussian = new GaussianMutator(0.5, 0.1)
        {
            MutationStrengthAdaptation = adaptation
        };
        var strategy = new EvolutionStrategy<RealVector>
        {
            Creator = creator,
            Mutator = gaussian.Timed(duration),
            MutationStrengthAdaptation = adaptation
        };
        #endregion adaptation-wiring

        var run = (strategy with { MaximumGenerations = 3 }).CreateRun(problem, RandomNumberGenerator.Create(42));
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(0.1, gaussian.MutationStrength);
        Assert.True(run.Result(duration).CurrentDuration >= TimeSpan.Zero);
    }
}
