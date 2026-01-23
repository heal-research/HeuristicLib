using System.Globalization;
using System.Text;
using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math.Variables;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Formatter;

/// <summary>
/// Parses mathematical expressions in infix form. E.g. x1 * (3.0 * x2 + x3)
/// Identifier format (functions or variables): '_' | letter { '_' | letter | digit }
/// Variables names and variable values can be set under quotes "" or '' because variable names might contain spaces. 
///   Variable = ident | " ident " | ' ident ' 
/// It is also possible to use functions e.g. log("x1") or real-valued constants e.g. 3.1415 . 
/// Variable names are case sensitive. Function names are not case sensitive.
/// 
/// 
/// S             = Expr EOF
/// Expr          = Term { '+' Term | '-' Term }
/// Term          = Fact { '*' Fact | '/' Fact }
/// Fact          = SimpleFact [ '^' SimpleFact ]
/// SimpleFact    = '(' Expr ')'
///                 | '{' Expr '}'
///                 | 'LAG' '(' varId ',' ['+' | '-' ] number ')
///                 | funcId '(' ArgList ')'
///                 | VarExpr
///                 | number
///                 | ['+' | '-'] SimpleFact
/// ArgList       = Expr { ',' Expr }
/// VarExpr       = varId OptFactorPart
/// OptFactorPart = [ ('=' varVal | '[' ['+' | '-' ]  number {',' ['+' | '-' ]  number } ']' ) ]
/// varId         =  ident | ' ident ' | " ident "
/// varVal        =  ident | ' ident ' | " ident "
/// ident         =  '_' | letter { '_' | letter | digit }
/// </summary>
public sealed class InfixExpressionParser {
  private enum TokenType { Operator, Identifier, Number, LeftPar, RightPar, LeftBracket, RightBracket, LeftAngleBracket, RightAngleBracket, Comma, Eq, End, Na };

  private class Token {
    internal double DoubleVal;
    internal string StrVal = "";
    internal TokenType TokenType;
  }

  private class SymbolComparer : IEqualityComparer<Symbol> {
    public bool Equals(Symbol? x, Symbol? y) {
      return x?.GetType() == y?.GetType();
    }

    public int GetHashCode(Symbol obj) {
      return obj.GetType().GetHashCode();
    }
  }

  // format name <-> symbol 
  // the lookup table is also used in the corresponding formatter
  internal static readonly BidirectionalLookup<string, Symbol>
    KnownSymbols = new BidirectionalLookup<string, Symbol>(StringComparer.InvariantCulture, new SymbolComparer());
  internal static readonly SubFunctionSymbol SubFunctionSymbol = new SubFunctionSymbol();

  private Number number = new Number();
  private Constant minusOne = new Constant() { Value = -1 };
  private Variable variable = new Variable();
  private BinaryFactorVariable binaryFactorVar = new BinaryFactorVariable();
  private FactorVariable factorVar = new FactorVariable();

  private ProgramRootSymbol programRootSymbol = new ProgramRootSymbol();
  private StartSymbol startSymbol = new StartSymbol();

  static InfixExpressionParser() {
    // populate bidirectional lookup
    var dict = new Dictionary<string, Symbol> {
      { "+", new Addition() },
      { "/", new Division() },
      { "*", new Multiplication() },
      { "-", new Subtraction() },
      { "^", new Power() },
      { "ABS", new Absolute() },
      { "EXP", new Exponential() },
      { "LOG", new Logarithm() },
      { "POW", new Power() },
      { "ROOT", new Root() },
      { "SQR", new Square() },
      { "SQRT", new SquareRoot() },
      { "CUBE", new Cube() },
      { "CUBEROOT", new CubeRoot() },
      { "SIN", new Sine() },
      { "COS", new Cosine() },
      { "TAN", new Tangent() },
      { "TANH", new HyperbolicTangent() },
      { "AIRYA", new AiryA() },
      { "AIRYB", new AiryB() },
      { "BESSEL", new Bessel() },
      { "COSINT", new CosineIntegral() },
      { "SININT", new SineIntegral() },
      { "HYPCOSINT", new HyperbolicCosineIntegral() },
      { "HYPSININT", new HyperbolicSineIntegral() },
      { "FRESNELSININT", new FresnelSineIntegral() },
      { "FRESNELCOSINT", new FresnelCosineIntegral() },
      { "NORM", new Norm() },
      { "ERF", new Erf() },
      { "GAMMA", new Gamma() },
      { "PSI", new Psi() },
      { "DAWSON", new Dawson() },
      { "EXPINT", new ExponentialIntegralEi() },
      { "AQ", new AnalyticQuotient() },
      { "MEAN", new Average() },
      { "IF", new IfThenElse() },
      { "GT", new GreaterThan() },
      { "LT", new LessThan() },
      { "AND", new And() },
      { "OR", new Or() },
      { "NOT", new Not() },
      { "XOR", new Xor() },
      { "DIFF", new Derivative() },
      { "LAG", new LaggedVariable() },
    };

    foreach (var kvp in dict) {
      KnownSymbols.Add(kvp.Key, kvp.Value);
    }
  }

