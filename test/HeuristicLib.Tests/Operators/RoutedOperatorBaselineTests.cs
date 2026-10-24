using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Refiners;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.Operators;

public sealed class RoutedOperatorBaselineTests
{
    // Captured against 7a8ab072 before the routing migration: results, next RNG draw and child-batch order.
    [Theory]
    [InlineData("creation7", "487,1161,744,564,344,1046,523,248,460,280,606,1173;next=704064227;calls=100:0,1,2/200:0,1,2,3,4,5,6,7,8")]
    [InlineData("crossover7", "487,1162,745,566,346,1049,526,251,463,283,609,1176;next=704064227;calls=100:0,2,4/200:1,3,5,6,7,8,9,10,11")]
    [InlineData("mutation7", "487,1162,745,566,346,1049,526,251,463,283,609,1176;next=704064227;calls=100:0,2,4/200:1,3,5,6,7,8,9,10,11")]
    [InlineData("refinement7", "487,1162,745,566,346,1049,526,251,463,283,609,1176;next=704064227;calls=100:0,2,4/200:1,3,5,6,7,8,9,10,11")]
    [InlineData("selection7", "838,346,922,752,783,939,987,382,501,802,375,223;next=1238026765;calls=100:0,1,2,3,4,5,6,7,8,9,10,11")]
    [InlineData("replacement7", "838,346,922,752,783,939,987,382,501,802,375,223;next=1238026765;calls=100:0,1,2,3,4,5,6,7,8,9,10,11")]
    [InlineData("creation42", "880,820,1046,527,248,455,221,486,600,644,278,244;next=1785605914;calls=100:0,1,2/200:0,1,2,3,4,5,6,7,8")]
    [InlineData("crossover42", "880,821,1047,528,251,457,225,489,603,647,281,247;next=1785605914;calls=100:0,4,6/200:1,2,3,5,7,8,9,10,11")]
    [InlineData("mutation42", "880,821,1047,528,251,457,225,489,603,647,281,247;next=1785605914;calls=100:0,4,6/200:1,2,3,5,7,8,9,10,11")]
    [InlineData("refinement42", "880,821,1047,528,251,457,225,489,603,647,281,247;next=1785605914;calls=100:0,4,6/200:1,2,3,5,7,8,9,10,11")]
    [InlineData("selection42", "987,503,202,478,683,760,760,875,916,535,749,610;next=1982420427;calls=100:0,1,2,3,4,5,6,7,8,9,10,11")]
    [InlineData("replacement42", "987,503,202,478,683,760,760,875,916,535,749,610;next=1982420427;calls=100:0,1,2,3,4,5,6,7,8,9,10,11")]
    public void RoutedOperations_PreserveSeededBehavior(string scenario, string baseline) =>
        Snapshots[scenario].ShouldBe(baseline);

    private static readonly Dictionary<string, string> Snapshots = Capture();

