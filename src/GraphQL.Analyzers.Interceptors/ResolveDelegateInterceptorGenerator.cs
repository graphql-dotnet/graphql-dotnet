using Microsoft.CodeAnalysis;

namespace GraphQL.Analyzers.Interceptors;

/// <summary>
/// Source generator that creates interceptors for FieldBuilder.ResolveDelegate calls
/// to avoid using Expression.Lambda and AutoRegisteringHelper.BuildFieldResolver
/// which require dynamic code, enabling AOT compilation.
/// </summary>
[Generator(LanguageNames.CSharp)]
public class ResolveDelegateInterceptorGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Check if interceptors should be enabled based on MSBuild properties
        var isEnabled = InterceptorEnabledProvider.Create(context);

        // Step 1: Provider - Find all invocation expressions that might be ResolveDelegate calls
        var interceptorInfos = ResolveDelegateInvocationProvider.Create(
            context,
            static (invocation, semanticModel, cancellationToken) => (Invocation: invocation, SemanticModel: semanticModel, CancellationToken: cancellationToken))
            .Combine(isEnabled)
            .Where(static tuple => tuple.Right) // Only continue if interceptors are enabled
            .Select(static (tuple, _) => ResolveDelegateInvocationTransformer.Transform(tuple.Left.Invocation, tuple.Left.SemanticModel));

        // Step 2: Generator - Generate source code from valid interceptor infos
        context.RegisterSourceOutput(interceptorInfos, static (spc, info) =>
        {
            if (info == null)
                return;

            var result = ResolveDelegateInterceptorSourceGenerator.Generate(info);
            if (result.HasValue)
            {
                spc.AddSource(result.Value.FileName, result.Value.SourceText);
            }
        });
    }
}
