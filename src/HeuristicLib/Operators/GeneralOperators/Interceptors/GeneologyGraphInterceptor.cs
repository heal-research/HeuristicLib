using System.Reflection.Emit;
using System.Text;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public static class GenealogyGraph {
  public static GenealogyGraphInterceptor<TGenotype> GetInterceptor<TGenotype>(this GenealogyGraph<TGenotype> graph) where TGenotype : notnull => new(graph);

  public static GenealogyGraphCrossover<TGenotype, TEncoding, TProblem> WrapCrossover<TGenotype, TEncoding, TProblem>(this GenealogyGraph<TGenotype> graph, ICrossover<TGenotype, TEncoding, TProblem> crossover)
    where TEncoding : class, IEncoding<TGenotype>
    where TProblem : class, IProblem<TGenotype, TEncoding>
    where TGenotype : notnull => new(crossover, graph);

  public static GenealogyGraphMutator<TGenotype, TEncoding, TProblem> WrapMutator<TGenotype, TEncoding, TProblem>(this GenealogyGraph<TGenotype> graph, IMutator<TGenotype, TEncoding, TProblem> mutator)
    where TEncoding : class, IEncoding<TGenotype>
    where TProblem : class, IProblem<TGenotype, TEncoding>
    where TGenotype : notnull => new(mutator, graph);
}

public class GenealogyGraph<TGenotype> where TGenotype : notnull {
  private int nextId;

  public class Node(int id, TGenotype value, int generation, int layer, int rank) {
    public readonly int Id = id;
    public readonly HashSet<Node> Children = [];
    public readonly HashSet<Node> Parents = [];
    public readonly int Generation = generation;
    public readonly int Layer = layer;
    public readonly TGenotype Value = value;
    public readonly int Rank = rank;

    public HashSet<Node> GetAllDescendants() {
      return Children.SelectMany(x => x.GetAllDescendants()).Append(Children).ToHashSet();
    }

    public HashSet<Node> GetAllAncestors() {
      return Parents.SelectMany(x => x.GetAllAncestors()).Append(Children).ToHashSet();
    }
  }

  public Dictionary<TGenotype, Node> CurrentGeneration => Nodes[^1];
  public readonly List<Dictionary<TGenotype, Node>> Nodes = [];
  private readonly IEqualityComparer<TGenotype> equality;

  public GenealogyGraph(IEqualityComparer<TGenotype> equality) {
    Nodes.Add(new Dictionary<TGenotype, Node>(equality));
    this.equality = equality;
  }

  public void AddConnection(ICollection<TGenotype> parent, TGenotype child) {
    if (CurrentGeneration.TryGetValue(child, out var cNode) && parent.Any(x => equality.Equals(x, child))) return; //operators sometimes just "give up" and return one of the parents as child

    if (CurrentGeneration.ContainsKey(child)) { }

    var pNodes = parent.Where(x => !equality.Equals(x, child)).Select(x => CurrentGeneration[x]).ToArray();
    cNode = new Node(nextId++, child, Nodes.Count - 1, pNodes.Max(x => x.Layer) + 1, -1);
    CurrentGeneration.Add(child, cNode);
    foreach (var p in pNodes) {
      p.Children.Add(cNode);
      cNode.Parents.Add(p);
    }
  }

  public void SetAsNewGeneration(IEnumerable<TGenotype> survivors, bool saveSpace = false) {
    var newGen = new Dictionary<TGenotype, Node>(CurrentGeneration.Comparer);
    var rank = 0;
    foreach (var survivor in survivors) {
      var newNode = new Node(nextId++, survivor, Nodes.Count, 0, rank++);
      newGen[survivor] = newNode;
      if (!CurrentGeneration.TryGetValue(survivor, out var oldNode))
        continue;
      oldNode.Children.Add(newNode);
      newNode.Parents.Add(oldNode);
    }

    //only keep parents for the last generation to save space
    if (saveSpace && Nodes.Count > 1) {
      foreach (var node in CurrentGeneration.Values)
        node.Parents.Clear();
      Nodes[^1].Clear();
    }

    Nodes.Add(newGen);
  }

