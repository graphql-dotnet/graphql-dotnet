using Microsoft.CodeAnalysis;

namespace GraphQL.Analyzers.Interceptors;

/// <summary>
/// Source generator that creates interceptors for ComplexGraphType.Field calls with expressions
/// to avoid using ExpressionFieldResolver and enable AOT compilation.
/// <para>
/// This generator is opt-in and will only run when the <c>GraphQLEnableFieldInterceptors</c> MSBuild property is set to <c>true</c>.
/// The GraphQL.Analyzers NuGet package automatically sets this property when trimming is enabled
/// (<c>PublishTrimmed</c> or <c>EnableTrimAnalyzer</c>).
/// </para>
/// <para>
/// To enable interceptors explicitly, add to your .csproj:
/// <code>
/// &lt;PropertyGroup&gt;
///   &lt;GraphQLEnableFieldInterceptors&gt;true&lt;/GraphQLEnableFieldInterceptors&gt;
/// &lt;/PropertyGroup&gt;
/// </code>
/// </para>
/// <para>
/// The following public Field overloads from <c>ComplexGraphType&lt;TSourceType&gt;</c> are intercepted:
/// </para>
/// <list type="number">
/// <item>
/// <description><c>Field&lt;TProperty&gt;(string name, Expression&lt;Func&lt;TSourceType, TProperty&gt;&gt; expression)</c> - Basic field with name and expression</description>
/// </item>
/// <item>
/// <description><c>Field&lt;TProperty&gt;(string name, Expression&lt;Func&lt;TSourceType, TProperty&gt;&gt; expression, bool nullable)</c> - Field with name, expression, and explicit nullable flag</description>
/// </item>
/// <item>
/// <description><c>Field&lt;TProperty&gt;(string name, Expression&lt;Func&lt;TSourceType, TProperty&gt;&gt; expression, Type type)</c> - Field with name, expression, and explicit graph type</description>
/// </item>
/// <item>
/// <description><c>Field&lt;TProperty&gt;(Expression&lt;Func&lt;TSourceType, TProperty&gt;&gt; expression)</c> - Field with only expression (name inferred from expression)</description>
/// </item>
/// <item>
/// <description><c>Field&lt;TProperty&gt;(Expression&lt;Func&lt;TSourceType, TProperty&gt;&gt; expression, bool nullable)</c> - Field with expression and explicit nullable flag (name inferred)</description>
/// </item>
/// <item>
/// <description><c>Field&lt;TProperty&gt;(Expression&lt;Func&lt;TSourceType, TProperty&gt;&gt; expression, Type type)</c> - Field with expression and explicit graph type (name inferred)</description>
/// </item>
/// </list>
/// <para>
/// All interceptors call <c>FieldBuilderHelpers.CreateFieldBuilder</c> with null resolver parameter,
/// allowing the field metadata to be set up without using <c>ExpressionFieldResolver</c> which is not AOT-compatible.
/// </para>
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
