using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static HEAL.HeuristicLib.Tests.Architecture.SourceCompilation;

namespace HEAL.HeuristicLib.Tests.Architecture;

public sealed class LayerDependencyTests
{
    private const string MainRoot = "src/HeuristicLib/";
    private const string ExperimentalRoot = "src/HeuristicLib.Experimental/";

    [Fact]
    public void SourceDependencies_RespectLayerBoundaries()
    {
        var actual = FindViolations();
        actual.ShouldBeEmpty();
    }

    [Fact]
    public void IterativeAlgorithmBases_RequireAnIterativeExecutorFromTheirHook()
    {
        var (compilation, _) = CreateMainCompilation(FindRepositoryRoot());
        foreach (var arity in new[] { 3, 5 })
        {
            var algorithm = compilation.Assembly.GetTypeByMetadataName($"HEAL.HeuristicLib.Algorithms.IterativeAlgorithm`{arity}")!;
            var hook = algorithm.GetMembers().OfType<IMethodSymbol>()
                .Single(method => method.IsAbstract && !method.IsImplicitlyDeclared);

            hook.Name.ShouldBe("CreateExecutor");
            hook.DeclaredAccessibility.ShouldBe(Accessibility.Protected);
            hook.Parameters.ShouldHaveSingleItem().Type.OriginalDefinition.ToDisplayString()
                .ShouldBe("HEAL.HeuristicLib.Execution.ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState>");
            hook.ReturnType.ToDisplayString().ShouldBe("HEAL.HeuristicLib.Algorithms.IterativeAlgorithmExecutor<TSearchState>");
        }
    }

    [Fact]
    public void CorePublicTypes_LiveInTheirAgreedConcepts()
    {
        var repositoryRoot = FindRepositoryRoot();
        var (compilation, _) = CreateMainCompilation(repositoryRoot);

        // Reviewed ownership boundaries only; grouping folders do not introduce namespaces.
        AssertPublicTypeLocations(compilation, repositoryRoot, MainRoot,
        [
            ("Execution.IExecutionNode", "Execution/"),
            ("Operators.IAdaptation", "Operators/Adaptation/"),
            ("Operators.Adaptation`1", "Operators/Adaptation/"),
            ("Operators.ScaleAdaptation", "Operators/Adaptation/"),
            ("Operators.AdaptedScale", "Operators/Adaptation/"),
            ("Operators.OffsetAdaptation", "Operators/Adaptation/"),
            ("Operators.AdaptedOffset", "Operators/Adaptation/"),
            ("Inspection.CountAccumulator", "Inspection/"),
            ("Inspection.DurationAccumulator", "Inspection/"),
            ("Execution.SearchConfigurationValidation", "Execution/"),
            ("Execution.ValidationReport", "Execution/"),
            ("Execution.ValidationDiagnostic", "Execution/"),
            ("Execution.AlgorithmRun", "Execution/Runs/"),
            ("Execution.AlgorithmRun`4", "Execution/Runs/"),
            ("Execution.ExecutionStream`1", "Execution/Runs/"),
            ("Execution.RunLifecycleState", "Execution/Runs/"),
            ("Execution.ExecutionConcurrency", "Execution/Concurrency/"),
            ("Execution.ExecutionConcurrencyKind", "Execution/Concurrency/"),
            ("Execution.BatchExecution", "Execution/Concurrency/"),
            ("Execution.ISubstitution", "Execution/"),
            ("Execution.RunResults", "Execution/Results/"),
            ("Inspection.AlgorithmObservation`2", "Inspection/Algorithms/"),
            ("Inspection.AlgorithmCallObservation`2", "Inspection/Algorithms/"),
            ("Inspection.AlgorithmCallEvent", "Inspection/Algorithms/"),
            ("Inspection.AlgorithmSource`3", "Inspection/Algorithms/"),
            ("Inspection.CountedAlgorithm`3", "Inspection/Algorithms/"),
            ("Inspection.TimedAlgorithm`2", "Inspection/Algorithms/"),
            ("Inspection.ObservedAlgorithm`3", "Inspection/Algorithms/"),
            ("Inspection.ObservationSource`1", "Inspection/"),
            ("Inspection.InvocationSource`3", "Inspection/"),
            ("Inspection.IObservationReceiver`1", "Inspection/"),
            ("Inspection.IClock", "Inspection/"),
            ("Inspection.IClock`1", "Inspection/"),
            ("Inspection.IClockReading", "Inspection/"),
            ("Inspection.IClockReading`1", "Inspection/"),
            ("Inspection.CounterClock", "Inspection/"),
            ("Inspection.DurationClock", "Inspection/"),
            ("Inspection.ElapsedClock", "Inspection/"),
            ("Inspection.Clock", "Inspection/"),
            ("Inspection.Counter", "Inspection/"),
            ("Inspection.Duration", "Inspection/"),
            ("Inspection.Counted`2", "Inspection/"),
            ("Inspection.Timed`2", "Inspection/"),
            ("Inspection.Observed`3", "Inspection/"),
            ("Experiments.ExperimentRun`5", "Experiments/"),
        ]);
    }

