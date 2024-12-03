using GraphQL.DataLoader.Sample.DI.Types;
using GraphQL.Types;

namespace GraphQL.DataLoader.Sample.DI.GraphQl;

public sealed class SalespersonGraphType : ObjectGraphType<Salesperson>
{
    public SalespersonGraphType()
    {
        Field(x => x.Id, type: typeof(IntGraphType)).Description("Id of the salesman");
        Field(x => x.Name, type: typeof(StringGraphType)).Description("Name of the salesman");
        Field<ListGraphType<CarsGraphType>, IEnumerable<Car>>("assignedCars").Description("Assigned cars").ResolveAsync(ctx =>
        {
            var loader = ctx.RequestServices!.GetRequiredService<CarsBySalespersonDataLoader>();
            return loader.LoadAsync(ctx.Source.Id);
        });
    }
}
