using FluentValidation;
using FluentValidation.Results;

namespace HEAL.HeuristicLib.Algorithms;

public interface IBuilder<out TResult> {
  TResult Build();
}

// public interface IConsumer<in T> {
//   void Consume(T item);
// }
//
// public interface IProvider<out T> {
//   void ProvideTo(IConsumer<T> consumer);
// }
//
// public static class BuilderExtensions {
//   public static TBuilder With<TBuilder, T>(this TBuilder builder, IProvider<T> provider) 
//     where TBuilder : IConsumer<T> {
//     provider.ProvideTo(builder);
//     return builder;
//   }
//   public static TBuilder With<TBuilder, T>(this TBuilder builder, T value) where TBuilder : IConsumer<T> {
//     builder.Consume(value);
//     return builder;
//   }
// }

//
//
// public interface IConfigBagSource<TConfigBag> {
//   TConfigBag Collect(TConfigBag configBag);
// }
//
// public abstract class AccumulatorBuilder<TResult, TConfigBag/*, TResolvedConfig*/> : IBuilder<TResult> 
//   where TConfigBag : new()
// {
//   private readonly TConfigBag baseConfigBag = new();
//   private readonly List<IConfigBagSource<TConfigBag>> sources = [];
//
//   public AccumulatorBuilder<TResult, TConfigBag/*, TResolvedConfig*/> AddSource(IConfigBagSource<TConfigBag> source) {
//     sources.Add(source);
//     return this;
//   }
//   
//   public virtual TResult Build() {
//     var collectedConfigBag = CollectConfigBag();
//
//     var configBagValidationResult = ValidateConfigBag(collectedConfigBag);
//     if (!configBagValidationResult.IsValid) throw new ValidationException(configBagValidationResult.Errors);
//
//     // var resolvedConfig = ResolveConfig(collectedConfigBag);
//     //
//     // var resolvedConfigValidationResult = ValidateResolvedConfig(resolvedConfig);
//     // if (!resolvedConfigValidationResult.IsValid) throw new ValidationException(resolvedConfigValidationResult.Errors);
//     
//     return Build(collectedConfigBag);
//   }
//
//   protected virtual TConfigBag CollectConfigBag() {
//     var configBag = baseConfigBag;
//     foreach (var source in sources) {
//       configBag = source.Collect(configBag);
//     }
//     return configBag;
//   }
//
//   protected virtual ValidationResult ValidateConfigBag(TConfigBag config) {
//     return new ValidationResult();
//   }
//   
//   //protected abstract TResolvedConfig ResolveConfig(TConfigBag config);
//
//   // protected virtual ValidationResult ValidateResolvedConfig(TResolvedConfig config) {
//   //   return new ValidationResult();
//   // }
//   
//   protected abstract TResult Build(TConfigBag config);
// }
//
//
//
// public interface IConfigProperty<out T> {
//   bool HasValue { get; }
//   T Value { get; }
// }
//
// public class DirectProperty<T> : IConfigProperty<T> {
//   private readonly T? value;
//   
//   public bool HasValue => value is not null;
//   public T Value => value ?? throw new InvalidOperationException("Value is not set.");
//   
//   public DirectProperty(T? value = default) {
//     this.value = value;F
//   }
// }
//
// public class DependentProperty<TDep, T> : IConfigProperty<T> {
//   private readonly Func<TDep, T> factory;
//   private readonly IConfigProperty<TDep> dependentProperty;
//
//   public bool HasValue => dependentProperty.HasValue;
//   public T Value => factory(dependentProperty.Value);
//
//   public DependentProperty(Func<TDep, T> factory, IConfigProperty<TDep> dependentProperty) {
//     this.factory = factory;
//     this.dependentProperty = dependentProperty;
//   }
//
// }
