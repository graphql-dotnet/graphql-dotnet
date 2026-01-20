using VerifyCS = GraphQL.Analyzers.Tests.VerifiersExtensions.CSharpAnalyzerVerifier<
    GraphQL.Analyzers.AotSchemaAttributeAnalyzer>;

namespace GraphQL.Analyzers.Tests;

public class AotSchemaAttributeAnalyzerTests
{
    [Fact]
    public async Task Sanity_NoDiagnostics()
    {
        const string source = "";
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Theory]
    [InlineData("AotInputType<MyModel>", "public class MyModel { public string Name { get; set; } }")]
    [InlineData("AotOutputType<MyModel>", "public class MyModel { public string Name { get; set; } }")]
    [InlineData("AotGraphType<MyGraphType>", "public class MyGraphType : ObjectGraphType { }")]
    [InlineData("AotTypeMapping<DateTime, DateTimeGraphType>", "")]
    [InlineData("AotQueryType<Query>", "public class Query { public string Hello => \"World\"; }")]
    [InlineData("AotMutationType<Mutation>", "public class Mutation { public string DoSomething(string input) => input; }")]
    [InlineData("AotSubscriptionType<Subscription>", "public class Subscription { }")]
    public async Task AotAttribute_OnAotSchema_NoDiagnostic(string attributeUsage, string supportingClasses)
    {
        var source =
            $$"""
            using System;
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            {{supportingClasses}}

            [{{attributeUsage}}]
            public partial class MySchema : AotSchema
            {
                public MySchema() : base(null!, null!) { }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task MultipleAotAttributes_OnAotSchema_NoDiagnostic()
    {
        const string source =
            """
            using System;
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class MyModel
            {
                public string Name { get; set; }
            }

            public class Query
            {
                public string Hello => "World";
            }

            [AotInputType<MyModel>]
            [AotOutputType<MyModel>]
            [AotTypeMapping<DateTime, DateTimeGraphType>]
            [AotQueryType<Query>]
            public partial class MySchema : AotSchema
            {
                public MySchema() : base(null!, null!) { }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Theory]
    [InlineData("AotInputTypeAttribute", "AotInputType<MyModel>", "public class MyModel { public string Name { get; set; } }")]
    [InlineData("AotOutputTypeAttribute", "AotOutputType<MyModel>", "public class MyModel { public string Name { get; set; } }")]
    [InlineData("AotGraphTypeAttribute", "AotGraphType<MyGraphType>", "public class MyGraphType : ObjectGraphType { }")]
    [InlineData("AotTypeMappingAttribute", "AotTypeMapping<DateTime, DateTimeGraphType>", "")]
    [InlineData("AotQueryTypeAttribute", "AotQueryType<Query>", "public class Query { public string Hello => \"World\"; }")]
    [InlineData("AotMutationTypeAttribute", "AotMutationType<Mutation>", "public class Mutation { public string DoSomething(string input) => input; }")]
    [InlineData("AotSubscriptionTypeAttribute", "AotSubscriptionType<Subscription>", "public class Subscription { }")]
    public async Task AotAttribute_OnRegularClass_Diagnostic(string attributeName, string attributeUsage, string supportingClasses)
    {
        var source =
            $$"""
            using System;
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            {{supportingClasses}}

            [{|#0:{{attributeUsage}}|}]
            public class MyClass
            {
            }
            """;

        var expected = VerifyCS.Diagnostic(AotSchemaAttributeAnalyzer.AotSchemaAttributeMustBeOnAotSchema)
            .WithLocation(0)
            .WithArguments(attributeName);

        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task MultipleAotAttributes_OnRegularClass_MultipleDiagnostics()
    {
        const string source =
            """
            using System;
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class MyModel
            {
                public string Name { get; set; }
            }

            public class Query
            {
                public string Hello => "World";
            }

            [{|#0:AotInputType<MyModel>|}]
            [{|#1:AotOutputType<MyModel>|}]
            [{|#2:AotTypeMapping<DateTime, DateTimeGraphType>|}]
            [{|#3:AotQueryType<Query>|}]
            public class MyClass
            {
            }
            """;

        var expected = new[]
        {
            VerifyCS.Diagnostic(AotSchemaAttributeAnalyzer.AotSchemaAttributeMustBeOnAotSchema)
                .WithLocation(0)
                .WithArguments("AotInputTypeAttribute"),
            VerifyCS.Diagnostic(AotSchemaAttributeAnalyzer.AotSchemaAttributeMustBeOnAotSchema)
                .WithLocation(1)
                .WithArguments("AotOutputTypeAttribute"),
            VerifyCS.Diagnostic(AotSchemaAttributeAnalyzer.AotSchemaAttributeMustBeOnAotSchema)
                .WithLocation(2)
                .WithArguments("AotTypeMappingAttribute"),
            VerifyCS.Diagnostic(AotSchemaAttributeAnalyzer.AotSchemaAttributeMustBeOnAotSchema)
                .WithLocation(3)
                .WithArguments("AotQueryTypeAttribute"),
        };

        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task AotAttribute_OnClassDerivingFromSchema_Diagnostic()
    {
        const string source =
            """
            using System;
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class MyModel
            {
                public string Name { get; set; }
            }

            [{|#0:AotInputType<MyModel>|}]
            public class MySchema : Schema
            {
            }
            """;

        var expected = VerifyCS.Diagnostic(AotSchemaAttributeAnalyzer.AotSchemaAttributeMustBeOnAotSchema)
            .WithLocation(0)
            .WithArguments("AotInputTypeAttribute");

        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task AotAttribute_OnAbstractClassDerivingFromAotSchema_NoDiagnostic()
    {
        const string source =
            """
            using System;
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class MyModel
            {
                public string Name { get; set; }
            }

            [AotInputType<MyModel>]
            public abstract partial class MyBaseSchema : AotSchema
            {
                protected MyBaseSchema() : base(null!, null!) { }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task AotAttribute_OnDerivedAotSchema_NoDiagnostic()
    {
        const string source =
            """
            using System;
            using GraphQL;
            using GraphQL.Types;

            namespace Sample;

            public class MyModel
            {
                public string Name { get; set; }
            }

            public abstract partial class MyBaseSchema : AotSchema
            {
                protected MyBaseSchema() : base(null!, null!) { }
            }

            [AotInputType<MyModel>]
            public partial class MyDerivedSchema : MyBaseSchema
            {
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }
}