    [Fact]
    public void DynamicAnalysisTypes_BelongToExperimentalAnalysis()
    {
        var repositoryRoot = FindRepositoryRoot();
        var (compilation, _) = CreateCompilation(repositoryRoot, "HeuristicLib.Experimental", includeMain: true);

        AssertPublicTypeLocations(compilation, repositoryRoot, ExperimentalRoot,
        [
            ("Analysis.EpochClock`2", "Analysis/Dynamic/"),
            ("Analysis.DynamicClocks", "Analysis/Dynamic/"),
            ("Analysis.EpochWork", "Analysis/Dynamic/"),
            ("Analysis.EpochWorkTrace", "Analysis/Dynamic/"),
            ("Analysis.BestBeforeChangePerformanceAnalyzer`3", "Analysis/Dynamic/"),
            ("Analysis.BestBeforeChangePerformanceEntry`1", "Analysis/Dynamic/"),
            ("Analysis.BestBeforeChangePerformance`1", "Analysis/Dynamic/"),
        ]);
    }

    private static void AssertPublicTypeLocations(CSharpCompilation compilation, string repositoryRoot,
        string projectRoot, (string Name, string Folder)[] expectedTypes)
    {
        foreach (var (name, folder) in expectedTypes)
        {
            var type = compilation.Assembly.GetTypeByMetadataName("HEAL.HeuristicLib." + name);
            type.ShouldNotBeNull($"{name} must belong to {projectRoot} in its agreed namespace.");
            type.DeclaredAccessibility.ShouldBe(Accessibility.Public);
            var sourcePaths = type.DeclaringSyntaxReferences.Select(reference =>
                Path.GetRelativePath(repositoryRoot, reference.SyntaxTree.FilePath).Replace('\\', '/')).ToArray();
            sourcePaths.ShouldNotBeEmpty();
            sourcePaths.ShouldAllBe(path => path.StartsWith(projectRoot + folder, StringComparison.Ordinal));
        }
    }

    [Fact]
    public void MainAssembly_DoesNotReferenceExperimental() =>
        typeof(IAlgorithm<>).Assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ShouldNotContain("HEAL.HeuristicLib.Experimental");

    [Fact]
    public void SemanticCheck_CatchesAQualifiedForbiddenReference()
    {
        var repositoryRoot = FindRepositoryRoot();
        var (compilation, _) = CreateMainCompilation(repositoryRoot);
        var probe = CSharpSyntaxTree.ParseText(
            "namespace HEAL.HeuristicLib.Operators; internal class LayerProbe { private HEAL.HeuristicLib.Analysis.TraceAnalyzer<int, int>? analyzer; }",
            SourceParseOptions,
            Path.Combine(repositoryRoot, MainRoot, "Operators", "LayerProbe.cs"),
            cancellationToken: TestContext.Current.CancellationToken);
        compilation = compilation.AddSyntaxTrees(probe);

        ScanTree(compilation, probe, repositoryRoot).ShouldContain("src/HeuristicLib/Operators/LayerProbe.cs | Operators -> Analysis");
    }

