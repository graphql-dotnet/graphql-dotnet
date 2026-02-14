namespace GraphQL.Analyzers.Tests.Federation.TestData;

/// <summary>
/// Shared test data for Federation directive analyzers.
/// </summary>
public static class FederationTestData
{
    /// <summary>
    /// Valid field expression syntax variations (literals, const, nameof, interpolation).
    /// </summary>
    /// <remarks>
    /// Each entry contains:
    /// <list type="bullet">
    /// <item><description>int: Test case index number</description></item>
    /// <item><description>string: Field expression to test (e.g., "\"id\"", "nameof(User.Id)", "$\"{ConstFieldName}\"")</description></item>
    /// </list>
    /// </remarks>
    public static TheoryData<int, string> ValidFieldExpressions => new()
    {
        // literals
        { 10, "\"id\"" },
        { 11, "\"id name\"" },
        { 12, "[\"id\", \"name\"]" },
        { 13, "new[] { \"id\", \"name\" }" },
        { 14, "new string[] { \"id\", \"name\" }" },
        // const
        { 15, "ConstFieldName" },
        { 16, "Constants.ConstFieldName" },
        { 17, "[ConstFieldName, \"name\"]" },
        { 18, "new[] { ConstFieldName, \"name\" }" },
        { 19, "new string[] { ConstFieldName, \"name\" }" },
        // nameof
        { 20, "nameof(User.Id)" },
        { 21, "new[] { nameof(User.Id), \"name\" }" },
        { 22, "new string[] { nameof(User.Id), \"name\" }" },
        // interpolation
        { 23, "$\"{nameof(User.Id)}\"" },
        { 24, "$\"{nameof(User.Id)} name\"" },
        { 25, "$\"{ConstFieldName} name\"" },
        { 26, "$\"{ConstFieldName} name {nameof(User.Organization)}\"" },
        { 27, "[$\"{ConstFieldName} organization\", \"name\"]" },
        { 28, "new[] { $\"{ConstFieldName} organization\", \"name\" }" },
        { 29, "new string[] { $\"{ConstFieldName} organization\", \"name\" }" },
        { 30, "[$\"{ConstFieldName} {nameof(User.Organization)}\", \"name\"]" },
        { 31, "new[] { $\"{ConstFieldName} {nameof(User.Organization)}\", \"name\" }" },
        { 32, "new string[] { $\"{ConstFieldName} {nameof(User.Organization)}\", \"name\" }" },
    };

    /// <summary>
    /// Invalid field expressions where fields don't exist.
    /// </summary>
    /// <remarks>
    /// Each entry contains:
    /// <list type="bullet">
    /// <item><description>int: Test case index number</description></item>
    /// <item><description>string: Field expression with error markers (e.g., "\"{|#0:nonExistentField|}\"")</description></item>
    /// <item><description>string[]: Array of field names that are expected to be missing</description></item>
    /// </list>
    /// Note: Error markers use the syntax {|#N:fieldName|} where N is the diagnostic location index.
    /// For string interpolation expressions, braces must be escaped as {{ and }}.
    /// </remarks>
    public static TheoryData<int, string, string[]> InvalidFieldExpressions => new()
    {
        // literal cases
        { 10, "\"{|#0:nonExistentField|}\"", ["nonExistentField"] },
        { 11, "\"id {|#0:nonExistentField|}\"", ["nonExistentField"] },
        { 12, "[\"id\", \"{|#0:nonExistentField|}\"]", ["nonExistentField"] },
        { 13, "[\"id\", \"name {|#0:nonExistentField|}\"]", ["nonExistentField"] },
        { 14, "new [] { \"id\", \"{|#0:nonExistentField|}\" }", ["nonExistentField"] },
        { 15, "new string[] { \"id\", \"{|#0:nonExistentField|}\" }", ["nonExistentField"] },
        // const cases
        { 16, "{|#0:ConstFieldName|}", ["nonExistentField"] },
        { 17, "{|#0:Constants.ConstFieldName|}", ["nonExistentField"] },
        { 18, "[{|#0:ConstFieldName|}]", ["nonExistentField"] },
        { 19, "[{|#0:Constants.ConstFieldName|}]", ["nonExistentField"] },
        { 20, "new [] { \"id\", {|#0:ConstFieldName|} }", ["nonExistentField"] },
        { 21, "new string[] { \"id\", {|#0:ConstFieldName|} }", ["nonExistentField"] },
        // nameof cases
        { 22, "{|#0:nameof(User.AnotherField)|}", ["AnotherField"] },
        { 23, "new [] { \"id\", {|#0:nameof(User.AnotherField)|} }", ["AnotherField"] },
        { 24, "new string[] { \"id\", {|#0:nameof(User.AnotherField)|} }", ["AnotherField"] },
        { 25, "[{|#0:nameof(User.AnotherField)|}]", ["AnotherField"] },
        { 26, "[\"id\", {|#0:nameof(User.AnotherField)|}]", ["AnotherField"] },
        // string interpolation cases
        { 27, "$\"{{|#0:nameof(User.AnotherField)|}}\"", ["AnotherField"] },
        { 28, "$\"id {{|#0:nameof(User.AnotherField)|}}\"", ["AnotherField"] },
        { 29, "$\"{{|#0:ConstFieldName|}}\"", ["nonExistentField"] },
        { 30, "$\"id {{|#0:ConstFieldName|}}\"", ["nonExistentField"] },
        { 31, "[$\"{{|#0:nameof(User.AnotherField)|}}\"]", ["AnotherField"] },
        { 32, "new[] { $\"{{|#0:nameof(User.AnotherField)|}}\" }", ["AnotherField"] },
        { 33, "new string[] { $\"{{|#0:nameof(User.AnotherField)|}}\" }", ["AnotherField"] },
        { 34, "[$\"id {{|#0:nameof(User.AnotherField)|}}\"]", ["AnotherField"] },
        { 35, "new[] { $\"id {{|#0:nameof(User.AnotherField)|}}\" }", ["AnotherField"] },
        // mixed interpolation cases
        { 36, "$\"{{|#0:ConstFieldName|}} {{|#1:nameof(User.AnotherField)|}}\"", ["nonExistentField", "AnotherField"] },
        { 37, "$\"{{|#0:ConstFieldName|}} {|#1:whateverField|} {{|#2:nameof(User.AnotherField)|}}\"", ["nonExistentField", "whateverField", "AnotherField"] },
        { 38, "$\"{{|#0:ConstFieldName|}} {|#1:whateverField1|} {|#2:whateverField2|} {{|#3:nameof(User.AnotherField)|}}\"", ["nonExistentField", "whateverField1", "whateverField2", "AnotherField"] },
        { 39, "$\"{{|#0:ConstFieldName|}} {|#1:whateverField1|} {|#2:whateverField2|} {{|#3:nameof(User.AnotherField)|}} {|#4:whateverField3|}\"", ["nonExistentField", "whateverField1", "whateverField2", "AnotherField", "whateverField3"] },
        { 40, "[$\"{{|#0:ConstFieldName|}}\", $\"{{|#1:nameof(User.AnotherField)|}}\"]", ["nonExistentField", "AnotherField"] },
        { 41, "new[] { $\"{{|#0:ConstFieldName|}}\", $\"{{|#1:nameof(User.AnotherField)|}}\" }", ["nonExistentField", "AnotherField"] },
    };

