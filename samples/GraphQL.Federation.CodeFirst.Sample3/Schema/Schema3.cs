namespace GraphQL.Federation.CodeFirst.Sample3.Schema;

public class Schema3 : GraphQL.Types.Schema
{
    public Schema3(IServiceProvider services) : base(services)
    {
        Query = services.GetRequiredService<QueryGraphType>();
        // since no fields are defined in the QueryGraphType, we need to register the types manually
        this.RegisterType<ReviewGraphType>();
    }
}
