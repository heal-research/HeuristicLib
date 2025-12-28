using AutoDiff;
using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math.Variables;
using Variable = HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math.Variable;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

public class TreeToAutoDiffTermConverter {
  public delegate double ParametricFunction(double[] vars, double[] @params);

  public delegate Tuple<double[], double> ParametricFunctionGradient(double[] vars, double[] @params);

  #region helper class
  public class DataForVariable(string varName, string varValue, int lag) {
    public readonly string VariableName = varName;
    public readonly string VariableValue = varValue; // for factor vars
    public readonly int Lag = lag;

    public override bool Equals(object? obj) {
      if (obj is not DataForVariable other) return false;
      return other.VariableName.Equals(VariableName) &&
             other.VariableValue.Equals(VariableValue) &&
             other.Lag == Lag;
    }

    public override int GetHashCode() {
      return VariableName.GetHashCode() ^ VariableValue.GetHashCode() ^ Lag;
    }
  }
  #endregion

  #region derivations of functions
  // create function factory for arctangent
  private static readonly Func<Term, UnaryFunc> Arctan = UnaryFunc.Factory(
    eval: Math.Atan,
    diff: x => 1 / (1 + x * x));

  private static readonly Func<Term, UnaryFunc> Sin = UnaryFunc.Factory(
    eval: Math.Sin,
    diff: Math.Cos);

  private static readonly Func<Term, UnaryFunc> Cos = UnaryFunc.Factory(
    eval: Math.Cos,
    diff: x => -Math.Sin(x));

  private static readonly Func<Term, UnaryFunc> Tan = UnaryFunc.Factory(
    eval: Math.Tan,
    diff: x => 1 + Math.Tan(x) * Math.Tan(x));
  private static readonly Func<Term, UnaryFunc> Tanh = UnaryFunc.Factory(
    eval: Math.Tanh,
    diff: x => 1 - Math.Tanh(x) * Math.Tanh(x));
  private static readonly Func<Term, UnaryFunc> Erf = UnaryFunc.Factory(
    eval: _ => throw new NotImplementedException("Error function is currently not implemented"), //errorfunction,
    diff: x => 2.0 * Math.Exp(-(x * x)) / Math.Sqrt(Math.PI));

  private static readonly Func<Term, UnaryFunc> Norm = UnaryFunc.Factory(
    eval: _ => throw new NotImplementedException("Normal distribution function is currently not implemented"), //normaldistribution,
    diff: x => -(Math.Exp(-(x * x)) * Math.Sqrt(Math.Exp(x * x)) * x) / Math.Sqrt(2 * Math.PI));

