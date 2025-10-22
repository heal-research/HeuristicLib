using System.Text;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public static class GenealogyGraph {
  public static GenealogyGraphAnalyzer<TGenotype> GetInterceptor<TGenotype>(this GenealogyGraph<TGenotype> graph) where TGenotype : notnull => new(graph);

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
      foreach (var node in Nodes[^1].Values.Where(x => x.Layer == 0))
        node.Parents.Clear();
      Nodes[^2].Clear();
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
        sb.AppendLine("label=\"\"");
        sb.AppendLine(nl.Key != 0 ? "style=invis;" : "style=solid;");
        sb.AppendLine("{");
        sb.AppendLine("rank=same;");

        if (nl.Key != 0) {
          foreach (var t in nl) {
            var shape = t.Parents.Any(x => equality.Equals(x.Value, t.Value) && x.Layer == 0) ? "doublecircle" : "circle";
            sb.AppendLine($"\"{t.Id}\" [label = {t.Id}, shape={shape}]");
          }
        } else {
          var ranked = nl.Where(x => x.Rank != -1).OrderBy(x => x.Rank).ToArray();
          elites.Add(ranked[0]);
          foreach (var t in ranked) {
            var shape = t.Parents.Any(x => equality.Equals(x.Value, t.Value) && x.Layer == 0) ? "doublecircle" : "circle";
            sb.AppendLine($"\"{t.Id}\" [label = {t.Id}, shape={shape}]");
          }

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
