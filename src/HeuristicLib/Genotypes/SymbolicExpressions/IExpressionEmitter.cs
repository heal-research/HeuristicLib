namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public interface IExpressionEmitter
{
    void EmitChild(int childIndex);
    void EmitVariable(string name);
    void EmitConstant(double value);
    void EmitOperator(OpCode opCode);
}
