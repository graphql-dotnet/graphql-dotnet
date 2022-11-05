using GraphQL.Types;

namespace GraphQL.Tests.Bugs.Bug1046;

public class Bug1046
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void any_registration_order_should_work(bool order)
    {
        var schema = new Schema { Query = new QueryGraphType() };

        if (order)
        {
            schema.RegisterType<ImplementationGraphType>();
            schema.RegisterType<InterfaceGraphType>();
        }
        else
        {
            schema.RegisterType<InterfaceGraphType>();
            schema.RegisterType<ImplementationGraphType>();
        }

        var response = new DocumentExecuter().ExecuteAsync(_ =>
        {
            _.Schema = schema;
            _.Query = @"
query {
  inst {
    id
  }
}
";
        }).Result;
        response.Data.ShouldNotBeNull();
    }
}

public class QueryGraphType : ObjectGraphType
{
    public QueryGraphType()
    {
        Field<InterfaceGraphType>("inst").Resolve(_ => new Implementation());
    }
}

public interface IInterface
{
    string Id { get; }
}

public class InterfaceGraphType : InterfaceGraphType<IInterface>
{
    public InterfaceGraphType()
    {
        Field(i => i.Id);
    }
}

public class Implementation : IInterface
{
    public string Id => "Data!";
}

public class ImplementationGraphType : ObjectGraphType<Implementation>
{
    public ImplementationGraphType()
    {
        Field(i => i.Id);
        Interface<InterfaceGraphType>();
    }
}
