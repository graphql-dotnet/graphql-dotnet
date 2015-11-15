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

        [Test]
        public void recursively_registers_children()
        {
            var schema = new ARootSchema();
            schema.EnsureLookup();

            ContainsTypeNames(
                schema,
                "RootSchemaType",
                "ASchemaType",
                "BSchemaType",
                "CSchemaType",
                "DSchemaType");
        }

        [Test]
        public void registers_argument_input_objects()
        {
            var schema = new ARootSchema();
            schema.EnsureLookup();

            ContainsTypeNames(
                schema,
                "DInputType");
        }

        public void ContainsTypeNames(Schema schema, params string[] typeNames)
        {
            typeNames.Apply(typeName =>
            {
                var type = schema.FindType(typeName);
                type.ShouldNotBeNull("Did not find {0} in type lookup.".ToFormat(typeName));
            });
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

    public class ARootSchema : Schema
    {
        public ARootSchema()
        {
            Query = new RootSchemaType();
        }
    }

    public class RootSchemaType : ObjectGraphType
    {
        public RootSchemaType()
        {
            Field<ASchemaType>("a");
        }
    }

    public class ASchemaType : ObjectGraphType
    {
        public ASchemaType()
        {
            Field<BSchemaType>("b");
        }
    }

    public class BSchemaType : ObjectGraphType
    {
        public BSchemaType()
        {
            Field<CSchemaType>("c");
        }
    }

    public class CSchemaType : ObjectGraphType
    {
        public CSchemaType()
        {
            Field<DSchemaType>("d");
        }
    }

    public class DSchemaType : ObjectGraphType
    {
        public DSchemaType()
        {
            Field<StringGraphType>("id", resolve: ctx => new {id = "id"});
            Field<StringGraphType>(
                "filter",
                arguments: new QueryArguments(new [] { new QueryArgument<DInputType> {Name = "input"} }),
                resolve: ctx => new {id = "id"});
        }
    }

    public class DInputType : InputObjectGraphType
    {
        public DInputType()
        {
            Field<StringGraphType>("one");
        }
    }
}