  public SymbolicExpressionTree Parse(string str) {
    SymbolicExpressionTreeNode root = programRootSymbol.CreateTreeNode();
    SymbolicExpressionTreeNode start = startSymbol.CreateTreeNode();
    var allTokens = GetAllTokens(str).ToArray();
    SymbolicExpressionTreeNode mainBranch = ParseS(new Queue<Token>(allTokens));

    // only a main branch was given => insert the main branch into the default tree template
    root.AddSubtree(start);
    start.AddSubtree(mainBranch);
    return new SymbolicExpressionTree(root);
  }

  private IEnumerable<Token> GetAllTokens(string str) {
    int pos = 0;
    while (true) {
      while (pos < str.Length && char.IsWhiteSpace(str[pos])) pos++;
      if (pos >= str.Length) {
        yield return new Token {
          TokenType = TokenType.End,
          StrVal = ""
        };
        yield break;
      }

      if (char.IsDigit(str[pos])) {
        // read number (=> read until white space or other symbol)
        var sb = new StringBuilder();
        sb.Append(str[pos]);
        pos++;
        while (pos < str.Length && !char.IsWhiteSpace(str[pos])
                                && (str[pos] != '+' || str[pos - 1] == 'e' || str[pos - 1] == 'E') // continue reading exponents
                                && (str[pos] != '-' || str[pos - 1] == 'e' || str[pos - 1] == 'E')
                                && str[pos] != '*'
                                && str[pos] != '/'
                                && str[pos] != '^'
                                && str[pos] != ')'
                                && str[pos] != ']'
                                && str[pos] != '}'
                                && str[pos] != ','
                                && str[pos] != '>') {
          sb.Append(str[pos]);
          pos++;
        }

        if (double.TryParse(sb.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var dblVal))
          yield return new Token {
            TokenType = TokenType.Number,
            StrVal = sb.ToString(),
            DoubleVal = dblVal
          };
        else
          yield return new Token {
            TokenType = TokenType.Na,
            StrVal = sb.ToString()
          };
      } else if (char.IsLetter(str[pos]) || str[pos] == '_') {
        // read ident
        var sb = new StringBuilder();
        sb.Append(str[pos]);
        pos++;
        while (pos < str.Length &&
               (char.IsLetter(str[pos]) || str[pos] == '_' || char.IsDigit(str[pos]))) {
          sb.Append(str[pos]);
          pos++;
        }

        yield return new Token {
          TokenType = TokenType.Identifier,
          StrVal = sb.ToString()
        };
      } else if (str[pos] == '"') {
        // read to next " 
        pos++;
        var sb = new StringBuilder();
        while (pos < str.Length && str[pos] != '"') {
          sb.Append(str[pos]);
          pos++;
        }

        if (pos < str.Length && str[pos] == '"') {
          pos++; // skip "
          yield return new Token {
            TokenType = TokenType.Identifier,
            StrVal = sb.ToString()
          };
        } else
          yield return new Token { TokenType = TokenType.Na };
      } else if (str[pos] == '\'') {
        // read to next '
        pos++;
        var sb = new StringBuilder();
        while (pos < str.Length && str[pos] != '\'') {
          sb.Append(str[pos]);
          pos++;
        }

        if (pos < str.Length && str[pos] == '\'') {
          pos++; // skip '
          yield return new Token {
            TokenType = TokenType.Identifier,
            StrVal = sb.ToString()
          };
        } else
          yield return new Token { TokenType = TokenType.Na };
      } else if (str[pos] == '+') {
        pos++;
        yield return new Token {
          TokenType = TokenType.Operator,
          StrVal = "+"
        };
      } else if (str[pos] == '-') {
        pos++;
        yield return new Token {
          TokenType = TokenType.Operator,
          StrVal = "-"
        };
      } else if (str[pos] == '/') {
        pos++;
        yield return new Token {
          TokenType = TokenType.Operator,
          StrVal = "/"
        };
      } else if (str[pos] == '*') {
        pos++;
        yield return new Token {
          TokenType = TokenType.Operator,
          StrVal = "*"
        };
      } else if (str[pos] == '^') {
        pos++;
        yield return new Token {
          TokenType = TokenType.Operator,
          StrVal = "^"
        };
      } else if (str[pos] == '(') {
        pos++;
        yield return new Token {
          TokenType = TokenType.LeftPar,
          StrVal = "("
        };
      } else if (str[pos] == ')') {
        pos++;
        yield return new Token {
          TokenType = TokenType.RightPar,
          StrVal = ")"
        };
      } else if (str[pos] == '[') {
        pos++;
        yield return new Token {
          TokenType = TokenType.LeftBracket,
          StrVal = "["
        };
      } else if (str[pos] == ']') {
        pos++;
        yield return new Token {
          TokenType = TokenType.RightBracket,
          StrVal = "]"
        };
      } else if (str[pos] == '{') {
        pos++;
        yield return new Token {
          TokenType = TokenType.LeftPar,
          StrVal = "{"
        };
      } else if (str[pos] == '}') {
        pos++;
        yield return new Token {
          TokenType = TokenType.RightPar,
          StrVal = "}"
        };
      } else if (str[pos] == '=') {
        pos++;
        yield return new Token {
          TokenType = TokenType.Eq,
          StrVal = "="
        };
      } else if (str[pos] == ',') {
        pos++;
        yield return new Token {
          TokenType = TokenType.Comma,
          StrVal = ","
        };
      } else if (str[pos] == '<') {
        pos++;
        yield return new Token {
          TokenType = TokenType.LeftAngleBracket,
          StrVal = "<"
        };
      } else if (str[pos] == '>') {
        pos++;
        yield return new Token {
          TokenType = TokenType.RightAngleBracket,
          StrVal = ">"
        };
      } else {
        throw new ArgumentException("Invalid character: " + str[pos]);
      }
    }
  }

