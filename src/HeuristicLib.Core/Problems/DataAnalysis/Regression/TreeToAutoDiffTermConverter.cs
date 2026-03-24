using AutoDiff;
using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math.Variables;
using Variable = HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math.Variable;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

public class TreeToAutoDiffTermConverter
{
  public delegate double CompiledModel(double[] vars, double[] @params);

  public delegate Tuple<double[], double> CompiledModelGradient(double[] vars, double[] @params);

  private readonly HashSet<SymbolicExpressionTreeNode> excludedNodes;

  // state for recursive transformation of trees 
  private readonly List<double> initialParamValues;
  private readonly bool makeVariableWeightsVariable;
  private readonly Dictionary<DataForVariable, AutoDiff.Variable> parameters;
  private readonly List<AutoDiff.Variable> variables;

  private TreeToAutoDiffTermConverter(bool makeVariableWeightsVariable, IEnumerable<SymbolicExpressionTreeNode> excludedNodes)
  {
    this.makeVariableWeightsVariable = makeVariableWeightsVariable;
    this.excludedNodes = [.. excludedNodes];

    initialParamValues = [];
    parameters = [];
    variables = [];
  }

  public static bool TryConvertToAutoDiff(SymbolicExpressionTree tree, bool makeVariableWeightsVariable,
                                          out List<DataForVariable>? parameters, out double[]? initialParamValues,
                                          out CompiledModel? func,
                                          out CompiledModelGradient? funcGrad)
    => TryConvertToAutoDiff(tree, makeVariableWeightsVariable, [], out parameters, out initialParamValues, out func, out funcGrad);

  public static bool TryConvertToAutoDiff(SymbolicExpressionTree tree, bool makeVariableWeightsVariable, IEnumerable<SymbolicExpressionTreeNode> excludedNodes,
                                          out List<DataForVariable>? parameters, out double[]? initialParamValues,
                                          out CompiledModel? func,
                                          out CompiledModelGradient? funcGrad)
  {
    // use a transformator object which holds the state (variable list, parameter list, ...) for recursive transformation of the tree
    var transformator = new TreeToAutoDiffTermConverter(makeVariableWeightsVariable, excludedNodes);
    try {
      var term = transformator.ConvertToAutoDiff(tree.Root[0]);
      var parameterEntries = transformator.parameters.ToArray(); // guarantee same order for keys and values
      var compiledTerm = term.Compile(transformator.variables.ToArray(),
        parameterEntries.Select(kvp => kvp.Value).ToArray());
      parameters = [.. parameterEntries.Select(kvp => kvp.Key)];
      initialParamValues = transformator.initialParamValues.ToArray();
      func = compiledTerm.Evaluate;
      funcGrad = compiledTerm.Differentiate;

      return true;
    } catch (NotSupportedException) {
      func = null;
      funcGrad = null;
      parameters = null;
      initialParamValues = null;
    }

    return false;
  }

  private Term CreateWeightedTerm(Term parameterTerm, double initialWeight, bool optimizeWeight)
  {
    if (!optimizeWeight)
      return initialWeight * parameterTerm;
    var wVar = new AutoDiff.Variable();
    variables.Add(wVar);
    initialParamValues.Add(initialWeight);
    return TermBuilder.Product(wVar, parameterTerm);
  }

  private AutoDiff.Variable CreateWeightTerm(double initialWeight)
  {
    var wVar = new AutoDiff.Variable();
    variables.Add(wVar);
    initialParamValues.Add(initialWeight);
    return wVar;
  }

