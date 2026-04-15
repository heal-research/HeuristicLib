using System.Diagnostics.CodeAnalysis;

namespace HEAL.HeuristicLib.Execution;

public class ExecutionInstanceRegistry
{
  public Run Run { get; }

  private readonly ExecutionInstanceRegistry? parentRegistry;

  private readonly Dictionary<IExecutable<IExecutionInstance>, IExecutionInstance> registry = new(ReferenceEqualityComparer.Instance);
  private readonly Dictionary<IExecutable<IExecutionInstance>, IExecutable<IExecutionInstance>> replacementExecutables = new(ReferenceEqualityComparer.Instance);
  private readonly HashSet<IExecutable<IExecutionInstance>> executablesBeingCreated = new(ReferenceEqualityComparer.Instance);

  public ExecutionInstanceRegistry(Run run, ExecutionInstanceRegistry? parentRegistry = null)
  {
    Run = run;
    this.parentRegistry = parentRegistry;
  }

  public ExecutionInstanceRegistry CreateChildRegistry()
  {
    return new ExecutionInstanceRegistry(Run, this);
  }

  private bool TryResolve(IExecutable<IExecutionInstance> executable, [MaybeNullWhen(false)] out IExecutionInstance instance)
  {
    if (registry.TryGetValue(executable, out instance)) {
      return true;
    }

    if (parentRegistry is not null && parentRegistry.TryResolve(executable, out instance)) {
      return true;
    }

    return false;
  }

  private bool TryGetReplacementExecutable(IExecutable<IExecutionInstance> executable, [MaybeNullWhen(false)] out IExecutable<IExecutionInstance> replacementExecutable)
  {
    if (replacementExecutables.TryGetValue(executable, out replacementExecutable)) {
      return true;
    }

    if (parentRegistry is not null && parentRegistry.TryGetReplacementExecutable(executable, out replacementExecutable)) {
      return true;
    }

    return false;
  }

  public TExecutionInstance? ResolveOptional<TExecutionInstance>(IExecutable<TExecutionInstance>? executable) where TExecutionInstance : class, IExecutionInstance
  {
    return executable is null ? null : Resolve(executable);
  }

  public TExecutionInstance Resolve<TExecutionInstance>(IExecutable<TExecutionInstance> executable)
    where TExecutionInstance : class, IExecutionInstance
  {
    IExecutable<IExecutionInstance> untypedExecutable = executable;

    if (registry.TryGetValue(untypedExecutable, out var localInstance)) {
      return (TExecutionInstance)localInstance;
    }

    if (TryGetReplacementExecutable(untypedExecutable, out var replacementExecutable)) {
      if (!executablesBeingCreated.Add(untypedExecutable)) {
        return executable.CreateExecutionInstance(this);
      }

      try {
        var createdInstance = replacementExecutable.CreateExecutionInstance(this);
        registry.Add(untypedExecutable, createdInstance);
        return (TExecutionInstance)createdInstance;
      } finally {
        executablesBeingCreated.Remove(untypedExecutable);
      }
    }

    if (parentRegistry is not null && parentRegistry.TryResolve(untypedExecutable, out var parentInstance)) {
      return (TExecutionInstance)parentInstance;
    }

    var instance = executable.CreateExecutionInstance(this);
    registry.Add(untypedExecutable, instance);
    return instance;
  }

  public void PreRegister<TExecutionInstance>(IExecutable<TExecutionInstance> executable, TExecutionInstance instance)
    where TExecutionInstance : class, IExecutionInstance
  {
    IExecutable<IExecutionInstance> untypedExecutable = executable;
    if (!registry.TryAdd(untypedExecutable, instance)) {
      throw new InvalidOperationException("Object has already been registered");
    }
  }

  public void PreRegister<TExecutionInstance>(IExecutable<TExecutionInstance> executable, IExecutable<TExecutionInstance> replacementExecutable)
    where TExecutionInstance : class, IExecutionInstance
  {
    IExecutable<IExecutionInstance> untypedExecutable = executable;
    if (!replacementExecutables.TryAdd(untypedExecutable, replacementExecutable)) {
      throw new InvalidOperationException("Replacement executable has already been registered");
    }
  }
}
