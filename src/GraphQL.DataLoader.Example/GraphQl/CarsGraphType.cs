using DataLoaderGql.Types;
using GraphQL.Types;

namespace DataLoaderGql.GraphQl;

public class CarsGraphType : ObjectGraphType<Car>
{
    public CarsGraphType()
    {
        Field(x => x.Id, type: typeof(IntGraphType)).Description("Id of the car");
        Field(x => x.Model, type: typeof(StringGraphType)).Description("Model of the car");
        Field(x => x.Price, type: typeof(IntGraphType)).Description("Price of the car");
        Field(x => x.ModelYear, type: typeof(IntGraphType)).Description("Model year of the car");
    }
}