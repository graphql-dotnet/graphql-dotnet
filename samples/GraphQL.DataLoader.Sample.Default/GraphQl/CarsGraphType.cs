using GraphQL.DataLoader.Di.Sample.Types;
using GraphQL.Types;

namespace GraphQL.DataLoader.Di.Sample.GraphQl;

public sealed class CarsGraphType : ObjectGraphType<Car>
{
    public CarsGraphType()
    {
        Field(x => x.Id).Description("Id of the car");
        Field(x => x.Model).Description("Model of the car");
        Field(x => x.Price).Description("Price of the car");
        Field(x => x.ModelYear).Description("Model year of the car");
    }
}
