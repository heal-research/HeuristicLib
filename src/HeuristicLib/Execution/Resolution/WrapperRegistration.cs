using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace HEAL.HeuristicLib.Execution;

/// <summary>
/// One declared wrapper recipe. It owns a separate preparation for each selected source through weak keys;
/// placement and ordering belong to the requesting execution bindings.
/// </summary>
internal sealed class WrapperRegistration
{
    private readonly ConditionalWeakTable<ExecutionPreparation, PreparedWrapper> preparations = new();
    private readonly IConfigurationNode source;
    private readonly Func<IConfigurationNode, IConfigurationNode> wrap;
    private readonly ExecutionSharingScope sharing;

    private WrapperRegistration(IConfigurationNode source, Func<IConfigurationNode, IConfigurationNode> wrap,
        ExecutionSharingScope sharing, bool registeredByModule, int sequence)
    {
        this.source = source;
        this.wrap = wrap;
        this.sharing = sharing;
        RegisteredByModule = registeredByModule;
        Sequence = sequence;
    }

    public bool RegisteredByModule { get; }
    public int Sequence { get; }

    public static WrapperRegistration Create<TConfiguration>(TConfiguration source, Func<TConfiguration, TConfiguration> wrap,
        ExecutionSharingScope sharing, bool registeredByModule, int sequence)
        where TConfiguration : class, IConfigurationNode =>
        new(source, configuration => wrap((TConfiguration)configuration), sharing, registeredByModule, sequence);

    public bool Matches(IConfigurationNode configuration) => ReferenceEquals(configuration, source);

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
                privateSharing = new ExecutionSharingScope(parent, retainParent: inheritFromSource);
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
