using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Algorithms.MetaAlgorithms;
using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Operators.Creators.SymbolicExpressionTreeCreators;
using HEAL.HeuristicLib.Operators.Crossovers.SymbolicExpressionTreeCrossovers;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Mutators.SymbolicExpressionTreeMutators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.DataAnalysis;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Evaluators;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Trees;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Grammars;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math;

namespace HEAL.HeuristicLib.PythonInterOptScripts;

/// <summary>
/// This is a toy problem that uses a "normal" symbolic regression problem and adds more objectives provided by a generic function
/// </summary>
public class ExtendedSymbolicRegressionProblem(Objective objective, SymbolicExpressionTreeSearchSpace searchSpace, Func<SymbolicExpressionTree, double[]> myEval)
  : Problem<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace>(objective, searchSpace)
{
  public required SymbolicRegressionProblem InnerProblem { get; init; }

  public override ObjectiveVector Evaluate(SymbolicExpressionTree solution, IRandomNumberGenerator random) => InnerProblem.Evaluate(solution).Concat(myEval(solution)).ToArray();

  #region CallTheseFromPython
  public static ExtendedSymbolicRegressionProblem DefaultConf(string file, Func<SymbolicExpressionTree, double[]> myEval)
  {
    var data = RegressionCsvInstanceProvider.ImportData(file, 0.75); //rows are not shuffled
    var grammar = new SimpleSymbolicExpressionGrammar(); //Trees have 3 node min
    var root = grammar.AddLinearScaling(); //adds Keijzer scaling to top of tree (+4 Nodes)
    var symbols = new Symbol[] { //add a bunch of symbols and allow all combinations of them 
      new Addition(),
      new Subtraction(),
      new Multiplication(),
      new Division(),
      new Number(),
      new SquareRoot(),
      new Logarithm(),
      new Variable { VariableNames = data.InputVariables }
    };
    grammar.AddFullyConnectedSymbols(root, symbols);

    var symbolicExpressionTreeSearchSpace = new SymbolicExpressionTreeSearchSpace(grammar, 40, 20);
    var p = new SymbolicRegressionProblem(data,
      symbolicExpressionTreeSearchSpace,
      new PearsonR2Evaluator(),
      new RootMeanSquaredErrorEvaluator(),
      new TreeLengthEvaluator() //... other evaluators
    ) {
      ParameterOptimizationIterations = 5 //this is the effort spent on Constant-Optimization
    };

    //tell the objective that your func is going to append 3 maximization objectives to whatever your inner problem does
    var directions = p.Objective.Directions.Concat([
      ObjectiveDirection.Maximize,
      ObjectiveDirection.Maximize,
      ObjectiveDirection.Maximize
    ]).ToArray();
    var objective = new Objective(directions, new LexicographicComparer(directions));

    return new ExtendedSymbolicRegressionProblem(objective, symbolicExpressionTreeSearchSpace, myEval) { InnerProblem = p };
  }

  public static Population<SymbolicExpressionTree> RunDefault(ExtendedSymbolicRegressionProblem p, int seed = 42)
  {
    var symRegAllMutator = MultiMutator.Create(
      new ChangeNodeTypeManipulation(),
      new FullTreeShaker(),
      new OnePointShaker(),
      new RemoveBranchManipulation(),
      new ReplaceBranchManipulation()
    );

    var ga = GeneticAlgorithm.GetBuilder(new ProbabilisticTreeCreator(), new SubtreeCrossover(), symRegAllMutator);
    ga.MutationRate = 0.1;
    ga.Selector = new TournamentSelector<SymbolicExpressionTree>(4);
    ga.PopulationSize = 300;

    var res = ga.Build()
                .WithMaxIterations(200)
                .RunToCompletion(p, RandomNumberGenerator.Create(seed), null, CancellationToken.None);
    return res.Population;
  }

  public static Population<SymbolicExpressionTree> RunDefault(string file, Func<SymbolicExpressionTree, double[]> myEval, int seed = 42) => RunDefault(DefaultConf(file, myEval), seed);
  #endregion
}
