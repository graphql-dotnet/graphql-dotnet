using GraphQL.Types;
using GraphQL.Utilities;
using GraphQL.Validation.Complexity;

namespace GraphQL.Attributes;

/// <summary>
/// Specifies the complexity impact and/or child impact multiplier of a field.
/// Not applicable to input fields.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public class ComplexityAttribute : GraphQLAttribute
{
    /// <inheritdoc cref="FieldComplexityResult.FieldImpact"/>
    /// <remarks>
    /// Ignored if <see cref="FieldComplexityAnalyzer"/> is set.
    /// </remarks>
    public double? FieldImpact { get; }

    /// <inheritdoc cref="FieldComplexityResult.ChildImpactMultiplier"/>
    /// <remarks>
    /// Ignored if <see cref="FieldComplexityAnalyzer"/> is set.
    /// </remarks>
    public double? ChildImpactMultiplier { get; }

    private readonly Func<FieldImpactContext, FieldComplexityResult>? _fieldComplexityAnalyzerDelegate;
    /// <summary>
    /// Gets or sets the type of the field complexity analyzer.
    /// The type must be a class that implements <see cref="IFieldComplexityAnalyzer"/> and have a public parameterless constructor.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
    public Type? FieldComplexityAnalyzer { get; }

    /// <inheritdoc cref="ComplexityAnalayzerMetadataExtensions.WithComplexityImpact{TMetadataProvider}(TMetadataProvider, double)"/>
    public ComplexityAttribute(double fieldImpact)
    {
        FieldImpact = fieldImpact;
    }

    /// <inheritdoc cref="ComplexityAnalayzerMetadataExtensions.WithComplexityImpact{TMetadataProvider}(TMetadataProvider, double, double)"/>
    public ComplexityAttribute(double fieldImpact, double childImpactMultiplier)
    {
        FieldImpact = fieldImpact;
        ChildImpactMultiplier = childImpactMultiplier;
    }

    /// <inheritdoc cref="ComplexityAnalayzerMetadataExtensions.WithComplexityImpact{TMetadataProvider}(TMetadataProvider, Func{FieldImpactContext, FieldComplexityResult})"/>
    /// <remarks>
    /// The specified type must implement <see cref="IFieldComplexityAnalyzer"/> and have a public parameterless constructor.
    /// </remarks>
    public ComplexityAttribute(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        Type fieldComplexityAnalyzer)
    {
        if (!typeof(IFieldComplexityAnalyzer).IsAssignableFrom(fieldComplexityAnalyzer))
            throw new ArgumentOutOfRangeException(nameof(fieldComplexityAnalyzer), $"The type '{fieldComplexityAnalyzer.GetFriendlyName()}' must implement '{typeof(IFieldComplexityAnalyzer).GetFriendlyName()}'.");
        if (!fieldComplexityAnalyzer.IsClass)
            throw new ArgumentOutOfRangeException(nameof(fieldComplexityAnalyzer), $"The type '{fieldComplexityAnalyzer.GetFriendlyName()}' must be a class.");
        // any other issues, like if value was an open generic or does not have a public parameterless
        // constructor, will be thrown during the call to CreateInstance
        _fieldComplexityAnalyzerDelegate = ((IFieldComplexityAnalyzer)Activator.CreateInstance(fieldComplexityAnalyzer)!).Analyze;
    }

    /// <inheritdoc/>
    public override void Modify(FieldConfig field)
        => ModifyField(field);

    /// <inheritdoc/>
    public override void Modify(FieldType fieldType, bool isInputType)
    {
        if (!isInputType)
            ModifyField(fieldType);
    }

    private void ModifyField(IFieldMetadataWriter field)
    {
        if (_fieldComplexityAnalyzerDelegate != null)
        {
            field.WithComplexityImpact(_fieldComplexityAnalyzerDelegate);
        }
        else if (FieldImpact != null)
        {
            if (ChildImpactMultiplier != null)
                field.WithComplexityImpact(FieldImpact.Value, ChildImpactMultiplier.Value);
            else
                field.WithComplexityImpact(FieldImpact.Value);
        }
        else if (ChildImpactMultiplier != null)
        {
            field.WithComplexityImpact(context =>
            {
                var ret = context.Configuration.DefaultComplexityImpactDelegate(context);
                ret.ChildImpactMultiplier = ChildImpactMultiplier.Value;
                return ret;
            });
        }
    }
}

/// <summary>
/// Specifies the complexity analyzer for a field.
/// Not applicable to input fields.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public class ComplexityAttribute<T> : GraphQLAttribute
    where T : class, IFieldComplexityAnalyzer, new()
{
    private readonly Func<FieldImpactContext, FieldComplexityResult> _func = new T().Analyze;

    /// <inheritdoc/>
    [Complexity(fieldImpact: 1, childImpactMultiplier: 100)]
    public override void Modify(FieldConfig field)
        => field.WithComplexityImpact(_func);

    /// <inheritdoc/>
    public override void Modify(FieldType fieldType, bool isInputType)
    {
        if (!isInputType)
            fieldType.WithComplexityImpact(_func);
    }
}
