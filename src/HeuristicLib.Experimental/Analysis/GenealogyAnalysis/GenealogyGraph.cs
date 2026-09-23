using System.Text;

namespace HEAL.HeuristicLib.Analysis.GenealogyAnalysis;

/// <summary>
/// The descent graph of a run, built one generation at a time.
/// </summary>
/// <remarks>
/// This is an accumulator: one mutable graph that grows while the run executes, and reading it means reading that
/// graph. Everything it exposes is a live view, so read it once the run has finished rather than during it. Its own
/// writes take a lock, which is what lets one analyzer build a graph from several runs at once.
/// </remarks>
public class GenealogyGraph<TCandidate> where TCandidate : notnull
{
    private readonly Lock sync = new();
    private readonly List<Dictionary<TCandidate, Node>> nodes = [];
    private readonly IEqualityComparer<TCandidate> equality;
    private int nextId;

    public GenealogyGraph(IEqualityComparer<TCandidate> equality)
    {
        nodes.Add(new Dictionary<TCandidate, Node>(equality));
        this.equality = equality;
    }

    /// <summary>How many generations the graph holds, including the one in progress.</summary>
    public int GenerationCount { get { lock (sync) return nodes.Count; } }

    /// <summary>The nodes of one generation, counting from the oldest.</summary>
    public IReadOnlyCollection<Node> Generation(int index)
    {
        lock (sync)
            return nodes[index].Values;
    }

    /// <summary>The generation being built.</summary>
    public IReadOnlyCollection<Node> CurrentGeneration
    {
        get { lock (sync) return nodes[^1].Values; }
    }

    public void AddConnection(ICollection<TCandidate> parent, TCandidate child)
    {
        lock (sync)
        {
            var current = nodes[^1];
            if (current.ContainsKey(child) && parent.Any(x => equality.Equals(x, child)))
            {
                return; // operators sometimes just "give up" and return one of the parents as child
            }

            var parentNodes = parent.Where(x => !equality.Equals(x, child))
                                    .Select(x => current.TryGetValue(x, out var node) ? node : null)
                                    .Where(x => x is not null)
                                    .Cast<Node>()
                                    .ToArray();
            var layer = parentNodes.Length > 0 ? parentNodes.Max(x => x.Layer) + 1 : 1;
            var childNode = new Node(nextId++, child, nodes.Count - 1, layer, -1);
            current.Add(child, childNode);
            foreach (var parentNode in parentNodes)
                parentNode.Link(childNode);
        }
    }

    public void SetAsNewGeneration(IEnumerable<TCandidate> survivors, bool saveSpace = false)
    {
        lock (sync)
        {
            var current = nodes[^1];
            var newGeneration = new Dictionary<TCandidate, Node>(current.Comparer);
            var rank = 0;
            foreach (var survivor in survivors)
            {
                var newNode = new Node(nextId++, survivor, nodes.Count, 0, rank++);
                newGeneration[survivor] = newNode;
                if (current.TryGetValue(survivor, out var oldNode))
                    oldNode.Link(newNode);
            }

            // only keep parents for the last generation to save space
            if (saveSpace && nodes.Count > 1)
            {
                foreach (var node in current.Values.Where(x => x.Layer == 0))
                    node.ForgetParents();

                nodes[^2].Clear();
            }

            nodes.Add(newGeneration);
        }
    }

    /// <summary>
    /// For every candidate of the generation before the one in progress, the average rank of its descendants.
    /// A candidate without a ranked descendant contributes <see cref="double.NaN"/>. Empty while the graph holds
    /// fewer than two generations.
    /// </summary>
    internal ImmutableArray<double> AverageDescendantRanksOfPreviousGeneration()
    {
        lock (sync)
        {
            if (nodes.Count < 2)
                return [];

            var averages = ImmutableArray.CreateBuilder<double>();
            foreach (var survivor in nodes[^2].Values.Where(node => node.Layer == 0).OrderBy(node => node.Rank))
            {
                var ranks = survivor.Descendants().Where(node => node.Rank >= 0).Select(node => (double)node.Rank);
                averages.Add(ranks.DefaultIfEmpty(double.NaN).Average());
            }

            return averages.ToImmutable();
        }
    }

