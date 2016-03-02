using System;
using System.Collections.Generic;
using GraphQL.Types;

namespace GraphQL.Tests.Execution
{
    public class UnionInterfaceTests : QueryTestBase<UnionSchema>
    {
        private readonly Person _john;

        public UnionInterfaceTests()
        {
            Services.Register<DogType>();
            Services.Register<CatType>();
            Services.Register<PetType>();
            Services.Register<PersonType>();

            Services.Singleton(() => new UnionSchema(type => (GraphType) Services.Get(type)));

            var garfield = new Cat
            {
                Name = "Garfield",
                Meows = false
            };

            var odie = new Dog
            {
                Name = "Odie",
                Barks = true
            };

            var liz = new Person
            {
                Name = "Liz",
                Pets = new List<IPet>(),
                Friends = new List<INamed>()
            };

            _john = new Person
            {
                Name = "John",
                Pets = new List<IPet>
                {
                    garfield,
                    odie
                },
                Friends = new List<INamed>
                {
                    liz,
                    odie
                }
            };
        }

        [Test]
        public void can_introspect_on_union_and_intercetion_types()
        {
            var query = @"
            query AQuery {
                Named: __type(name: 'Named') {
                  kind
                  name
                  fields { name }
                    interfaces { name }
                    possibleTypes { name }
                    enumValues { name }
                    inputFields { name }
                }
                Pet: __type(name: 'Pet') {
                  kind
                  name
                  fields { name }
                    interfaces { name }
                    possibleTypes { name }
                    enumValues { name }
                    inputFields { name }
                }
            }
            ";

            var expected = @"{
                named: {
                  kind: 'INTERFACE',
                  name: 'Named',
                  fields: [
                    { name: 'name' }
                  ],
                  interfaces: null,
                  possibleTypes: [
                    { name: 'Person' },
                    { name: 'Dog' },
                    { name: 'Cat' }
                  ],
                  enumValues: null,
                  inputFields: null
                },
                pet: {
                  kind: 'UNION',
                  name: 'Pet',
                  fields: null,
                  interfaces: null,
                  possibleTypes: [
                    { name: 'Dog' },
                    { name: 'Cat' }
                  ],
                  enumValues: null,
                  inputFields: null
                }
            }";

            AssertQuerySuccess(query, expected);
        }

        [Test]
        public void executes_using_union_types()
        {
            // NOTE: This is an *invalid* query, but it should be an *executable* query.

            var query = @"
                query AQuery {
                  __typename
                  name
                  pets {
                    __typename
                    name
                    barks
                    meows
                  }
                }
            ";

            var expected = @"
                {
                  __typename: 'Person',
                  name: 'John',
                  pets: [
                    { __typename:  'Cat', name: 'Garfield', meows: false },
                    { __typename:  'Dog', name: 'Odie', barks: true }
                  ]
                }
            ";

            AssertQuerySuccess(query, expected, root: _john);
        }

        [Test]
        public void executes_union_types_with_inline_fragments()
        {
            // This is the valid version of the query in the above test.

            var query = @"
                query AQuery {
                  __typename
                  name
                  pets {
                    __typename
                    ... on Dog {
                      name
                      barks
                    },
                    ... on Cat {
                      name
                      meows
                    }
                  }
                }
            ";

            var expected = @"
                {
                  __typename: 'Person',
                  name: 'John',
                  pets: [
                    { __typename:  'Cat', name: 'Garfield', meows: false },
                    { __typename:  'Dog', name: 'Odie', barks: true }
                  ]
                }
            ";

            AssertQuerySuccess(query, expected, root: _john);
        }

        [Test]
        public void executes_using_interface_types()
        {
            // NOTE: This is an *invalid* query, but it should be an *executable* query.

            var query = @"
                query AQuery {
                  __typename
                  name
                  friends {
                    __typename
                    name
                    barks
                    meows
                  }
                }
            ";

            var expected = @"
                {
                  __typename: 'Person',
                  name: 'John',
                  friends: [
                    { __typename:  'Person', name: 'Liz' },
                    { __typename:  'Dog', name: 'Odie', barks: true }
                  ]
                }
            ";

            AssertQuerySuccess(query, expected, root: _john);
        }

        [Test]
        public void allows_fragment_conditions_to_be_abstract_types()
        {
            var query = @"
                query AQuery {
                  __typename
                  name
                  pets { ...PetFields }
                  friends { ...FriendFields }
                }
                fragment PetFields on Pet {
                  __typename
                  ... on Dog {
                    name
                    barks
                  },
                  ... on Cat {
                    name
                    meows
                  }
                }
                fragment FriendFields on Named {
                  __typename
                  name
                  ... on Dog {
                    barks
                  },
                  ... on Cat {
                    meows
                  }
                }
            ";

            var expected = @"
                {
                  __typename: 'Person',
                  name: 'John',
                  pets: [
                    { __typename:  'Cat', name: 'Garfield', meows: false },
                    { __typename:  'Dog', name: 'Odie', barks: true }
                  ],
                  friends: [
                    { __typename:  'Person', name: 'Liz' },
                    { __typename:  'Dog', name: 'Odie', barks: true }
                  ]
                }
            ";

            AssertQuerySuccess(query, expected, root: _john);
        }
    }

    public interface INamed
    {
        string Name { get; set; }
    }

    public interface IPet : INamed
    {
    }

    public class Dog : IPet
    {
        public string Name { get; set; }
        public bool Barks { get; set; }
    }

    public class Cat : IPet
    {
        public string Name { get; set; }
        public bool Meows { get; set; }
    }

    public class Person : INamed
    {
        public string Name { get; set; }
        public List<IPet> Pets { get; set; }
        public List<INamed> Friends { get; set; }
    }

    public class NamedType : InterfaceGraphType
    {
        public NamedType(DogType dogType, CatType catType, PersonType personType)
        {
            Name = "Named";

            Field<StringGraphType>("name");

            ResolveType = obj =>
            {
                if (obj is Dog)
                {
                    return dogType;
                }

                if (obj is Cat)
                {
                    return catType;
                }

                if (obj is Person)
                {
                    return personType;
                }

                return null;
            };
        }
    }

    public class DogType : ObjectGraphType
    {
        public DogType()
        {
            Name = "Dog";

            Field<StringGraphType>("name");
            Field<BooleanGraphType>("barks");

            Interface<NamedType>();
        }
    }

    public class CatType : ObjectGraphType
    {
        public CatType()
        {
            Name = "Cat";

            Field<StringGraphType>("name");
            Field<BooleanGraphType>("meows");

            Interface<NamedType>();
        }
    }

    public class PetType : UnionGraphType
    {
        public PetType(DogType dogType, CatType catType)
        {
            Name = "Pet";

            Type<DogType>();
            Type<CatType>();

            ResolveType = obj =>
            {
                if (obj is Dog)
                {
                    return dogType;
                }

                if (obj is Cat)
                {
                    return catType;
                }

                return null;
            };
        }
    }

    public class PersonType : ObjectGraphType
    {
        public PersonType()
        {
            Name = "Person";

            Field<StringGraphType>("name");
            Field<ListGraphType<PetType>>("pets");
            Field<ListGraphType<NamedType>>("friends");

            Interface<NamedType>();
        }
    }

    public class UnionSchema : Schema
    {
        public UnionSchema(Func<Type, GraphType> resolveType)
            : base(resolveType)
        {
            Query = (ObjectGraphType)resolveType(typeof(PersonType));
        }
    }
}
