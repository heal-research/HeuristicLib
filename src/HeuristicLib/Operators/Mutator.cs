namespace HEAL.HeuristicLib.Operators;

public interface IMutator<TSolution> : IOperator {
  TSolution Mutate(TSolution parent);
}

public interface IMutatorTemplate<out TMutator, TGenotype, in TParams> 
  : IOperatorTemplate<TMutator, TParams>
  where TMutator : IMutator<TGenotype>
  where TParams : MutationParameters {
}

public record MutationParameters();

public abstract class MutatorBase<TSolution> : IMutator<TSolution> {
  public abstract TSolution Mutate(TSolution parent);
}

public abstract class MutatorTemplateBase<TMutator, TGenotype, TParams> : IMutatorTemplate<TMutator, TGenotype, TParams>
  where TMutator : IMutator<TGenotype>
  where TParams : MutationParameters {
  public abstract TMutator Parameterize(TParams parameters);
}
