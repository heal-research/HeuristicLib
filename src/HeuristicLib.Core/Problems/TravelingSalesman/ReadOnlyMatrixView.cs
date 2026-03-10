using System.Collections;

#pragma warning disable S2368

namespace HEAL.HeuristicLib.Problems.TravelingSalesman;

public sealed class ReadOnlyMatrixView<T>
{
  private readonly T[,] matrix;

  public ReadOnlyMatrixView(T[,] matrix)
  {
    this.matrix = matrix;
  }

  public int Rows => matrix.GetLength(0);
  public int Columns => matrix.GetLength(1);
  public int Size => matrix.GetLength(0);

  public ref readonly T this[int row, int column] => ref matrix[row, column];
}