    [Theory]
    [InlineData("Inspection", "Inspection")]
    [InlineData("Encodings/Permutations", "Encodings.Permutations")]
    [InlineData("Execution", "Execution")]
    public void SemanticCheck_KeepsInspectionEncodingsAndResolutionFreeOfAnalysis(string folder, string concept)
    {
        var repositoryRoot = FindRepositoryRoot();
        var (compilation, _) = CreateMainCompilation(repositoryRoot);
        var probe = CSharpSyntaxTree.ParseText(
            $"namespace HEAL.HeuristicLib.{concept}; internal class LayerProbe {{ private HEAL.HeuristicLib.Analysis.TraceAnalyzer<int, int>? analyzer; }}",
            SourceParseOptions,
            Path.Combine(repositoryRoot, MainRoot, folder, "LayerProbe.cs"),
            cancellationToken: TestContext.Current.CancellationToken);
        compilation = compilation.AddSyntaxTrees(probe);

        var kind = folder.Split('/')[0];
        ScanTree(compilation, probe, repositoryRoot).ShouldContain($"src/HeuristicLib/{folder}/LayerProbe.cs | {kind} -> Analysis");
    }

    [Fact]
    public void SemanticCheck_CatchesGlobalUsingAndInternalAccess()
    {
        var repositoryRoot = FindRepositoryRoot();
        var (compilation, _) = CreateMainCompilation(repositoryRoot);
        var probe = CSharpSyntaxTree.ParseText(
            "global using HEAL.HeuristicLib.Analysis; namespace HEAL.HeuristicLib.Operators; internal class LayerProbe { object Empty => new HEAL.HeuristicLib.Execution.RunResults(System.TimeProvider.System); }",
            SourceParseOptions,
            Path.Combine(repositoryRoot, MainRoot, "Operators", "LayerProbe.cs"),
            cancellationToken: TestContext.Current.CancellationToken);
        compilation = compilation.AddSyntaxTrees(probe);

        var violations = ScanTree(compilation, probe, repositoryRoot).ToArray();
        violations.ShouldContain("src/HeuristicLib/Operators/LayerProbe.cs | Operators -> Analysis");
        violations.ShouldContain("src/HeuristicLib/Operators/LayerProbe.cs | OutsideExecution -> ResolutionInternal");
    }

    [Fact]
    public void SemanticCheck_DistinguishesConcreteAlgorithmsAndEncodings()
    {
        var repositoryRoot = FindRepositoryRoot();
        var (compilation, _) = CreateMainCompilation(repositoryRoot);
        var hostProbe = CSharpSyntaxTree.ParseText(
            "namespace HEAL.HeuristicLib.Execution; internal class HostProbe { private HEAL.HeuristicLib.Algorithms.GeneticAlgorithm<int>? algorithm; object Empty => new RunResults(TimeProvider.System); }",
            SourceParseOptions,
            Path.Combine(repositoryRoot, MainRoot, "Execution", "Runs", "HostProbe.cs"),
            cancellationToken: TestContext.Current.CancellationToken);
        var objectiveProbe = CSharpSyntaxTree.ParseText(
            "namespace HEAL.HeuristicLib.Objectives; internal class ObjectiveProbe { private HEAL.HeuristicLib.Encodings.Permutations.Permutation? order; }",
            SourceParseOptions,
            Path.Combine(repositoryRoot, MainRoot, "Objectives", "ObjectiveProbe.cs"),
            cancellationToken: TestContext.Current.CancellationToken);
        compilation = compilation.AddSyntaxTrees(hostProbe, objectiveProbe);

        ScanTree(compilation, hostProbe, repositoryRoot)
            .ShouldContain("src/HeuristicLib/Execution/Runs/HostProbe.cs | Hosting -> ConcreteAlgorithm");
        ScanTree(compilation, hostProbe, repositoryRoot)
            .ShouldNotContain("src/HeuristicLib/Execution/Runs/HostProbe.cs | OutsideExecution -> ResolutionInternal");
        ScanTree(compilation, objectiveProbe, repositoryRoot)
            .ShouldContain("src/HeuristicLib/Objectives/ObjectiveProbe.cs | Domain -> Encodings");
    }

