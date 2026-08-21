namespace HEAL.HeuristicLib.Analysis;

public abstract record Analyzer<TResult> : IAnalyzer<TResult>
    where TResult : class
{
    public virtual IAnalyzerRunState<TResult> CreateAnalyzerState() => new RunState(this, CreateInitialResult());
    public abstract TResult CreateInitialResult();

    public abstract void RegisterObservations(ObservationPlan observations, TResult result);

    private sealed class RunState(Analyzer<TResult> analyzer, TResult result) : IAnalyzerRunState<TResult>
    {
        public TResult Result { get; } = result;

        public void RegisterObservations(ObservationPlan observations)
        {
            analyzer.RegisterObservations(observations, Result);
        }
    }
}
