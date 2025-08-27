using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HEAL.HeuristicLib;

public static class Extensions {
  public static bool IsAlmost(this double a, double b, double tolerance = 1E-10) {
    return Math.Abs(a - b) <= tolerance;
  }
}
