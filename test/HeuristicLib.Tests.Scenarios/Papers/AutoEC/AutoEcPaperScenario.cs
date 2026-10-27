using HEAL.HeuristicLib.Algorithms.AutoEC;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Encodings.IntegerVectors;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems.Dynamic;
using HEAL.HeuristicLib.SearchSpaces;
using IntegerVectorUniformDistributedCreator =
    HEAL.HeuristicLib.Encodings.IntegerVectors.UniformDistributedCreator;
using MetaOptimizationGenotype =
    HEAL.HeuristicLib.Encodings.Composite.CompositeGenotype<HEAL.HeuristicLib.Encodings.RealVectors.RealVector,
        HEAL.HeuristicLib.Encodings.IntegerVectors.IntegerVector>;
using MetaOptimizationSearchSpace =
    HEAL.HeuristicLib.Encodings.Composite.CompositeSearchSpace<HEAL.HeuristicLib.Encodings.RealVectors.RealVector,
        HEAL.HeuristicLib.Encodings.RealVectors.BoundedRealVectorSearchSpace,
        HEAL.HeuristicLib.Encodings.IntegerVectors.IntegerVector,
        HEAL.HeuristicLib.Encodings.IntegerVectors.IntegerVectorSearchSpace>;
using RealVectorUniformDistributedCreator =
    HEAL.HeuristicLib.Encodings.RealVectors.UniformDistributedCreator;

namespace HEAL.HeuristicLib.Tests.Scenarios.Papers.AutoEC;

public partial class AutoEcPaperScenarioTests
{
    private abstract class AutoEcPaperScenario<TCandidate, TSearchSpace, TProblem>(
        AutoEcChildAlgorithmKind childAlgorithmKind = AutoEcChildAlgorithmKind.GeneticAlgorithm)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : DynamicProblem<TProblem, TCandidate, TSearchSpace>
    {
        protected const double HypermutationRate = 1.0;
        protected const int HypermutationDuration = 2;
        protected const double ChangeTriggeredImmigrantRate = 0.2;

        public virtual int TargetEpochChanges => MatrixScaffoldTargetEpochChanges;
        protected virtual int EpochLength => MatrixScaffoldEpochLength;
        public abstract string ScenarioName { get; }
        public AutoEcChildAlgorithmKind ChildAlgorithmKind { get; } = childAlgorithmKind;
        public virtual int ProblemSeed => 321;
        public virtual int RunSeed => 123;
        public virtual int NoRacers => 2;
        public virtual int BurnInEpochs => 1;
        public virtual double EarlyTerminationStrength => 0.0;
        public virtual double HallOfFameStrength => 0.25;
        public virtual int MinimumModelObservationCount => 10;
        public virtual int ModelObservationInterval => 10;
        public virtual double MetaMutationStrength => 0.15;
        protected abstract AutoEcParameterRange MutationRateRange { get; }
        protected abstract AutoEcParameterRange PopulationSizeRange { get; }
        protected abstract IReadOnlyList<AutoEcOperatorChoice<ICrossover<TCandidate>>> CrossoverChoices { get; }
        protected abstract IReadOnlyList<AutoEcOperatorChoice<IMutator<TCandidate>>> MutatorChoices { get; }
        public IReadOnlyList<AutoEcParameterRange> HyperParameterRanges =>
        [
            MutationRateRange,
            PopulationSizeRange,
            new("crossover", "categorical", 0, CrossoverChoices.Count - 1, CrossoverChoices.Select(choice => choice.Name).ToArray()),
            new("mutator", "categorical", 0, MutatorChoices.Count - 1, MutatorChoices.Select(choice => choice.Name).ToArray())
        ];
        public DynamicRacingQualityTrace<TCandidate> QualityTrace { get; private set; } = new();

        public abstract TProblem CreateProblem();

        public abstract AutoEcProblemDescription CreateProblemDescription();

        public abstract AutoEcBaseAlgorithmDescription CreateBaseAlgorithmDescription();

        protected MetaOptimizationSearchSpace CreateHyperParameterSearchSpace() =>
            new(new BoundedRealVectorSearchSpace(1, new RealVector(MutationRateRange.Minimum), new RealVector(MutationRateRange.Maximum)),
                new IntegerVectorSearchSpace(3,
                    new IntegerVector((int)PopulationSizeRange.Minimum, 0, 0),
                    new IntegerVector((int)PopulationSizeRange.Maximum, CrossoverChoices.Count - 1, MutatorChoices.Count - 1)));

        protected ICrossover<TCandidate> SelectCrossover(MetaOptimizationGenotype hyperParameters) =>
            CrossoverChoices[hyperParameters.Part2[1]].Operator;

        protected IMutator<TCandidate> SelectMutator(MetaOptimizationGenotype hyperParameters) =>
            MutatorChoices[hyperParameters.Part2[2]].Operator;

        public virtual IEvaluator<TCandidate> CreateEvaluator(TProblem problem) =>
            ProblemEvaluator.For(problem);

        public DynamicRacingAlgorithm<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>,
            IAlgorithm<TCandidate, PopulationState<TCandidate>>> CreateRacingAlgorithm(
            TProblem problem,
            IEvaluator<TCandidate> evaluator,
            MetaOptimizationGenotype? initialConfiguration = null)
        {
            var metaSpace = CreateHyperParameterSearchSpace();
            ICreator<MetaOptimizationGenotype> metaCreator = metaSpace.CombineCreators(
                new RealVectorUniformDistributedCreator(),
                new IntegerVectorUniformDistributedCreator());
            if (initialConfiguration is not null)
                metaCreator = new PredefinedCandidatesCreator<MetaOptimizationGenotype>([initialConfiguration], metaCreator);
            var metaMutator = metaSpace.CombineMutator(
                new GaussianMutator(mutationRate: 1.0, mutationStrength: MetaMutationStrength),
                new UniformOnePositionMutator());

            return new DynamicRacingAlgorithm<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>,
                IAlgorithm<TCandidate, PopulationState<TCandidate>>>(
                metaSpace,
                metaCreator,
                metaMutator,
                new BestPopulationStateMerger<TCandidate>(),
                candidate => CreateAlgorithm(problem, evaluator, candidate),
                _ => evaluator)
            {
                NoRacers = NoRacers,
                BurnInEpochs = BurnInEpochs,
                EarlyTerminationStrength = EarlyTerminationStrength,
                HallOfFameStrength = HallOfFameStrength,
                MinimumModelObservationCount = MinimumModelObservationCount,
                ModelObservationInterval = ModelObservationInterval,
                QualityTraceSinkFactory = CreateQualityTraceSink
            };
        }

        public BestBeforeChangePerformanceAnalyzer<TCandidate, TSearchSpace, TProblem> CreateBestBeforeChangePerformanceAnalysis(TProblem problem, IEvaluator<TCandidate> evaluator) => new(problem, [evaluator]);

        protected abstract IAlgorithm<TCandidate, PopulationState<TCandidate>> CreateAlgorithm(
            TProblem problem,
            IEvaluator<TCandidate> evaluator,
            MetaOptimizationGenotype hyperParameters);

        private DynamicRacingQualityTrace<TCandidate> CreateQualityTraceSink()
        {
            QualityTrace = new DynamicRacingQualityTrace<TCandidate>();
            return QualityTrace;
        }
    }

    private sealed record AutoEcOperatorChoice<TOperator>(string Name, TOperator Operator);
}
