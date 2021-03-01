using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Dummy;
using GraphQL.Language.AST;
using GraphQL.StarWars.Types;
using GraphQL.Types;
using GraphQL.Utilities;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Extensions
{
    public class GraphQLExtensionsTests
    {
        public class IsValidDefaultTestData : IEnumerable<object[]>
        {
            public class Person
            {
                public int Age { get; set; }

                public string Name { get; set; }
            }

            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { new BooleanGraphType(), true, true };
                yield return new object[] { new BooleanGraphType(), false, true };
                yield return new object[] { new BooleanGraphType(), null, true };

                yield return new object[] { new NonNullGraphType(new BooleanGraphType()), false, true };
                yield return new object[] { new NonNullGraphType(new BooleanGraphType()), null, false };

                yield return new object[] { new ListGraphType(new BooleanGraphType()), null, true };
                yield return new object[] { new ListGraphType(new BooleanGraphType()), new object[] { true, false, null }, true };
                yield return new object[] { new NonNullGraphType(new ListGraphType(new BooleanGraphType())), null, false };
                yield return new object[] { new ListGraphType(new NonNullGraphType(new BooleanGraphType())), new object[] { true, false, null }, false };
                yield return new object[] { new ListGraphType(new NonNullGraphType(new BooleanGraphType())), new object[] { true, false, true }, true };

                yield return new object[] { new InputObjectGraphType<Person>(), null, true };
                yield return new object[] { new NonNullGraphType(new InputObjectGraphType<Person>()), null, false };
                yield return new object[] { new InputObjectGraphType<Person>(), new Person(), true };
                yield return new object[] { new InputObjectGraphType<Person>(), "aaa", false };

                // https://github.com/graphql-dotnet/graphql-dotnet/issues/2334
                yield return new object[] { new ListGraphType(new BooleanGraphType()), true, true };
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public class IsValidDefaultExceptionTestData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { null, 0, new ArgumentNullException("type") };
                yield return new object[] { new ObjectGraphType(), 0, new ArgumentOutOfRangeException("type", "Must provide Input Type, cannot use ObjectGraphType 'Object'") };
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public class ToASTTestData : IEnumerable<object[]>
        {
            public class Person
            {
                public int Age { get; set; }

                public string Name { get; set; }
            }

            public class PersonInputType : InputObjectGraphType<Person>
            {
                public override IValue ToAST(object value)
                {
                    var person = (Person)value;

                    return new ObjectValue(new[]
                    {
                        new ObjectField("Name", new StringValue(person.Name)),
                        new ObjectField("Age", new IntValue(person.Age))
                    });
                }
            }

            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { null, true, new NullValue() };

                yield return new object[] { new BooleanGraphType(), true, new BooleanValue(true) };
                yield return new object[] { new BooleanGraphType(), false, new BooleanValue(false) };
                yield return new object[] { new BooleanGraphType(), null, new NullValue() };

                yield return new object[] { new NonNullGraphType(new BooleanGraphType()), false, new BooleanValue(false) };
                //yield return new object[] { new NonNullGraphType(new BooleanGraphType()), null, false }; //TODO: exception ?

                yield return new object[] { new ListGraphType(new BooleanGraphType()), null, new NullValue() };
                yield return new object[] { new ListGraphType(new BooleanGraphType()), new object[] { true, false, null }, new ListValue(new IValue[] { new BooleanValue(true), new BooleanValue(false), new NullValue() }) };
                //yield return new object[] { new NonNullGraphType(new ListGraphType(new BooleanGraphType())), null, false }; //TODO: exception ?
                //yield return new object[] { new ListGraphType(new NonNullGraphType(new BooleanGraphType())), new object[] { true, false, null }, false }; //TODO: exception ?
                yield return new object[] { new ListGraphType(new NonNullGraphType(new BooleanGraphType())), new object[] { true, false, true }, new ListValue(new IValue[] { new BooleanValue(true), new BooleanValue(false), new BooleanValue(true) }) };

                yield return new object[] { new InputObjectGraphType<Person>(), null, new NullValue() };
                // yield return new object[] { new NonNullGraphType(new InputObjectGraphType<Person>()), null, false }; //TODO: exception ?
                yield return new object[] { new PersonInputType(), new Person { Name = "Tom", Age = 42 }, new ObjectValue(new[]
                    {
                        new ObjectField("Name", new StringValue("Tom")),
                        new ObjectField("Age", new IntValue(42))
                    }) };

                yield return new object[] { new ListGraphType(new BooleanGraphType()), true, new BooleanValue(true) };
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public class ToASTExceptionTestData : IEnumerable<object[]>
        {
            public class Person
            {
                public int Age { get; set; }

                public string Name { get; set; }
            }

            public class BadPersonInputType : InputObjectGraphType<Person>
            {
                public override IValue ToAST(object value) => null;
            }

            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { new ObjectGraphType(), 0, new ArgumentOutOfRangeException("type", "Must provide Input Type, cannot use ObjectGraphType 'Object'") };
                yield return new object[] { new InputObjectGraphType<Person>(), new Person(), new NotImplementedException("Please override the 'ToAST' method of the 'InputObjectGraphType`1' scalar to support this operation.") };
                yield return new object[] { new BadPersonInputType(), new Person(), new InvalidOperationException("Unable to convert the 'GraphQL.Tests.Extensions.GraphQLExtensionsTests+ToASTExceptionTestData+Person' of the input object type 'BadPersonInputType' to an AST representation.") };
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        [Theory]
        [ClassData(typeof(IsValidDefaultTestData))]
        public void IsValidDefault_Test(IGraphType type, object value, bool expected) => type.IsValidDefault(value).ShouldBe(expected);

        [Theory]
        [ClassData(typeof(IsValidDefaultExceptionTestData))]
        public void IsValidDefault_Exception_Test(IGraphType type, object value, Exception expected)
        {
            Should.Throw(() => type.IsValidDefault(value), expected.GetType()).Message.ShouldBe(expected.Message);
        }

        [Theory]
        [ClassData(typeof(ToASTTestData))]
        public void ToAST_Test(IGraphType type, object value, IValue expected)
        {
            var actual = AstPrinter.Print(type.ToAST(value));
            var result = AstPrinter.Print(expected);
            actual.ShouldBe(result);
        }

        [Theory]
        [ClassData(typeof(ToASTExceptionTestData))]
        public void ToAST_Exception_Test(IGraphType type, object value, Exception expected)
        {
            Should.Throw(() => type.ToAST(value), expected.GetType()).Message.ShouldBe(expected.Message);
        }

        [Fact]
        public void BuildNamedType_ResolveReturnNull_Throws()
        {
            Should.Throw<InvalidOperationException>(() => typeof(ListGraphType<ListGraphType<EpisodeEnum>>).BuildNamedType(_ => null));
        }

        [Fact]
        public void GetResult_Extension_Should_Work()
        {
            var task1 = Task.FromResult(42);
            task1.GetResult().ShouldBe(42);

            var obj = new object();
            var task2 = Task.FromResult(obj);
            task2.GetResult().ShouldBe(obj);

            IEnumerable collection = new List<string>();
            var task3 = Task.FromResult(collection);
            task3.GetResult().ShouldBe(collection);

            ILookup<string, EqualityComparer<DateTime>> lookup = new List<EqualityComparer<DateTime>>().ToLookup(i => i.GetHashCode().ToString());
            var task4 = Task.FromResult(lookup);
            task4.GetResult().ShouldBe(lookup);

            var task5 = DataSource.GetSomething();
            task5.GetResult().ShouldNotBeNull();
        }
    }
}
