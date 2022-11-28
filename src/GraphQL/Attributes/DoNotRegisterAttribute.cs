using GraphQL.Types;

namespace GraphQL
{
    /// <summary>
    /// Indicates that <see cref="GraphQLBuilderExtensions.AddGraphTypes(DI.IGraphQLBuilder, System.Reflection.Assembly)"/>
    /// should skip this class when scanning an assembly for classes that implement <see cref="IGraphType"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class DoNotRegisterAttribute : Attribute
    {
    }
}
