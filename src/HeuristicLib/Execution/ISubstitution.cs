namespace HEAL.HeuristicLib.Execution;

/// <summary>A rewrite applied while building a configuration, never stored or serialized.</summary>
public interface ISubstitution
{
    /// <summary>The single substitutions it stands for, in application order, with the same instances on every call.</summary>
    /// <remarks>Applying and deduplicating the substitutions is the call's responsibility; expansion preserves repeated parts.</remarks>
    IReadOnlyList<Substitution> Expand();
}
