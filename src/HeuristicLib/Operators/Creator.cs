using HEAL.HeuristicLib.Encodings;

namespace HEAL.HeuristicLib.Operators;

public interface ICreator<out TGenotype, in TEncoding> : IOperator where TEncoding : IEncoding<TGenotype, TEncoding> {
  TGenotype Create<TContext>(TContext context) where TContext : IEncodingContext<TEncoding>, IRandomContext;
}

// public static class Creator {
//   public static ICreator<TGenotype> Create<TGenotype>(Func<TGenotype> creator) => new Creator<TGenotype>(creator);
// }
//
// public sealed class Creator<TGenotype> : ICreator<TGenotype> {
//   private readonly Func<TGenotype> creator;
//   internal Creator(Func<TGenotype> creator) {
//     this.creator = creator;
//   }
//   public TGenotype Create() => creator();
// }

public abstract class CreatorBase<TGenotype, TEncoding> : ICreator<TGenotype, TEncoding> where TEncoding : IEncoding<TGenotype, TEncoding> {
  public abstract TGenotype Create<TContext>(TContext context) where TContext : IEncodingContext<TEncoding>, IRandomContext;
}

