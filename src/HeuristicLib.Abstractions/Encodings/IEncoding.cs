namespace HEAL.HeuristicLib.Encodings;

public interface IEncoding;

public interface IEncoding<in TGenotype> : IEncoding {
    bool Contains(TGenotype genotype);

    //bool IsSubspaceOf(IEncoding<TGenotype> other);
}