  private Term ConvertToAutoDiff(SymbolicExpressionTreeNode node)
  {
    switch (node.Symbol) {
      case Number: {
        var numberTreeNode = (NumberTreeNode)node;
        return CreateWeightTerm(numberTreeNode.Value);
      }
      case Variable:
      case BinaryFactorVariable: {
        var varNode = (VariableTreeNodeBase)node;
        // factor variable values are only 0 or 1 and set in x accordingly
        var varValue = node is BinaryFactorVariableTreeNode factorVarNode ? factorVarNode.VariableValue : string.Empty;
        var par = FindOrCreateParameter(parameters, varNode.VariableName, varValue);
        var optimizeWeight = makeVariableWeightsVariable && !excludedNodes.Contains(node);
        return CreateWeightedTerm(par, varNode.Weight, optimizeWeight);
      }
      case FactorVariable: {
        var factorVarNode = (FactorVariableTreeNode)node;
        var optimizeWeight = makeVariableWeightsVariable && !excludedNodes.Contains(node);
        var products = factorVarNode.Symbol.GetVariableValues(factorVarNode.VariableName).Select(variableValue => {
          var par = FindOrCreateParameter(parameters, factorVarNode.VariableName, variableValue);
          return CreateWeightedTerm(par, factorVarNode.GetValue(variableValue), optimizeWeight);
        });
        return TermBuilder.Sum(products);
      }
      case LaggedVariable: {
        var varNode = (LaggedVariableTreeNode)node;
        var par = FindOrCreateParameter(parameters, varNode.VariableName, string.Empty, varNode.Lag);
        var optimizeWeight = makeVariableWeightsVariable && !excludedNodes.Contains(node);
        return CreateWeightedTerm(par, varNode.Weight, optimizeWeight);
      }
      case Addition: {
        var s = node.Subtrees.Select(ConvertToAutoDiff).ToArray();
        return TermBuilder.Sum(s);
      }
      case Subtraction: {
        var terms = new List<Term>();
        for (var i = 0; i < node.SubtreeCount; i++) {
          var t = ConvertToAutoDiff(node[i]);
          if (i > 0) {
            t = -t;
          }

          terms.Add(t);
        }

        return terms.Count == 1 ? -terms[0] : TermBuilder.Sum(terms);
      }
      case Multiplication: {
        var terms = node.Subtrees.Select(ConvertToAutoDiff).ToList();
        return terms.Count == 1 ? terms[0] : terms.Aggregate((a, b) => new Product(a, b));
      }
      case Division: {
        var terms = node.Subtrees.Select(ConvertToAutoDiff).ToList();
        if (terms.Count == 1) {
          return 1.0 / terms[0];
        }

        return terms.Aggregate((a, b) => new Product(a, 1.0 / b));
      }
      case Absolute: {
        var x1 = ConvertToAutoDiff(node[0]);

        return Abs(x1);
      }
      case AnalyticQuotient: {
        var x1 = ConvertToAutoDiff(node[0]);
        var x2 = ConvertToAutoDiff(node[1]);

        return x1 / TermBuilder.Power(1 + x2 * x2, 0.5);
      }
      case Logarithm:
        return TermBuilder.Log(
          ConvertToAutoDiff(node[0]));
      case Exponential:
        return TermBuilder.Exp(
          ConvertToAutoDiff(node[0]));
      case Square:
        return TermBuilder.Power(
          ConvertToAutoDiff(node[0]), 2.0);
      case SquareRoot:
        return TermBuilder.Power(
          ConvertToAutoDiff(node[0]), 0.5);
      case Cube:
        return TermBuilder.Power(
          ConvertToAutoDiff(node[0]), 3.0);
      case CubeRoot:
        return Cbrt(ConvertToAutoDiff(node[0]));
      case Power: {
        if (node[1] is not NumberTreeNode powerNode)
          throw new NotSupportedException("Only numeric powers are allowed in parameter optimization. Try to use exp() and log() instead of the power symbol.");
        var intPower = Math.Truncate(powerNode.Value);
        return Math.Abs(intPower - powerNode.Value) > 1e-15 ? throw new NotSupportedException("Only integer powers are allowed in parameter optimization. Try to use exp() and log() instead of the power symbol.") : TermBuilder.Power(ConvertToAutoDiff(node[0]), intPower);
      }
      case Sine:
        return Sin(
          ConvertToAutoDiff(node[0]));
      case Cosine:
        return Cos(
          ConvertToAutoDiff(node[0]));
      case Tangent:
        return Tan(
          ConvertToAutoDiff(node[0]));
      case HyperbolicTangent:
        return Tanh(
          ConvertToAutoDiff(node[0]));
      case SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math.Erf:
        return Erf(
          ConvertToAutoDiff(node[0]));
      case SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math.Norm:
        return Norm(
          ConvertToAutoDiff(node[0]));
      case StartSymbol:
      case SubFunctionSymbol:
        return ConvertToAutoDiff(node[0]);
      default:
        throw new NotSupportedException($"could not convert tree because symbol {node.Symbol} is not supported");
    }
  }