  private static readonly Func<Term, UnaryFunc> Abs = UnaryFunc.Factory(
    eval: Math.Abs,
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

  public static bool TryConvertToAutoDiff(SymbolicExpressionTree tree, bool makeVariableWeightsVariable,
                                          out List<DataForVariable>? parameters, out double[]? initialParamValues,
                                          out ParametricFunction? func,
                                          out ParametricFunctionGradient? funcGrad) {
    return TryConvertToAutoDiff(tree, makeVariableWeightsVariable, [],
      out parameters, out initialParamValues, out func, out funcGrad);
  }

  public static bool TryConvertToAutoDiff(SymbolicExpressionTree tree, bool makeVariableWeightsVariable, IEnumerable<SymbolicExpressionTreeNode> excludedNodes,
                                          out List<DataForVariable>? parameters, out double[]? initialParamValues,
                                          out ParametricFunction? func,
                                          out ParametricFunctionGradient? funcGrad) {
    // use a transformator object which holds the state (variable list, parameter list, ...) for recursive transformation of the tree
    var transformator = new TreeToAutoDiffTermConverter(makeVariableWeightsVariable, excludedNodes);
    try {
      var term = transformator.ConvertToAutoDiff(tree.Root[0]);
      var parameterEntries = transformator.parameters.ToArray(); // guarantee same order for keys and values
      var compiledTerm = term.Compile(transformator.variables.ToArray(),
        parameterEntries.Select(kvp => kvp.Value).ToArray());
      parameters = [..parameterEntries.Select(kvp => kvp.Key)];
      initialParamValues = transformator.initialParamValues.ToArray();
      func = compiledTerm.Evaluate;
      funcGrad = compiledTerm.Differentiate;
      return true;
    }
    catch (ConversionException) {
      func = null;
      funcGrad = null;
      parameters = null;
      initialParamValues = null;
    }

    return false;
  }

  // state for recursive transformation of trees 
  private readonly List<double> initialParamValues;
  private readonly Dictionary<DataForVariable, AutoDiff.Variable> parameters;
  private readonly List<AutoDiff.Variable> variables;
  private readonly bool makeVariableWeightsVariable;
  private readonly HashSet<SymbolicExpressionTreeNode> excludedNodes;

  private TreeToAutoDiffTermConverter(bool makeVariableWeightsVariable, IEnumerable<SymbolicExpressionTreeNode> excludedNodes) {
    this.makeVariableWeightsVariable = makeVariableWeightsVariable;
    this.excludedNodes = [..excludedNodes];

    initialParamValues = [];
    parameters = [];
    variables = [];
  }

  private Term ConvertToAutoDiff(SymbolicExpressionTreeNode node) {
    switch (node.Symbol) {
      case Number: {
        initialParamValues.Add(((NumberTreeNode)node).Value);
        var var = new AutoDiff.Variable();
        variables.Add(var);
        return var;
      }
      case Variable:
      case BinaryFactorVariable: {
        var varNode = (VariableTreeNodeBase)node;
        // factor variable values are only 0 or 1 and set in x accordingly
        var varValue = node is BinaryFactorVariableTreeNode factorVarNode ? factorVarNode.VariableValue : string.Empty;
        var par = FindOrCreateParameter(parameters, varNode.VariableName, varValue);

        if (!makeVariableWeightsVariable || excludedNodes.Contains(node))
          return varNode.Weight * par;

        initialParamValues.Add(varNode.Weight);
        var w = new AutoDiff.Variable();
        variables.Add(w);
        return TermBuilder.Product(w, par);
      }
      case FactorVariable: {
        var factorVarNode = (FactorVariableTreeNode)node;
        var products = new List<Term>();
        foreach (var variableValue in factorVarNode.Symbol.GetVariableValues(factorVarNode.VariableName)) {
          var par = FindOrCreateParameter(parameters, factorVarNode.VariableName, variableValue);

          if (makeVariableWeightsVariable && !excludedNodes.Contains(node)) {
            initialParamValues.Add(factorVarNode.GetValue(variableValue));
            var wVar = new AutoDiff.Variable();
            variables.Add(wVar);

            products.Add(TermBuilder.Product(wVar, par));
          } else {
            var weight = factorVarNode.GetValue(variableValue);
            products.Add(weight * par);
          }
        }

        return TermBuilder.Sum(products);
      }
      case LaggedVariable: {
        var varNode = (LaggedVariableTreeNode)node;
        var par = FindOrCreateParameter(parameters, varNode.VariableName, string.Empty, varNode.Lag);

        if (!makeVariableWeightsVariable || excludedNodes.Contains(node))
          return varNode.Weight * par;

        initialParamValues.Add(varNode.Weight);
        var w = new AutoDiff.Variable();
        variables.Add(w);
        return TermBuilder.Product(w, par);
      }
      case Addition: {
        var terms = node.Subtrees.Select(ConvertToAutoDiff).ToList();
        return TermBuilder.Sum(terms);
      }
      case Subtraction: {
        var terms = new List<Term>();
        for (var i = 0; i < node.SubtreeCount; i++) {
          var t = ConvertToAutoDiff(node[i]);
          if (i > 0) t = -t;
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
        if (terms.Count == 1) return 1.0 / terms[0];
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
        if (Math.Abs(intPower - powerNode.Value) > 1e-15)
          throw new NotSupportedException("Only integer powers are allowed in parameter optimization. Try to use exp() and log() instead of the power symbol.");
        return TermBuilder.Power(ConvertToAutoDiff(node[0]), intPower);
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
        throw new ConversionException();
    }
  }

  // for each factor variable value we need a parameter which represents a binary indicator for that variable & value combination
  // each binary indicator is only necessary once. So we only create a parameter if this combination is not yet available
  private static Term FindOrCreateParameter(Dictionary<DataForVariable, AutoDiff.Variable> parameters, string varName, string varValue = "", int lag = 0) {
    var data = new DataForVariable(varName, varValue, lag);

    if (parameters.TryGetValue(data, out var par)) {
      return par;
    }

    // not found -> create new parameter and entries in names and values lists
    par = new AutoDiff.Variable();
    parameters.Add(data, par);
    return par;
  }

  public static bool IsCompatible(SymbolicExpressionTree tree) {
    var containsUnknownSymbol = (
      from n in tree.Root[0].IterateNodesPrefix()
      where
        n.Symbol is not Variable &&
        n.Symbol is not BinaryFactorVariable &&
        n.Symbol is not FactorVariable &&
        n.Symbol is not LaggedVariable &&
        n.Symbol is not Number &&
        n.Symbol is not Addition &&
        n.Symbol is not Subtraction &&
        n.Symbol is not Multiplication &&
        n.Symbol is not Division &&
        n.Symbol is not Logarithm &&
        n.Symbol is not Exponential &&
        n.Symbol is not SquareRoot &&
        n.Symbol is not Square &&
        n.Symbol is not Sine &&
        n.Symbol is not Cosine &&
        n.Symbol is not Tangent &&
        n.Symbol is not HyperbolicTangent &&
        n.Symbol is not SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math.Erf &&
        n.Symbol is not SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math.Norm &&
        n.Symbol is not StartSymbol &&
        n.Symbol is not Absolute &&
        n.Symbol is not AnalyticQuotient &&
        n.Symbol is not Cube &&
        n.Symbol is not CubeRoot &&
        n.Symbol is not Power &&
        n.Symbol is not SubFunctionSymbol
      select n).Any();
    return !containsUnknownSymbol;
  }

  #region exception class
  public class ConversionException : Exception {
    public ConversionException() { }

    public ConversionException(string message) : base(message) { }

    public ConversionException(string message, Exception inner) : base(message, inner) { }
  }
  #endregion
}
