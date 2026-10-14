namespace HEAL.HeuristicLib.Execution;

/// <summary>A typed-input factory whose reference identifies one set of attached aspects.</summary>
public abstract class AspectFactory<TInput>
{
    public abstract Aspect Create(TInput input);
}

/// <remarks>The factory's reference identifies its attached results; each input receives its own aspect.</remarks>
public sealed class AspectFactory<TInput, TAspect>(Func<TInput, TAspect> create) : AspectFactory<TInput>
    where TAspect : Aspect
{
    public override TAspect Create(TInput input) => create(input);
}

public static class AspectFactory
{
    public static AspectFactory<TInput, TAspect> Create<TInput, TAspect>(Func<TInput, TAspect> create)
        where TAspect : Aspect => new(create);
}
