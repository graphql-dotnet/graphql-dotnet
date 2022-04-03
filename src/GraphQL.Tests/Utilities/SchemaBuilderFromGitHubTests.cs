using GraphQL.Types;
using GraphQL.Utilities;
using GraphQLParser.AST;

namespace GraphQL.Tests.Utilities;

public class SchemaBuilderFromGitHubTests
{
    private class MySchemaBuilder : SchemaBuilder
    {
        protected override UnionGraphType ToUnionType(GraphQLUnionTypeDefinition unionDef)
        {
            var type = base.ToUnionType(unionDef);
            type.ResolveType = _ => null;
            return type;
        }

        protected override InterfaceGraphType ToInterfaceType(GraphQLInterfaceTypeDefinition interfaceDef)
        {
            var type = base.ToInterfaceType(interfaceDef);
            type.ResolveType = _ => null;
            return type;
        }
    }

    private class ScalarStub : ScalarGraphType
    {
        public ScalarStub(string name)
        {
            Name = name;
        }

        public override object ParseValue(object value) => throw new System.NotImplementedException();
    }

    [Fact]
    public void Should_Build_Huge_Schema_With_Many_TypeReferences()
    {
        var schema = new MySchemaBuilder().Build("GitHub".ReadSDL());

        schema.RegisterType(new ScalarStub("URI"));
        schema.RegisterType(new ScalarStub("HTML"));
        schema.RegisterType(new ScalarStub("GitObjectID"));
        schema.RegisterType(new ScalarStub("PreciseDateTime"));
        schema.RegisterType(new ScalarStub("X509Certificate"));
        schema.RegisterType(new ScalarStub("GitSSHRemote"));
        schema.RegisterType(new ScalarStub("GitTimestamp"));

        schema.Initialize();
    }
}
