using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

/// <summary>
/// Regression test for: a recursively referenced input object doesn't initialize all its fields properly.
///
/// InputB is processed first by CoerceInputTypeDefaultValues. Field 'a' has a GraphQLValue default
/// { b: { a: null } }, so ExamineType(InputA) is called recursively. Inside InputA, field 'b' has
/// NO default value, so the old code hit an early 'continue' that skipped the recursive
/// ExamineType(InputB) call for that field. Control returns to InputB, which then calls CoerceValue
/// on InputB.a's default. CoerceValue expands the object using InputB's field defaults — but
/// InputB.value's default hasn't been coerced yet (still a GraphQLIntValue AST node), causing
/// schema validation to throw "The default value of Input Object type field 'InputB.a' is invalid."
/// </summary>
public class Bug4447
{
    private const string Sdl = """
        schema {
          query: Query
        }

        type Query {
          ping: String
        }

        input InputB {
          a: InputA = { b: { a: null } }
          value: Int = 42
        }

        input InputA {
          b: InputB!
        }
        """;

    [Fact]
    public void Recursive_Input_Type_Initializes_All_Field_Default_Values()
    {
        // Schema.Initialize() -> Validate() -> CoerceInputTypeDefaultValues() must fully coerce
        // all nested default values, including those reached through recursive input type references.
        var schema = Schema.For(Sdl);
        schema.Initialize();

        var inputB = schema.AllTypes["InputB"] as InputObjectGraphType;
        inputB.ShouldNotBeNull();

        var aField = inputB.Fields.Find("a");
        aField.ShouldNotBeNull();

        // InputB.a's coerced default should be a Dictionary containing 'b' -> Dictionary containing
        // 'value' -> 42 (int). Before the fix, 'value' would be a raw GraphQLIntValue AST node.
        var aDefault = aField.DefaultValue.ShouldBeOfType<Dictionary<string, object?>>();
        var bDefault = aDefault["b"].ShouldBeOfType<Dictionary<string, object?>>();
        bDefault["value"].ShouldBeOfType<int>().ShouldBe(42);
    }
}