  /// S             = Expr EOF
  private SymbolicExpressionTreeNode ParseS(Queue<Token> tokens) {
    var expr = ParseExpr(tokens);

    var endTok = tokens.Dequeue();
    if (endTok.TokenType != TokenType.End)
      throw new ArgumentException(string.Format("Expected end of expression (got {0})", endTok.StrVal));

    return expr;
  }

  /// Expr          = Term { '+' Term | '-' Term }
  private SymbolicExpressionTreeNode ParseExpr(Queue<Token> tokens) {
    // build tree from bottom to top and left to right
    // a + b - c => ((a + b) - c)
    // a - b - c => ((a - b) - c)
    // and then flatten as far as possible
    var left = ParseTerm(tokens);

    var next = tokens.Peek();
    while (next.StrVal == "+" || next.StrVal == "-") {
      switch (next.StrVal) {
        case "+": {
          tokens.Dequeue();
          var right = ParseTerm(tokens);
          var op = GetSymbol("+").CreateTreeNode();
          op.AddSubtree(left);
          op.AddSubtree(right);
          left = op;
          break;
        }
        case "-": {
          tokens.Dequeue();
          var right = ParseTerm(tokens);
          var op = GetSymbol("-").CreateTreeNode();
          op.AddSubtree(left);
          op.AddSubtree(right);
          left = op;
          break;
        }
      }

      next = tokens.Peek();
    }

    FoldLeftRecursive(left);
    return left;
  }

  private Symbol GetSymbol(string tok) {
    if (KnownSymbols.ContainsFirst(tok))
      return KnownSymbols.GetByFirst(tok).First();
    return SubFunctionSymbol;
  }

