using Microsoft.CodeAnalysis;

namespace GraphQL.Analyzers.Interceptors;

/// <summary>
/// Source generator that creates interceptors for ComplexGraphType.Field calls with expressions
/// to avoid using ExpressionFieldResolver and enable AOT compilation.
/// </summary>
[Generator(LanguageNames.CSharp)]
public class FieldExpressionInterceptorGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Check if interceptors should be enabled based on MSBuild properties
        var isEnabled = InterceptorEnabledProvider.Create(context);

        // Step 1: Provider - Find all invocation expressions that might be Field calls
        var interceptorInfos = FieldInvocationProvider.Create(
            context,
            static (invocation, semanticModel, cancellationToken) => (Invocation: invocation, SemanticModel: semanticModel, CancellationToken: cancellationToken))
            .Combine(isEnabled)
            .Where(static tuple => tuple.Right) // Only continue if interceptors are enabled
            .Select(static (tuple, _) => FieldInvocationTransformer.Transform(tuple.Left.Invocation, tuple.Left.SemanticModel));

        // Step 3: Generator - Generate source code from valid interceptor infos
        context.RegisterSourceOutput(interceptorInfos, static (spc, info) =>
        {
            if (info == null)
                return;

            var result = FieldInterceptorSourceGenerator.Generate(info);
            if (result.HasValue)
            {
                spc.AddSource(result.Value.FileName, result.Value.SourceText);
            }
        });
    }
}

internal static class ComplexGraphType
{
    internal const string ORIGINAL_EXPRESSION_PROPERTY_NAME = "ORIGINAL_EXPRESSION_PROPERTY_NAME";
}
