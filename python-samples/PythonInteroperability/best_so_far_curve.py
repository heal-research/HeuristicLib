"""Plot a run's retained best-so-far points; publish PythonInterop first."""
from pathlib import Path
import sys
from pythonnet import load

load("coreclr")
import clr
import matplotlib.pyplot as plt

sys.path.append(str(Path(__file__).resolve().parents[2] /
                    "src/HeuristicLib.PythonInterop/bin/Release/net10.0/publish"))
clr.AddReference("HEAL.HeuristicLib.PythonInterop")
from HEAL.HeuristicLib.PythonInterop import (
    PythonGenealogyAnalysis, TestFunctionExperimentParameters,
)

parameters = TestFunctionExperimentParameters()
parameters.AlgorithmName = "ga"
parameters.Problem = 1
parameters.Dimension = 5
parameters.Iterations = 50
parameters.PopulationSize = 50
parameters.Seed = 42
result = PythonGenealogyAnalysis.RunTestFunctionConfigurable("", parameters)
points = list(result.QualityCurve)
plt.step([p.Evaluations for p in points], [p.Value[0] for p in points], where="post")
plt.xlabel("Evaluations")
plt.ylabel("Best quality")
plt.show()
