using System.Text.RegularExpressions;
using VerifyTestSG = GraphQL.Analyzers.Tests.VerifiersExtensions.CSharpIncrementalGeneratorVerifier<
    GraphQL.Analyzers.Interceptors.ResolveDelegateInterceptorGenerator>;

namespace GraphQL.Analyzers.Tests.Interceptors;

/// <summary>
/// Tests for ResolveDelegateInterceptorGenerator.
/// These tests verify that the generator correctly identifies and intercepts
/// FieldBuilder.ResolveDelegate calls to produce AOT-compatible resolvers.
/// </summary>
public class ResolveDelegateInterceptorGeneratorTests
{
    private static string ReplaceHash(string input)
    {
        string result = Regex.Replace(
            input,
            @"(?<=ResolveDelegateInterceptors_)[A-Za-z0-9_+/=]+(?=\.g\.cs)" +                    // ResolveDelegateInterceptors_<hash>.g.cs
            @"|(?<=InterceptsLocationAttribute\(\d+,\s*"")[A-Za-z0-9_+/=]+(?=""\))" +            // InterceptsLocationAttribute(..., "<hash>")
            @"|(?<=\bResolveDelegate_)[A-Za-z0-9_]+(?=\()",                                       // ResolveDelegate_<hash>(
            "hash"
        );

        return result;
    }

