using GraphQL.Analyzers.Interceptors.Models;
using Microsoft.CodeAnalysis;

namespace GraphQL.Analyzers.Interceptors;

/// <summary>
/// Reports diagnostics for Field interceptor generation.
/// </summary>
internal static class FieldInterceptorDiagnostics
{
    /// <summary>
    /// Reports a diagnostic from a FieldInterceptorInfo record.
    /// </summary>
    public static void Report(SourceProductionContext context, FieldInterceptorInfo info)
    {
        if (info.Diagnostic == null)
            return;

        var diagnostic = info.Diagnostic.CreateDiagnostic(null);
        context.ReportDiagnostic(diagnostic);
    }
}
