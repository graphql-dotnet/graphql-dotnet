using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace GraphQL.Conversion;

/// <summary>
/// This converter uses reflection to instantiate a list and add items to it.
/// The list must implement <see cref="IList.Add(object?)"/>.
/// Although slower than other factories, AOT is fully supported.
/// </summary>
internal class ReflectionListConverterFactory : IListConverterFactory
{
    private ReflectionListConverterFactory()
    {
    }

    public static ReflectionListConverterFactory Instance { get; } = new();

    public IListConverter Create(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type listType)
    {
        if (!typeof(IList).IsAssignableFrom(listType))
        {
            throw new ArgumentException($"Type '{listType.GetFriendlyName()}' does not implement IList.");
        }
        var elementType = listType.GetListElementType();
#pragma warning disable IL2072 // Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.
        var elementDefault = elementType.IsValueType ? Activator.CreateInstance(elementType) : null;
#pragma warning restore IL2072 // Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.

        return new ListConverter(elementType, CreateConverter(listType, elementDefault));
    }

    private static Func<object?[], object> CreateConverter(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type listType, object? elementDefault)
    {
        return array =>
        {
            IList list;
            try
            {
                list = (IList)Activator.CreateInstance(listType)!;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                //preserve stack trace and throw inner exception
#if NETSTANDARD2_0
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
#else
                ExceptionDispatchInfo.Throw(ex.InnerException);
#endif
                throw; //unreachable
            }
            foreach (var item in array)
            {
                list.Add(item ?? elementDefault);
            }
            return list;
        };
    }
}
