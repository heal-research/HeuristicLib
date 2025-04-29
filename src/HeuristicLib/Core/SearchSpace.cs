namespace HEAL.HeuristicLib;

public interface ISearchSpace { }

public interface ISearchSpace<in TGenotype> : ISearchSpace {
  bool IsValid(TGenotype genotype);
  //bool IsValidOperator(IEncodingOperator<TGenotype, TSelf> @operator);
}

public abstract record class SearchSpace<TGenotype> : ISearchSpace<TGenotype> {
  protected SearchSpace() { }
  public abstract bool IsValid(TGenotype genotype);
  // public virtual bool IsValidOperator(IEncodingOperator<TGenotype, TSelf> @operator) {
  //   return @operator.Encoding.Equals(this);
  // }
}