    [Theory]
    [InlineData("new RunResults(TimeProvider.System)")]
    [InlineData("ResolutionScope.Create(TimeProvider.System)")]
    public void SemanticCheckKeepsResultStorageOutOfContent(string operation)
    {
        var repositoryRoot = FindRepositoryRoot();
        var (compilation, _) = CreateMainCompilation(repositoryRoot);
        var probe = CSharpSyntaxTree.ParseText(
            $"namespace HEAL.HeuristicLib.Execution; internal class HostProbe {{ object Value => {operation}; }}",
            SourceParseOptions,
            Path.Combine(repositoryRoot, MainRoot, "Operators", "HostProbe.cs"),
            cancellationToken: TestContext.Current.CancellationToken);
        compilation = compilation.AddSyntaxTrees(probe);

        ScanTree(compilation, probe, repositoryRoot)
            .ShouldContain("src/HeuristicLib/Operators/HostProbe.cs | OutsideExecution -> ResolutionInternal");
    }

    [Fact]
    public void SemanticCheckAllowsPublicResultLookupInContent()
    {
        var repositoryRoot = FindRepositoryRoot();
        var (compilation, _) = CreateMainCompilation(repositoryRoot);
        var probe = CSharpSyntaxTree.ParseText(
            "namespace HEAL.HeuristicLib.Execution; internal class HostProbe(RunResults results, IConfigurationNode<IRunResult> analyzer) { object Value => results.Result(analyzer); }",
            SourceParseOptions,
            Path.Combine(repositoryRoot, MainRoot, "Operators", "HostProbe.cs"),
            cancellationToken: TestContext.Current.CancellationToken);
        compilation = compilation.AddSyntaxTrees(probe);

        ScanTree(compilation, probe, repositoryRoot)
            .ShouldNotContain("src/HeuristicLib/Operators/HostProbe.cs | OutsideExecution -> ResolutionInternal");
    }

    [Fact]
    public void SemanticCheck_AllowsPublicExecutionTypesInHosting()
    {
        var repositoryRoot = FindRepositoryRoot();
        var (compilation, _) = CreateMainCompilation(repositoryRoot);
        var probe = CSharpSyntaxTree.ParseText(
            "namespace HEAL.HeuristicLib.Execution; internal class HostProbe { private Substitution? substitution; }",
            SourceParseOptions,
            Path.Combine(repositoryRoot, MainRoot, "Execution", "Runs", "HostProbe.cs"),
            cancellationToken: TestContext.Current.CancellationToken);
        compilation = compilation.AddSyntaxTrees(probe);

        ScanTree(compilation, probe, repositoryRoot).ShouldBeEmpty();
    }

    [Fact]
    public void SemanticCheck_KeepsDomainVocabularyAtTheBottom()
    {
        var repositoryRoot = FindRepositoryRoot();
        var (compilation, _) = CreateMainCompilation(repositoryRoot);
        var probe = CSharpSyntaxTree.ParseText(
            "namespace HEAL.HeuristicLib.Random; internal class DomainProbe { private HEAL.HeuristicLib.Operators.IOperator? candidateOperator; private HEAL.HeuristicLib.Execution.ExecutionConcurrency? concurrency; private HEAL.HeuristicLib.Problems.IProblem? problem; }",
            SourceParseOptions,
            Path.Combine(repositoryRoot, MainRoot, "Random", "DomainProbe.cs"),
            cancellationToken: TestContext.Current.CancellationToken);
        var delegateProbe = CSharpSyntaxTree.ParseText(
            "namespace HEAL.HeuristicLib.Random; internal delegate HEAL.HeuristicLib.Operators.IOperator DomainFactory();",
            SourceParseOptions,
            Path.Combine(repositoryRoot, MainRoot, "Random", "DelegateProbe.cs"),
            cancellationToken: TestContext.Current.CancellationToken);
        compilation = compilation.AddSyntaxTrees(probe, delegateProbe);

        ScanTree(compilation, probe, repositoryRoot).ShouldBe(["src/HeuristicLib/Random/DomainProbe.cs | Domain -> Operators"]);
        ScanTree(compilation, delegateProbe, repositoryRoot).ShouldBe(["src/HeuristicLib/Random/DelegateProbe.cs | Domain -> Operators"]);
    }

    private static string[] FindViolations()
    {
        var repositoryRoot = FindRepositoryRoot();
        var (main, mainTrees) = CreateMainCompilation(repositoryRoot);
        main.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => diagnostic.ToString()).ShouldBeEmpty();
        var violations = mainTrees.SelectMany(tree => ScanTree(main, tree, repositoryRoot)).ToList();

