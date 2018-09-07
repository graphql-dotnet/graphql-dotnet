using GraphQL.Execution;
using GraphQL.Tests.Execution;
using GraphQL.Types;
using Xunit;

namespace GraphQL.Tests.NodeFilter
{
    public class NodeFilterTest : BasicNodeFilterQueryTestBase

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

            var address = new ObjectGraphType();
            address.Name = "Address";
            address.Field("id", new IdGraphType());
            address.Field("street", new StringGraphType());
            address.Field("city", new StringGraphType());
            address.Field("state", new StringGraphType());

            var business = new ObjectGraphType();
            business.Name = "Business";
            business.Field("id", new IdGraphType());
            business.Field("name", new StringGraphType());
            business.Field("address", address);

            var person = new ObjectGraphType();
            person.Name = "Person";
            person.Field("id", new StringGraphType());
            person.Field("name", new StringGraphType());
            person.Field("business", business);

            var query = new ObjectGraphType();
            query.Name = "Query";
            query.Field(
                "person",
                person,
                resolve: ctx =>
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
        public void FilterOutTopNodeAndChildren()
        {
            var schema = build_schema();
            AssertQuerySuccess(_ =>
                {
                    _.NodeFilter = node => !(node.Source is Person);
                    _.Schema = build_schema();
                    _.Query = @"
                {
                  person {
                    name
                    business { id name }
                    business { id address { id street city state } }
                  }
                }";
                },
                "{ person: { } }");
        }

        [Fact]
        public void FilterOutLeafNode()
        {
            var schema = build_schema();
            AssertQuerySuccess(_ =>
                {
                    _.NodeFilter = node => !(node.Source is Address);
                    _.Schema = build_schema();
                    _.Query = @"
                {
                  person {
                    business { id name address { id street city state } }
                  }
                }";
                },
                "{ person: { business: { id: \"4\", name: \"Stuntman Express\", address: { } } } }");
        }
    }
}
