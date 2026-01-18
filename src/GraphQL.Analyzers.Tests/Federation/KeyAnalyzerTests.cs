using GraphQL.Analyzers.Federation;
using VerifyCS = GraphQL.Analyzers.Tests.VerifiersExtensions.CSharpCodeFixVerifier<
    GraphQL.Analyzers.Federation.KeyAnalyzer,
    GraphQL.Analyzers.Federation.KeyAnalyzerCodeFixProvider>;

namespace GraphQL.Analyzers.Tests.Federation;

public class KeyAnalyzerTests
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
    // literals
    [InlineData(10, "\"id\"")]
    [InlineData(11, "\"id name\"")]
    [InlineData(12, "[\"id\", \"name\"]")]
    [InlineData(13, "new[] { \"id\", \"name\" }")]
    [InlineData(14, "new string[] { \"id\", \"name\" }")]
    // const
    [InlineData(15, "ConstFieldName")]
    [InlineData(16, "Constants.ConstFieldName")]
    [InlineData(17, "[ConstFieldName, \"name\"]")]
    [InlineData(18, "new[] { ConstFieldName, \"name\" }")]
    [InlineData(19, "new string[] { ConstFieldName, \"name\" }")]
    // nameof
    [InlineData(20, "nameof(User.Id)")]
    [InlineData(21, "new[] { nameof(User.Id), \"name\" }")]
    [InlineData(22, "new string[] { nameof(User.Id), \"name\" }")]
    // interpolation
    [InlineData(23, "$\"{nameof(User.Id)}\"")]
    [InlineData(24, "$\"{nameof(User.Id)} name\"")]
    [InlineData(25, "$\"{ConstFieldName} name\"")]
    [InlineData(26, "$\"{ConstFieldName} name {nameof(User.Organization)}\"")]
    [InlineData(27, "[$\"{ConstFieldName} organization\", \"name\"]")]
    [InlineData(28, "new[] { $\"{ConstFieldName} organization\", \"name\" }")]
    [InlineData(29, "new string[] { $\"{ConstFieldName} organization\", \"name\" }")]
    [InlineData(30, "[$\"{ConstFieldName} {nameof(User.Organization)}\", \"name\"]")]
    [InlineData(31, "new[] { $\"{ConstFieldName} {nameof(User.Organization)}\", \"name\" }")]
    [InlineData(32, "new string[] { $\"{ConstFieldName} {nameof(User.Organization)}\", \"name\" }")]
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
    public async Task ValidKey_MultipleKeys_NoDiagnostics()
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
    // literal cases
    [InlineData(10, "\"{|#0:nonExistentField|}\"", "nonExistentField")]
    [InlineData(11, "\"id {|#0:nonExistentField|}\"", "nonExistentField")]
    [InlineData(12, "[\"id\", \"{|#0:nonExistentField|}\"]", "nonExistentField")]
    [InlineData(13, "[\"id\", \"name {|#0:nonExistentField|}\"]", "nonExistentField")]
    [InlineData(14, "new [] { \"id\", \"{|#0:nonExistentField|}\" }", "nonExistentField")]
    [InlineData(15, "new string[] { \"id\", \"{|#0:nonExistentField|}\" }", "nonExistentField")]
    // const cases
    [InlineData(16, "{|#0:ConstFieldName|}", "nonExistentField")]
    [InlineData(17, "{|#0:Constants.ConstFieldName|}", "nonExistentField")]
    [InlineData(18, "[{|#0:ConstFieldName|}]", "nonExistentField")]
    [InlineData(19, "[{|#0:Constants.ConstFieldName|}]", "nonExistentField")]
    [InlineData(20, "new [] { \"id\", {|#0:ConstFieldName|} }", "nonExistentField")]
    [InlineData(21, "new string[] { \"id\", {|#0:ConstFieldName|} }", "nonExistentField")]
    // nameof cases
    [InlineData(22, "{|#0:nameof(User.AnotherField)|}", "AnotherField")]
    [InlineData(23, "new [] { \"id\", {|#0:nameof(User.AnotherField)|} }", "AnotherField")]
    [InlineData(24, "new string[] { \"id\", {|#0:nameof(User.AnotherField)|} }", "AnotherField")]
    [InlineData(25, "[{|#0:nameof(User.AnotherField)|}]", "AnotherField")]
    [InlineData(26, "[\"id\", {|#0:nameof(User.AnotherField)|}]", "AnotherField")]
    // string interpolation cases
    [InlineData(27, "$\"{{|#0:nameof(User.AnotherField)|}}\"", "AnotherField")]
    [InlineData(28, "$\"id {{|#0:nameof(User.AnotherField)|}}\"", "AnotherField")]
    [InlineData(29, "$\"{{|#0:ConstFieldName|}}\"", "nonExistentField")]
    [InlineData(30, "$\"id {{|#0:ConstFieldName|}}\"", "nonExistentField")]
    [InlineData(31, "[$\"{{|#0:nameof(User.AnotherField)|}}\"]", "AnotherField")]
    [InlineData(32, "new[] { $\"{{|#0:nameof(User.AnotherField)|}}\" }", "AnotherField")]
    [InlineData(33, "new string[] { $\"{{|#0:nameof(User.AnotherField)|}}\" }", "AnotherField")]
    [InlineData(34, "[$\"id {{|#0:nameof(User.AnotherField)|}}\"]", "AnotherField")]
    [InlineData(35, "new[] { $\"id {{|#0:nameof(User.AnotherField)|}}\" }", "AnotherField")]
    // mixed interpolation cases
    [InlineData(36, "$\"{{|#0:ConstFieldName|}} {{|#1:nameof(User.AnotherField)|}}\"", "nonExistentField", "AnotherField")]
    [InlineData(37, "$\"{{|#0:ConstFieldName|}} {|#1:whateverField|} {{|#2:nameof(User.AnotherField)|}}\"", "nonExistentField", "whateverField", "AnotherField")]
    [InlineData(38, "$\"{{|#0:ConstFieldName|}} {|#1:whateverField1|} {|#2:whateverField2|} {{|#3:nameof(User.AnotherField)|}}\"", "nonExistentField", "whateverField1", "whateverField2", "AnotherField")]
    [InlineData(39, "$\"{{|#0:ConstFieldName|}} {|#1:whateverField1|} {|#2:whateverField2|} {{|#3:nameof(User.AnotherField)|}} {|#4:whateverField3|}\"", "nonExistentField", "whateverField1", "whateverField2", "AnotherField", "whateverField3")]
    [InlineData(40, "[$\"{{|#0:ConstFieldName|}}\", $\"{{|#1:nameof(User.AnotherField)|}}\"]", "nonExistentField", "AnotherField")]
    [InlineData(41, "new[] { $\"{{|#0:ConstFieldName|}}\", $\"{{|#1:nameof(User.AnotherField)|}}\" }", "nonExistentField", "AnotherField")]
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
            VerifyCS.Diagnostic(KeyAnalyzer.KeyFieldDoesNotExist)
                .WithLocation(index)
                .WithArguments(field, "UserGraphType"))
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

        var expected = VerifyCS.Diagnostic(KeyAnalyzer.KeyFieldDoesNotExist)
            .WithLocation(0)
            .WithArguments("nonExistent", "UserGraphType");
        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Theory]
    [InlineData(1, "\"organization { id }\"")]
    [InlineData(2, "\"organization { id name }\"")]
    [InlineData(3, "\"id organization { id }\"")]
    [InlineData(4, "\"organization { id } name\"")]
    [InlineData(5, "\"organization { id name } name\"")]
    [InlineData(6, "\"organization { address { city } }\"")]
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
    [InlineData(1, "\"organization { {|#0:nonExistent|} }\"", "nonExistent", "OrganizationGraphType")]
    [InlineData(2, "\"organization { id {|#0:nonExistent|} }\"", "nonExistent", "OrganizationGraphType")]
    [InlineData(3, "\"id organization { {|#0:nonExistent|} }\"", "nonExistent", "OrganizationGraphType")]
    [InlineData(4, "\"{|#0:nonExistent|} { id }\"", "nonExistent", "UserGraphType")]
    [InlineData(5, "\"organization { id {|#0:nonExistent|} name }\"", "nonExistent", "OrganizationGraphType")]
    [InlineData(6, "\"organization { address { {|#0:nonExistent|} } }\"", "nonExistent", "AddressGraphType")]
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

        var expected = VerifyCS.Diagnostic(KeyAnalyzer.KeyFieldDoesNotExist)
            .WithLocation(0)
            .WithArguments(missingField, typeName);
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
            VerifyCS.Diagnostic(KeyAnalyzer.KeyFieldDoesNotExist)
                .WithLocation(0)
                .WithArguments("nonExistent", "UserGraphType"),
            VerifyCS.Diagnostic(KeyAnalyzer.KeyFieldDoesNotExist)
                .WithLocation(1)
                .WithArguments("nonExistent", "OrganizationGraphType")
        };

        await VerifyCS.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }

    [Theory]
    // literals
    [InlineData(10, "\"\"")]
    [InlineData(11, "\"  \"")]
    [InlineData(12, "\"\\t\"")]
    // const
    [InlineData(13, "EmptyFieldName")]
    [InlineData(14, "Constants.EmptyFieldName")]
    [InlineData(15, "WhitespaceFieldName")]
    [InlineData(16, "Constants.WhitespaceFieldName")]
    // interpolation
    [InlineData(17, "$\"\"")]
    [InlineData(18, "$\"  \"")]
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

        var expected = VerifyCS.Diagnostic(KeyAnalyzer.KeyMustNotBeNullOrEmpty)
            .WithLocation(0)
            .WithArguments("UserGraphType");
        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Theory]
    [InlineData(10, "[\"\"]")]
    [InlineData(11, "new[] { \"\" }")]
    [InlineData(12, "new string[] { \"\" }")]
    [InlineData(13, "[\"  \"]")]
    [InlineData(14, "new[] { \"  \" }")]
    [InlineData(15, "new string[] { \"  \" }")]
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

        var expected = VerifyCS.Diagnostic(KeyAnalyzer.KeyMustNotBeNullOrEmpty)
            .WithLocation(0)
            .WithArguments("UserGraphType");
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

        var expected = VerifyCS.Diagnostic(KeyAnalyzer.KeyMustNotBeNullOrEmpty)
            .WithLocation(0)
            .WithArguments("UserGraphType");
        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Theory]
    [InlineData(1, "\"id\"", "[\"id\"]", "id")]
    [InlineData(2, "\"id\"", "new[] { \"id\" }", "id")]
    [InlineData(3, "\"id\"", "new string[] { \"id\" }", "id")]
    [InlineData(4, "\"id name\"", "[\"id\", \"name\"]", "id name")]
    [InlineData(5, "\"id name\"", "new[] { \"id\", \"name\" }", "id name")]
    [InlineData(6, "\"name id\"", "[\"id\", \"name\"]", "id name")]
    [InlineData(7, "$\"{nameof(User.Id)}\"", "nameof(User.Id)", "Id")]
    [InlineData(8, "ConstFieldName", "\"id\"", "id")]
    public async Task DuplicateKey_DifferentSyntaxSameValue_ReportsWarning(int idx, string firstKey, string secondKey, string expectedFieldsString)
    {
        _ = idx;

        string source =
            $$"""
            using GraphQL.Federation;
            using GraphQL.Types;

            namespace Sample.Server;

            public class UserGraphType : ObjectGraphType<User>
            {
                private const string ConstFieldName = "id";

                public UserGraphType()
                {
                    this.Key({{firstKey}});
                    {|#0:this.Key({{secondKey}})|};

                    Field(x => x.Id, type: typeof(NonNullGraphType<IdGraphType>));
                    Field(x => x.Name, type: typeof(NonNullGraphType<StringGraphType>));
                }
            }

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }
            """;

        var expected = VerifyCS.Diagnostic(KeyAnalyzer.DuplicateKey)
            .WithLocation(0)
            .WithArguments(expectedFieldsString, "UserGraphType");
        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task DuplicateKey_CaseInsensitive_ReportsWarning()
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
                    {|#0:this.Key("Id")|};
                    {|#1:this.Key("ID")|};

                    Field(x => x.Id, type: typeof(NonNullGraphType<IdGraphType>));
                }
            }

            public class User
            {
                public int Id { get; set; }
            }
            """;

        var expectedDiagnostics = new[]
        {
            VerifyCS.Diagnostic(KeyAnalyzer.DuplicateKey)
                .WithLocation(0)
                .WithArguments("Id", "UserGraphType"),
            VerifyCS.Diagnostic(KeyAnalyzer.DuplicateKey)
                .WithLocation(1)
                .WithArguments("ID", "UserGraphType")
        };

        await VerifyCS.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }

    [Fact]
    public async Task RemoveDuplicateKey_ExactDuplicate()
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
                    {|#0:this.Key("id")|};

                    Field(x => x.Id, type: typeof(NonNullGraphType<IdGraphType>));
                }
            }

            public class User
            {
                public int Id { get; set; }
            }
            """;

        const string fixedSource =
            """
            using GraphQL.Federation;
            using GraphQL.Types;

            namespace Sample.Server;

            public class UserGraphType : ObjectGraphType<User>
            {
                public UserGraphType()
                {
                    this.Key("id");

                    Field(x => x.Id, type: typeof(NonNullGraphType<IdGraphType>));
                }
            }

            public class User
            {
                public int Id { get; set; }
            }
            """;

        var expected = VerifyCS.Diagnostic(KeyAnalyzer.DuplicateKey)
            .WithLocation(0)
            .WithArguments("id", "UserGraphType");

        await VerifyCS.VerifyCodeFixAsync(source, expected, fixedSource);
    }

    [Fact]
    public async Task RemoveDuplicateKey_DifferentOrder()
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
                    this.Key("id name");
                    {|#0:this.Key("name id")|};

                    Field(x => x.Id, type: typeof(NonNullGraphType<IdGraphType>));
                    Field(x => x.Name, type: typeof(NonNullGraphType<StringGraphType>));
                }
            }

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }
            """;

        const string fixedSource =
            """
            using GraphQL.Federation;
            using GraphQL.Types;

            namespace Sample.Server;

            public class UserGraphType : ObjectGraphType<User>
            {
                public UserGraphType()
                {
                    this.Key("id name");

                    Field(x => x.Id, type: typeof(NonNullGraphType<IdGraphType>));
                    Field(x => x.Name, type: typeof(NonNullGraphType<StringGraphType>));
                }
            }

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }
            """;

        var expected = VerifyCS.Diagnostic(KeyAnalyzer.DuplicateKey)
            .WithLocation(0)
            .WithArguments("name id", "UserGraphType");

        await VerifyCS.VerifyCodeFixAsync(source, expected, fixedSource);
    }

    [Fact]
    public async Task RemoveDuplicateKey_MultipleDuplicates_FixesAll()
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
                    {|#0:this.Key("id")|};
                    {|#1:this.Key("id")|};

                    Field(x => x.Id, type: typeof(NonNullGraphType<IdGraphType>));
                }
            }

            public class User
            {
                public int Id { get; set; }
            }
            """;

        const string fixedSource =
            """
            using GraphQL.Federation;
            using GraphQL.Types;

            namespace Sample.Server;

            public class UserGraphType : ObjectGraphType<User>
            {
                public UserGraphType()
                {
                    this.Key("id");

                    Field(x => x.Id, type: typeof(NonNullGraphType<IdGraphType>));
                }
            }

            public class User
            {
                public int Id { get; set; }
            }
            """;

        var expectedDiagnostics = new[]
        {
            VerifyCS.Diagnostic(KeyAnalyzer.DuplicateKey)
                .WithLocation(0)
                .WithArguments("id", "UserGraphType"),
            VerifyCS.Diagnostic(KeyAnalyzer.DuplicateKey)
                .WithLocation(1)
                .WithArguments("id", "UserGraphType")
        };

        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource);
    }

    [Fact]
    public async Task RemoveDuplicateKey_KeepsOtherKeys()
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
                    {|#0:this.Key("id")|};

                    Field(x => x.Id, type: typeof(NonNullGraphType<IdGraphType>));
                    Field(x => x.Name, type: typeof(NonNullGraphType<StringGraphType>));
                }
            }

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }
            """;

        const string fixedSource =
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
                    Field(x => x.Name, type: typeof(NonNullGraphType<StringGraphType>));
                }
            }

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }
            """;

        var expected = VerifyCS.Diagnostic(KeyAnalyzer.DuplicateKey)
            .WithLocation(0)
            .WithArguments("id", "UserGraphType");

        await VerifyCS.VerifyCodeFixAsync(source, expected, fixedSource);
    }

    [Fact]
    public async Task RemoveDuplicateKey_NestedFields()
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
                    this.Key("organization { id name }");
                    {|#0:this.Key("organization { name id }")|};

                    Field<NonNullGraphType<IdGraphType>>("id");
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
                public Organization Organization { get; set; }
            }

            public class Organization
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }
            """;

        const string fixedSource =
            """
            using GraphQL.Federation;
            using GraphQL.Types;

            namespace Sample.Server;

            public class UserGraphType : ObjectGraphType<User>
            {
                public UserGraphType()
                {
                    this.Key("organization { id name }");

                    Field<NonNullGraphType<IdGraphType>>("id");
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
                public Organization Organization { get; set; }
            }

            public class Organization
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }
            """;

        var expected = VerifyCS.Diagnostic(KeyAnalyzer.DuplicateKey)
            .WithLocation(0)
            .WithArguments("organization { name id }", "UserGraphType");

        await VerifyCS.VerifyCodeFixAsync(source, expected, fixedSource);
    }

    [Fact]
    public async Task RemoveDuplicateKey_WithComments()
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
                    // Primary key
                    this.Key("id");
                    // Duplicate key
                    {|#0:this.Key("id")|};

                    Field(x => x.Id, type: typeof(NonNullGraphType<IdGraphType>));
                }
            }

            public class User
            {
                public int Id { get; set; }
            }
            """;

        const string fixedSource =
            """
            using GraphQL.Federation;
            using GraphQL.Types;

            namespace Sample.Server;

            public class UserGraphType : ObjectGraphType<User>
            {
                public UserGraphType()
                {
                    // Primary key
                    this.Key("id");

                    Field(x => x.Id, type: typeof(NonNullGraphType<IdGraphType>));
                }
            }

            public class User
            {
                public int Id { get; set; }
            }
            """;

        var expected = VerifyCS.Diagnostic(KeyAnalyzer.DuplicateKey)
            .WithLocation(0)
            .WithArguments("id", "UserGraphType");

        await VerifyCS.VerifyCodeFixAsync(source, expected, fixedSource);
    }

    [Theory]
    [InlineData(1, "id", "id name")]
    [InlineData(2, "id", "name id")]
    [InlineData(3, "Id", "id Name")]
    [InlineData(4, "id Name", "id name email")]
    [InlineData(5, "organization { id }", "organization { id name }")]
    [InlineData(6, "id organization { name }", "id name organization { name }")]
    [InlineData(7, "organization { address { city } }", "organization { address { city state } }")]
    public async Task RedundantKey_ReportsWarningWithCodeFix(int idx, string firstKey, string secondKey)
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
                    this.Key("{{firstKey}}");
                    {|#0:this.Key("{{secondKey}}")|};

                    Field<NonNullGraphType<IdGraphType>>("id");
                    Field<NonNullGraphType<StringGraphType>>("name");
                    Field<NonNullGraphType<StringGraphType>>("email");
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
                public string Email { get; set; }
            }

            public class Organization
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class Address
            {
                public string City { get; set; }
                public string State { get; set; }
            }
            """;

        string fixedSource =
            $$"""
            using GraphQL.Federation;
            using GraphQL.Types;

            namespace Sample.Server;

            public class UserGraphType : ObjectGraphType<User>
            {
                public UserGraphType()
                {
                    this.Key("{{firstKey}}");

                    Field<NonNullGraphType<IdGraphType>>("id");
                    Field<NonNullGraphType<StringGraphType>>("name");
                    Field<NonNullGraphType<StringGraphType>>("email");
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
                public string Email { get; set; }
            }

            public class Organization
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class Address
            {
                public string City { get; set; }
                public string State { get; set; }
            }
            """;

        var expected = VerifyCS.Diagnostic(KeyAnalyzer.RedundantKey)
            .WithLocation(0)
            .WithArguments(secondKey, firstKey, "UserGraphType");
        await VerifyCS.VerifyCodeFixAsync(source, expected, fixedSource);
    }

    [Fact]
    public async Task RedundantKey_MultipleRedundant_ReportsAllWarningsWithCodeFix()
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
                    {|#0:this.Key("id name")|};
                    {|#1:this.Key("id name email")|};

                    Field(x => x.Id, type: typeof(NonNullGraphType<IdGraphType>));
                    Field(x => x.Name, type: typeof(NonNullGraphType<StringGraphType>));
                    Field(x => x.Email, type: typeof(NonNullGraphType<StringGraphType>));
                }
            }

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
                public string Email { get; set; }
            }
            """;

        const string fixedSource =
            """
            using GraphQL.Federation;
            using GraphQL.Types;

            namespace Sample.Server;

            public class UserGraphType : ObjectGraphType<User>
            {
                public UserGraphType()
                {
                    this.Key("id");

                    Field(x => x.Id, type: typeof(NonNullGraphType<IdGraphType>));
                    Field(x => x.Name, type: typeof(NonNullGraphType<StringGraphType>));
                    Field(x => x.Email, type: typeof(NonNullGraphType<StringGraphType>));
                }
            }

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
                public string Email { get; set; }
            }
            """;

        var expectedDiagnostics = new[]
        {
            VerifyCS.Diagnostic(KeyAnalyzer.RedundantKey)
                .WithLocation(0)
                .WithArguments("id name", "id", "UserGraphType"),
            VerifyCS.Diagnostic(KeyAnalyzer.RedundantKey)
                .WithLocation(1)
                .WithArguments("id name email", "id", "UserGraphType")
        };

        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource);
    }

    [Fact]
    public async Task RedundantKey_NoRedundancy_NoDiagnostics()
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
                    this.Key("email");

                    Field(x => x.Id, type: typeof(NonNullGraphType<IdGraphType>));
                    Field(x => x.Email, type: typeof(NonNullGraphType<StringGraphType>));
                }
            }

            public class User
            {
                public int Id { get; set; }
                public string Email { get; set; }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task RedundantKey_PartialOverlap_NoDiagnostics()
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
                    this.Key("id name");
                    this.Key("id email");

                    Field(x => x.Id, type: typeof(NonNullGraphType<IdGraphType>));
                    Field(x => x.Name, type: typeof(NonNullGraphType<StringGraphType>));
                    Field(x => x.Email, type: typeof(NonNullGraphType<StringGraphType>));
                }
            }

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
                public string Email { get; set; }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task RedundantKey_KeepsNonRedundantKeys_WithCodeFix()
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
                    this.Key("email");
                    {|#0:this.Key("id name")|};

                    Field(x => x.Id, type: typeof(NonNullGraphType<IdGraphType>));
                    Field(x => x.Name, type: typeof(NonNullGraphType<StringGraphType>));
                    Field(x => x.Email, type: typeof(NonNullGraphType<StringGraphType>));
                }
            }

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
                public string Email { get; set; }
            }
            """;

        const string fixedSource =
            """
            using GraphQL.Federation;
            using GraphQL.Types;

            namespace Sample.Server;

            public class UserGraphType : ObjectGraphType<User>
            {
                public UserGraphType()
                {
                    this.Key("id");
                    this.Key("email");

                    Field(x => x.Id, type: typeof(NonNullGraphType<IdGraphType>));
                    Field(x => x.Name, type: typeof(NonNullGraphType<StringGraphType>));
                    Field(x => x.Email, type: typeof(NonNullGraphType<StringGraphType>));
                }
            }

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
                public string Email { get; set; }
            }
            """;

        var expected = VerifyCS.Diagnostic(KeyAnalyzer.RedundantKey)
            .WithLocation(0)
            .WithArguments("id name", "id", "UserGraphType");

        await VerifyCS.VerifyCodeFixAsync(source, expected, fixedSource);
    }

    [Fact]
    public async Task RedundantKey_WithComments_CodeFix()
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
                    // Primary key
                    this.Key("id");
                    // Redundant key with extra field
                    {|#0:this.Key("id name")|};

                    Field(x => x.Id, type: typeof(NonNullGraphType<IdGraphType>));
                    Field(x => x.Name, type: typeof(NonNullGraphType<StringGraphType>));
                }
            }

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }
            """;

        const string fixedSource =
            """
            using GraphQL.Federation;
            using GraphQL.Types;

            namespace Sample.Server;

            public class UserGraphType : ObjectGraphType<User>
            {
                public UserGraphType()
                {
                    // Primary key
                    this.Key("id");

                    Field(x => x.Id, type: typeof(NonNullGraphType<IdGraphType>));
                    Field(x => x.Name, type: typeof(NonNullGraphType<StringGraphType>));
                }
            }

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }
            """;

        var expected = VerifyCS.Diagnostic(KeyAnalyzer.RedundantKey)
            .WithLocation(0)
            .WithArguments("id name", "id", "UserGraphType");

        await VerifyCS.VerifyCodeFixAsync(source, expected, fixedSource);
    }
}

