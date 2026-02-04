using VerifyTestSG = GraphQL.Analyzers.Tests.VerifiersExtensions.CSharpIncrementalGeneratorVerifier<
    GraphQL.Analyzers.Tests.SourceGenerators.ProcessedSchemaDataTransformerTests.ReportingGenerator>;

namespace GraphQL.Analyzers.Tests.SourceGenerators;

/*
 * 
 * These tests rely on the following dependencies working properly:
 *   - CandidateProvider
 *   - KnownSymbolsProvider
 *   - CandidateClassTransformer
 *   - TypeSymbolTransformer
 *   - SchemaAttributeDataTransformer
 * 
 */

/// <summary>
/// Tests for GeneratedTypeDataTransformer that converts ISymbol-based data to primitive-only data.
/// </summary>
public partial class ProcessedSchemaDataTransformerTests
{
    [Fact]
    public async Task TransformsAllDataTypes()
    {
        const string source =
            """
            using GraphQL;
            using GraphQL.DI;
            using GraphQL.Types;
            using System;
            using System.Collections.Generic;
            
            // Output types
            public class Person
            {
                public string Name { get; set; }
                public int Age;
                public List<string> Tags { get; set; }
                public string GetFullName(string title) => title + " " + Name;
            }

            public interface IEntity
            {
                int Id { get; }
            }

            [InstanceSource(InstanceSource.NewInstance)]
            public class Product
            {
                public Product(string name, decimal price)
                {
                    Name = name;
                    Price = price;
                }
                
                public string Name { get; init; }
                public decimal Price { get; init; }
                public string Category { get; set; }
            }

            // Input types
            public class PersonInput
            {
                public PersonInput(string name, int age)
                {
                    Name = name;
                    Age = age;
                }
                
                public string Name { get; init; }
                public int Age { get; init; }
            }

            public class SearchInput
            {
                public string Query { get; set; }
                public int[]? Tags { get; set; }
                public HashSet<string>? Categories { get; set; }
            }

            // Query root
            public class Query
            {
                public Person GetPerson() => new Person();
                public IEntity GetEntity() => null;
            }

            [AotQueryType<Query>]
            [AotOutputType<Person>]
            [AotOutputType<IEntity>(Kind = OutputTypeKind.Interface)]
            [AotOutputType<Product>]
            [AotInputType<PersonInput>]
            [AotInputType<SearchInput>]
            public partial class MySchema : AotSchema
            {
                public MySchema(IServiceProvider services, IEnumerable<IConfigureSchema> configurations)
                    : base(services, configurations)
                {
                }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);
        output.ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public async Task WorksForNestedClasses()
    {
        const string source =
            """
            using GraphQL;
            using GraphQL.DI;
            using GraphQL.Types;
            using System;
            using System.Collections.Generic;

            namespace MyApp.GraphQL;

            public partial class Outer
            {
                internal partial class Inner
                {
                    public class Query
                    {
                    }
            
                    [AotQueryType<Query>]
                    internal partial class MySchema : AotSchema
                    {
                        public MySchema(IServiceProvider services, IEnumerable<IConfigureSchema> configurations)
                            : base(services, configurations)
                        {
                        }
                    }
                }
            }

            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);
        output.ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public async Task NoDuplicateNames()
    {
        const string source =
            """
            using GraphQL;
            using GraphQL.DI;
            using GraphQL.Types;
            using System;
            using System.Collections.Generic;

            namespace MyApp.GraphQL;

            public class Product
            {
            }

            public static class MyStatic
            {
                public class Product
                {
                }
            }

            public class Item<T>
            {
            }

            [AotOutputType<Product>]
            [AotOutputType<MyStatic.Product>]
            [AotInputType<Product>]
            [AotOutputType<Item<int>>]
            [AotOutputType<Item<long>>]
            internal partial class MySchema : AotSchema
            {
                public MySchema(IServiceProvider services, IEnumerable<IConfigureSchema> configurations)
                    : base(services, configurations)
                {
                }
            }

            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);
        output.ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public async Task HasConstructor_WithPublicConstructor_ReturnsTrue()
    {
        const string source =
            """
            using GraphQL;
            using GraphQL.DI;
            using GraphQL.Types;
            using System;
            using System.Collections.Generic;

            namespace MyApp.GraphQL;

            public class MyType
            {
                public string Name { get; set; }
            }

            [AotGraphType<MyType>]
            public partial class MySchema : AotSchema
            {
                public MySchema() : base(null!, null!) { }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);
        output.ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public async Task HasConstructor_WithNoConstructor_ReturnsFalse()
    {
        const string source =
            """
            using GraphQL;
            using GraphQL.DI;
            using GraphQL.Types;
            using System;
            using System.Collections.Generic;

            namespace MyApp.GraphQL;

            public class MyType
            {
                public string Name { get; set; }
            }

            [AotGraphType<MyType>]
            public partial class MySchema : AotSchema
            {
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);
        output.ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public async Task HasConstructor_WithGraphQLConstructorAttribute_ReturnsFalse()
    {
        const string source =
            """
            using GraphQL;
            using GraphQL.DI;
            using GraphQL.Types;
            using System;
            using System.Collections.Generic;

            namespace MyApp.GraphQL;

            public class MyType
            {
                public string Name { get; set; }
            }

            public partial class MySchema : AotSchema
            {
                [GraphQLConstructor]
                public MySchema() : base(null!, null!) { }
            }

            [AotGraphType<MyType>]
            public partial class MySchema : AotSchema
            {
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);
        output.ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public async Task PopulatesOverrideTypeNameFromAotRemapType()
    {
        const string source =
            """
            using GraphQL;
            using GraphQL.DI;
            using GraphQL.Types;
            using System;
            using System.Collections.Generic;

            namespace MyApp.GraphQL;

            public class Query
            {
                public string GetData() => "test";
            }

            [AotQueryType<Query>]
            [AotGraphType<IdGraphType>]
            [AotRemapType<IdGraphType, GuidGraphType>]
            [AotRemapType<DateGraphType, DateOnlyGraphType>]
            public partial class MySchema : AotSchema
            {
                public MySchema(IServiceProvider services, IEnumerable<IConfigureSchema> configurations)
                    : base(services, configurations)
                {
                }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);
        output.ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public async Task IdentifiesSourceStreamResolvers()
    {
        const string source =
            """
            using GraphQL;
            using GraphQL.DI;
            using GraphQL.Types;
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;

            namespace MyApp.GraphQL;

            public class Message
            {
                public string Text { get; set; }
            }

            public class Subscription
            {
                // IObservable<T>
                public IObservable<Message> OnMessage1() => null!;
                
                // Task<IObservable<T>>
                public Task<IObservable<Message>> OnMessage2() => null!;
                
                // ValueTask<IObservable<T>>
                public ValueTask<IObservable<Message>> OnMessage3() => default!;
                
                // IAsyncEnumerable<T>
                public IAsyncEnumerable<Message> OnMessage4() => null!;
                
                // Task<IAsyncEnumerable<T>>
                public Task<IAsyncEnumerable<Message>> OnMessage5() => null!;
                
                // ValueTask<IAsyncEnumerable<T>>
                public ValueTask<IAsyncEnumerable<Message>> OnMessage6() => default!;
                
                // Regular method (not a stream resolver)
                public Message GetMessage() => null!;
                
                // Task<T> (not a stream resolver)
                public Task<Message> GetMessageAsync() => null!;
            }

            [AotSubscriptionType<Subscription>]
            public partial class MySchema : AotSchema
            {
                public MySchema(IServiceProvider services, IEnumerable<IConfigureSchema> configurations)
                    : base(services, configurations)
                {
                }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);
        output.ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public async Task CapturesConstructorDataForCustomScalar()
    {
        const string source =
            """
            using GraphQL;
            using GraphQL.DI;
            using GraphQL.Types;
            using System;
            using System.Collections.Generic;

            namespace MyApp.GraphQL;

            public class Query
            {
                public string GetData() => "test";
            }

            // Custom scalar with constructor parameter
            public class CustomScalarGraphType : ScalarGraphType
            {
                public CustomScalarGraphType(string name)
                {
                    Name = name;
                }
            }

            // Custom scalar with multiple constructor parameters
            public class ComplexScalarGraphType : ScalarGraphType
            {
                public ComplexScalarGraphType(string name, string description, IServiceProvider services)
                {
                    Name = name;
                    Description = description;
                }
            }

            [AotQueryType<Query>]
            [AotGraphType<CustomScalarGraphType>]
            [AotGraphType<ComplexScalarGraphType>]
            public partial class MySchema : AotSchema
            {
                public MySchema(IServiceProvider services, IEnumerable<IConfigureSchema> configurations)
                    : base(services, configurations)
                {
                }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);
        output.ShouldMatchApproved(o => o.NoDiff());
    }
}
