using VerifyCS = GraphQL.Analyzers.Tests.VerifiersExtensions.CSharpAnalyzerVerifier<
    GraphQL.Analyzers.AotSchemaConstructorAnalyzer>;

namespace GraphQL.Analyzers.Tests;

public class AotSchemaConstructorAnalyzerTests
{
    [Fact]
    public async Task Constructor_CallsConfigure_NoDiagnostic()
    {
        const string source =
            """
            using GraphQL.Types;
            using System;

            namespace Sample.Server;

            public class MySchema : AotSchema
            {
                public MySchema() : base(null!, null!)
                {
                    Configure();
                }

                protected override void Configure()
                {
                    // Schema configuration
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Constructor_ChainsToThisConstructor_NoDiagnostic()
    {
        const string source =
            """
            using GraphQL.Types;
            using System;

            namespace Sample.Server;

            public class MySchema : AotSchema
            {
                public MySchema() : this(true)
                {
                }

                public MySchema(bool initialize) : base(null!, null!)
                {
                    Configure();
                }

                protected override void Configure()
                {
                    // Schema configuration
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Constructor_DoesNotCallConfigure_Diagnostic()
    {
        const string source =
            """
            using GraphQL.Types;
            using System;

            namespace Sample.Server;

            public class MySchema : AotSchema
            {
                public {|#0:MySchema|}() : base(null!, null!)
                {
                }

                protected override void Configure()
                {
                    // Schema configuration
                }
            }
            """;

        var expected = VerifyCS.Diagnostic(AotSchemaConstructorAnalyzer.AotSchemaConstructorMustCallConfigure)
            .WithLocation(0).WithArguments("MySchema");

        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task MultipleConstructors_OneMissingConfigureOrChain_Diagnostic()
    {
        const string source =
            """
            using GraphQL.Types;
            using System;

            namespace Sample.Server;

            public class MySchema : AotSchema
            {
                public MySchema() : this(true)
                {
                    // Chains to other constructor - OK
                }

                public {|#0:MySchema|}(bool initialize) : base(null!, null!)
                {
                    // Missing Configure() call
                }

                protected override void Configure()
                {
                    // Schema configuration
                }
            }
            """;

        var expected = VerifyCS.Diagnostic(AotSchemaConstructorAnalyzer.AotSchemaConstructorMustCallConfigure)
            .WithLocation(0).WithArguments("MySchema");

        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task DerivedFromDerivedAotSchema_MissingConfigure_Diagnostic()
    {
        const string source =
            """
            using GraphQL.Types;
            using System;

            namespace Sample.Server;

            public abstract class BaseSchema : AotSchema
            {
                protected BaseSchema() : base(null!, null!)
                {
                    Configure();
                }

                protected override void Configure()
                {
                    // Base configuration
                }
            }

            public class MySchema : BaseSchema
            {
                public {|#0:MySchema|}() : base()
                {
                    // Missing Configure() call
                }

                protected override void Configure()
                {
                    base.Configure();
                    // My configuration
                }
            }
            """;

        var expected = VerifyCS.Diagnostic(AotSchemaConstructorAnalyzer.AotSchemaConstructorMustCallConfigure)
            .WithLocation(0).WithArguments("MySchema");

        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task NonAotSchemaClass_NoDiagnostic()
    {
        const string source =
            """
            using GraphQL.Types;
            using System;

            namespace Sample.Server;

            public class MySchema : Schema
            {
                public MySchema(IServiceProvider services)
                {
                    // Not derived from AotSchema, no diagnostic
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Constructor_CallsConfigureInConditionalBlock_NoDiagnostic()
    {
        const string source =
            """
            using GraphQL.Types;
            using System;

            namespace Sample.Server;

            public class MySchema : AotSchema
            {
                public MySchema(bool initialize) : base(null!, null!)
                {
                    if (initialize)
                    {
                        Configure();
                    }
                    else
                    {
                        Configure();
                    }
                }

                protected override void Configure()
                {
                    // Schema configuration
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Constructor_CallsSomeOtherConfigureMethod_Diagnostic()
    {
        const string source =
            """
            using GraphQL.Types;
            using System;

            namespace Sample.Server;

            public class MySchema : AotSchema
            {
                public {|#0:MySchema|}() : base(null!, null!)
                {
                    ConfigureOther(); // Different method name
                }

                private void ConfigureOther()
                {
                    // Not the Configure() method
                }

                protected override void Configure()
                {
                    // Schema configuration
                }
            }
            """;

        var expected = VerifyCS.Diagnostic(AotSchemaConstructorAnalyzer.AotSchemaConstructorMustCallConfigure)
            .WithLocation(0).WithArguments("MySchema");

        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task AbstractClass_DoesNotCallConfigure_NoDiagnostic()
    {
        const string source =
            """
            using GraphQL.Types;
            using System;

            namespace Sample.Server;

            public abstract class BaseSchema : AotSchema
            {
                protected BaseSchema() : base(null!, null!)
                {
                    // Abstract class - should not trigger diagnostic even without Configure() call
                }

                protected override void Configure()
                {
                    // Schema configuration
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

}
