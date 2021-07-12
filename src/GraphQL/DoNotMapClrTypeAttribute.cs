#nullable enable

using System;

namespace GraphQL
{
    /// <summary>
    /// Indicates that <see cref="SchemaExtensions.RegisterTypeMappings(Types.ISchema)"/> should
    /// skip this class when scanning an assembly for CLR type mappings.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class DoNotMapClrTypeAttribute : Attribute
    {
    }
}