  // for each factor variable value we need a parameter which represents a binary indicator for that variable & value combination
  // each binary indicator is only necessary once. So we only create a parameter if this combination is not yet available
  private static Term FindOrCreateParameter(Dictionary<DataForVariable, AutoDiff.Variable> parameters, string varName, string varValue = "", int lag = 0)
  {
    var data = new DataForVariable(varName, varValue, lag);

    if (parameters.TryGetValue(data, out var par)) {
      return par;
    }

    // not found -> create new parameter and entries in names and values lists
    par = new AutoDiff.Variable();
    parameters.Add(data, par);

    return par;
  }

  public static bool IsCompatible(SymbolicExpressionTree tree) => tree.Root[0].IterateNodesPrefix().All(n => IsSupportedSymbol(n.Symbol));

  public static bool IsSupportedSymbol(Symbol symbol) =>
    symbol is Variable or
      BinaryFactorVariable or
      FactorVariable or
      LaggedVariable or
      Number or
      Addition or
      Subtraction or
      Multiplication or
      Division or
      Logarithm or
      Exponential or
      SquareRoot or
      Square or
      Sine or
      Cosine or
      Tangent or
      HyperbolicTangent or
      SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math.Erf or
      SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math.Norm or
      StartSymbol or
      Absolute or
      AnalyticQuotient or
      Cube or
      CubeRoot or
      Power or
      SubFunctionSymbol;

  #region helper class
  public readonly record struct DataForVariable(string VariableName, string VariableValue, int Lag);
  #endregion

  #region exception class
  private static readonly Func<Term, UnaryFunc> Sin = UnaryFunc.Factory(
    Math.Sin,
    Math.Cos);

  private static readonly Func<Term, UnaryFunc> Cos = UnaryFunc.Factory(
    Math.Cos,
    diff: x => -Math.Sin(x));

  private static readonly Func<Term, UnaryFunc> Tan = UnaryFunc.Factory(
    Math.Tan,
    diff: x => 1 + Math.Tan(x) * Math.Tan(x));

  private static readonly Func<Term, UnaryFunc> Tanh = UnaryFunc.Factory(
    Math.Tanh,
    diff: x => 1 - Math.Tanh(x) * Math.Tanh(x));

  private static readonly Func<Term, UnaryFunc> Erf = UnaryFunc.Factory(
    eval: _ => throw new NotImplementedException("Error function is currently not implemented"), // errorfunction,
    diff: x => 2.0 * Math.Exp(-(x * x)) / Math.Sqrt(Math.PI));

  private static readonly Func<Term, UnaryFunc> Norm = UnaryFunc.Factory(
    eval: _ => throw new NotImplementedException("Normal distribution function is currently not implemented"), // normaldistribution,
    diff: x => -(Math.Exp(-(x * x)) * Math.Sqrt(Math.Exp(x * x)) * x) / Math.Sqrt(2 * Math.PI));

  private static readonly Func<Term, UnaryFunc> Abs = UnaryFunc.Factory(
    Math.Abs,
    diff: x => Math.Sign(x)
  );

  private static readonly Func<Term, UnaryFunc> Cbrt = UnaryFunc.Factory(
    eval: x => x < 0 ? -Math.Pow(-x, 1.0 / 3) : Math.Pow(x, 1.0 / 3),
    diff: x => {
      var cbrtX = x < 0 ? -Math.Pow(-x, 1.0 / 3) : Math.Pow(x, 1.0 / 3);

      return 1.0 / (3 * cbrtX * cbrtX);
    }
  );
  #endregion
}
