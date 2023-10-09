using Microsoft.CodeAnalysis.Testing;
using VerifyCS = GraphQL.Analyzers.Tests.VerifiersExtensions.CSharpCodeFixVerifier<
    GraphQL.Analyzers.ResolverAnalyzer,
    GraphQL.Analyzers.ResolverCodeFixProvider>;

namespace GraphQL.Analyzers.Tests;

public class ResolverAnalyzerTests
{
    [Fact]
    public async Task Sanity_NoDiagnostics()
    {
        const string source = @"";
        await VerifyCS.VerifyAnalyzerAsync(source).ConfigureAwait(false);
    }

    [Theory]
    [InlineData("ObjectGraphType", "Resolve", "Resolve")]
    [InlineData("ObjectGraphType", "ResolveAsync", "ResolveAsync")]
    [InlineData("ObjectGraphType", "ResolveDelegate", "Resolve")]
    [InlineData("ObjectGraphType", "ResolveScoped", "Resolve")]
    [InlineData("ObjectGraphType", "ResolveScopedAsync", "ResolveAsync")]
    [InlineData("ObjectGraphType", "ResolveStream", "ResolveStream")]
    [InlineData("ObjectGraphType", "ResolveStreamAsync", "ResolveStreamAsync")]
    [InlineData("CustomObjectGraphType", "Resolve", "Resolve")]
    [InlineData("CustomObjectGraphType", "ResolveAsync", "ResolveAsync")]
    [InlineData("CustomObjectGraphType", "ResolveDelegate", "Resolve")]
    [InlineData("CustomObjectGraphType", "ResolveScoped", "Resolve")]
    [InlineData("CustomObjectGraphType", "ResolveScopedAsync", "ResolveAsync")]
    [InlineData("CustomObjectGraphType", "ResolveStream", "ResolveStream")]
    [InlineData("CustomObjectGraphType", "ResolveStreamAsync", "ResolveStreamAsync")]
    public async Task FieldMethodCalledInAllowedGraphTypeImplementation_NoDiagnostic(string baseType, string method, string resolver)
    {
        string source = $$"""
            using System;
            using System.Threading.Tasks;
            using GraphQL;
            using GraphQL.MicrosoftDI;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : {{baseType}}
            {
                public MyGraphType() =>
                    Field<StringGraphType, string>("Test").{{method}}({{resolver}});

                private string Resolve(IResolveFieldContext<object> context) =>
                    throw new NotImplementedException();

                private Task<string> ResolveAsync(IResolveFieldContext<object> context) =>
                    throw new NotImplementedException();

                private IObservable<string> ResolveStream(IResolveFieldContext<object> context) =>
                    throw new NotImplementedException();

                private Task<IObservable<string>> ResolveStreamAsync(IResolveFieldContext<object> arg) =>
                    throw new NotImplementedException();
            }

            public class CustomObjectGraphType : ObjectGraphType { }
            """
        ;

        await VerifyCS.VerifyAnalyzerAsync(source).ConfigureAwait(false);
    }

