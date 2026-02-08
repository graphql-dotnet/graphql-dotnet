using Microsoft.CodeAnalysis;

namespace GraphQL.Analyzers.Interceptors;

/// <summary>
/// Source generator that creates interceptors for ComplexGraphType.Field calls with expressions
/// to avoid using ExpressionFieldResolver and enable AOT compilation.
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
        // Step 1: Provider - Find all invocation expressions that might be Field calls
        // Step 2: Transformer - Transform invocations into FieldInterceptorInfo records
        var interceptorInfos = FieldInvocationProvider.Create(
            context,
            static (invocation, semanticModel, _) => FieldInvocationTransformer.Transform(invocation, semanticModel))
            .Where(static info => info is not null);

        // Step 3: Generator - Generate source code from valid interceptor infos
        // Step 4: Diagnostics - Report any diagnostics
        context.RegisterSourceOutput(interceptorInfos, static (spc, info) =>
        {
            if (info == null)
                return;

            // Report diagnostics if any
            if (info.Diagnostic != null)
            {
                FieldInterceptorDiagnostics.Report(spc, info);
                return;
            }

            // Generate source code
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
