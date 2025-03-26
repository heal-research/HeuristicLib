using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Algorithms;

public interface IBuilder<out TResult> {
  TResult Build();
}

public interface IExecutableOperatorFactory<out TOperator> where TOperator : IExecutableOperator {
  TOperator Create(OperatorCreationContext context);
}

public interface IExecutableEncodingOperatorFactory<out TOperator, TEncodingParameter> where TOperator : IExecutableOperator where TEncodingParameter : IEncodingParameter {
  TOperator Create(EncodingOperatorCreationContext<TEncodingParameter> context);
}

public class OperatorCreationContext {
  public required IRandomSource RandomSource { get; init; }
}

public class EncodingOperatorCreationContext<TEncodingParameter> : OperatorCreationContext 
  where TEncodingParameter : IEncodingParameter {
  public required TEncodingParameter EncodingParameter { get; init; }
}

public class FixedOperatorFactory<TOperator> : IExecutableOperatorFactory<TOperator> where TOperator : IExecutableOperator {
  private readonly TOperator @operator;
  public FixedOperatorFactory(TOperator @operator) {
    this.@operator = @operator;
  }
  public TOperator Create(OperatorCreationContext context) {
    return @operator;
  }
}
public class FixedEncodingOperatorFactory<TOperator, TEncodingParameter> : IExecutableEncodingOperatorFactory<TOperator, TEncodingParameter> where TOperator : IExecutableOperator where TEncodingParameter : IEncodingParameter {
  private readonly TOperator @operator;
  public FixedEncodingOperatorFactory(TOperator @operator) {
    this.@operator = @operator;
  }
  public TOperator Create(EncodingOperatorCreationContext<TEncodingParameter> context) {
    return @operator;
  }
}
