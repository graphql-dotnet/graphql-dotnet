using GraphQL.Analyzers.Federation;
using VerifyCS = GraphQL.Analyzers.Tests.VerifiersExtensions.CSharpAnalyzerVerifier<
    GraphQL.Analyzers.Federation.KeyAnalyzer>;

namespace GraphQL.Analyzers.Tests.Federation;

public class KeyAnalyzerTests
{
    [Fact]
    public async Task Test()
    {
        string source =
            $$"""
              using GraphQL.Federation;
              using GraphQL.Types;
              using System.Collections.Generic;

              namespace Sample.Server;

              public class UserGraphType : ObjectGraphType<User>
              {
                  private const string Address = "Address";
                  public UserGraphType()
                  {
                      this.Key(["id", "name"]);
                      this.Key(new[] { "id", "name" });
                      this.Key(new string[] { "id", "name" });
                      this.Key("id");
                      this.Key("id name");
                      this.Key("id organization { id }");
                      this.Key("id organization { location { city zip } }");
                      this.ResolveReference((_, source) => source);

                      Field(x => x.Id, type: typeof(NonNullGraphType<IdGraphType>));
                      Field<NonNullGraphType<IdGraphType>>("Name");
                      Field<StringGraphType>(Address);
                      Field<OrganizationGraphType>("Organization");
                  }
              }

              public class OrganizationGraphType : ObjectGraphType<Organization>
              {
                  public OrganizationGraphType()
                  {
                      Field<NonNullGraphType<IdGraphType>>("Id").Resolve(ctx => ctx.Source.Id);
                  }
              }

              public class User
              {
                  public int Id { get; set; }
                  public string Name { get; set; }
                  public string Address { get; set; }
              }

              public class Organization
              {
                  public int Id { get; set; }
              }
              """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

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
    [InlineData("\"id\"")]
    [InlineData("\"id name\"")]
    [InlineData("[\"id\", \"name\"]")]
    [InlineData("new[] { \"id\", \"name\" }")]
    [InlineData("new string[] { \"id\", \"name\" }")]
    // const
    [InlineData("ConstFieldName")]
    [InlineData("Constants.ConstFieldName")]
    [InlineData("[ConstFieldName, \"name\"]")]
    [InlineData("new[] { ConstFieldName, \"name\" }")]
    [InlineData("new string[] { ConstFieldName, \"name\" }")]
    // nameof
    [InlineData("nameof(User.Id)")]
    [InlineData("new[] { nameof(User.Id), \"name\" }")]
    [InlineData("new string[] { nameof(User.Id), \"name\" }")]
    // interpolation
    [InlineData("$\"{nameof(User.Id)}\"")]
    [InlineData("$\"{nameof(User.Id)} name\"")]
    [InlineData("$\"{ConstFieldName} name\"")]
    [InlineData("$\"{ConstFieldName} name {nameof(User.Organization)}\"")]
    [InlineData("[$\"{ConstFieldName} organization\", \"name\"]")]
    [InlineData("new[] { $\"{ConstFieldName} organization\", \"name\" }")]
    [InlineData("new string[] { $\"{ConstFieldName} organization\", \"name\" }")]
    [InlineData("[$\"{ConstFieldName} {nameof(User.Organization)}\", \"name\"]")]
    [InlineData("new[] { $\"{ConstFieldName} {nameof(User.Organization)}\", \"name\" }")]
    [InlineData("new string[] { $\"{ConstFieldName} {nameof(User.Organization)}\", \"name\" }")]
    public async Task ValidKey_SingleKey_NoDiagnostics(string keyExpression)
    {
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
    public async Task InvalidKey_SingleFieldDoesNotExist_ReportsError(int idx, string keyFields, params string[] missingFields)
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
    public async Task InvalidKey_MultipleFieldsDoNotExist_ReportsMultipleErrors()
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
                    this.Key("{|#0:nonExistent1|} {|#1:nonExistent2|}");

                    Field(x => x.Id, type: typeof(NonNullGraphType<IdGraphType>));
                }
            }

            public class User
            {
                public int Id { get; set; }
            }
            """;

        var expected1 = VerifyCS.Diagnostic(KeyAnalyzer.KeyFieldDoesNotExist)
            .WithLocation(0)
            .WithArguments("nonExistent1", "UserGraphType");
        var expected2 = VerifyCS.Diagnostic(KeyAnalyzer.KeyFieldDoesNotExist)
            .WithLocation(1)
            .WithArguments("nonExistent2", "UserGraphType");
        await VerifyCS.VerifyAnalyzerAsync(source, expected1, expected2);
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
}
