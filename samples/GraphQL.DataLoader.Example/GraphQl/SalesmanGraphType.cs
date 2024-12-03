using GraphQL;
using GraphQL.DataLoader.Di.Sample.Types;
using GraphQL.Types;

namespace GraphQL.DataLoader.Di.Sample.GraphQl;

public class SalesmanGraphType : ObjectGraphType<Salesperson>
{
    public SalesmanGraphType()
    {
        Field(x => x.Id, type: typeof(IntGraphType)).Description("Id of the salesman");
        Field(x => x.Name, type: typeof(StringGraphType)).Description("Name of the salesman");
        Field<ListGraphType<CarsGraphType>, List<Car>>("assignedCars").Description("Assigned cars").ResolveAsync(ctx =>
        {
            var loader = ctx.RequestServices!.GetRequiredService<CarsBySalespersonDataLoader>();
            return loader.LoadAsync(ctx.Source.Id);
        });
    }
}
