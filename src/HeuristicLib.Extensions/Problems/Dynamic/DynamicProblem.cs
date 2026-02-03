using System.Collections.Concurrent;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators.Analyzer;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems.Dynamic;

public abstract class DynamicProblem<TGenotype, TEncoding> : AttachedAnalysis<TGenotype>,
                                                             IDynamicProblem<TGenotype, TEncoding>,
                                                             IDisposable
  where TEncoding : class, IEncoding<TGenotype>
  where TGenotype : class {
  private readonly ReaderWriterLockSlim rwLock = new();

  public readonly UpdatePolicy UpdatePolicy;
  public EvaluationClock EpochClock { get; }

  public abstract TEncoding SearchSpace { get; }
  public abstract Objective Objective { get; }

  private readonly ConcurrentBag<(TGenotype solution, ObjectiveVector objective, EvaluationTiming timing)> evaluationLog = [];

  public event EventHandler<IReadOnlyList<(TGenotype, ObjectiveVector, EvaluationTiming)>>? OnEvaluation;
  protected IRandomNumberGenerator EnvironmentRandom { get; }

  protected DynamicProblem(IRandomNumberGenerator environmentRandom, UpdatePolicy updatePolicy = UpdatePolicy.AfterEvaluation, int epochLength = int.MaxValue) {
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(epochLength);
    UpdatePolicy = updatePolicy;
    EpochClock = new EvaluationClock(epochLength);
    EnvironmentRandom = environmentRandom;
  }

  //this method will be called in parallel
  public ObjectiveVector Evaluate(TGenotype solution, IRandomNumberGenerator random) {
    //PredictAndTrain in parallel read lock
    var timing = EpochClock.IncreaseCount();

    rwLock.EnterReadLock();
    ObjectiveVector r;
    try {
      r = Evaluate(solution, random, timing);
    }
    finally { rwLock.ExitReadLock(); }

    evaluationLog.Add((solution, r, timing));

    if (UpdatePolicy == UpdatePolicy.Asynchronous)
      ResolvePendingUpdates();

    return r;
  }

  public abstract ObjectiveVector Evaluate(TGenotype solution, IRandomNumberGenerator random, EvaluationTiming timing);

  public override void AfterEvaluation(IReadOnlyList<TGenotype> genotypes, IReadOnlyList<ObjectiveVector> values,
                                       IEncoding<TGenotype> searchSpace, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    OnEvaluation?.Invoke(this, evaluationLog.OrderBy(x => x.timing.EpochCount).ToArray());
    evaluationLog.Clear();
    if (UpdatePolicy == UpdatePolicy.AfterEvaluation)
      ResolvePendingUpdates();
  }

  public override void AfterInterception(IAlgorithmState currentAlgorithmState, IAlgorithmState? previousIterationResult, IEncoding<TGenotype> searchSpace, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    if (UpdatePolicy == UpdatePolicy.AfterInterception)
      ResolvePendingUpdates();
  }

  public void Dispose() {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  ~DynamicProblem() {
    Dispose(false);
  }

  protected virtual void Dispose(bool disposing) {
    rwLock.Dispose();
  }

  protected abstract void Update();

  public void UpdateOnce() {
    EpochClock.AdvanceEpoch();
    ResolvePendingUpdates();
  }

  private void ResolvePendingUpdates() {
    if (EpochClock.PendingEpochs == 0)
      return; //pre-check
    rwLock.EnterWriteLock();
    try {
      EpochClock.ResolvePendingEpochs(Update);
    }
    finally { rwLock.ExitWriteLock(); }
  }
}
