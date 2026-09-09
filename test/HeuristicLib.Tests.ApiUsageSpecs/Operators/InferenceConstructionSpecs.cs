using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Experiments;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;
using HEAL.HeuristicLib.SearchSpaces;
using UniformDistributedCreator = HEAL.HeuristicLib.Encodings.RealVectors.UniformDistributedCreator;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Operators;

public class InferenceConstructionSpecs
{
    [Fact]
    public void StaticAndFluentCompositionHelpers_InferTheirGenericArguments()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 3));
        var algorithm = CreateAlgorithm(problem);

        var problemDirectEvaluator = ProblemEvaluator.For(problem);
        var directEvaluator = ProblemEvaluator.For(problem);
        var problemTournamentSelector = TournamentSelector.For(problem, tournamentSize: 3);
        var tournamentSelector = TournamentSelector.For(problem, tournamentSize: 4);
        var randomSelector = RandomSelector.For(problem);
        var bestSelector = BestSelector.For(problem);
        var worstSelector = WorstSelector.For(problem);
        var linearRankSelector = LinearRankSelector.For(problem);
        var generalizedRankSelector = GeneralizedRankSelector.For(problem, pressure: 2.0);
        var proportionalSelector = ProportionalSelector.For(problem, windowing: false);
        var paretoSelector = ParetoCrowdingTournamentSelector.For(problem, dominateOnEqualities: false);
        var commaReplacer = CommaSelectionReplacer.For(problem);
        var plusReplacer = PlusSelectionReplacer.For(problem);
        var elitismReplacer = ElitismReplacer.For(problem, elites: 2);
        var paretoReplacer = ParetoCrowdingReplacer.For(problem, dominateOnEqualities: true);
        var randomCrossover = RandomCrossover.For(problem, bias: 0.75);
        var identityInterceptor = new IdentityInterceptor<RealVector, PopulationState<RealVector>>();
        var reconfiguredAlgorithm = algorithm with
        {
            Evaluator = directEvaluator,
            Selector = tournamentSelector,
            Interceptor = identityInterceptor
        };

        var pipelineMutator = PipelineMutator.Create(algorithm.Mutator, algorithm.Mutator);
        var fluentPipelineMutator = algorithm.Mutator.Then(algorithm.Mutator);
        var transformedCreator = TransformedCreator.Create(algorithm.Creator, algorithm.Mutator);
        var fluentTransformedCreator = algorithm.Creator.TransformWith(algorithm.Mutator);
        var transformedCrossover = TransformedCrossover.Create(algorithm.Crossover, algorithm.Mutator);
        var fluentTransformedCrossover = algorithm.Crossover.TransformWith(algorithm.Mutator);
        var pipelineInterceptor = PipelineInterceptor.Create(identityInterceptor, identityInterceptor);
        var fluentPipelineInterceptor = identityInterceptor.Then(identityInterceptor);
        var observableInterceptor = ObservableInterceptor.Create(identityInterceptor, (PopulationState<RealVector> _) => { });
        var countingInterceptor = CountingInterceptor.Create(identityInterceptor, new ObservationCounter());
        var measuredInterceptor = DurationMeasuringInterceptor.Create(identityInterceptor, new ObservationDuration());
        var eliteSelector = EliteSelector.Create(algorithm.Selector, elites: 1);
        var fluentEliteSelector = algorithm.Selector.CombinedWithElites(elites: 1);
        var genderSpecificSelector = GenderSpecificSelector.Create(algorithm.Selector, algorithm.Selector);
        var fluentGenderSpecificSelector = algorithm.Selector.PairWith(algorithm.Selector);
        var noSameMatesSelector = NoSameMatesSelector.Create(algorithm.Selector, maximumAttempts: 3);
        var fluentNoSameMatesSelector = algorithm.Selector.AvoidSameMates(maximumAttempts: 3);
        var predefinedCreator = PredefinedCandidatesCreator.Create([RealVector.Create(0.0)], algorithm.Creator);
        var fluentPredefinedCreator = algorithm.Creator.SeededWith([RealVector.Create(0.0)]);
        var unchangedMutator = NoChangeMutator.For(problem);
        var firstParentCrossover = SelectFirstParentCrossover.For(problem);
        var secondParentCrossover = SelectSecondParentCrossover.For(problem);
        var iterationTerminator = AfterIterationsTerminator.For(problem, maximumIterations: 3);
        var stagnationTerminator = StagnationTerminator.For(problem, stagnationThreshold: 4);
        var targetTerminator = TargetTerminator.For(problem, new ObjectiveVector(0.0));
        var neverTerminator = NeverTerminator.For(problem);

        var firstTerminator = algorithm.TerminatedAfterIterations(2).Terminator;
        var secondTerminator = algorithm.TerminatedAfterIterations(3).Terminator;
        var anyTerminator = AnyTerminator.Create(firstTerminator, secondTerminator);
        var fluentAnyTerminator = firstTerminator.Or(secondTerminator);
        var allTerminator = AllTerminator.Create(firstTerminator, secondTerminator);
        var fluentAllTerminator = firstTerminator.And(secondTerminator);
        var observableTerminator = ObservableTerminator.Create<RealVector, PopulationState<RealVector>>(firstTerminator, _ => { });
        var countingTerminator = CountingTerminator.Create(firstTerminator, new ObservationCounter());
        var measuredTerminator = DurationMeasuringTerminator.Create(firstTerminator, new ObservationDuration());
        var terminatedAlgorithm = StateTerminatedAlgorithm.Create(algorithm, firstTerminator);
        var fluentTerminatedAlgorithm = algorithm.TerminatedBy(firstTerminator);

        var firstStage = algorithm.TerminatedAfterIterations(2);
        var secondStage = algorithm.TerminatedAfterIterations(3);
        var pipelineAlgorithm = PipelineAlgorithm.Create(firstStage, secondStage);
        var fluentPipelineAlgorithm = firstStage.Then(secondStage);
        var cycleAlgorithm = CycleAlgorithm.Create(firstStage, secondStage);
        var fluentCycleAlgorithm = firstStage.CycleWith(secondStage, maximumCycles: 2);
        var fluentMultiCycleAlgorithm = firstStage.CycleWith([secondStage], maximumCycles: 2);
        var interfacePipelineAlgorithm = PipelineAlgorithm.Create(firstStage.Algorithm, firstStage);

        problemDirectEvaluator.ShouldBeOfType<ProblemEvaluator<RealVector>>();
        directEvaluator.ShouldBeOfType<ProblemEvaluator<RealVector>>();
        problemTournamentSelector.TournamentSize.ShouldBe(3);
        tournamentSelector.TournamentSize.ShouldBe(4);
        randomSelector.ShouldBeOfType<RandomSelector<RealVector>>();
        bestSelector.ShouldBeOfType<BestSelector<RealVector>>();
        worstSelector.ShouldBeOfType<WorstSelector<RealVector>>();
        linearRankSelector.ShouldBeOfType<LinearRankSelector<RealVector>>();
        generalizedRankSelector.Pressure.ShouldBe(2.0);
        proportionalSelector.Windowing.ShouldBeFalse();
        paretoSelector.ShouldBeOfType<ParetoCrowdingTournamentSelector<RealVector>>();
        commaReplacer.ShouldBeOfType<CommaSelectionReplacer<RealVector>>();
        plusReplacer.ShouldBeOfType<PlusSelectionReplacer<RealVector>>();
        elitismReplacer.Elites.ShouldBe(2);
        paretoReplacer.ShouldBeOfType<ParetoCrowdingReplacer<RealVector>>();
        randomCrossover.Bias.ShouldBe(0.75);
        fluentCycleAlgorithm.MaximumCycles.ShouldBe(2);
        fluentMultiCycleAlgorithm.MaximumCycles.ShouldBe(2);
        identityInterceptor.ShouldBeOfType<IdentityInterceptor<RealVector, PopulationState<RealVector>>>();
        reconfiguredAlgorithm.Evaluator.ShouldBeSameAs(directEvaluator);
        reconfiguredAlgorithm.Selector.ShouldBeSameAs(tournamentSelector);
        reconfiguredAlgorithm.Interceptor.ShouldBeSameAs(identityInterceptor);
        pipelineMutator.ChildMutators.Count.ShouldBe(2);
        fluentPipelineMutator.ChildMutators.Count.ShouldBe(2);
        transformedCreator.SourceCreator.ShouldBeSameAs(algorithm.Creator);
        fluentTransformedCreator.SourceCreator.ShouldBeSameAs(algorithm.Creator);
        transformedCrossover.SourceCrossover.ShouldBeSameAs(algorithm.Crossover);
        fluentTransformedCrossover.SourceCrossover.ShouldBeSameAs(algorithm.Crossover);
        pipelineInterceptor.ChildInterceptors.Count.ShouldBe(2);
        fluentPipelineInterceptor.ChildInterceptors.Count.ShouldBe(2);
        observableInterceptor.ChildInterceptor.ShouldBeSameAs(identityInterceptor);
        countingInterceptor.ChildInterceptor.ShouldBeSameAs(identityInterceptor);
        measuredInterceptor.ChildInterceptor.ShouldBeSameAs(identityInterceptor);
        eliteSelector.SelectorForRemaining.ShouldBeSameAs(algorithm.Selector);
        fluentEliteSelector.SelectorForRemaining.ShouldBeSameAs(algorithm.Selector);
        genderSpecificSelector.FemaleSelector.ShouldBeSameAs(algorithm.Selector);
        fluentGenderSpecificSelector.MaleSelector.ShouldBeSameAs(algorithm.Selector);
        noSameMatesSelector.MaxAttempts.ShouldBe(3);
        fluentNoSameMatesSelector.MaxAttempts.ShouldBe(3);
        predefinedCreator.PredefinedCandidates.Count.ShouldBe(1);
        fluentPredefinedCreator.PredefinedCandidates.Count.ShouldBe(1);
        unchangedMutator.ShouldBeSameAs(NoChangeMutator<RealVector>.Instance);
        firstParentCrossover.ShouldBeSameAs(SelectFirstParentCrossover<RealVector>.Instance);
        secondParentCrossover.ShouldBeSameAs(SelectSecondParentCrossover<RealVector>.Instance);
        iterationTerminator.MaximumIterations.ShouldBe(3);
        stagnationTerminator.ShouldBeOfType<StagnationTerminator<RealVector>>();
        targetTerminator.Target.ShouldBe(new ObjectiveVector(0.0));
        neverTerminator.ShouldBeOfType<NeverTerminator<RealVector>>();
        anyTerminator.ShouldNotBeNull();
        fluentAnyTerminator.ShouldNotBeNull();
        allTerminator.ShouldNotBeNull();
        fluentAllTerminator.ShouldNotBeNull();
        observableTerminator.ChildTerminator.ShouldBeSameAs(firstTerminator);
        countingTerminator.ChildTerminator.ShouldBeSameAs(firstTerminator);
        measuredTerminator.ChildTerminator.ShouldBeSameAs(firstTerminator);
        terminatedAlgorithm.Algorithm.ShouldBeSameAs(algorithm);
        fluentTerminatedAlgorithm.Algorithm.ShouldBeSameAs(algorithm);
        pipelineAlgorithm.Algorithms.Count.ShouldBe(2);
        pipelineAlgorithm.Algorithms[0].Terminator.ShouldBeSameAs(firstStage.Terminator);
        fluentPipelineAlgorithm.Algorithms.Count.ShouldBe(2);
        fluentPipelineAlgorithm.Algorithms[0].Terminator.ShouldBeSameAs(firstStage.Terminator);
        cycleAlgorithm.Algorithms.Count.ShouldBe(2);
        cycleAlgorithm.Algorithms[0].Terminator.ShouldBeSameAs(firstStage.Terminator);
        fluentCycleAlgorithm.Algorithms.Count.ShouldBe(2);
        fluentCycleAlgorithm.Algorithms[0].Terminator.ShouldBeSameAs(firstStage.Terminator);
        interfacePipelineAlgorithm.Algorithms[0].ShouldBeSameAs(firstStage.Algorithm);
    }

    [Fact]
    public void SmallRecordHelpers_InferTheirGenericArguments()
    {
        var first = RealVector.Repeat(1.0, 3);
        var second = RealVector.Repeat(2.0, 3);
        var parents = Parents.From(first, second);
        var fluentParents = (first, second).ToParents();
        var evaluatedCandidate = EvaluatedCandidate.From(first, new ObjectiveVector(1.0));
        var singleSolutionState = SingleSolutionState.From(evaluatedCandidate);
        var fluentSingleSolutionState = evaluatedCandidate.ToSingleSolutionState();
        var population = Population.From([evaluatedCandidate]);
        var populationState = PopulationState.From(population);
        var fluentPopulationState = population.ToPopulationState();
        var bestMedianWorst = BestMedianWorstEntry.From(evaluatedCandidate, evaluatedCandidate, evaluatedCandidate);
        var experimentCase = ExperimentCase.From("algorithm", "configuration", [1, 2]);
        var streamEntry = ExperimentStreamEntry.From(experimentCase, populationState);

        parents.Parent1.ShouldBeSameAs(first);
        fluentParents.Parent2.ShouldBeSameAs(second);
        singleSolutionState.EvaluatedCandidate.ShouldBeSameAs(evaluatedCandidate);
        fluentSingleSolutionState.EvaluatedCandidate.ShouldBeSameAs(evaluatedCandidate);
        populationState.Population.ShouldBeSameAs(population);
        fluentPopulationState.Population.ShouldBeSameAs(population);
        bestMedianWorst.Best.ShouldBeSameAs(evaluatedCandidate);
        experimentCase.RandomForkPath.ShouldBe([1, 2]);
        streamEntry.Trial.ShouldBeSameAs(experimentCase);
    }

    private static GeneticAlgorithm<RealVector> CreateAlgorithm(TestFunctionProblem problem)
    {
        return new GeneticAlgorithm<RealVector>
        {
            PopulationSize = 10,
            MaximumGenerations = 2,
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Crossover = new AlphaBetaBlendCrossover(),
            Mutator = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.1),
            Selector = TournamentSelector.For(problem, tournamentSize: 2)
        };
    }

    /// <summary>
    /// One problem argument yields the problem, the candidate and the search space, all inferred.
    /// </summary>
    /// <remarks>
    /// This is the mechanism the algorithm arity reduction stands on. Once an algorithm names only its candidate,
    /// nothing on the receiver says what search space a run uses, and a plain <c>TProblem problem</c> parameter
    /// cannot supply it: the search space sits in constraint position, where C# inference does not reach. Naming the
    /// problem's own type on <see cref="Problem{TSelf,TCandidate,TSearchSpace}"/> is what makes all three
    /// inferable from the single argument a user already passes.
    /// <para>
    /// Every problem declares it, so this holds for problems that state no operator defaults at all — which is what
    /// the anchor being a problem contract rather than a defaults one is about.
    /// </para>
    /// </remarks>
    [Fact]
    public void OneProblemArgument_InfersTheProblemTheCandidateAndTheSearchSpace()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 3));

        var (problemType, candidate, searchSpace) = DescribeRun(problem);

        problemType.ShouldBe(typeof(TestFunctionProblem));
        candidate.ShouldBe(typeof(RealVector));
        searchSpace.ShouldBe(typeof(BoundedRealVectorSearchSpace));
    }

    /// <summary>Shaped like the run methods the algorithm package will declare, and nothing is named at the call.</summary>
    private static (Type Problem, Type Candidate, Type SearchSpace) DescribeRun<TProblem, TCandidate, TSearchSpace>(
        Problem<TProblem, TCandidate, TSearchSpace> problem)
        where TProblem : Problem<TProblem, TCandidate, TSearchSpace>
        where TSearchSpace : class, ISearchSpace<TCandidate> =>
        (typeof(TProblem), typeof(TCandidate), typeof(TSearchSpace));
}
