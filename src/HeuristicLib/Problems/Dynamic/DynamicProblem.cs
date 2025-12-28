using System.Collections.Concurrent;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators.Analyzer;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Problems.Dynamic;

public class EvaluationClock : IEpochClock {
  private readonly Lock epochLocker = new();

  public int PendingEpochs { get; private set; }
  public int EpochCount { get; private set; }
  public required int EpochLength { get; init; }
  public int CurrentEpoch => EpochCount / EpochLength;

  //returns whether no unresolved epoch changes are pending
  public EvaluationTiming IncreaseCount() {
    lock (epochLocker) {
      EpochCount++;
      var valid = PendingEpochs == 0;
      if (EpochCount % EpochLength == 0)
        PendingEpochs++;
      return new EvaluationTiming(EpochCount, CurrentEpoch, valid);
    }
  }

  public void ResolvePendingEpochs(Action update) {
    lock (epochLocker) {
      var changed = PendingEpochs > 0;
      while (PendingEpochs > 0) {
        update();
        PendingEpochs--;
      }

      if (changed)
        OnEpochChange?.Invoke(this, CurrentEpoch);
    }
  }

  public event EventHandler<int>? OnEpochChange;

  public void AdvanceEpoch() {
    lock (epochLocker) {
      EpochCount = (CurrentEpoch + 1) * EpochLength;
      PendingEpochs++;
    }
  }
}

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

  protected DynamicProblem(UpdatePolicy updatePolicy = UpdatePolicy.AfterEvaluation, int epochLength = int.MaxValue) {
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(epochLength);
    UpdatePolicy = updatePolicy;
    EpochClock = new EvaluationClock { EpochLength = epochLength };
  }

  private readonly ConcurrentBag<(TGenotype solution, ObjectiveVector objective, EvaluationTiming timing)> evaluationLog = [];

  //this method will be called in parallel
  public ObjectiveVector Evaluate(TGenotype solution, IRandomNumberGenerator random) {
    //Evaluate in parallel read lock
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

  public event EventHandler<IReadOnlyList<(TGenotype, ObjectiveVector, EvaluationTiming)>>? OnEvaluation;

  public override void AfterEvaluation(IReadOnlyList<TGenotype> genotypes, IReadOnlyList<ObjectiveVector> values,
                                       IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    OnEvaluation?.Invoke(this, evaluationLog.OrderBy(x => x.timing.EpochCount).ToArray());
    evaluationLog.Clear();
    if (UpdatePolicy == UpdatePolicy.AfterEvaluation)
      ResolvePendingUpdates();
  }

  public override void AfterInterception(IIterationResult currentIterationResult, IIterationResult? previousIterationResult, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
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