    /// <summary>
    /// Renders the graph in the DOT language. Ranked survivors sit on one row per generation, and a node drawn as a
    /// double circle carries the value of a survivor it descends from unchanged.
    /// </summary>
    public string ToGraphViz()
    {
        lock (sync)
        {
            StringBuilder sb = new();
            sb.AppendLine("digraph G {");
            sb.AppendLine("rankdir=TB;");

            foreach (var (generationId, generation) in nodes.Select((x, i) => (i, x.Values)))
            {
                if (generation.Count == 0)
                {
                    continue;
                }

                sb.AppendLine($"subgraph cluster_gen{generationId} {{");
                sb.AppendLine($"label=\"Generation {generationId}\"");
                foreach (var layer in generation.GroupBy(x => x.Layer).OrderBy(x => x.Key))
                {
                    sb.AppendLine($"subgraph cluster_gen{generationId}_{layer.Key} {{");
                    sb.AppendLine("label=\"\"");
                    sb.AppendLine(layer.Key != 0 ? "style=invis;" : "style=solid;");
                    sb.AppendLine("{");
                    sb.AppendLine("rank=same;");

                    if (layer.Key != 0)
                    {
                        foreach (var node in layer)
                        {
                            sb.AppendLine($"\"{node.Id}\" [label = {node.Id}, shape={Shape(node)}]");
                        }
                    }
                    else
                    {
                        var ranked = layer.Where(x => x.Rank != -1).OrderBy(x => x.Rank).ToArray();
                        foreach (var node in ranked)
                        {
                            sb.AppendLine($"\"{node.Id}\" [label = {node.Id}, shape={Shape(node)}]");
                        }

                        // set invisible edges to help ranked-layout
                        for (var i = 0; i < ranked.Length - 1; i++)
                        {
                            sb.AppendLine($"\"{ranked[i].Id}\"->\"{ranked[i + 1].Id}\" [style=invis]");
                        }
                    }

                    sb.AppendLine("}");
                    sb.AppendLine("}");
                }

                sb.AppendLine("}");
            }

            foreach (var node in nodes.SelectMany(x => x.Values))
            {
                foreach (var parent in node.Parents)
                {
                    sb.AppendLine($"\"{parent.Id}\"->\"{node.Id}\"");
                }
            }

            sb.AppendLine("}");
            return sb.ToString();
        }
    }

    private string Shape(Node node) =>
        node.Parents.Any(parent => equality.Equals(parent.Value, node.Value) && parent.Layer == 0)
            ? "doublecircle"
            : "circle";

    /// <summary>
    /// One candidate in the graph. Its links are live: the graph adds to them while the run builds it.
    /// </summary>
    public sealed class Node(int id, TCandidate value, int generation, int layer, int rank)
    {
        private readonly HashSet<Node> children = [];
        private readonly HashSet<Node> parents = [];

        public int Id { get; } = id;
        public TCandidate Value { get; } = value;
        public int Generation { get; } = generation;
        public int Layer { get; } = layer;
        public int Rank { get; } = rank;

        public IReadOnlyCollection<Node> Children => children;
        public IReadOnlyCollection<Node> Parents => parents;

        /// <summary>Every node reachable by following children, breadth first and each one once.</summary>
        public IEnumerable<Node> Descendants() => Reachable(node => node.children);

        /// <summary>Every node reachable by following parents, breadth first and each one once.</summary>
        public IEnumerable<Node> Ancestors() => Reachable(node => node.parents);

        internal void Link(Node child)
        {
            children.Add(child);
            child.parents.Add(this);
        }

        internal void ForgetParents() => parents.Clear();

        private IEnumerable<Node> Reachable(Func<Node, HashSet<Node>> step)
        {
            var seen = new HashSet<Node> { this };
            var queue = new Queue<Node>();
            queue.Enqueue(this);

            while (queue.TryDequeue(out var current))
            {
                foreach (var next in step(current))
                {
                    if (!seen.Add(next))
                        continue;

                    yield return next;
                    queue.Enqueue(next);
                }
            }
        }
    }
}
