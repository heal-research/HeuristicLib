using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace HEAL.HeuristicLib.Execution;

/// <summary>
/// One declared wrapper recipe for the source configurations its selector matches. It owns a separate preparation
/// for each selected source through weak keys; placement and ordering belong to the requesting execution bindings.
/// </summary>
internal sealed class WrapperRegistration
{
    private readonly ConditionalWeakTable<ExecutionPreparation, PreparedWrapper> preparations = new();
    // Marks this registration's private sharing scopes without giving them a path back to the registration.
    private readonly object identity = new();
    private readonly Func<IConfigurationNode, bool> matches;
    private readonly Func<IConfigurationNode, IConfigurationNode> wrap;
    private readonly ExecutionSharingScope sharing;

    private WrapperRegistration(Func<IConfigurationNode, bool> matches, Func<IConfigurationNode, IConfigurationNode> wrap,
        ExecutionSharingScope sharing, bool registeredByModule, int sequence)
    {
        this.matches = matches;
        this.wrap = wrap;
        this.sharing = sharing;
        RegisteredByModule = registeredByModule;
        Sequence = sequence;
    }

    public bool RegisteredByModule { get; }
    public int Sequence { get; }

    public static WrapperRegistration Create<TConfiguration>(NodeSelector<TConfiguration> selector, Func<TConfiguration, TConfiguration> wrap,
        ExecutionSharingScope sharing, bool registeredByModule, int sequence)
        where TConfiguration : class, IConfigurationNode =>
        new(selector.Matches, configuration => wrap((TConfiguration)configuration), sharing, registeredByModule, sequence);

    /// <remarks>
    /// Only original source configurations are offered; predicate exceptions propagate to the binding. Sources selected
    /// beneath an internal composition are excluded from every registration. Sources selected
    /// beneath this registration's own generated wrappers are not offered, so a recipe adding a child of the selected
    /// role cannot wrap that child again without bound. Other registrations still see such children.
    /// </remarks>
    public bool Matches(ExecutionPreparation source) =>
        !source.Sharing.IsInternal && !source.Sharing.IsWithinWrapperOf(identity) && matches(source.Source);

    public ExecutionPreparation GetPreparation(ExecutionPreparation source) =>
        preparations.GetValue(source, static execution => new PreparedWrapper(execution)).PrepareOnce(this);

    /// <summary>Owns this recipe's generated configuration, state and private dependencies for one source.</summary>
    private sealed class PreparedWrapper(ExecutionPreparation source)
    {
        private PreparationStatus status;
        private ExecutionPreparation? wrapper;
        [SuppressMessage("Major Code Smell", "S1450", Justification = "The wrapper owns this scope strongly; execution preparations keep only weak references to it.")]
        private ExecutionSharingScope? privateSharing;
        private ExceptionDispatchInfo? fault;

        // Never retain the registration here. A value-to-table-owner path can retain a discarded registration
        // while its source survives, including paths through retained child scopes or pinned dependencies.
        public ExecutionPreparation PrepareOnce(WrapperRegistration registration)
        {
            if (status == PreparationStatus.Faulted)
                fault!.Throw();
            if (status == PreparationStatus.Ready)
                return wrapper!;
            if (status == PreparationStatus.Preparing)
                throw new InvalidOperationException("Recursive wrapper preparation.");

            status = PreparationStatus.Preparing;
            try
            {
                var configuration = registration.wrap(source.Source)
                    ?? throw new InvalidOperationException("A wrapper recipe returned null.");
                if (ReferenceEquals(configuration, source.Source))
                    throw new InvalidOperationException("A wrapper recipe must wrap its supplied source, not return it unchanged.");

                var inheritFromSource = source.Sharing.IsWithin(registration.sharing);
                var parent = inheritFromSource ? source.Sharing : registration.sharing;
                privateSharing = new ExecutionSharingScope(parent, retainParent: inheritFromSource, registration.identity);
                wrapper = source.CreateWrapper(configuration, privateSharing);
                wrapper.PrepareOnce();
                status = PreparationStatus.Ready;
                return wrapper;
            }
            catch (Exception exception)
            {
                fault = ExceptionDispatchInfo.Capture(exception);
                status = PreparationStatus.Faulted;
                throw;
            }
        }

        private enum PreparationStatus { Unprepared, Preparing, Ready, Faulted }
    }
}
