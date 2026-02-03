namespace HEAL.HeuristicLib.Problems.Dynamic;

public class EvaluationClock : IEpochClock {
  private readonly Lock epochLocker = new();

  public EvaluationClock(int epochLength) {
    EpochLength = epochLength;
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(epochLength);
  }

  public int PendingEpochs { get; private set; }
  public long Ticks { get; private set; }
  public int EpochLength { get; }
  public int CurrentEpoch => (int)(Ticks / EpochLength);

  //returns whether no unresolved epoch changes are pending
  public EvaluationTiming IncreaseCount() {
    lock (epochLocker) {
      Ticks++;
      var valid = PendingEpochs == 0;
      if (Ticks % EpochLength == 0)
        PendingEpochs++;
      return new EvaluationTiming(Ticks, CurrentEpoch, valid);
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
      Ticks = (CurrentEpoch + 1) * EpochLength;
      PendingEpochs++;
    }
  }
}