    [Theory]
    [InlineData("ObjectGraphType")]
    [InlineData("CustomObjectGraphType")]
    public async Task FieldMethodCalledOnVariable_AllowedGraphType_NoDiagnostics(string graphType)
    {
        string source = $$"""
            using System;
            using GraphQL;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType
            {
                public void Register({{graphType}} graphType) =>
                    graphType.Field<StringGraphType, string>("Test").Resolve(Resolve);

                private string Resolve(IResolveFieldContext<object> context) =>
                    throw new NotImplementedException();
            }

            public class CustomObjectGraphType : ObjectGraphType { }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source).ConfigureAwait(false);
    }

    [Theory]
    [InlineData("ObjectGraphType")]
    [InlineData("CustomObjectGraphType")]
    public async Task FieldMethodCalledOnMethodReturningGraphType_AllowedGraphType_NoDiagnostics(string returnGraphType)
    {
        string source = $$"""
            using System;
            using GraphQL;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType
            {
                public void Register() =>
                    GetGraphType().Field<StringGraphType, string>("Test").Resolve(Resolve);

                private {{returnGraphType}} GetGraphType() => new();

                private string Resolve(IResolveFieldContext<object> context) =>
                    throw new NotImplementedException();
            }

            public class CustomObjectGraphType : ObjectGraphType { }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source).ConfigureAwait(false);
    }

    [Theory]
    [InlineData("InterfaceGraphType", "Resolve", "Resolve")]
    [InlineData("InterfaceGraphType", "ResolveAsync", "ResolveAsync")]
    [InlineData("InterfaceGraphType", "ResolveDelegate", "Resolve")]
    [InlineData("InterfaceGraphType", "ResolveScoped", "Resolve")]
    [InlineData("InterfaceGraphType", "ResolveScopedAsync", "ResolveAsync")]
    [InlineData("InterfaceGraphType", "ResolveStream", "ResolveStream")]
    [InlineData("InterfaceGraphType", "ResolveStreamAsync", "ResolveStreamAsync")]
    [InlineData("InputObjectGraphType", "Resolve", "Resolve")]
    [InlineData("InputObjectGraphType", "ResolveAsync", "ResolveAsync")]
    [InlineData("InputObjectGraphType", "ResolveDelegate", "Resolve")]
    [InlineData("InputObjectGraphType", "ResolveScoped", "Resolve")]
    [InlineData("InputObjectGraphType", "ResolveScopedAsync", "ResolveAsync")]
    [InlineData("InputObjectGraphType", "ResolveStream", "ResolveStream")]
    [InlineData("InputObjectGraphType", "ResolveStreamAsync", "ResolveStreamAsync")]
    [InlineData("CustomInterfaceGraphType", "Resolve", "Resolve")]
    [InlineData("CustomInterfaceGraphType", "ResolveAsync", "ResolveAsync")]
    [InlineData("CustomInterfaceGraphType", "ResolveDelegate", "Resolve")]
    [InlineData("CustomInterfaceGraphType", "ResolveScoped", "Resolve")]
    [InlineData("CustomInterfaceGraphType", "ResolveScopedAsync", "ResolveAsync")]
    [InlineData("CustomInterfaceGraphType", "ResolveStream", "ResolveStream")]
    [InlineData("CustomInterfaceGraphType", "ResolveStreamAsync", "ResolveStreamAsync")]
    [InlineData("CustomInputObjectGraphType", "Resolve", "Resolve")]
    [InlineData("CustomInputObjectGraphType", "ResolveAsync", "ResolveAsync")]
    [InlineData("CustomInputObjectGraphType", "ResolveDelegate", "Resolve")]
    [InlineData("CustomInputObjectGraphType", "ResolveScoped", "Resolve")]
    [InlineData("CustomInputObjectGraphType", "ResolveScopedAsync", "ResolveAsync")]
    [InlineData("CustomInputObjectGraphType", "ResolveStream", "ResolveStream")]
    [InlineData("CustomInputObjectGraphType", "ResolveStreamAsync", "ResolveStreamAsync")]
    public async Task FieldMethodCalledInForbiddenGraphTypeImplementation_IllegalResolverUsage(string baseType, string method, string resolver)
    {
        string source = $$"""
            using System;
            using System.Threading.Tasks;
            using GraphQL;
            using GraphQL.MicrosoftDI;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : {{baseType}}
            {
                public MyGraphType() =>
                    Field<StringGraphType, string>("Test").{{method}}({{resolver}});

                private string Resolve(IResolveFieldContext<object> context) =>
                    throw new NotImplementedException();

                private Task<string> ResolveAsync(IResolveFieldContext<object> context) =>
                    throw new NotImplementedException();

                private IObservable<string> ResolveStream(IResolveFieldContext<object> context) =>
                    throw new NotImplementedException();

                private Task<IObservable<string>> ResolveStreamAsync(IResolveFieldContext<object> arg) =>
                    throw new NotImplementedException();
            }

            public class CustomInterfaceGraphType : InterfaceGraphType { }

            public class CustomInputObjectGraphType : InputObjectGraphType { }
            """;

        string fix = $$"""
            using System;
            using System.Threading.Tasks;
            using GraphQL;
            using GraphQL.MicrosoftDI;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : {{baseType}}
            {
                public MyGraphType() =>
                    Field<StringGraphType, string>("Test");

                private string Resolve(IResolveFieldContext<object> context) =>
                    throw new NotImplementedException();

                private Task<string> ResolveAsync(IResolveFieldContext<object> context) =>
                    throw new NotImplementedException();

                private IObservable<string> ResolveStream(IResolveFieldContext<object> context) =>
                    throw new NotImplementedException();

                private Task<IObservable<string>> ResolveStreamAsync(IResolveFieldContext<object> arg) =>
                    throw new NotImplementedException();
            }

            public class CustomInterfaceGraphType : InterfaceGraphType { }

            public class CustomInputObjectGraphType : InputObjectGraphType { }
            """;

        const int startColumn = 48;
        int endColumn = startColumn + $"{method}({resolver})".Length;

        var expected = VerifyCS.Diagnostic(ResolverAnalyzer.IllegalResolverUsage).WithSpan(12, startColumn, 12, endColumn);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix).ConfigureAwait(false);
    }

    [Theory]
    [InlineData("InterfaceGraphType")]
    [InlineData("InputObjectGraphType")]
    [InlineData("CustomInterfaceGraphType")]
    [InlineData("CustomInputObjectGraphType")]
    public async Task FieldMethodCalledOnVariable_ForbiddenGraphType_IllegalResolverUsage(string graphType)
    {
        string source = $$"""
            using System;
            using GraphQL;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType
            {
                public void Register({{graphType}} graphType) =>
                    graphType.Field<StringGraphType, string>("Test").Resolve(Resolve);

                private string Resolve(IResolveFieldContext<object> context) =>
                    throw new NotImplementedException();
            }

            public class CustomInterfaceGraphType : InterfaceGraphType { }

            public class CustomInputObjectGraphType : InputObjectGraphType { }
            """;

        string fix = $$"""
            using System;
            using GraphQL;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType
            {
                public void Register({{graphType}} graphType) =>
                    graphType.Field<StringGraphType, string>("Test");

                private string Resolve(IResolveFieldContext<object> context) =>
                    throw new NotImplementedException();
            }

            public class CustomInterfaceGraphType : InterfaceGraphType { }

            public class CustomInputObjectGraphType : InputObjectGraphType { }
            """;

        var expected = VerifyCS.Diagnostic(ResolverAnalyzer.IllegalResolverUsage).WithSpan(10, 58, 10, 74);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix).ConfigureAwait(false);
    }

    [Theory]
    [InlineData("InterfaceGraphType")]
    [InlineData("InputObjectGraphType")]
    [InlineData("CustomInterfaceGraphType")]
    [InlineData("CustomInputObjectGraphType")]
    public async Task FieldMethodCalledOnMethodReturningGraphType_ForbiddenGraphType_IllegalResolverUsage(string returnGraphType)
    {
        string source = $$"""
            using System;
            using GraphQL;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType
            {
                public void Register() =>
                    GetGraphType().Field<StringGraphType, string>("Test").Resolve(Resolve);

                private {{returnGraphType}} GetGraphType() => new();

                private string Resolve(IResolveFieldContext<object> context) =>
                    throw new NotImplementedException();
            }

            public class CustomInterfaceGraphType : InterfaceGraphType { }

            public class CustomInputObjectGraphType : InputObjectGraphType { }
            """;

        string fix = $$"""
            using System;
            using GraphQL;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType
            {
                public void Register() =>
                    GetGraphType().Field<StringGraphType, string>("Test");

                private {{returnGraphType}} GetGraphType() => new();

                private string Resolve(IResolveFieldContext<object> context) =>
                    throw new NotImplementedException();
            }

            public class CustomInterfaceGraphType : InterfaceGraphType { }

            public class CustomInputObjectGraphType : InputObjectGraphType { }
            """;

        var expected = VerifyCS.Diagnostic(ResolverAnalyzer.IllegalResolverUsage).WithSpan(10, 63, 10, 79);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix).ConfigureAwait(false);
    }

    [Fact]
    public async Task FieldMethodCalledInForbiddenGraphType_OnNewLineAndLastCall_IllegalResolverUsage_CorrectFormatting()
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : InputObjectGraphType
            {
                public MyGraphType() =>
                    Field<StringGraphType, string>("Test")
                        .Resolve(context => "Test");
            }
            """;

        const string fix = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : InputObjectGraphType
            {
                public MyGraphType() =>
                    Field<StringGraphType, string>("Test");
            }
            """;

        var expected = VerifyCS.Diagnostic(ResolverAnalyzer.IllegalResolverUsage).WithSpan(9, 14, 9, 40);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix).ConfigureAwait(false);
    }

    [Fact]
    public async Task FieldMethodCalledInForbiddenGraphType_OnNewLineButNotLastCall_IllegalResolverUsage_CorrectFormatting()
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : InputObjectGraphType
            {
                public MyGraphType() =>
                    Field<StringGraphType, string>("Test")
                        .Resolve(context => "Test")
                        .Description("description");
            }
            """;

        const string fix = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : InputObjectGraphType
            {
                public MyGraphType() =>
                    Field<StringGraphType, string>("Test")
                        .Description("description");
            }
            """;

        var expected = VerifyCS.Diagnostic(ResolverAnalyzer.IllegalResolverUsage).WithSpan(9, 14, 9, 40);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix).ConfigureAwait(false);
    }

    [Fact]
    public async Task FieldMethodCalledInForbiddenGraphType_NotOnNewLineAndNotLastCall_IllegalResolverUsage_CorrectFormatting()
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : InputObjectGraphType
            {
                public MyGraphType() =>
                    Field<StringGraphType, string>("Test").Resolve(context => "Test")
                        .Description("description");
            }
            """;

        const string fix = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : InputObjectGraphType
            {
                public MyGraphType() =>
                    Field<StringGraphType, string>("Test")
                        .Description("description");
            }
            """;

        var expected = VerifyCS.Diagnostic(ResolverAnalyzer.IllegalResolverUsage).WithSpan(8, 48, 8, 74);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix).ConfigureAwait(false);
    }

    [Fact]
    public async Task FieldMethodCalledInForbiddenGraphType_ResolveOverloadUnknown_IllegalResolverUsage()
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : InputObjectGraphType
            {
                public MyGraphType() =>
                    Field<StringGraphType, string>("Test").Resolve()
                        .Description("description");
            }
            """;

        const string fix = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : InputObjectGraphType
            {
                public MyGraphType() =>
                    Field<StringGraphType, string>("Test")
                        .Description("description");
            }
            """;

        var expected = VerifyCS.Diagnostic(ResolverAnalyzer.IllegalResolverUsage).WithSpan(8, 48, 8, 57);
        var test = new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fix,
            CompilerDiagnostics = CompilerDiagnostics.None,
            ExpectedDiagnostics = { expected }
        };
        await test.RunAsync().ConfigureAwait(false);
    }
}
