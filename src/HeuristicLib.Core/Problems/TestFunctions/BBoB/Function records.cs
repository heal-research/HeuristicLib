using HEAL.HeuristicLib.Encodings.RealVector;
using HEAL.HeuristicLib.Optimization;

// ReSharper disable UnusedParameter.Local
#pragma warning disable S1172
#pragma warning disable S1172

namespace HEAL.HeuristicLib.Problems.TestFunctions.BBoB;

public abstract record BBoBFunction() : ITestFunction {
  public required int Dimension { get; init; }
  public double Min { get; init; } = -5;
  public double Max { get; init; } = 5;
  public ObjectiveDirection Objective => ObjectiveDirection.Minimize;
  public abstract double Evaluate(RealVector solution);
}

public record AttractiveSectorFunction(FAttractiveSectorData Data) : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FAttractiveSectorRaw(solution, Data);
}

public record BentCigarFunction : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FBentCigarRaw(solution);
}

public record BentCigarGeneralizedFunction(FBentCigarGeneralizedVersatileData Data) : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FBentCigarGeneralizedRaw(solution, Data);
}

public record BuecheRastriginFunction : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FBuecheRastriginRaw(solution);
}

public record DifferentPowersFunction : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FDifferentPowersRaw(solution);
}

public record DiscusFunction : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FDiscusRaw(solution);
}

public record DiscusGeneralizedFunction(FDiscusGeneralizedVersatileData Data) : BBoBFunction {
  public override double Evaluate(RealVector solution) => Functions.FDiscusGeneralizedRaw(solution, Data);
}

public record EllipsoidFunction : BBoBFunction {
  public override double Evaluate(RealVector solution) => Functions.FEllipsoidRaw(solution);
}

public record GallagherFunction(FGallagherData Data) : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FGallagherRaw(solution, Data);
}

public record GriewankRosenbrockFunction : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FGriewankRosenbrockRaw(solution);
}

public record KatsuuraFunction : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FKatsuuraRaw(solution);
}

public record LinearSlopeFunction(double[] BestParameter) : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FLinearSlopeRaw(solution, BestParameter);
}

public record LunacekBiRastriginFunction(FLunacekBiRastriginData Data) : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FLunacekBiRastriginRaw(solution, Data);
}

public record RastriginFunction : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FRastriginRaw(solution);
}

public record RosenbrockFunction : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FRosenbrockRaw(solution);
}

public record SchaffersFunction : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FSchaffersRaw(solution);
}

public record SchwefelFunction : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FSchwefelRaw(solution);
}

public record SchwefelGeneralizedFunction : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FSchwefelGeneralizedRaw(solution);
}

public record SharpRidgeFunction : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FSharpRidgeRaw(solution);
}

public record SharpRidgeGeneralizedFunction(FSharpRidgeGeneralizedVersatileData Data) : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FSharpRidgeGeneralizedRaw(solution, Data);
}

public record SphereFunction : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FSphereRaw(solution);
}

public record StepEllipsoidFunction(FStepEllipsoidData Data) : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FStepEllipsoidRaw(solution, Data);
}

public record WeierstrassFunction(FWeierstrassData Data) : BBoBFunction {
  public override double Evaluate(RealVector solution) =>
    Functions.FWeierstrassRaw(solution, Data);
}
