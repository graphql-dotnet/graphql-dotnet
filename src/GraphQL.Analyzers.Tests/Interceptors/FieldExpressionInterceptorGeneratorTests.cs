using VerifyTestSG = GraphQL.Analyzers.Tests.VerifiersExtensions.CSharpIncrementalGeneratorVerifier<
    GraphQL.Analyzers.Interceptors.FieldExpressionInterceptorGenerator>;

namespace GraphQL.Analyzers.Tests.Interceptors;

/// <summary>
/// Tests for FieldExpressionInterceptorGenerator.
/// These tests verify that the generator correctly identifies and intercepts
/// ComplexGraphType.Field calls with expression parameters.
/// </summary>
public class FieldExpressionInterceptorGeneratorTests
{
    [Fact]
    public async Task DoesNotGenerateForFieldWithoutExpression()
    {
        // Field call without expression parameter should not be intercepted
        const string source =
            """
            using GraphQL.Types;

            namespace Sample;

            public class Person
            {
                public string Name { get; set; } = "";
            }

            public class PersonType : ObjectGraphType<Person>
            {
                public PersonType()
                {
                    Field<StringGraphType>("name");
                }
            }
            """;

        // Should not generate any interceptors
        await VerifyTestSG.VerifyIncrementalGeneratorAsync(source);
    }