    private static Dictionary<string, string> Capture()
    {
        var problem = FuncProblem.Create((int x) => x, new Space(), SingleObjective.Minimize);
        var scope = ResolutionScope.Create().For<int, Space, IProblem<int, Space>>();
        int[] inputs = Enumerable.Range(0, 12).ToArray();
        var population = inputs.Select(x => new EvaluatedCandidate<int>(x, new ObjectiveVector(x))).ToArray();
        var parents = inputs.Select(x => Parents.From(x, x + 1000)).ToArray();
        Dictionary<string, string> snapshots = [];
        foreach (var seed in new[] { 7, 42 })
        {
            var log = new List<string>();
            var random = RandomNumberGenerator.Create(seed);
            void Save(string role, IEnumerable<int> values) => snapshots.Add(role + seed,
                string.Join(",", values) + ";next=" + random.NextInt() + ";calls=" + string.Join("/", log));
            Save("creation", scope.Resolve(ChoosePerItem.Create([new Creator(100, log), new Creator(200, log)], [1.0, 3.0]))
                .Create(12, random, problem.SearchSpace, problem));
            random = RandomNumberGenerator.Create(seed);
            log.Clear();
            Save("crossover", scope.Resolve(ChoosePerItem.Create([new DrawingCross(100, log), new DrawingCross(200, log)], [1.0, 3.0]))
                .Cross(parents, random, problem.SearchSpace, problem));
            random = RandomNumberGenerator.Create(seed);
            log.Clear();
            Save("mutation", scope.Resolve(ChoosePerItem.Create([new DrawingMutate(100, log), new DrawingMutate(200, log)], [1.0, 3.0]))
                .Mutate(inputs, random, problem.SearchSpace, problem));
            random = RandomNumberGenerator.Create(seed);
            log.Clear();
            Save("refinement", scope.Resolve(ChoosePerItem.Create([new DrawingRefine(100, log), new DrawingRefine(200, log)], [1.0, 3.0]))
                .Refine(inputs, random, problem.SearchSpace, problem));
            random = RandomNumberGenerator.Create(seed);
            log.Clear();
            Save("selection", scope.Resolve(ChoosePerCall.Create<Selection, int>([new DrawingSelect(100, log), new DrawingSelect(200, log)], [1.0, 3.0]))
                .Select(population, problem.Objective, 12, random, problem.SearchSpace, problem).Select(x => x.Candidate));
            random = RandomNumberGenerator.Create(seed);
            log.Clear();
            Save("replacement", scope.Resolve(ChoosePerCall.Create<Replacement, int>([new DrawingReplace(100, log), new DrawingReplace(200, log)], [1.0, 3.0]))
                .Replace(population, population, problem.Objective, 12, random, problem.SearchSpace, problem).Select(x => x.Candidate));
        }
        return snapshots;
    }

    private static int[] Apply(IEnumerable<int> values, int offset, List<string> log, IRandomNumberGenerator random)
    {
        var batch = values.ToArray();
        log.Add(offset + ":" + string.Join(",", batch));
        return batch.Select(x => x + offset + (random.NextInt() % 1000)).ToArray();
    }

    private sealed class Space : ISearchSpace<int> { public bool Contains(int candidate) => true; }
    private sealed record Creator(int Offset, List<string> Log) : StatelessCreator<int>
    {
        public override IReadOnlyList<int> Create(int count, IRandomNumberGenerator random) => Apply(Enumerable.Range(0, count), Offset, Log, random);
    }
    private sealed record DrawingCross(int Offset, List<string> Log) : StatelessCrossover<int>
    {
        public override IReadOnlyList<int> Cross(IReadOnlyList<Parents<int>> parents, IRandomNumberGenerator random) => Apply(parents.Select(x => x.Parent1), Offset, Log, random);
    }
    private sealed record DrawingMutate(int Offset, List<string> Log) : StatelessMutator<int>
    {
        public override IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random) => Apply(parents, Offset, Log, random);
    }
    private sealed record DrawingRefine(int Offset, List<string> Log) : StatelessRefiner<int>
    {
        public override IReadOnlyList<int> Refine(IReadOnlyList<int> candidates, IRandomNumberGenerator random) => Apply(candidates, Offset, Log, random);
    }
    private sealed record DrawingSelect(int Offset, List<string> Log) : StatelessSelector<int>
    {
        public override IReadOnlyList<EvaluatedCandidate<int>> Select(IReadOnlyList<EvaluatedCandidate<int>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random) =>
            Apply(population.Take(count).Select(x => x.Candidate), Offset, Log, random).Select(x => new EvaluatedCandidate<int>(x, new ObjectiveVector(x))).ToArray();
    }
    private sealed record DrawingReplace(int Offset, List<string> Log) : StatelessReplacer<int>
    {
        public override IReadOnlyList<EvaluatedCandidate<int>> Replace(IReadOnlyList<EvaluatedCandidate<int>> previousPopulation, IReadOnlyList<EvaluatedCandidate<int>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random) =>
            Apply(offspringPopulation.Take(count).Select(x => x.Candidate), Offset, Log, random).Select(x => new EvaluatedCandidate<int>(x, new ObjectiveVector(x))).ToArray();
    }
}