  public string ToGraphViz() {
    StringBuilder sb = new();
    sb.AppendLine("digraph G {");
    sb.AppendLine("rankdir=TB;");

    var elites = new List<Node>();

    foreach (var (genId, gen) in Nodes.Select((x, i) => (i, x))) {
      if (gen.Count == 0)
        continue;
      sb.AppendLine($"subgraph cluster_gen{genId} {{");
      sb.AppendLine($"label=\"Generation {genId}\"");
      foreach (var nl in gen.Values.GroupBy(x => x.Layer).OrderBy(x => x.Key)) {
        sb.AppendLine($"subgraph cluster_gen{genId}_{nl.Key} {{");
        sb.AppendLine($"label=\"\"");
        if (nl.Key != 0)
          sb.AppendLine("style=invis;");
        else
          sb.AppendLine("style=solid;");
        sb.AppendLine("{");
        sb.AppendLine("rank=same;");

        if (nl.Key != 0) {
          foreach (var t in nl)
            sb.AppendLine($"\"{t.Id}\"");
        } else {
          var ranked = nl.Where(x => x.Rank != -1).OrderBy(x => x.Rank).ToArray();
          elites.Add(ranked[0]);
          foreach (var t in ranked)
            sb.AppendLine($"\"{t.Id}\"");

          //set invisible edges to help ranked-layout
          for (int i = 0; i < ranked.Length - 1; i++)
            sb.AppendLine($"\"{ranked[i].Id}\"->\"{ranked[i + 1].Id}\" [style=invis]");
        }

        sb.AppendLine("}");
        sb.AppendLine("}");
      }

      sb.AppendLine("}");
    }

    foreach (var node in Nodes.SelectMany(x => x.Values)) {
      foreach (var nodeParent in node.Parents) {
        sb.AppendLine($"\"{nodeParent.Id}\"->\"{node.Id}\"");
      }
    }

    sb.AppendLine("}");
    return sb.ToString();
  }
}

public class GenealogyGraphInterceptor<T1>(GenealogyGraph<T1> graph) : Interceptor<T1, PopulationIterationResult<T1>, IEncoding<T1>, IProblem<T1, IEncoding<T1>>>
  where T1 : notnull {
  /// <summary>
  /// forget all but the last generation to save space
  /// </summary>
  public bool SaveSpace { get; set; } = true;

  public override PopulationIterationResult<T1> Transform(PopulationIterationResult<T1> currentIterationResult, PopulationIterationResult<T1>? previousIterationResult, IEncoding<T1> encoding, IProblem<T1, IEncoding<T1>> problem) {
    var ordered = currentIterationResult.Population.OrderBy(x => x.ObjectiveVector, problem.Objective.TotalOrderComparer).ToArray();
    graph.SetAsNewGeneration(ordered.Select(x => x.Genotype), SaveSpace);
    return currentIterationResult;
  }
}

public class GenealogyGraphCrossover<T1, T2, T3>(ICrossover<T1, T2, T3> internalCrossover, GenealogyGraph<T1> graph) : ICrossover<T1, T2, T3>
  where T2 : class, IEncoding<T1>
  where T3 : class, IProblem<T1, T2>
  where T1 : notnull {
  public IReadOnlyList<T1> Cross(IReadOnlyList<(T1, T1)> parents, IRandomNumberGenerator random, T2 encoding, T3 problem) {
    var res = internalCrossover.Cross(parents, random, encoding, problem);
    foreach (var (parents1, child) in parents.Zip(res))
      graph.AddConnection([parents1.Item1, parents1.Item2], child);
    return res;
  }
}

public class GenealogyGraphMutator<T1, T2, T3>(IMutator<T1, T2, T3> internalMutator, GenealogyGraph<T1> graph) : IMutator<T1, T2, T3>
  where T2 : class, IEncoding<T1>
  where T3 : class, IProblem<T1, T2>
  where T1 : notnull {
  public IReadOnlyList<T1> Mutate(IReadOnlyList<T1> parent, IRandomNumberGenerator random, T2 encoding, T3 problem) {
    var res = internalMutator.Mutate(parent, random, encoding, problem);
    foreach (var (parents1, child) in parent.Zip(res))
      graph.AddConnection([parents1], child);
    return res;
  }
}