  /// Term          = Fact { '*' Fact | '/' Fact }
  private SymbolicExpressionTreeNode ParseTerm(Queue<Token> tokens) {
    // build tree from bottom to top and left to right
    // a / b * c => ((a / b) * c)
    // a / b / c => ((a / b) / c)
    // and then flatten as far as possible

    var left = ParseFact(tokens);

    var next = tokens.Peek();
    while (next.StrVal == "*" || next.StrVal == "/") {
      switch (next.StrVal) {
        case "*": {
          tokens.Dequeue();
          var right = ParseFact(tokens);

          var op = GetSymbol("*").CreateTreeNode();
          op.AddSubtree(left);
          op.AddSubtree(right);
          left = op;
          break;
        }
        case "/": {
          tokens.Dequeue();
          var right = ParseFact(tokens);
          var op = GetSymbol("/").CreateTreeNode();
          op.AddSubtree(left);
          op.AddSubtree(right);
          left = op;
          break;
        }
      }

      next = tokens.Peek();
    }
    // remove all nodes where the child op is the same as the parent op
    // (a * b) * c) => (a * b * c)
    // (a / b) / c) => (a / b / c)

    FoldLeftRecursive(left);
    return left;
  }

  private void FoldLeftRecursive(SymbolicExpressionTreeNode parent) {
    if (parent.SubtreeCount <= 1)
      return;

    var child = parent.GetSubtree(0);
    FoldLeftRecursive(child);
    if (parent.Symbol != child.Symbol || !IsAssociative(parent.Symbol)) {
      return;
    }

    parent.RemoveSubtree(0);
    for (int i = 0; i < child.SubtreeCount; i++) {
      parent.InsertSubtree(i, child.GetSubtree(i));
    }
  }

  // Fact = SimpleFact ['^' SimpleFact]
  private SymbolicExpressionTreeNode ParseFact(Queue<Token> tokens) {
    var expr = ParseSimpleFact(tokens);
    var next = tokens.Peek();
    if (next is not { TokenType: TokenType.Operator, StrVal: "^" }) {
      return expr;
    }

    tokens.Dequeue(); // skip;

    var p = GetSymbol("^").CreateTreeNode();
    p.AddSubtree(expr);
    p.AddSubtree(ParseSimpleFact(tokens));
    expr = p;

    return expr;
  }

  /// SimpleFact   = '(' Expr ')' 
  ///                 | '{' Expr '}'
  ///                 | 'LAG' '(' varId ',' ['+' | '-' ] number ')'
  ///                 | funcId '(' ArgList ')
  ///                 | VarExpr
  ///                 | '<' 'num' [ '=' [ '+' | '-' ] number ] '>' 
  ///                 | number
  ///                 | ['+' | '-' ] SimpleFact
  /// ArgList       = Expr { ',' Expr }
  /// VarExpr       = varId OptFactorPart
  /// OptFactorPart = [ ('=' varVal | '[' ['+' | '-' ] number {',' ['+' | '-' ] number } ']' ) ]
  /// varId         =  ident | ' ident ' | " ident "
  /// varVal        =  ident | ' ident ' | " ident "
  /// ident         =  '_' | letter { '_' | letter | digit }
  private SymbolicExpressionTreeNode ParseSimpleFact(Queue<Token> tokens) {
    var next = tokens.Peek();
    switch (next.TokenType) {
      case TokenType.LeftPar: {
        var initPar = tokens.Dequeue(); // match par type
        var expr = ParseExpr(tokens);
        var rPar = tokens.Dequeue();
        if (rPar.TokenType != TokenType.RightPar)
          throw new ArgumentException("expected closing parenthesis");
        return initPar.StrVal switch {
          "(" when rPar.StrVal == "}" => throw new ArgumentException("expected closing )"),
          "{" when rPar.StrVal == ")" => throw new ArgumentException("expected closing }"),
          _ => expr
        };
      }
      case TokenType.Identifier: {
        var idTok = tokens.Dequeue();
        return tokens.Peek().TokenType == TokenType.LeftPar
          ?
          // function identifier or LAG
          ParseFunctionOrLaggedVariable(tokens, idTok)
          : ParseVariable(tokens, idTok);
      }
      case TokenType.LeftAngleBracket:
        // '<' 'num' [ '=' ['+'|'-'] number ] '>'
        return ParseNumber(tokens);
    }

    if (next is { TokenType: TokenType.Operator, StrVal: "-" or "+" }) {
      // ['+' | '-' ] SimpleFact
      if (tokens.Dequeue().StrVal != "-") {
        return ParseSimpleFact(tokens);
      }

      var arg = ParseSimpleFact(tokens);
      switch (arg) {
        case NumberTreeNode numNode:
          numNode.Value *= -1;
          return numNode;
        case ConstantTreeNode constNode: {
          var constSy = new Constant { Value = -constNode.Value };
          return constSy.CreateTreeNode();
        }
        case VariableTreeNode varNode:
          varNode.Weight *= -1;
          return varNode;
      }

      var mul = GetSymbol("*").CreateTreeNode();
      var neg = minusOne.CreateTreeNode();
      mul.AddSubtree(neg);
      mul.AddSubtree(arg);
      return mul;
    }

    if (next.TokenType != TokenType.Number) {
      throw new ArgumentException($"unexpected token in expression {next.StrVal}");
    }

    {
      // number
      var numTok = tokens.Dequeue();
      var constSy = new Constant { Value = numTok.DoubleVal };
      return constSy.CreateTreeNode();
    }
  }

