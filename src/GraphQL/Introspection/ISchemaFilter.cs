using GraphQL.Types;
using System.Linq;
using System.Threading.Tasks;

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
        public virtual Task<bool> AllowType(IGraphType type) => Task.FromResult(true);

        public virtual Task<bool> AllowField(IGraphType parent, IFieldType field) => Task.FromResult(true);

        public virtual Task<bool> AllowArgument(IFieldType field, QueryArgument argument) => Task.FromResult(true);

        public virtual Task<bool> AllowEnumValue(EnumerationGraphType parent, EnumValueDefinition enumValue) => Task.FromResult(true);

        public virtual Task<bool> AllowDirective(DirectiveGraphType directive)
        {
            // true for known "deprecated" directive
            if (directive == DirectiveGraphType.Deprecated)
                return Task.FromResult(true);

            // true for all ExecutableDirectiveLocation
            return Task.FromResult(directive.Locations.All(l =>
                l == DirectiveLocation.Query ||
                l == DirectiveLocation.Mutation ||
                l == DirectiveLocation.Subscription ||
                l == DirectiveLocation.Field ||
                l == DirectiveLocation.FragmentDefinition ||
                l == DirectiveLocation.FragmentSpread ||
                l == DirectiveLocation.InlineFragment));
        }
    }
}
