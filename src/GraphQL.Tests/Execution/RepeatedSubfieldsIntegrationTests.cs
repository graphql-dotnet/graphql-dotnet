using GraphQL.Types;

namespace GraphQL.Tests.Execution;

public class RepeatedSubfieldsIntegrationTests : BasicQueryTestBase
{
    public class Person
    {
        public string Name { get; set; }
        public Business Business { get; set; }
    }

    public class Business
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public Address Address { get; set; }
    }

    public class Address
    {
        public string Id { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
    }

    public ISchema build_schema()
    {
        var schema = new Schema();

        var address = new ObjectGraphType { Name = "Address" };
        address.Field("id", new IdGraphType());
        address.Field("street", new StringGraphType());
        address.Field("city", new StringGraphType());
        address.Field("state", new StringGraphType());

        var business = new ObjectGraphType { Name = "Business" };
        business.Field("id", new IdGraphType());
        business.Field("name", new StringGraphType());
        business.Field("address", address);

        var person = new ObjectGraphType { Name = "Person" };
        person.Field("id", new StringGraphType());
        person.Field("name", new StringGraphType());
        person.Field("business", business);

        var query = new ObjectGraphType { Name = "Query" };
        query.Field("person", person)
            .Resolve(ctx =>
            {
                return new Person
                {
                    Name = "Quinn",
                    Business = new Business
                    {
                        Id = "4",
                        Name = "Stuntman Express",
                        Address = new Address
                        {
                            Id = "123",
                            Street = "Las Vegas Blvd",
                            City = "Las Vegas",
                            State = "NV"
                        }
                    }
                };
            });

        schema.Query = query;
        return schema;
    }

    [Fact]
    public void combines_fields()
    {
        var schema = build_schema();
        AssertQuerySuccess(_ =>
        {
            _.Schema = schema;
            _.Query = @"
                {
                  person {
                    business { id name }
                    business { id address { id street city state } }
                  }
                }";
        },
        @"{
              ""person"": {
                ""business"": {
                  ""id"": ""4"",
                  ""name"": ""Stuntman Express"",
                  ""address"": {
                    ""id"": ""123"",
                    ""street"": ""Las Vegas Blvd"",
                    ""city"": ""Las Vegas"",
                    ""state"": ""NV""
                  }
                }
              }
            }");
    }
}
