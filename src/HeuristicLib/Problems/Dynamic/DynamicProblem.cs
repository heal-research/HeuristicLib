using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators.Analyzer;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems.Dynamic;

public abstract class DynamicProblem<TGenotype, TEncoding> : SimpleAnalysis<TGenotype>,
                                                             IDynamicProblem<TGenotype, TEncoding>,
                                                             IDisposable
  where TEncoding : class, IEncoding<TGenotype>
  where TGenotype : class {
  private readonly Lock epochLocker = new();
  private readonly ReaderWriterLockSlim rwLock = new();

  private int pendingEpochs;
  public int EpochCount { get; private set; }
  public int EpochLength { get; }
  public int CurrentEpoch => EpochCount / EpochLength;
  public readonly UpdatePolicy UpdatePolicy;

  public abstract TEncoding SearchSpace { get; }
  public abstract Objective Objective { get; }

  protected DynamicProblem(UpdatePolicy updatePolicy = UpdatePolicy.AfterEvaluation, int epochLength = int.MaxValue) {
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(epochLength);
    UpdatePolicy = updatePolicy;
    EpochLength = epochLength;
  }

  //this method will be called in parallel
  public ObjectiveVector Evaluate(TGenotype solution, IRandomNumberGenerator random) {
    //Evaluate in parallel read lock
    rwLock.EnterReadLock();
    ObjectiveVector r;
    try {
      r = EvaluateInner(solution, random);
    }
    finally { rwLock.ExitReadLock(); }

    //Update epoch count
    lock (epochLocker) {
      EpochCount++;
      if (EpochCount % EpochLength == 0) {
        pendingEpochs++;
      }
    }

    if (UpdatePolicy == UpdatePolicy.Asynchronous)
      ResolvePendingUpdates();

    return r;
  }

  public abstract ObjectiveVector EvaluateInner(TGenotype solution, IRandomNumberGenerator random);

  protected abstract void Update();

  public override void AfterEvaluation(IReadOnlyList<TGenotype> genotypes, IReadOnlyList<ObjectiveVector> values,
                                       IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
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

  private void ResolvePendingUpdates() {
    if (pendingEpochs == 0)
      return; //pre-check
    rwLock.EnterWriteLock();

    try {
      lock (epochLocker) {
        while (pendingEpochs > 0) {
          Update();
          pendingEpochs--;
        }
      }
    }
    finally { rwLock.ExitWriteLock(); }
  }
}
