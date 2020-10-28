using System.Threading.Tasks;
using GraphQL.Types;

namespace GraphQL.Introspection
{
    public interface ISchemaFilter
    {
        Task<bool> AllowType(IGraphType type);
        Task<bool> AllowField(IGraphType parent, IFieldType field);
        Task<bool> AllowArgument(IFieldType field, QueryArgument argument);
        Task<bool> AllowEnumValue(EnumerationGraphType parent, EnumValueDefinition enumValue);
        Task<bool> AllowDirective(DirectiveGraphType directive);
    }

    public class DefaultSchemaFilter : ISchemaFilter
    {
        public virtual Task<bool> AllowType(IGraphType type)
        {
            return Task.FromResult(true);
        }

        public virtual Task<bool> AllowField(IGraphType parent, IFieldType field)
        {
            return Task.FromResult(true);
        }

        public virtual Task<bool> AllowArgument(IFieldType field, QueryArgument argument)
        {
            return Task.FromResult(true);
        }

        public virtual Task<bool> AllowEnumValue(EnumerationGraphType parent, EnumValueDefinition enumValue)
        {
            return Task.FromResult(true);
        }

        public virtual Task<bool> AllowDirective(DirectiveGraphType directive)
        {
            return Task.FromResult(true);
        }
    }
}