        var (experimental, experimentalTrees) = CreateCompilation(repositoryRoot, "HeuristicLib.Experimental", includeMain: true);
        experimental.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => diagnostic.ToString()).ShouldBeEmpty();
        violations.AddRange(experimentalTrees.SelectMany(tree => ScanTree(experimental, tree, repositoryRoot)));

        return [.. violations.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
    }

    private static IEnumerable<string> ScanTree(CSharpCompilation compilation, SyntaxTree tree, string repositoryRoot)
    {
        var path = Path.GetRelativePath(repositoryRoot, tree.FilePath).Replace('\\', '/');
        var root = tree.GetRoot();
        var model = compilation.GetSemanticModel(tree);
        var sourceKind = SourceKind(path, root);
        var found = new HashSet<string>(StringComparer.Ordinal);

        foreach (var node in root.DescendantNodes())
        {
            if (node is not (SimpleNameSyntax or ObjectCreationExpressionSyntax or UsingDirectiveSyntax))
                continue;

            var symbol = model.GetSymbolInfo(node).Symbol;
            if (symbol is null && node is UsingDirectiveSyntax usingDirective && usingDirective.Name is not null)
                symbol = model.GetSymbolInfo(usingDirective.Name).Symbol;
            if (symbol is null)
                continue;

            var targetType = symbol as INamedTypeSymbol ?? symbol.ContainingType;
            var targetNamespace = (symbol as INamespaceSymbol)?.ToDisplayString() ??
                                  targetType?.ContainingNamespace.ToDisplayString() ?? string.Empty;

            if (sourceKind is "Operators" or "Algorithms" or "Problems" or "Encodings" or "Inspection" or "Execution" or "Hosting" &&
                targetNamespace.StartsWith("HEAL.HeuristicLib.Analysis", StringComparison.Ordinal))
                found.Add($"{path} | {sourceKind} -> Analysis");

            if (targetType is not null && IsDomainReference(node, model, targetType))
                found.Add($"{path} | Domain -> {Concept(targetType)}");

            if (sourceKind == "Hosting" && targetType is { TypeKind: TypeKind.Class, IsAbstract: false } &&
                targetNamespace.StartsWith("HEAL.HeuristicLib.Algorithms", StringComparison.Ordinal) &&
                targetType.AllInterfaces.Any(@interface => @interface.Name == "IAlgorithm"))
                found.Add($"{path} | Hosting -> ConcreteAlgorithm");

            if (path.StartsWith(MainRoot, StringComparison.Ordinal) && targetType is not null &&
                targetType.Locations.Any(location => location.IsInSource &&
                    location.SourceTree?.FilePath.Replace('\\', '/').Contains(ExperimentalRoot, StringComparison.Ordinal) == true))
                found.Add($"{path} | Main -> Experimental");

            if (IsResolutionInternal(symbol, targetType) &&
                sourceKind != "Hosting" &&
                (!path.StartsWith(MainRoot + "Execution/", StringComparison.Ordinal) ||
                 path.StartsWith(MainRoot + "Execution/Runs/", StringComparison.Ordinal) ||
                 path.StartsWith(MainRoot + "Execution/Concurrency/", StringComparison.Ordinal)))
                found.Add($"{path} | OutsideExecution -> ResolutionInternal");

            if (IsRunInternal(symbol, targetType) &&
                !path.StartsWith(MainRoot + "Execution/Runs/", StringComparison.Ordinal) &&
                !path.StartsWith(MainRoot + "Experiments/", StringComparison.Ordinal))
                found.Add($"{path} | OutsideHosting -> RunInternal");
        }

        return found;
    }

    // Domain vocabulary (L0) references only domain vocabulary. Classified by type, because Problems, SearchSpaces and
    // Execution also hold types of higher layers. Encodings are concrete representations, so none of them is L0.
    private static bool IsDomainReference(SyntaxNode node, SemanticModel model, INamedTypeSymbol targetType)
    {
        var declaration = node.Ancestors().LastOrDefault(ancestor => ancestor is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax);
        return declaration is not null && model.GetDeclaredSymbol(declaration) is INamedTypeSymbol sourceType &&
               IsDomain(sourceType) && IsLibrary(targetType) && !IsDomain(targetType);
    }

    private static bool IsDomain(INamedTypeSymbol type)
    {
        type = Outermost(type);
        var ns = type.ContainingNamespace.ToDisplayString();
        return ns switch
        {
            _ when InNamespace(ns, "SearchSpaces") => type.Name is not ("OperatorContractComposition" or
                "OperatorContractVerification" or "SearchSpaceCompatibility"),
            _ when InNamespace(ns, "Objectives") || InNamespace(ns, "Random") || InNamespace(ns, "Collections") ||
                   InNamespace(ns, "Numerics") => true,
            "HEAL.HeuristicLib.Execution" => type.Name is "ExecutionConcurrency" or "ExecutionConcurrencyKind" or "BatchExecution",
            "HEAL.HeuristicLib.Problems" => type.Name is "IProblem" or "IStochasticProblem" or "Problem" or "SingleSolutionProblem",
            _ => false,
        };
    }

    private static bool InNamespace(string ns, string concept) =>
        ns == "HEAL.HeuristicLib." + concept || ns.StartsWith("HEAL.HeuristicLib." + concept + ".", StringComparison.Ordinal);

    private static bool IsLibrary(INamedTypeSymbol type) =>
        Outermost(type).ContainingNamespace.ToDisplayString().StartsWith("HEAL.HeuristicLib.", StringComparison.Ordinal);

    private static INamedTypeSymbol Outermost(INamedTypeSymbol type)
    {
        while (type.ContainingType is { } containing)
            type = containing;
        return type;
    }

    private static string Concept(INamedTypeSymbol type) =>
        Outermost(type).ContainingNamespace.ToDisplayString().Split('.').ElementAtOrDefault(2) ?? string.Empty;

    private static bool IsResolutionInternal(ISymbol symbol, INamedTypeSymbol? type)
    {
        if (type is null)
            return false;
        type = Outermost(type);
        return type.ContainingNamespace.ToDisplayString() == "HEAL.HeuristicLib.Execution" &&
               type.Name is ("ResolutionScope" or "RunResults") &&
               (symbol.DeclaredAccessibility == Accessibility.Internal || type.DeclaredAccessibility == Accessibility.Internal);
    }

    private static bool IsRunInternal(ISymbol symbol, INamedTypeSymbol? type) =>
        type?.Name.StartsWith("AlgorithmRun", StringComparison.Ordinal) == true &&
        symbol.Name is "StreamForTerminalCancellation" or "CancelAsync" &&
        symbol.DeclaredAccessibility == Accessibility.Internal;

    private static string SourceKind(string path, SyntaxNode root)
    {
        if (path.StartsWith(MainRoot + "Operators/", StringComparison.Ordinal) ||
            path.StartsWith(ExperimentalRoot + "Operators/", StringComparison.Ordinal))
            return "Operators";
        if (path.StartsWith(MainRoot + "Algorithms/", StringComparison.Ordinal) ||
            path.StartsWith(ExperimentalRoot + "Algorithms/", StringComparison.Ordinal))
            return "Algorithms";
        if (path.StartsWith(MainRoot + "Problems/", StringComparison.Ordinal) ||
            path.StartsWith(ExperimentalRoot + "Problems/", StringComparison.Ordinal))
            return "Problems";
        if (path.StartsWith(MainRoot + "Encodings/", StringComparison.Ordinal) ||
            path.StartsWith(ExperimentalRoot + "Encodings/", StringComparison.Ordinal))
            return "Encodings";
        if (path.StartsWith(MainRoot + "Inspection/", StringComparison.Ordinal))
            return "Inspection";
        if (path.StartsWith(MainRoot + "Execution/Runs/", StringComparison.Ordinal))
            return "Hosting";
        // Resolution, substitution and composition contracts; hosting is classified above.
        if (path.StartsWith(MainRoot + "Execution/", StringComparison.Ordinal))
            return "Execution";
        if (path.StartsWith(MainRoot + "Experiments/", StringComparison.Ordinal) &&
            !root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>()
                .Any(declaration => declaration.Name.ToString() == "HEAL.HeuristicLib.Analysis"))
            return "Hosting";
        return "Other";
    }
}
