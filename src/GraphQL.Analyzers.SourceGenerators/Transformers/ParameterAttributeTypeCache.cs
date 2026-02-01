using GraphQL.Analyzers.SourceGenerators.Models;
using Microsoft.CodeAnalysis;

namespace GraphQL.Analyzers.SourceGenerators.Transformers;

/// <summary>
/// Cache for tracking types that should be skipped during parameter processing
/// based on ParameterAttribute&lt;T&gt; attributes defined at assembly and module level.
/// </summary>
public sealed class ParameterAttributeTypeCache
{
    private readonly Dictionary<ISymbol, HashSet<ITypeSymbol>> _cache = new(SymbolEqualityComparer.Default);
    private readonly KnownSymbols _knownSymbols;

    public ParameterAttributeTypeCache(KnownSymbols knownSymbols)
    {
        _knownSymbols = knownSymbols;
    }

    /// <summary>
    /// Checks if a parameter type should be skipped based on ParameterAttribute&lt;T&gt; attributes
    /// defined on the parameter's assembly or module.
    /// </summary>
    /// <param name="parameter">The parameter to check.</param>
    /// <returns>True if the parameter type should be skipped; otherwise, false.</returns>
    public bool ShouldSkipParameterType(IParameterSymbol parameter)
    {
        if (_knownSymbols.ParameterAttributeT == null)
            return false;

        var parameterType = parameter.Type;
        var assembly = parameter.ContainingAssembly;
        var module = parameter.ContainingModule;

        // Check assembly-level attributes
        if (assembly != null)
        {
            var assemblyTypes = GetOrPopulateCache(assembly);
            if (assemblyTypes.Contains(parameterType))
                return true;
        }

        // Check module-level attributes
        if (module != null)
        {
            var moduleTypes = GetOrPopulateCache(module);
            if (moduleTypes.Contains(parameterType))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the cached hashset for a symbol (assembly or module), populating it if not present.
    /// </summary>
    private HashSet<ITypeSymbol> GetOrPopulateCache(ISymbol symbol)
    {
        if (_cache.TryGetValue(symbol, out var cachedSet))
            return cachedSet;

        var typeSet = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

        // Scan attributes on the symbol
        foreach (var attribute in symbol.GetAttributes())
        {
            ExtractParameterAttributeType(attribute, typeSet);
        }

        _cache[symbol] = typeSet;
        return typeSet;
    }

    /// <summary>
    /// Extracts the type argument T from a ParameterAttribute&lt;T&gt; or derived attribute and adds it to the set.
    /// </summary>
    private void ExtractParameterAttributeType(AttributeData attribute, HashSet<ITypeSymbol> typeSet)
    {
        if (attribute.AttributeClass == null)
            return;

        // Walk the inheritance tree to find if this attribute derives from ParameterAttribute<T>
        var current = attribute.AttributeClass;
        while (current != null)
        {
            if (current.IsGenericType)
            {
                var originalDef = current.OriginalDefinition;
                if (SymbolEqualityComparer.Default.Equals(originalDef, _knownSymbols.ParameterAttributeT) &&
                    current.TypeArguments.Length == 1)
                {
                    typeSet.Add(current.TypeArguments[0]);
                    return;
                }
            }

            current = current.BaseType;
        }
    }
}
