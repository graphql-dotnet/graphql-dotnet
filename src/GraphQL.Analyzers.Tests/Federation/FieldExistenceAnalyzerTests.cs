using GraphQL.Analyzers.Federation;
using GraphQL.Analyzers.Tests.Federation.TestData;
using VerifyCS = GraphQL.Analyzers.Tests.VerifiersExtensions.CSharpAnalyzerVerifier<GraphQL.Analyzers.Federation.FieldExistenceAnalyzer>;

namespace GraphQL.Analyzers.Tests.Federation;

public class FieldExistenceAnalyzerTests
{
    [Fact]
    public async Task Sanity_NoDiagnostics()
    {
        const string source = "";
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task GraphTypeWithoutKey_NoDiagnostics()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class UserGraphType : ObjectGraphType<User>
            {
                public UserGraphType()
                {
                    Field(x => x.Id, type: typeof(NonNullGraphType<IdGraphType>));
                }
            }

            public class User
            {
                public int Id { get; set; }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Theory]
    [MemberData(nameof(FederationTestData.ValidFieldExpressions), MemberType = typeof(FederationTestData))]
    public async Task ValidKey_SingleKey_NoDiagnostics(int idx, string keyExpression)
    {
        _ = idx;

        string source =
            $$"""
            using GraphQL.Federation;
            using GraphQL.Types;

            namespace Sample.Server;

            public class UserGraphType : ObjectGraphType<User>
            {
                private const string ConstFieldName = "Id";

                public UserGraphType()
                {
                    this.Key({{keyExpression}});

                    Field(x => x.Id, type: typeof(NonNullGraphType<IdGraphType>));
                    Field<NonNullGraphType<StringGraphType>>("name");
                    Field<NonNullGraphType<StringGraphType>>("organization");
                }
            }

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
                public string Organization { get; set; }
            }

            public class Constants
            {
                public const string ConstFieldName = "Id";
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task InvalidKey_MultipleKeys_NoDiagnostics()
    {
        const string source =
            """
            using GraphQL.Federation;
            using GraphQL.Types;

            namespace Sample.Server;

            public class UserGraphType : ObjectGraphType<User>
            {
                public UserGraphType()
                {
                    this.Key("id");
                    this.Key("name");

                    Field(x => x.Id, type: typeof(NonNullGraphType<IdGraphType>));
                    Field<NonNullGraphType<StringGraphType>>("name");
                }
            }

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Theory]
    [MemberData(nameof(FederationTestData.EmptyFieldExpressions), MemberType = typeof(FederationTestData))]
    public async Task InvalidKey_NullOrEmpty_ReportsError(int idx, string keyExpression)
    {
        _ = idx;

        string source =
            $$"""
            using GraphQL.Federation;
            using GraphQL.Types;

            namespace Sample.Server;

            public class UserGraphType : ObjectGraphType<User>
            {
                private const string EmptyFieldName = "";
                private const string WhitespaceFieldName = "  ";

                public UserGraphType()
                {
                    {|#0:this.Key({{keyExpression}})|};

                    Field(x => x.Id, type: typeof(NonNullGraphType<IdGraphType>));
                }
            }

            public class User
            {
                public int Id { get; set; }
            }

            public class Constants
            {
                public const string EmptyFieldName = "";
                public const string WhitespaceFieldName = "  ";
            }
            """;

        var expected = VerifyCS.Diagnostic(FieldExistenceAnalyzer.FieldsMustNotBeEmpty)
            .WithLocation(0)
            .WithArguments("Key", "UserGraphType");
        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Theory]
    [MemberData(nameof(FederationTestData.EmptyFieldExpressionsInArray), MemberType = typeof(FederationTestData))]
    public async Task InvalidKey_EmptyInArray_ReportsError(int idx, string keyExpression)
    {
        _ = idx;

        string source =
            $$"""
            using GraphQL.Federation;
            using GraphQL.Types;

            namespace Sample.Server;

            public class UserGraphType : ObjectGraphType<User>
            {
                public UserGraphType()
                {
                    {|#0:this.Key({{keyExpression}})|};

                    Field(x => x.Id, type: typeof(NonNullGraphType<IdGraphType>));
                }
            }

            public class User
            {
                public int Id { get; set; }
            }
            """;

        var expected = VerifyCS.Diagnostic(FieldExistenceAnalyzer.FieldsMustNotBeEmpty)
            .WithLocation(0)
            .WithArguments("Key", "UserGraphType");
        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task InvalidKey_MultipleKeys_OneEmpty_ReportsError()
    {
        const string source =
            """
            using GraphQL.Federation;
            using GraphQL.Types;

            namespace Sample.Server;

            public class UserGraphType : ObjectGraphType<User>
            {
                public UserGraphType()
                {
                    this.Key("id");
                    {|#0:this.Key("")|};

                    Field(x => x.Id, type: typeof(NonNullGraphType<IdGraphType>));
                }
            }

            public class User
            {
                public int Id { get; set; }
            }
            """;

        var expected = VerifyCS.Diagnostic(FieldExistenceAnalyzer.FieldsMustNotBeEmpty)
            .WithLocation(0)
            .WithArguments("Key", "UserGraphType");
        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task ValidKey_CaseInsensitiveFieldMatch_NoDiagnostics()
    {
        const string source =
            """
            using GraphQL.Federation;
            using GraphQL.Types;

            namespace Sample.Server;

            public class UserGraphType : ObjectGraphType<User>
            {
                public UserGraphType()
                {
                    this.Key("Id");

                    Field<NonNullGraphType<IdGraphType>>("id");
                }
            }

            public class User
            {
                public int Id { get; set; }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Theory]
    [MemberData(nameof(FederationTestData.InvalidFieldExpressions), MemberType = typeof(FederationTestData))]
    public async Task InvalidKey_FieldDoesNotExist_ReportsError(int idx, string keyFields, params string[] missingFields)
    {
        _ = idx;
        string source =
            $$"""
            using GraphQL.Federation;
            using GraphQL.Types;

            namespace Sample.Server;

            public class UserGraphType : ObjectGraphType<User>
            {
                private const string ConstFieldName = "nonExistentField";

                public UserGraphType()
                {
                    this.Key({{keyFields}});

                    Field(x => x.Id, type: typeof(NonNullGraphType<IdGraphType>));
                    Field(x => x.Name, type: typeof(NonNullGraphType<StringGraphType>));
                }
            }

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
                public string AnotherField { get; set; }
            }

            public class Constants
            {
                public const string ConstFieldName = "nonExistentField";
            }
            """;

        var expectedDiagnostics = missingFields.Select((field, index) =>
            VerifyCS.Diagnostic(FieldExistenceAnalyzer.FieldDoesNotExist)
                .WithLocation(index)
                .WithArguments("Key", field, "UserGraphType"))
            .ToArray();

        await VerifyCS.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }

    [Fact]
    public async Task InvalidKey_OneOfMultipleKeysInvalid_ReportsError()
    {
        const string source =
            """
            using GraphQL.Federation;
            using GraphQL.Types;

            namespace Sample.Server;

            public class UserGraphType : ObjectGraphType<User>
            {
                public UserGraphType()
                {
                    this.Key("id");
                    this.Key("{|#0:nonExistent|}");

                    Field(x => x.Id, type: typeof(NonNullGraphType<IdGraphType>));
                }
            }

            public class User
            {
                public int Id { get; set; }
            }
            """;

        var expected = VerifyCS.Diagnostic(FieldExistenceAnalyzer.FieldDoesNotExist)
            .WithLocation(0)
            .WithArguments("Key", "nonExistent", "UserGraphType");
        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Theory]
    [MemberData(nameof(FederationTestData.ValidNestedFieldExpressions), MemberType = typeof(FederationTestData))]
    public async Task ValidKey_NestedKey_NoDiagnostics(int idx, string keyExpression)
    {
        _ = idx;

        string source =
            $$"""
            using GraphQL.Federation;
            using GraphQL.Types;

            namespace Sample.Server;

            public class UserGraphType : ObjectGraphType<User>
            {
                public UserGraphType()
                {
                    this.Key({{keyExpression}});

                    Field<NonNullGraphType<IdGraphType>>("id");
                    Field<NonNullGraphType<StringGraphType>>("name");
                    Field<NonNullGraphType<OrganizationGraphType>>("organization");
                }
            }

            public class OrganizationGraphType : ObjectGraphType<Organization>
            {
                public OrganizationGraphType()
                {
                    Field<NonNullGraphType<IdGraphType>>("id");
                    Field<NonNullGraphType<StringGraphType>>("name");
                    Field<NonNullGraphType<AddressGraphType>>("address");
                }
            }

            public class AddressGraphType : ObjectGraphType<Address>
            {
                public AddressGraphType()
                {
                    Field<NonNullGraphType<StringGraphType>>("city");
                    Field<NonNullGraphType<StringGraphType>>("state");
                }
            }

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
                public Organization Organization { get; set; }
            }

            public class Organization
            {
                public int Id { get; set; }
                public string Name { get; set; }
                public Address Address { get; set; }
            }

            public class Address
            {
                public string City { get; set; }
                public string State { get; set; }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Theory]
    [MemberData(nameof(FederationTestData.InvalidNestedFieldExpressions), MemberType = typeof(FederationTestData))]
    public async Task InvalidKey_NestedFieldDoesNotExist_ReportsError(int idx, string keyExpression, string missingField, string typeName)
    {
        _ = idx;

        string source =
            $$"""
            using GraphQL.Federation;
            using GraphQL.Types;

            namespace Sample.Server;

            public class UserGraphType : ObjectGraphType<User>
            {
                public UserGraphType()
                {
                    this.Key({{keyExpression}});

                    Field<NonNullGraphType<IdGraphType>>("id");
                    Field<NonNullGraphType<StringGraphType>>("name");
                    Field<NonNullGraphType<OrganizationGraphType>>("organization");
                }
            }

            public class OrganizationGraphType : ObjectGraphType<Organization>
            {
                public OrganizationGraphType()
                {
                    Field<NonNullGraphType<IdGraphType>>("id");
                    Field<NonNullGraphType<StringGraphType>>("name");
                    Field<NonNullGraphType<AddressGraphType>>("address");
                }
            }

            public class AddressGraphType : ObjectGraphType<Address>
            {
                public AddressGraphType()
                {
                    Field<NonNullGraphType<StringGraphType>>("city");
                }
            }

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
                public Organization Organization { get; set; }
            }

            public class Organization
            {
                public int Id { get; set; }
                public string Name { get; set; }
                public Address Address { get; set; }
            }

            public class Address
            {
                public string City { get; set; }
            }
            """;

        var expected = VerifyCS.Diagnostic(FieldExistenceAnalyzer.FieldDoesNotExist)
            .WithLocation(0)
            .WithArguments("Key", missingField, typeName);
        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task InvalidKey_MultipleNestedFieldsDoNotExist_ReportsMultipleErrors()
    {
        const string source =
            """
            using GraphQL.Federation;
            using GraphQL.Types;

            namespace Sample.Server;

            public class UserGraphType : ObjectGraphType<User>
            {
                public UserGraphType()
                {
                    this.Key("{|#0:nonExistent|} organization { {|#1:nonExistent|} }");

                    Field<NonNullGraphType<IdGraphType>>("id");
                    Field<NonNullGraphType<StringGraphType>>("name");
                    Field<NonNullGraphType<OrganizationGraphType>>("organization");
                }
            }

            public class OrganizationGraphType : ObjectGraphType<Organization>
            {
                public OrganizationGraphType()
                {
                    Field<NonNullGraphType<IdGraphType>>("id");
                    Field<NonNullGraphType<StringGraphType>>("name");
                }
            }

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
                public Organization Organization { get; set; }
            }

            public class Organization
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }
            """;

        var expectedDiagnostics = new[]
        {
            VerifyCS.Diagnostic(FieldExistenceAnalyzer.FieldDoesNotExist)
                .WithLocation(0)
                .WithArguments("Key", "nonExistent", "UserGraphType"),
            VerifyCS.Diagnostic(FieldExistenceAnalyzer.FieldDoesNotExist)
                .WithLocation(1)
                .WithArguments("Key", "nonExistent", "OrganizationGraphType")
        };

        await VerifyCS.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }
}