  private SymbolicExpressionTreeNode ParseNumber(Queue<Token> tokens) {
    // we distinguish parameters and constants. The values of parameters can be changed.
    // a parameter is written as '<' 'num' [ '=' ['+'|'-'] number ] '>' with an optional initialization 
    Token numberTok;
    var leftAngleBracket = tokens.Dequeue();
    if (leftAngleBracket.TokenType != TokenType.LeftAngleBracket)
      throw new ArgumentException("opening bracket < expected");

    var idTok = tokens.Dequeue();
    if (idTok.TokenType != TokenType.Identifier || idTok.StrVal.ToLower() != "num")
      throw new ArgumentException("string 'num' expected");

    var numNode = (NumberTreeNode)number.CreateTreeNode();

    if (tokens.Peek().TokenType == TokenType.Eq) {
      tokens.Dequeue(); // skip "="
      var next = tokens.Peek();
      if (next.StrVal != "+" && next.StrVal != "-" && next.TokenType != TokenType.Number)
        throw new ArgumentException("Expected '+', '-' or number.");

      var sign = 1.0;
      if (next.StrVal == "+" || next.StrVal == "-") {
        if (tokens.Dequeue().StrVal == "-") sign = -1.0;
      }

      if (tokens.Peek().TokenType != TokenType.Number) {
        throw new ArgumentException("Expected number.");
      }

      numberTok = tokens.Dequeue();
      numNode.Value = sign * numberTok.DoubleVal;
    }

    var rightAngleBracket = tokens.Dequeue();
    if (rightAngleBracket.TokenType != TokenType.RightAngleBracket)
      throw new ArgumentException("closing bracket > expected");

    return numNode;
  }

  private SymbolicExpressionTreeNode ParseVariable(Queue<Token> tokens, Token idTok) {
    // variable
    if (tokens.Peek().TokenType == TokenType.Eq) {
      // binary factor
      tokens.Dequeue(); // skip Eq
      var valTok = tokens.Dequeue();
      if (valTok.TokenType != TokenType.Identifier) throw new ArgumentException("expected identifier");
      var binFactorNode = (BinaryFactorVariableTreeNode)binaryFactorVar.CreateTreeNode();
      binFactorNode.Weight = 1.0;
      binFactorNode.VariableName = idTok.StrVal;
      binFactorNode.VariableValue = valTok.StrVal;
      return binFactorNode;
    }

    if (tokens.Peek().TokenType == TokenType.LeftBracket) {
      // factor variable
      var factorVariableNode = (FactorVariableTreeNode)factorVar.CreateTreeNode();
      factorVariableNode.VariableName = idTok.StrVal;

      tokens.Dequeue(); // skip [
      var weights = new List<double>();
      // at least one weight is necessary
      var sign = 1.0;
      if (tokens.Peek().TokenType == TokenType.Operator) {
        var opToken = tokens.Dequeue();
        if (opToken.StrVal == "+") sign = 1.0;
        else if (opToken.StrVal == "-") sign = -1.0;
        else throw new ArgumentException();
      }

      if (tokens.Peek().TokenType != TokenType.Number) throw new ArgumentException("number expected");
      var weightTok = tokens.Dequeue();
      weights.Add(sign * weightTok.DoubleVal);
      while (tokens.Peek().TokenType == TokenType.Comma) {
        // skip comma
        tokens.Dequeue();
        if (tokens.Peek().TokenType == TokenType.Operator) {
          var opToken = tokens.Dequeue();
          if (opToken.StrVal == "+") sign = 1.0;
          else if (opToken.StrVal == "-") sign = -1.0;
          else throw new ArgumentException();
        }

        weightTok = tokens.Dequeue();
        if (weightTok.TokenType != TokenType.Number) throw new ArgumentException("number expected");
        weights.Add(sign * weightTok.DoubleVal);
      }

      var rightBracketToken = tokens.Dequeue();
      if (rightBracketToken.TokenType != TokenType.RightBracket) throw new ArgumentException("closing bracket ] expected");
      factorVariableNode.Weights = weights.ToArray();
      return factorVariableNode;
    }

    // variable
    var varNode = (VariableTreeNode)variable.CreateTreeNode();
    varNode.Weight = 1.0;
    varNode.VariableName = idTok.StrVal;
    return varNode;
  }

