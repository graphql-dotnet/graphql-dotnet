using System;
using GraphQL.Types;
using Xunit;

namespace GraphQL.Tests.Initialization
{
    public class SchemaInitializationTests : SchemaInitializationTestBase
    {
        [Fact]
        public void EmptyQuerySchema_Should_Throw()
        {
            ShouldThrow<EmptyQuerySchema, InvalidOperationException>("An Object type 'Empty' must define one or more fields.");
        }

        [Fact]
        public void SchemaWithDuplicateFields_Should_Throw()
        {
            ShouldThrow<SchemaWithDuplicateFields, InvalidOperationException>("The field 'field' must have a unique name within Object type 'Dup'; no two fields may share the same name.");
        }

        [Fact]
        public void EmptyInterfaceSchema_Should_Throw()
        {
            ShouldThrow<EmptyInterfaceSchema, InvalidOperationException>("An Interface type 'Empty' must define one or more fields.");
        }

        [Fact]
        public void SchemaWithDuplicateInterfaceFields_Should_Throw()
        {
            ShouldThrow<SchemaWithDuplicateInterfaceFields, InvalidOperationException>("The field 'field' must have a unique name within Interface type 'Dup'; no two fields may share the same name.");
        }
    }

    public class EmptyQuerySchema : Schema
    {
        public EmptyQuerySchema()
        {
            Query = new ObjectGraphType { Name = "Empty" };
        }
    }

    public class SchemaWithDuplicateFields : Schema
    {
        public SchemaWithDuplicateFields()
        {
            Query = new ObjectGraphType { Name = "Dup" };
            Query.AddField(new FieldType { Name = "field", ResolvedType = new StringGraphType() });
            Query.AddField(new FieldType { Name = "field_2", ResolvedType = new StringGraphType() }); // bypass HasField check
            Query.Fields.List[1].Name = "field";
        }
    }

    public class EmptyInterfaceSchema : Schema
    {
        public EmptyInterfaceSchema()
        {
            Query = new ObjectGraphType { Name = "Query" };
            Query.AddField(new FieldType { Name = "field", ResolvedType = new StringGraphType() });

            var iface = new InterfaceGraphType { Name = "Empty", ResolveType = _ => null };
            RegisterType(iface);
            Query.ResolvedInterfaces.Add(iface);
        }
    }

    public class SchemaWithDuplicateInterfaceFields : Schema
    {
        public SchemaWithDuplicateInterfaceFields()
        {
            Query = new ObjectGraphType { Name = "Query" };

            var iface = new InterfaceGraphType { Name = "Dup", ResolveType = _ => null };
            iface.AddField(new FieldType { Name = "field", ResolvedType = new StringGraphType() });
            iface.AddField(new FieldType { Name = "field_2", ResolvedType = new StringGraphType() }); // bypass HasField check
            iface.Fields.List[1].Name = "field";

            Query.AddField(new FieldType { Name = "field", ResolvedType = new StringGraphType() });
            RegisterType(iface);
            Query.ResolvedInterfaces.Add(iface);
        }
    }
}
