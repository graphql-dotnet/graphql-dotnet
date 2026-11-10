using System.Collections.Concurrent;
using System.Reflection;

namespace GraphQL;

internal static class OptionalTypes
{
    private static readonly ConcurrentDictionary<Type, ConstructorInfo> _types = new();
    private static readonly ConcurrentDictionary<Assembly, Lazy<bool>> _assemblies = new();

    static OptionalTypes()
    {
        RegisterType(typeof(Optional<>));
        // Subscribe first so assemblies loaded during discovery are also included.
        AppDomain.CurrentDomain.AssemblyLoad += (_, args) => RegisterAssembly(args.LoadedAssembly);
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            RegisterAssembly(assembly);
    }

    internal static bool IsOptionalType(Type type)
        => type.IsGenericType && _types.ContainsKey(type.GetGenericTypeDefinition());

    internal static ConstructorInfo GetValueConstructor(Type type)
        // Resolve the generic constructor even if another overload has the same closed parameter type.
        => (ConstructorInfo)MethodBase.GetMethodFromHandle(_types[type.GetGenericTypeDefinition()].MethodHandle, type.TypeHandle)!;

    private static void RegisterType(Type type)
        => _types.TryAdd(type, type.GetConstructor(type.GetGenericArguments())!);

    private static void RegisterAssembly(Assembly assembly)
    {
        _ = _assemblies.GetOrAdd(assembly, static assembly => new Lazy<bool>(() =>
        {
            var attributes = assembly.GetCustomAttributes<OptionalTypeAttribute>()
                .Concat(assembly.GetModules().SelectMany(module => module.GetCustomAttributes<OptionalTypeAttribute>()));
            foreach (var attribute in attributes)
                RegisterType(attribute.OptionalType);
            return true;
        })).Value;
    }
}
