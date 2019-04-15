using GraphQL.Types;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    public class Issue1110 : QueryTestBase<Issue1110Schema>
    {
        [Fact]
        public void Issue_1110_Should_Work()
        {
            var query = @"
query somequery($id: Int, $obj: ProductDataInput!) {
  productdata(id: $id, obj: $obj)
  {
    productId
    productName
    sKU
  }
}
";
            var expected = @"{
  ""productdata"": {
    ""productId"": 57,
    ""productName"": ""P2800"",
    ""sKU"": ""S100""
  }
}";

            var vars = @"
{
""id"":2,
""obj"":{
  ""dimensions"": [
    [""12"",""12"",""12"",""35""],
    [""15"",""10"",""14"",""45""]
  ],
  ""specifications"": [
    [""1800"",""2400""],
    [""2000"",""2800""]
  ],
  ""price"": 99.99
  }
}";

            AssertQuerySuccess(query, expected, vars.ToInputs());
        }
    }
    
    public class Issue1110Schema : Schema
    {
        public Issue1110Schema()
        {
            Query = new ProductQuery();
        }
    }

    public class Product
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string SKU { get; set; }
    }

    public class ProductData
    {
        public string[][] Dimensions { get; set; }
        public string[][] Specifications { get; set; }
        public decimal Price { get; set; }              // note - I changed it to decimal
    }

    public class ProductDataInput : InputObjectGraphType<ProductData>
    {
        public ProductDataInput()
        {
            Name = "ProductDataInput";
            Field<ListGraphType<ListGraphType<StringGraphType>>>("Dimensions");
            Field<ListGraphType<ListGraphType<StringGraphType>>>("Specifications");
            Field<NonNullGraphType<DecimalGraphType>>("Price");
        }
    }

    public class ProductQuery : ObjectGraphType
    {
        public ProductQuery(/*ContextServiceLocator contextServiceLocator*/)
        {
            Field<ProductType>(
               "productdata",
               arguments: new QueryArguments(new QueryArgument<IntGraphType> { Name = "id" },
               new QueryArgument<NonNullGraphType<ProductDataInput>> { Name = "obj" }),
               resolve: context =>
               {
                   var arg = context.GetArgument<ProductData>("obj");
                   return new Product
                   {
                       // these calculations are designed to demonstrate the existence of the necessary arguments
                       ProductId = int.Parse(arg.Dimensions[0][0]) + int.Parse(arg.Dimensions[1][3]),
                       ProductName = "P" + arg.Specifications[1][1],
                       SKU = "S" + (int)(arg.Price + 0.01m)
                   };
                   //return contextServiceLocator.GetRepository<ProductType>().GetById(context.GetArgument<int>("id"));
               });
        }
    }

    public class ProductType : ObjectGraphType<Product>
    {
        public ProductType()
        {
            Field(x => x.ProductId, type: typeof(IntGraphType));
            Field(x => x.ProductName, type: typeof(StringGraphType));
            Field(x => x.SKU, type: typeof(StringGraphType));
        }
    }
}
