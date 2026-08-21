using HEAL.HeuristicLib.Numerics;

namespace HEAL.HeuristicLib.Encodings.SymbolicExpressions;

public interface IExpressionEmitter
{
    void EmitChild(int childIndex);
    void EmitVariable(string name);
    void EmitConstant(double value);
    void EmitOperation(Operation operation);
}