    [Fact]
    public async Task DoesNotGenerateForNonResolveDelegateCall()
    {
        // Non-ResolveDelegate calls should not be intercepted
        const string source =
            """
            using GraphQL.Types;
            using GraphQL.Builders;

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
    public async Task GeneratesInterceptorForStaticMethodDelegate()
    {
        // ResolveDelegate with a static method group should be intercepted
        const string source =
            """
            using GraphQL.Types;
            using GraphQL.Builders;
            using GraphQL;

            namespace Sample;

            public class Person
            {
                public string Name { get; set; } = "";
            }

            public class PersonResolver
            {
                public static string ResolveName(IResolveFieldContext context)
                    => ((Person)context.Source!).Name;
            }

            public class PersonType : ObjectGraphType<Person>
            {
                public PersonType()
                {
                    Field<StringGraphType>("name")
                        .ResolveDelegate(PersonResolver.ResolveName);
                }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source, sort: false);
        ReplaceHash(output).ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public async Task GeneratesInterceptorForInstanceMethodDelegate()
    {
        // ResolveDelegate with an instance method group should be intercepted
        const string source =
            """
            using GraphQL.Types;
            using GraphQL.Builders;
            using GraphQL;

            namespace Sample;

            public class Person
            {
                public string Name { get; set; } = "";
            }

            public class PersonResolver
            {
                public string ResolveName(IResolveFieldContext context)
                    => ((Person)context.Source!).Name;
            }

            public class PersonType : ObjectGraphType<Person>
            {
                private readonly PersonResolver _resolver = new();

                public PersonType()
                {
                    Field<StringGraphType>("name")
                        .ResolveDelegate(_resolver.ResolveName);
                }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source, sort: false);
        ReplaceHash(output).ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public async Task GeneratesInterceptorForNullDelegate()
    {
        // ResolveDelegate(null) should generate a null resolver
        const string source =
            """
            using GraphQL.Types;
            using GraphQL.Builders;

            namespace Sample;

            public class Person
            {
                public string Name { get; set; } = "";
            }

            public class PersonType : ObjectGraphType<Person>
            {
                public PersonType()
                {
                    Field<StringGraphType>("name")
                        .ResolveDelegate(null);
                }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source, sort: false);
        ReplaceHash(output).ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public async Task GeneratesInterceptorForMethodWithGraphQLArguments()
    {
        // ResolveDelegate with a method that has GraphQL arguments should generate argument resolvers
        const string source =
            """
            using GraphQL.Types;
            using GraphQL.Builders;
            using GraphQL;

            namespace Sample;

            public class Person
            {
                public string Name { get; set; } = "";
            }

            public class PersonResolver
            {
                public static string ResolveName(string prefix)
                    => prefix + "Name";
            }

            public class PersonType : ObjectGraphType<Person>
            {
                public PersonType()
                {
                    Field<StringGraphType>("name")
                        .ResolveDelegate(PersonResolver.ResolveName);
                }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source, sort: false);
        ReplaceHash(output).ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public async Task GeneratesInterceptorForMethodWithCancellationToken()
    {
        // ResolveDelegate with a method that has CancellationToken should resolve it from context
        const string source =
            """
            using System.Threading;
            using System.Threading.Tasks;
            using GraphQL.Types;
            using GraphQL.Builders;
            using GraphQL;

            namespace Sample;

            public class Person
            {
                public string Name { get; set; } = "";
            }

            public class PersonResolver
            {
                public static Task<string> ResolveNameAsync(CancellationToken cancellationToken)
                    => Task.FromResult("Name");
            }

            public class PersonType : ObjectGraphType<Person>
            {
                public PersonType()
                {
                    Field<StringGraphType>("name")
                        .ResolveDelegate(PersonResolver.ResolveNameAsync);
                }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source, sort: false);
        ReplaceHash(output).ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public async Task GeneratesInterceptorForMethodWithMixedParameters()
    {
        // ResolveDelegate with a method that has both GraphQL arguments and context parameters
        const string source =
            """
            using System.Threading;
            using GraphQL.Types;
            using GraphQL.Builders;
            using GraphQL;

            namespace Sample;

            public class Person
            {
                public string Name { get; set; } = "";
            }

            public class PersonResolver
            {
                public static string ResolveName(string prefix, CancellationToken cancellationToken)
                    => prefix + "Name";
            }

            public class PersonType : ObjectGraphType<Person>
            {
                public PersonType()
                {
                    Field<StringGraphType>("name")
                        .ResolveDelegate(PersonResolver.ResolveName);
                }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source, sort: false);
        ReplaceHash(output).ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public async Task GeneratesInterceptorForMethodWithNoParameters()
    {
        // ResolveDelegate with a method that has no parameters
        const string source =
            """
            using GraphQL.Types;
            using GraphQL.Builders;

            namespace Sample;

            public class Person
            {
                public string Name { get; set; } = "";
            }

            public class PersonResolver
            {
                public static string ResolveConstantName()
                    => "ConstantName";
            }

            public class PersonType : ObjectGraphType<Person>
            {
                public PersonType()
                {
                    Field<StringGraphType>("name")
                        .ResolveDelegate(PersonResolver.ResolveConstantName);
                }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source, sort: false);
        ReplaceHash(output).ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public async Task GeneratesInterceptorForMethodWithFromServicesArgument()
    {
        // ResolveDelegate with a method that has a [FromServices] parameter
        // BuildArgument handles ParameterAttribute subclasses generically via GetParameterResolver
        const string source =
            """
            using GraphQL.Types;
            using GraphQL.Builders;
            using GraphQL;

            namespace Sample;

            public interface IMyService
            {
                string GetName();
            }

            public class Person
            {
                public string Name { get; set; } = "";
            }

            public class PersonResolver
            {
                public static string ResolveName([FromServices] IMyService service)
                    => service.GetName();
            }

            public class PersonType : ObjectGraphType<Person>
            {
                public PersonType()
                {
                    Field<StringGraphType>("name")
                        .ResolveDelegate(PersonResolver.ResolveName);
                }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source, sort: false);
        ReplaceHash(output).ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public async Task GeneratesInterceptorForMethodInSameClass()
    {
        // ResolveDelegate with an instance method defined in the same class as the graph type
        const string source =
            """
            using GraphQL.Types;
            using GraphQL.Builders;
            using GraphQL;

            namespace Sample;

            public class Person
            {
                public string Name { get; set; } = "";
            }

            public class PersonType : ObjectGraphType<Person>
            {
                public PersonType()
                {
                    Field<StringGraphType>("name")
                        .ResolveDelegate(ResolveName);
                }

                public string ResolveName(IResolveFieldContext context)
                    => ((Person)context.Source!).Name;
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source, sort: false);
        ReplaceHash(output).ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public async Task DoesNotGenerateForPrivateMethodInSameClass()
    {
        // ResolveDelegate with a private method cannot be intercepted (AOT-incompatible)
        // Private methods are not accessible from generated interceptor code
        const string source =
            """
            using GraphQL.Types;
            using GraphQL.Builders;
            using GraphQL;

            namespace Sample;

            public class Person
            {
                public string Name { get; set; } = "";
            }

            public class PersonType : ObjectGraphType<Person>
            {
                public PersonType()
                {
                    Field<StringGraphType>("name")
                        .ResolveDelegate(ResolveName);
                }

                private string ResolveName(IResolveFieldContext context)
                    => ((Person)context.Source!).Name;
            }
            """;

        // Should not generate any interceptors for private methods
        await VerifyTestSG.VerifyIncrementalGeneratorAsync(source);
    }

    [Fact]
    public async Task DoesNotGenerateForInternalMethodInSameClass()
    {
        // ResolveDelegate with an internal method cannot be intercepted (AOT-incompatible)
        // Internal methods are not accessible from generated interceptor code in a different assembly
        const string source =
            """
            using GraphQL.Types;
            using GraphQL.Builders;
            using GraphQL;

            namespace Sample;

            public class Person
            {
                public string Name { get; set; } = "";
            }

            public class PersonType : ObjectGraphType<Person>
            {
                public PersonType()
                {
                    Field<StringGraphType>("name")
                        .ResolveDelegate(ResolveName);
                }

                internal string ResolveName(IResolveFieldContext context)
                    => ((Person)context.Source!).Name;
            }
            """;

        // Should not generate any interceptors for internal methods
        await VerifyTestSG.VerifyIncrementalGeneratorAsync(source);
    }

    [Fact]
    public async Task DoesNotGenerateForPublicMethodInPrivateClass()
    {
        // ResolveDelegate with a public method in a private class cannot be intercepted (AOT-incompatible)
        // The declaring type is not publicly accessible from generated interceptor code
        const string source =
            """
            using GraphQL.Types;
            using GraphQL.Builders;
            using GraphQL;

            namespace Sample;

            public class Person
            {
                public string Name { get; set; } = "";
            }

            public class PersonType : ObjectGraphType<Person>
            {
                private class PersonResolver
                {
                    public static string ResolveName(IResolveFieldContext context)
                        => ((Person)context.Source!).Name;
                }

                public PersonType()
                {
                    Field<StringGraphType>("name")
                        .ResolveDelegate(PersonResolver.ResolveName);
                }
            }
            """;

        // Should not generate any interceptors when the declaring type is not publicly accessible
        await VerifyTestSG.VerifyIncrementalGeneratorAsync(source);
    }

    [Fact]
    public async Task GeneratesInterceptorForPublicMethodInInternalClass()
    {
        // ResolveDelegate with a public method in an internal class CAN be intercepted
        // because the generated interceptor code is emitted into the same compilation,
        // so internal types are accessible from the generated code.
        const string source =
            """
            using GraphQL.Types;
            using GraphQL.Builders;
            using GraphQL;

            namespace Sample;

            public class Person
            {
                public string Name { get; set; } = "";
            }

            internal class PersonResolver
            {
                public static string ResolveName(IResolveFieldContext context)
                    => ((Person)context.Source!).Name;
            }

            public class PersonType : ObjectGraphType<Person>
            {
                public PersonType()
                {
                    Field<StringGraphType>("name")
                        .ResolveDelegate(PersonResolver.ResolveName);
                }
            }
            """;

        var output = await VerifyTestSG.GetGeneratorOutputAsync(source, sort: false);
        ReplaceHash(output).ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public async Task DoesNotGenerateForFuncVariable()
    {
        // ResolveDelegate with a Func variable cannot be intercepted
        // because it's not a simple method group - it's a variable reference
        const string source =
            """
            using GraphQL.Types;
            using GraphQL.Builders;
            using GraphQL;
            using System;

            namespace Sample;

            public class Person
            {
                public string Name { get; set; } = "";
            }

            public class PersonType : ObjectGraphType<Person>
            {
                public PersonType()
                {
                    Func<IResolveFieldContext, string> resolver = context => ((Person)context.Source!).Name;
                    Field<StringGraphType>("name")
                        .ResolveDelegate(resolver);
                }
            }
            """;

        // Should not generate any interceptors for Func variable delegates
        await VerifyTestSG.VerifyIncrementalGeneratorAsync(source);
    }

    [Fact]
    public async Task DoesNotGenerateForLocalFunction()
    {
        // ResolveDelegate with a local function cannot be intercepted
        // because local functions are not accessible from generated interceptor code
        const string source =
            """
            using GraphQL.Types;
            using GraphQL.Builders;
            using GraphQL;

            namespace Sample;

            public class Person
            {
                public string Name { get; set; } = "";
            }

            public class PersonType : ObjectGraphType<Person>
            {
                public PersonType()
                {
                    static string ResolveName(IResolveFieldContext context)
                        => ((Person)context.Source!).Name;

                    Field<StringGraphType>("name")
                        .ResolveDelegate(ResolveName);
                }
            }
            """;

        // Should not generate any interceptors for local functions
        await VerifyTestSG.VerifyIncrementalGeneratorAsync(source);
    }

    [Fact]
    public async Task DoesNotGenerateForInlineFunction()
    {
        // ResolveDelegate with an inline lambda cannot be intercepted
        // because lambdas are not simple method groups
        const string source =
            """
            using GraphQL.Types;
            using GraphQL.Builders;
            using GraphQL;
            using System;

            namespace Sample;

            public class Person
            {
                public string Name { get; set; } = "";
            }

            public class PersonType : ObjectGraphType<Person>
            {
                public PersonType()
                {
                    Field<StringGraphType>("name")
                        .ResolveDelegate((Func<IResolveFieldContext, string>)(context => ((Person)context.Source!).Name));
                }
            }
            """;

        // Should not generate any interceptors for inline lambda delegates
        await VerifyTestSG.VerifyIncrementalGeneratorAsync(source);
    }
}
