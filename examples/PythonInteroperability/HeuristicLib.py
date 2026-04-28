import os
import sys
from pythonnet import load

load("coreclr")
import clr  # noqa: E402

_here = os.path.dirname(os.path.abspath(__file__))
dll_dir = os.path.join(_here, "csProject", "bin", "Release", "net10.0", "publish")

if dll_dir not in sys.path:
    sys.path.append(dll_dir)

clr.AddReference("HEAL.HeuristicLib.Core") # type: ignore # noqa: E1101

from System import Array, Double, Int32  # type: ignore  # noqa: E402 # noqa: E0401

def double_array(x):
    return Array[Double](x)

def int_array(x):
    return Array[Int32](x)

def double_array_2d(x):
    rows = len(x)
    cols = len(x[0]) if rows > 0 else 0

    arr = Array.CreateInstance(Double, rows, cols)

    for i in range(rows):
        if len(x[i]) != cols:
            raise ValueError("All rows must have the same length")
        for j in range(cols):
            arr[i, j] = float(x[i][j])

    return arr

__all__ = ["double_array", "int_array","double_array_2d"]