    /// <summary>
    /// Valid nested field expressions.
    /// </summary>
    /// <remarks>
    /// Each entry contains:
    /// <list type="bullet">
    /// <item><description>int: Test case index number</description></item>
    /// <item><description>string: Nested field expression (e.g., "\"organization { id }\"", "\"organization { address { city } }\"")</description></item>
    /// </list>
    /// </remarks>
    public static TheoryData<int, string> ValidNestedFieldExpressions => new()
    {
        { 1, "\"organization { id }\"" },
        { 2, "\"organization { id name }\"" },
        { 3, "\"id organization { id }\"" },
        { 4, "\"organization { id } name\"" },
        { 5, "\"organization { id name } name\"" },
        { 6, "\"organization { address { city } }\"" },
    };

    /// <summary>
    /// Invalid nested field expressions where nested fields don't exist.
    /// </summary>
    /// <remarks>
    /// Each entry contains:
    /// <list type="bullet">
    /// <item><description>int: Test case index number</description></item>
    /// <item><description>string: Nested field expression with error marker (e.g., "\"organization { {|#0:nonExistent|} }\"")</description></item>
    /// <item><description>string: Name of the missing field</description></item>
    /// <item><description>string: Name of the GraphType where the field is missing (e.g., "OrganizationGraphType", "UserGraphType")</description></item>
    /// </list>
    /// </remarks>
    public static TheoryData<int, string, string, string> InvalidNestedFieldExpressions => new()
    {
        { 1, "\"organization { {|#0:nonExistent|} }\"", "nonExistent", "OrganizationGraphType" },
        { 2, "\"organization { id {|#0:nonExistent|} }\"", "nonExistent", "OrganizationGraphType" },
        { 3, "\"id organization { {|#0:nonExistent|} }\"", "nonExistent", "OrganizationGraphType" },
        { 4, "\"{|#0:nonExistent|} { id }\"", "nonExistent", "UserGraphType" },
        { 5, "\"organization { id {|#0:nonExistent|} name }\"", "nonExistent", "OrganizationGraphType" },
        { 6, "\"organization { address { {|#0:nonExistent|} } }\"", "nonExistent", "AddressGraphType" },
    };

    /// <summary>
    /// Empty or whitespace field expressions.
    /// </summary>
    /// <remarks>
    /// Each entry contains:
    /// <list type="bullet">
    /// <item><description>int: Test case index number</description></item>
    /// <item><description>string: Empty or whitespace field expression (e.g., "\"\"", "\"  \"", "EmptyFieldName")</description></item>
    /// </list>
    /// </remarks>
    public static TheoryData<int, string> EmptyFieldExpressions => new()
    {
        // literals
        { 10, "\"\"" },
        { 11, "\"  \"" },
        { 12, "\"\\t\"" },
        // const
        { 13, "EmptyFieldName" },
        { 14, "Constants.EmptyFieldName" },
        { 15, "WhitespaceFieldName" },
        { 16, "Constants.WhitespaceFieldName" },
        // interpolation
        { 17, "$\"\"" },
        { 18, "$\"  \"" },
    };

    /// <summary>
    /// Empty field expressions in arrays.
    /// </summary>
    /// <remarks>
    /// Each entry contains:
    /// <list type="bullet">
    /// <item><description>int: Test case index number</description></item>
    /// <item><description>string: Array expression containing empty values (e.g., "[\"\"]", "new[] { \"\" }")</description></item>
    /// </list>
    /// </remarks>
    public static TheoryData<int, string> EmptyFieldExpressionsInArray => new()
    {
        { 10, "[\"\"]" },
        { 11, "new[] { \"\" }" },
        { 12, "new string[] { \"\" }" },
        { 13, "[\"  \"]" },
        { 14, "new[] { \"  \" }" },
        { 15, "new string[] { \"  \" }" },
    };
}
