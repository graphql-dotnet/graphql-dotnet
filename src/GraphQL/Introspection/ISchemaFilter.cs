using GraphQL.Types;

namespace GraphQL.Introspection
{
    public interface ISchemaFilter
    {
        bool Type(IGraphType type);
        bool Field(IGraphType parent, IFieldType field);
        bool Argument(IFieldType field, QueryArgument argument);
        bool EnumValue(EnumerationGraphType parent, EnumValueDefinition enumValue);
        bool Directive(DirectiveGraphType directive);
    }

    public class DefaultSchemaFilter : ISchemaFilter
    {
        public bool Type(IGraphType type)
        {
            return true;
        }

        public bool Field(IGraphType parent, IFieldType field)
        {
            return true;
        }

        public bool Argument(IFieldType field, QueryArgument argument)
        {
            return true;
        }

        public bool EnumValue(EnumerationGraphType parent, EnumValueDefinition enumValue)
        {
            return true;
        }

        public bool Directive(DirectiveGraphType directive)
        {
            return true;
        }
    }
}