  private SymbolicExpressionTreeNode ParseFunctionOrLaggedVariable(Queue<Token> tokens, Token idTok) {
    var funcId = idTok.StrVal.ToUpperInvariant();

    var funcNode = GetSymbol(funcId).CreateTreeNode();
    var lPar = tokens.Dequeue();
    if (lPar.TokenType != TokenType.LeftPar)
      throw new ArgumentException("expected (");

    // handle 'lag' specifically
    if (funcNode.Symbol is LaggedVariable) {
      ParseLaggedVariable(tokens, funcNode);
    } else if (funcNode.Symbol is SubFunctionSymbol) { // SubFunction
      var subFunction = (SubFunctionTreeNode)funcNode;
      subFunction.Name = idTok.StrVal;
      // input arguments
      var args = ParseArgList(tokens);
      IList<string> arguments = new List<string>();
      foreach (var arg in args)
        if (arg is VariableTreeNode varTreeNode)
          arguments.Add(varTreeNode.VariableName);
      subFunction.Arguments = arguments;
    } else {
      // functions
      var args = ParseArgList(tokens);
      // check number of arguments
      if (funcNode.Symbol.MinimumArity > args.Length || funcNode.Symbol.MaximumArity < args.Length) {
        throw new ArgumentException(string.Format("Symbol {0} requires between {1} and  {2} arguments.", funcId,
          funcNode.Symbol.MinimumArity, funcNode.Symbol.MaximumArity));
      }

      foreach (var arg in args) funcNode.AddSubtree(arg);
    }

    var rPar = tokens.Dequeue();
    if (rPar.TokenType != TokenType.RightPar)
      throw new ArgumentException("expected )");

    return funcNode;
  }

  private static void ParseLaggedVariable(Queue<Token> tokens, SymbolicExpressionTreeNode funcNode) {
    var varId = tokens.Dequeue();
    if (varId.TokenType != TokenType.Identifier) throw new ArgumentException("Identifier expected. Format for lagged variables: \"lag(x, -1)\"");
    var comma = tokens.Dequeue();
    if (comma.TokenType != TokenType.Comma) throw new ArgumentException("',' expected, Format for lagged variables: \"lag(x, -1)\"");
    double sign = 1.0;
    if (tokens.Peek().StrVal == "+" || tokens.Peek().StrVal == "-") {
      // read sign
      var signTok = tokens.Dequeue();
      if (signTok.StrVal == "-") sign = -1.0;
    }

    var lagToken = tokens.Dequeue();
    if (lagToken.TokenType != TokenType.Number) throw new ArgumentException("Number expected, Format for lagged variables: \"lag(x, -1)\"");
    if (!lagToken.DoubleVal.IsAlmost(Math.Round(lagToken.DoubleVal)))
      throw new ArgumentException("Time lags must be integer values");
    var laggedVarNode = (LaggedVariableTreeNode)funcNode;
    laggedVarNode.VariableName = varId.StrVal;
    laggedVarNode.Lag = (int)Math.Round(sign * lagToken.DoubleVal);
    laggedVarNode.Weight = 1.0;
  }

  // ArgList = Expr { ',' Expr }
  private SymbolicExpressionTreeNode[] ParseArgList(Queue<Token> tokens) {
    var exprList = new List<SymbolicExpressionTreeNode>();
    exprList.Add(ParseExpr(tokens));
    while (tokens.Peek().TokenType != TokenType.RightPar) {
      var comma = tokens.Dequeue();
      if (comma.TokenType != TokenType.Comma) throw new ArgumentException("expected ',' ");
      exprList.Add(ParseExpr(tokens));
    }

    return exprList.ToArray();
  }

  private bool IsAssociative(Symbol sy) {
    return sy == GetSymbol("+") || sy == GetSymbol("-") ||
           sy == GetSymbol("*") || sy == GetSymbol("/") ||
           sy == GetSymbol("AND") || sy == GetSymbol("OR") || sy == GetSymbol("XOR");
  }
}