    [Fact]
    public async Task GeneratesInterceptorForSimpleFieldWithExpression()
    {
        // Field call with expression parameter should be intercepted
        const string source =
            """
            using GraphQL.Types;

            namespace Sample;

            public class Person
            {
                public string Name { get; set; } = "";
            }

            public class PersonType : ObjectGraphType<Person>
            {
                public PersonType()
                {
                    Field("name", p => p.Name);
                }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        // Should generate an interceptor
        output.ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public async Task GeneratesInterceptorForFieldWithNullableParameter()
    {
        // Field call with nullable parameter should be intercepted
        const string source =
            """
            using GraphQL.Types;

            namespace Sample;

            public class Person
            {
                public string Name { get; set; } = "";
            }

            public class PersonType : ObjectGraphType<Person>
            {
                public PersonType()
                {
                    Field("name", p => p.Name, nullable: true);
                }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        // Should generate an interceptor
        output.ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public async Task GeneratesInterceptorForFieldWithTypeParameter()
    {
        // Field call with type parameter should be intercepted
        const string source =
            """
            using GraphQL.Types;

            namespace Sample;

            public class Person
            {
                public string Name { get; set; } = "";
            }

            public class PersonType : ObjectGraphType<Person>
            {
                public PersonType()
                {
                    Field("name", p => p.Name, typeof(StringGraphType));
                }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        // Should generate an interceptor
        output.ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public async Task GeneratesMultipleInterceptorsForMultipleFields()
    {
        // Multiple Field calls should generate multiple interceptors
        const string source =
            """
            using GraphQL.Types;

            namespace Sample;

            public class Person
            {
                public string Name { get; set; } = "";
                public int Age { get; set; }
                public string Email { get; set; } = "";
            }

            public class PersonType : ObjectGraphType<Person>
            {
                public PersonType()
                {
                    Field("name", p => p.Name);
                    Field("age", p => p.Age);
                    Field("email", p => p.Email);
                }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        // Should generate interceptors
        output.ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public async Task GeneratesInterceptorForFieldWithoutNameParameter()
    {
        // Field call with only expression (name inferred) should be intercepted
        const string source =
            """
            using GraphQL.Types;

            namespace Sample;

            public class Person
            {
                public string Name { get; set; } = "";
            }

            public class PersonType : ObjectGraphType<Person>
            {
                public PersonType()
                {
                    Field(p => p.Name);
                }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        // Should generate an interceptor
        output.ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public async Task GeneratesInterceptorForComplexPropertyType()
    {
        // Field with complex property type should be intercepted
        const string source =
            """
            using System.Collections.Generic;
            using GraphQL.Types;

            namespace Sample;

            public class Address
            {
                public string Street { get; set; } = "";
            }

            public class Person
            {
                public List<Address> Addresses { get; set; } = new();
            }

            public class PersonType : ObjectGraphType<Person>
            {
                public PersonType()
                {
                    Field("addresses", p => p.Addresses);
                }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        // Should generate an interceptor
        output.ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public async Task GeneratesInterceptorForNestedType()
    {
        // Field on nested type should be intercepted
        const string source =
            """
            using GraphQL.Types;

            namespace Sample;

            public class Outer
            {
                public class Person
                {
                    public string Name { get; set; } = "";
                }
            }

            public class PersonType : ObjectGraphType<Outer.Person>
            {
                public PersonType()
                {
                    Field("name", p => p.Name);
                }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        // Should generate an interceptor
        output.ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public async Task DoesNotGenerateForInputObjectGraphType()
    {
        // InputObjectGraphType should not generate interceptors (no resolvers)
        const string source =
            """
            using GraphQL.Types;

            namespace Sample;

            public class PersonInput
            {
                public string Name { get; set; } = "";
            }

            public class PersonInputType : InputObjectGraphType<PersonInput>
            {
                public PersonInputType()
                {
                    Field(p => p.Name);
                }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        // InputObjectGraphType uses Field with expressions but for parsing, not resolving
        // The interceptor should still be generated but won't set a resolver
        output.ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public async Task GeneratesInterceptorWithCorrectLineAndColumn()
    {
        // Verify that InterceptsLocation has correct file path, line, and column
        const string source =
            """
            using GraphQL.Types;

            namespace Sample;

            public class Person
            {
                public string Name { get; set; } = "";
            }

            public class PersonType : ObjectGraphType<Person>
            {
                public PersonType()
                {
                    Field("name", p => p.Name);
                }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        // Should contain InterceptsLocation attribute with file path and position
        output.ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public async Task GeneratesInterceptorForGenericPropertyType()
    {
        // Field with generic property type should be intercepted
        const string source =
            """
            using System.Threading.Tasks;
            using GraphQL.Types;

            namespace Sample;

            public class Person
            {
                public Task<string> NameAsync { get; set; } = Task.FromResult("");
            }

            public class PersonType : ObjectGraphType<Person>
            {
                public PersonType()
                {
                    Field("name", p => p.NameAsync);
                }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        // Should generate an interceptor
        output.ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public async Task GeneratesInterceptorForNullableValueType()
    {
        // Field with nullable value type should be intercepted
        const string source =
            """
            using GraphQL.Types;

            namespace Sample;

            public class Person
            {
                public int? Age { get; set; }
            }

            public class PersonType : ObjectGraphType<Person>
            {
                public PersonType()
                {
                    Field("age", p => p.Age);
                }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        // Should generate an interceptor
        output.ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public async Task GeneratesInterceptorPreservingMetadata()
    {
        // Verify that the generated interceptor preserves field metadata
        const string source =
            """
            using GraphQL.Types;

            namespace Sample;

            public class Person
            {
                public string Name { get; set; } = "";
            }

            public class PersonType : ObjectGraphType<Person>
            {
                public PersonType()
                {
                    Field("name", p => p.Name);
                }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        // Should contain metadata preservation code
        output.ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public async Task GeneratesInterceptorWithoutExpressionFieldResolver()
    {
        // Verify that the generated interceptor does NOT set ExpressionFieldResolver
        const string source =
            """
            using GraphQL.Types;

            namespace Sample;

            public class Person
            {
                public string Name { get; set; } = "";
            }

            public class PersonType : ObjectGraphType<Person>
            {
                public PersonType()
                {
                    Field("name", p => p.Name);
                }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        // Should not set ExpressionFieldResolver
        output.ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public async Task GeneratesInterceptorForMultipleTypesInSameFile()
    {
        // Multiple types with Field calls should generate separate interceptor files
        const string source =
            """
            using GraphQL.Types;

            namespace Sample;

            public class Person
            {
                public string Name { get; set; } = "";
            }

            public class Company
            {
                public string CompanyName { get; set; } = "";
            }

            public class PersonType : ObjectGraphType<Person>
            {
                public PersonType()
                {
                    Field("name", p => p.Name);
                }
            }

            public class CompanyType : ObjectGraphType<Company>
            {
                public CompanyType()
                {
                    Field("companyName", c => c.CompanyName);
                }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        // Should generate interceptors for both types
        output.ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public async Task GeneratesInterceptorForFieldWithExpressionAndNullable()
    {
        // Field call with expression (no name) and nullable parameter should be intercepted
        const string source =
            """
            using GraphQL.Types;

            namespace Sample;

            public class Person
            {
                public string Name { get; set; } = "";
            }

            public class PersonType : ObjectGraphType<Person>
            {
                public PersonType()
                {
                    Field(p => p.Name, nullable: true);
                }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        // Should generate an interceptor
        output.ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public async Task GeneratesInterceptorForFieldWithExpressionAndType()
    {
        // Field call with expression (no name) and type parameter should be intercepted
        const string source =
            """
            using GraphQL.Types;

            namespace Sample;

            public class Person
            {
                public string Name { get; set; } = "";
            }

            public class PersonType : ObjectGraphType<Person>
            {
                public PersonType()
                {
                    Field(p => p.Name, typeof(StringGraphType));
                }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        // Should generate an interceptor
        output.ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public async Task DoesNotGenerateForFieldWithTypeOnly()
    {
        // Field call with only Type parameter (no expression) should not be intercepted
        const string source =
            """
            using GraphQL.Types;

            namespace Sample;

            public class Person
            {
                public string Name { get; set; } = "";
            }

            public class PersonType : ObjectGraphType<Person>
            {
                public PersonType()
                {
                    Field("name", typeof(StringGraphType));
                }
            }
            """;

        // Should not generate any interceptors
        await VerifyTestSG.VerifyIncrementalGeneratorAsync(source);
    }

    [Fact]
    public async Task DoesNotGenerateForFieldWithIGraphTypeOnly()
    {
        // Field call with IGraphType parameter (no expression) should not be intercepted
        const string source =
            """
            using GraphQL.Types;

            namespace Sample;

            public class Person
            {
                public string Name { get; set; } = "";
            }

            public class PersonType : ObjectGraphType<Person>
            {
                public PersonType()
                {
                    Field("name", new StringGraphType());
                }
            }
            """;

        // Should not generate any interceptors
        await VerifyTestSG.VerifyIncrementalGeneratorAsync(source);
    }

    [Fact]
    public async Task DoesNotGenerateForFieldWithReturnTypeOnly()
    {
        // Field call with only TReturnType generic (no expression) should not be intercepted
        const string source =
            """
            using GraphQL.Types;

            namespace Sample;

            public class Person
            {
                public string Name { get; set; } = "";
            }

            public class PersonType : ObjectGraphType<Person>
            {
                public PersonType()
                {
                    Field<string>("name");
                }
            }
            """;

        // Should not generate any interceptors
        await VerifyTestSG.VerifyIncrementalGeneratorAsync(source);
    }

    [Fact]
    public async Task GeneratesDiagnosticForComplexExpression()
    {
        // Field call with complex expression (method call) should generate a diagnostic
        const string source =
            """
            using GraphQL.Types;

            namespace Sample;

            public class Person
            {
                public string Name { get; set; } = "";
            }

            public class PersonType : ObjectGraphType<Person>
            {
                public PersonType()
                {
                    Field("name", p => p.Name.ToString());
                }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source);

        // Should generate a diagnostic warning about complex expression
        output.ShouldMatchApproved(o => o.NoDiff());
    }
}
