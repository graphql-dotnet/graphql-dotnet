using System.Threading.Tasks;
using GraphQL.Types;

namespace GraphQL.Introspection
{
    /// <summary>
    /// Provides the ability to filter the schema upon introspection to hide types, fields, arguments, enum values, directives.
    /// </summary>
    public interface ISchemaFilter
    {
        /// <summary>
        /// Returns a value indicating whether to hide the specified type.
        /// </summary>
        /// <param name="type"> GraphQL type to consider. </param>
        Task<bool> AllowType(IGraphType type);

        /// <summary>
        /// Returns a value indicating whether to hide the specified field.
        /// </summary>
        /// <param name="parent"> Parent type to which the field belongs. </param>
        /// <param name="field"> Field to consider. </param>
        Task<bool> AllowField(IGraphType parent, IFieldType field);

        /// <summary>
        /// Returns a value indicating whether to hide the specified field argument.
        /// </summary>
        /// <param name="field"> The field to which the argument belongs. </param>
        /// <param name="argument"> Argument to consider. </param>
        Task<bool> AllowArgument(IFieldType field, QueryArgument argument);

        /// <summary>
        /// Returns a value indicating whether to hide the specified enum value.
        /// </summary>
        /// <param name="parent"> The enumeration to which the enum value belongs.  </param>
        /// <param name="enumValue"> Enum value to consider. </param>
        Task<bool> AllowEnumValue(EnumerationGraphType parent, EnumValueDefinition enumValue);

        /// <summary>
        /// Returns a value indicating whether to hide the specified directive.
        /// </summary>
        /// <param name="directive"> GraphQL directive to consider. </param>
        Task<bool> AllowDirective(DirectiveGraphType directive);
    }

    /// <summary>
    /// Default schema filter. By default nothing is hidden.
    /// </summary>
    public class DefaultSchemaFilter : ISchemaFilter
    {
        private static readonly Task<bool> _completed = Task.FromResult(true);

        /// <inheritdoc/>
        public virtual Task<bool> AllowType(IGraphType type) => _completed;

        /// <inheritdoc/>
        public virtual Task<bool> AllowField(IGraphType parent, IFieldType field) => _completed;

        /// <inheritdoc/>
        public virtual Task<bool> AllowArgument(IFieldType field, QueryArgument argument) => _completed;

        /// <inheritdoc/>
        public virtual Task<bool> AllowEnumValue(EnumerationGraphType parent, EnumValueDefinition enumValue) => _completed;

        /// <inheritdoc/>
        public virtual Task<bool> AllowDirective(DirectiveGraphType directive) => _completed;
    }
}
