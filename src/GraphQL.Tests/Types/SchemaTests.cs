using System.Linq;
using GraphQL.Types;
using Should;

namespace GraphQL.Tests.Types
{
    public class SchemaTests
    {
        [Test]
        public void registers_interfaces_when_not_used_in_fields()
        {
            var schema = new AnInterfaceSchema();
            schema.EnsureLookup();
            var result = schema.AllTypes.SingleOrDefault(x => x.Name == "AnInterfaceType");
            result.ShouldNotBeNull("Interface type should be registered");
        }
    }

    public class AnInterfaceSchema : Schema
    {
        public AnInterfaceSchema()
        {
            Query = new AnObjectType();
        }
    }

    public class AnObjectType : ObjectGraphType
    {
        public AnObjectType()
        {
            Name = "AnObjectType";
            Field<StringGraphType>("name");
            Interface<AnInterfaceType>();
        }
    }

    public class AnInterfaceType : InterfaceGraphType
    {
        public AnInterfaceType()
        {
            Name = "AnInterfaceType";
            Field<StringGraphType>("name");
        }
    }
}
