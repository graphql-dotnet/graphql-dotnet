using System.Linq;
using GraphQL.Language;
using GraphQL.Language.AST;
using Should;

namespace GraphQL.Tests.Language
{
    public class ParserTests
    {
        [Fact]
        public void name_valid()
        {
            var input = new SourceInput("  _fred");
            var result = GraphQLParser2.Name.Token()(input);
            result.Value.Name.ShouldEqual("_fred");
            result.Value.SourceLocation.Line.ShouldEqual(1);
            result.Value.SourceLocation.Column.ShouldEqual(3);
        }

        [Fact]
        public void name_newline()
        {
            var input = new SourceInput(@"  
  _fred");
            var result = GraphQLParser2.Name.Token()(input);
            result.Value.Name.ShouldEqual("_fred");
            result.Value.SourceLocation.Line.ShouldEqual(2);
            result.Value.SourceLocation.Column.ShouldEqual(3);
        }

        [Fact]
        public void name_invalid()
        {
            var input = new SourceInput("  1_fred");
            var result = GraphQLParser2.Name.Token()(input);
            result.WasSuccessful.ShouldBeFalse();
            result.Message.ShouldEqual("unexpected '1'");
            result.Expectations.Single().ShouldEqual("start of name [_A-Za-z]");
        }

        [Fact]
        public void variable_valid()
        {
            var input = new SourceInput("  $_one");
            var result = GraphQLParser2.Variable.Token()(input);
            result.Value.Name.ShouldEqual("_one");
            result.Value.SourceLocation.Column.ShouldEqual(3);
        }

        [Fact]
        public void variable_invalid()
        {
            var input = new SourceInput("  $1one");
            var result = GraphQLParser2.Variable.Token()(input);
            result.WasSuccessful.ShouldBeFalse();
        }

        [Fact]
        public void variable_definition_named_type()
        {
            var input = new SourceInput("  $one : String");
            var result = GraphQLParser2.VariableDefinition.Token()(input);

            result.WasSuccessful.ShouldBeTrue();

            var def = result.Value;
            def.Name.ShouldEqual("one");
            def.Type.ShouldBeType<NamedType>();
            def.Type.As<NamedType>().Name.ShouldEqual("String");
        }

        [Fact]
        public void variable_definition_nonnull_type()
        {
            var input = new SourceInput("  $one : String!");
            var result = GraphQLParser2.VariableDefinition.Token()(input);

            result.WasSuccessful.ShouldBeTrue();

            var def = result.Value;
            def.Name.ShouldEqual("one");
            def.Type.ShouldBeType<NonNullType>();
            def.Type.As<NonNullType>().Type.ShouldBeType<NamedType>();
            def.Type.As<NonNullType>().Type.As<NamedType>().Name.ShouldEqual("String");
        }

        [Fact]
        public void variable_definition_list_type()
        {
            var input = new SourceInput("  $one : [String]");
            var result = GraphQLParser2.VariableDefinition.Token()(input);

            result.WasSuccessful.ShouldBeTrue();

            var def = result.Value;
            def.Name.ShouldEqual("one");
            def.Type.ShouldBeType<ListType>();
            def.Type.As<ListType>().Type.ShouldBeType<NamedType>();
            def.Type.As<ListType>().Type.As<NamedType>().Name.ShouldEqual("String");
        }

        [Fact]
        public void variable_definition_default_value()
        {
            var input = new SourceInput("  $one : [String] = 1");
            var result = GraphQLParser2.VariableDefinition.Token()(input);

            result.WasSuccessful.ShouldBeTrue();

            var def = result.Value;
            def.Name.ShouldEqual("one");
            def.Type.ShouldBeType<ListType>();
            def.Type.As<ListType>().Type.ShouldBeType<NamedType>();
            def.Type.As<ListType>().Type.As<NamedType>().Name.ShouldEqual("String");
            def.DefaultValue.ShouldBeType<IntValue>();
            def.DefaultValue.As<IntValue>().Value.ShouldEqual(1);
        }

        [Fact]
        public void variable_definition_default_value_is_object_value()
        {
            var input = new SourceInput(@"
                $input: TestInputObject = {a: ""foo"", b: [""bar""] c: ""baz""}
            ");
            var result = GraphQLParser2.VariableDefinition.Token()(input);

            result.WasSuccessful.ShouldBeTrue();

            var def = result.Value;
            def.Name.ShouldEqual("input");
            def.Type.ShouldBeType<NamedType>();
            def.Type.As<NamedType>().Name.ShouldEqual("TestInputObject");
            def.DefaultValue.ShouldBeType<ObjectValue>();
            def.DefaultValue.As<ObjectValue>().FieldNames.ShouldContain("a");
            def.DefaultValue.As<ObjectValue>().FieldNames.ShouldContain("b");
            def.DefaultValue.As<ObjectValue>().FieldNames.ShouldContain("c");
        }

        [Fact]
        public void directive_valid()
        {
            var input = new SourceInput("  @skip(");
            var result = GraphQLParser2.Directive.Token()(input);
            result.Value.Name.ShouldEqual("skip");
            result.Value.SourceLocation.Column.ShouldEqual(3);
        }

        [Fact]
        public void argument_variable()
        {
            var input = new SourceInput("  one : $input");
            var result = GraphQLParser2.Argument.Token()(input);
            result.Value.Name.ShouldEqual("one");
            result.Value.SourceLocation.Column.ShouldEqual(3);
            result.Value.NamedNode.Name.ShouldEqual("one");
            result.Value.Value.SourceLocation.Column.ShouldEqual(9);
            result.Value.Value.ShouldBeType<VariableReference>();
            result.Value.Value.As<VariableReference>().Name.ShouldEqual("input");
            result.Value.Value.As<VariableReference>().NameNode.Name.ShouldEqual("input");
            result.Value.Value.As<VariableReference>().NameNode.SourceLocation.Column.ShouldEqual(10);
        }

        [Fact]
        public void argument_int()
        {
            var input = new SourceInput("  one : -345");
            var result = GraphQLParser2.Argument.Token()(input);
            result.Value.Name.ShouldEqual("one");
            result.Value.SourceLocation.Column.ShouldEqual(3);
            result.Value.NamedNode.Name.ShouldEqual("one");
            result.Value.Value.SourceLocation.Column.ShouldEqual(9);
            result.Value.Value.ShouldBeType<IntValue>();
            result.Value.Value.As<IntValue>().Value.ShouldEqual(-345);
        }

        [Fact]
        public void argument_float()
        {
            var input = new SourceInput("  one : -345.3");
            var result = GraphQLParser2.Argument.Token()(input);
            result.Value.Name.ShouldEqual("one");
            result.Value.SourceLocation.Column.ShouldEqual(3);
            result.Value.NamedNode.Name.ShouldEqual("one");
            result.Value.Value.SourceLocation.Column.ShouldEqual(9);
            result.Value.Value.ShouldBeType<FloatValue>();
            result.Value.Value.As<FloatValue>().Value.ShouldEqual(-345.3);
        }

        [Fact]
        public void arguments_single()
        {
            var input = new SourceInput("  (one : $input)");
            var result = GraphQLParser2.Arguments.Token()(input);
            result.WasSuccessful.ShouldBeTrue();
            result.Value.SourceLocation.Column.ShouldEqual(3);
            var arg = result.Value.Children.Single().As<Argument>();
            arg.Name.ShouldEqual("one");
            arg.SourceLocation.Column.ShouldEqual(4);
            arg.Value.As<VariableReference>().Name.ShouldEqual("input");
        }

        [Fact]
        public void ignores_commas()
        {
            var input = new SourceInput(" , (one, :, $input)");
            var result = GraphQLParser2.Arguments.Token()(input);
            result.WasSuccessful.ShouldBeTrue();
            result.Value.SourceLocation.Column.ShouldEqual(4);
            var arg = result.Value.Children.Single().As<Argument>();
            arg.Name.ShouldEqual("one");
            arg.SourceLocation.Column.ShouldEqual(5);
            arg.Value.As<VariableReference>().Name.ShouldEqual("input");
        }

        [Fact]
        public void arguments_multiple()
        {
            var input = new SourceInput("  (one : $input, two :  $input2)");

            var result = GraphQLParser2.Arguments.Token()(input);

            result.WasSuccessful.ShouldBeTrue();
            result.Value.SourceLocation.Column.ShouldEqual(3);

            var arg = result.Value.Children.First().As<Argument>();
            arg.Name.ShouldEqual("one");
            arg.SourceLocation.Column.ShouldEqual(4);
            arg.Value.As<VariableReference>().Name.ShouldEqual("input");

            arg = result.Value.Children.Last().As<Argument>();
            arg.Name.ShouldEqual("two");
            arg.SourceLocation.Column.ShouldEqual(18);
            arg.Value.As<VariableReference>().Name.ShouldEqual("input2");
        }

        [Fact]
        public void integer_part_negative()
        {
            var input = new SourceInput("-123");
            var result = Parse.IntegerPart(input);
            result.Value.ShouldEqual("-123");
        }

        [Fact]
        public void integer_part_positive()
        {
            var input = new SourceInput("1023456789");
            var result = Parse.IntegerPart(input);
            result.Value.ShouldEqual("1023456789");
        }

        [Fact]
        public void integer_part_location()
        {
            var input = new SourceInput("  1023456789");
            var result = Parse.IntegerPart.Token()(input);
            result.Value.ShouldEqual("1023456789");
            result.Position.ShouldNotBeNull();
            result.Position.Column.ShouldEqual(3);
        }

        [Fact]
        public void int_value_zero()
        {
            var input = new SourceInput("0");
            var result = GraphQLParser2.IntValue(input);
            result.Value.As<IntValue>().Value.ShouldEqual(0);
        }

        [Fact]
        public void int_value_zero_negative()
        {
            var input = new SourceInput("-0");
            var result = GraphQLParser2.IntValue(input);
            result.Value.As<IntValue>().Value.ShouldEqual(0);
        }

        [Fact]
        public void int_value_single_digit()
        {
            var input = new SourceInput("1");
            var result = GraphQLParser2.IntValue(input);
            result.Value.As<IntValue>().Value.ShouldEqual(1);
        }

        [Fact]
        public void int_value()
        {
            var input = new SourceInput("-123");
            var result = GraphQLParser2.IntValue(input);
            result.Value.As<IntValue>().Value.ShouldEqual(-123);
        }

        [Fact]
        public void long_value_positive()
        {
            var input = new SourceInput("12309809809809808");
            var result = GraphQLParser2.IntValue(input);
            result.Value.As<LongValue>().Value.ShouldEqual(12309809809809808);
        }

        [Fact]
        public void long_value_negative()
        {
            var input = new SourceInput("-12309809809809808");
            var result = GraphQLParser2.IntValue(input);
            result.Value.As<LongValue>().Value.ShouldEqual(-12309809809809808);
        }

        [Fact]
        public void exponent_part_minus()
        {
            var input = new SourceInput("e-1");
            var result = Parse.ExponentPart(input);
            result.Value.ShouldEqual("e-1");
        }

        [Fact]
        public void exponent_part_plus()
        {
            var input = new SourceInput("E+13");
            var result = Parse.ExponentPart(input);
            result.Value.ShouldEqual("E+13");
        }

        [Fact]
        public void exponent_part_none()
        {
            var input = new SourceInput("e50");
            var result = Parse.ExponentPart(input);
            result.Value.ShouldEqual("e50");
        }

        [Fact]
        public void fractional_part()
        {
            var input = new SourceInput(".95");
            var result = Parse.FractionalPart(input);
            result.Value.ShouldEqual(".95");
        }

        [Fact]
        public void double_valid()
        {
            var input = new SourceInput("1.0");
            var result = Parse.Double(input);
            result.Value.ShouldEqual(1.0);
        }

        [Fact]
        public void double_exponent()
        {
            var input = new SourceInput("1e50");
            var result = Parse.Double(input);
            result.Value.ShouldEqual(1e50);
        }

        [Fact]
        public void double_fractional_exponent()
        {
            var input = new SourceInput("-1.3E-50");
            var result = Parse.Double(input);
            result.Value.ShouldEqual(-1.3E-50);
        }

        [Fact]
        public void double_fractional_exponent2()
        {
            var input = new SourceInput(" 6.0221413e23");
            var result = Parse.Double.Token()(input);
            result.Value.ShouldEqual(6.0221413e23);
            result.Position.Column.ShouldEqual(2);
        }

        [Fact]
        public void double_location()
        {
            var input = new SourceInput("  6.0221413e23");
            var result = Parse.Double.Token()(input);
            result.Value.ShouldEqual(6.0221413e23);
            result.Position.Column.ShouldEqual(3);
        }

        [Fact]
        public void float_value()
        {
            var input = new SourceInput("  6.0221413e23");
            var result = GraphQLParser2.FloatValue.Token()(input);
            result.Value.Value.ShouldEqual(6.0221413e23);
            result.Value.SourceLocation.Column.ShouldEqual(3);
        }

        [Fact]
        public void float_value_basic()
        {
            var input = new SourceInput("  6.0");
            var result = GraphQLParser2.FloatValue.Token()(input);
            result.Value.Value.ShouldEqual(6.0);
            result.Value.SourceLocation.Column.ShouldEqual(3);
        }

        [Fact]
        public void bool_true()
        {
            var input = new SourceInput("true");
            var result = Parse.Bool(input);
            result.WasSuccessful.ShouldBeTrue();
            result.Value.ShouldBeTrue();
        }

        [Fact]
        public void bool_true_mixed_case()
        {
            var input = new SourceInput("TruE");
            var result = Parse.Bool(input);
            result.WasSuccessful.ShouldBeFalse();
        }

        [Fact]
        public void bool_false()
        {
            var input = new SourceInput("false");
            var result = Parse.Bool(input);
            result.WasSuccessful.ShouldBeTrue();
            result.Value.ShouldBeFalse();
        }

        [Fact]
        public void bool_false_mixed_case()
        {
            var input = new SourceInput("FaLse");
            var result = Parse.Bool(input);
            result.WasSuccessful.ShouldBeFalse();
        }

        [Fact]
        public void boolean_value()
        {
            var input = new SourceInput("  true  ");
            var result = GraphQLParser2.BooleanValue.Token()(input);
            result.WasSuccessful.ShouldBeTrue();
            result.Value.Value.ShouldBeTrue();
            result.Value.SourceLocation.Column.ShouldEqual(3);
        }

        [Fact]
        public void operation_type()
        {
            var input = new SourceInput("  query  ");
            var result = GraphQLParser2.OperationType.Token()(input);
            result.WasSuccessful.ShouldBeTrue();
            result.Value.ShouldEqual(OperationType.Query);
        }

        [Fact]
        public void default_value_int()
        {
            var input = new SourceInput("  = 123  ");
            var result = GraphQLParser2.DefaultValue.Token()(input);
            result.WasSuccessful.ShouldBeTrue();
            result.Value.ShouldBeType<IntValue>();
            result.Value.As<IntValue>().Value.ShouldEqual(123);
            result.Value.As<IntValue>().SourceLocation.Column.ShouldEqual(5);
        }

        [Fact]
        public void default_value_boolean()
        {
            var input = new SourceInput("  = true  ");
            var result = GraphQLParser2.DefaultValue.Token()(input);
            result.WasSuccessful.ShouldBeTrue();
            result.Value.ShouldBeType<BooleanValue>();
            result.Value.As<BooleanValue>().Value.ShouldEqual(true);
            result.Value.As<BooleanValue>().SourceLocation.Column.ShouldEqual(5);
        }

        [Fact]
        public void braces_empty()
        {
            var input = new SourceInput("  { }  ");
            var result = Parse.EmptyBraces((pos, rest) => Result.Success("{}", pos, rest)).Token()(input);
            result.WasSuccessful.ShouldBeTrue();
        }

        [Fact]
        public void braces_with_name()
        {
            var input = new SourceInput("  { abcd }  ");
            var result = GraphQLParser2.Name.Braces((l, name)=> name.Value)(input);
            result.WasSuccessful.ShouldBeTrue();
        }

        [Fact]
        public void braces_with_field()
        {
            var input = new SourceInput("  { abcd }  ");
            var result = GraphQLParser2.Field.Braces((l, name)=> name.Value)(input);
            result.WasSuccessful.ShouldBeTrue();
        }

        [Fact]
        public void field_with_int_argument()
        {
            var input = new SourceInput("  { name (id:1) }  ");
            var result = GraphQLParser2.Field.Braces((l, name)=> name.Value)(input);
            result.WasSuccessful.ShouldBeTrue();
            var val = result.Value.Arguments.ValueFor("id").As<IntValue>();
            val.Value.ShouldEqual(1);
        }

        [Fact]
        public void field_with_boolean_argument()
        {
            var input = new SourceInput("  { name (id: true) }  ");
            var result = GraphQLParser2.Field.Braces((l, name)=> name.Value)(input);
            result.WasSuccessful.ShouldBeTrue();
            var val = result.Value.Arguments.ValueFor("id").As<BooleanValue>();
            val.Value.ShouldBeTrue();
        }

        [Fact]
        public void field_with_enum_argument()
        {
            var input = new SourceInput("  { name (id: TRUE) }  ");
            var result = GraphQLParser2.Field.Braces((l, name)=> name.Value)(input);
            result.WasSuccessful.ShouldBeTrue();
            var val = result.Value.Arguments.ValueFor("id").As<EnumValue>();
            val.Name.ShouldEqual("TRUE");
        }

        [Fact]
        public void field_with_string_argument()
        {
            var input = new SourceInput("  { name (id:\"1\") }  ");
            var result = GraphQLParser2.Field.Braces((l, name)=> name.Value)(input);
            result.WasSuccessful.ShouldBeTrue();
            var val = result.Value.Arguments.ValueFor("id").As<StringValue>();
            val.Value.ShouldEqual("\"1\"");
        }

        [Fact]
        public void field_with_variable_argument()
        {
            var input = new SourceInput("  { name (id: $id) }  ");
            var result = GraphQLParser2.Field.Braces((l, name)=> name.Value)(input);
            result.WasSuccessful.ShouldBeTrue();
            var val = result.Value.Arguments.ValueFor("id").As<VariableReference>();
            val.Name.ShouldEqual("id");
        }

        [Fact]
        public void field_with_empty_list_argument()
        {
            var input = new SourceInput("  { name (id: []) }  ");
            var result = GraphQLParser2.Field.Braces((l, name)=> name.Value)(input);
            result.WasSuccessful.ShouldBeTrue();
            var val = result.Value.Arguments.ValueFor("id").As<ListValue>();
            val.Values.Count().ShouldEqual(0);
        }

        [Fact]
        public void field_with_int_list_argument()
        {
            var input = new SourceInput("  { name (id: [1, 2]) }  ");
            var result = GraphQLParser2.Field.Braces((l, name)=> name.Value)(input);
            result.WasSuccessful.ShouldBeTrue();
            var val = result.Value.Arguments.ValueFor("id").As<ListValue>();
            val.Values.Count().ShouldEqual(2);
        }

        [Fact]
        public void list_value_ints()
        {
            var input = new SourceInput(" [1, \"2\"] ");
            var result = GraphQLParser2.ListValue.Token()(input);
            result.WasSuccessful.ShouldBeTrue();
            result.Value.Values.Count().ShouldEqual(2);
        }

        [Fact]
        public void selection_set()
        {
            var input = new SourceInput("  { abcd }  ");
            var result = GraphQLParser2.SelectionSet(input);
            result.WasSuccessful.ShouldBeTrue();
            result.Value.Selections.Count().ShouldEqual(1);
            result.Value.Selections.First().As<Field>().Name.ShouldEqual("abcd");
        }

        [Fact]
        public void selection_set_aliased_field()
        {
            var input = new SourceInput("  { one : two { abcd } }  ");
            var result = GraphQLParser2.SelectionSet(input);

            result.WasSuccessful.ShouldBeTrue();

            var set = result.Value;
            set.Selections.Count().ShouldEqual(1);

            var field = set.Selections.First().As<Field>();
            field.Name.ShouldEqual("two");
            field.Alias.ShouldEqual("one");

            field.SelectionSet.ShouldNotBeNull();
            field.SelectionSet.Selections.Count().ShouldEqual(1);
            field.SelectionSet.Selections.Single().As<Field>().Name.ShouldEqual("abcd");
        }

        [Fact]
        public void operation_definition_query()
        {
            var input = new SourceInput("  query aname { one : two }");
            var result = GraphQLParser2.OperationDefinition.Token()(input);
            result.WasSuccessful.ShouldBeTrue();
            result.Value.OperationType.ShouldEqual(OperationType.Query);
        }

        [Fact]
        public void operation_definition_mutation()
        {
            var input = new SourceInput("  mutation aname { one : two }");
            var result = GraphQLParser2.OperationDefinition.Token()(input);
            result.WasSuccessful.ShouldBeTrue();
            result.Value.OperationType.ShouldEqual(OperationType.Mutation);
        }

        [Fact]
        public void operation_definition_with_variables()
        {
            var input = new SourceInput("  query aname ($id: String) { one : two }");
            var result = GraphQLParser2.OperationDefinition.Token()(input);
            result.WasSuccessful.ShouldBeTrue();
        }

        [Fact]
        public void field_with_object_argument()
        {
            var input = new SourceInput(@"
                  createUser(userInput:{
                    profileImage:""myimage.png"",
                    gender: Female
                  })");

            var result = GraphQLParser2.Field.Token()(input);
            result.WasSuccessful.ShouldBeTrue();
        }

        [Fact]
        public void field_with_list_argument()
        {
            var input = new SourceInput(@"{
                  complicatedArgs {
                    stringListArgField(stringListArg: [""one"", 2])
                  }
                }");

            var result = GraphQLParser2.OperationDefinition.Token()(input);
            result.WasSuccessful.ShouldBeTrue();

            var field = result.Value.SelectionSet.Selections.First().As<Field>();
            var selection = field.SelectionSet.Selections.First().As<Field>();
            selection.Arguments.ShouldNotBeNull();
            selection.Arguments.Count().ShouldEqual(1);
            var value = selection.Arguments.ValueFor("stringListArg");
            value.ShouldBeType<ListValue>();
            value.As<ListValue>().Values.Count().ShouldEqual(2);
        }

        [Fact]
        public void object_value_with_variable()
        {
            var input = new SourceInput(@"
                  {
                    profileImage:""myimage.png"",
                    gender: Female
                  }");

            var result = GraphQLParser2.ObjectValueWithVariable.Token()(input);
            result.WasSuccessful.ShouldBeTrue();
            result.Value.ObjectFields.Count().ShouldEqual(2);
            result.Value.FieldNames.ShouldContain("profileImage");
            result.Value.FieldNames.ShouldContain("gender");
        }

        [Fact]
        public void type_condition()
        {
            var input = new SourceInput(@"on Type");
            var result = GraphQLParser2.TypeCondition.Token()(input);
            result.WasSuccessful.ShouldBeTrue();
            result.Value.Name.ShouldEqual("Type");
        }

        [Fact]
        public void fragment_definition()
        {
            var input = new SourceInput(@"
                  fragment oneGoodArgOneInvalidArg on Dog {
                    doesKnowCommand(whoknows: 1, dogCommand: SIT, unknown: true)
                  }
            ");
            var result = GraphQLParser2.FragmentDefinition.Token()(input);
            result.WasSuccessful.ShouldBeTrue();
            result.Value.Name.ShouldEqual("oneGoodArgOneInvalidArg");
            result.Value.Type.Name.ShouldEqual("Dog");
        }

        [Fact]
        public void build_definition()
        {
            var input = new SourceInput(@"
                  fragment oneGoodArgOneInvalidArg on Dog {
                    doesKnowCommand(whoknows: 1, dogCommand: SIT, unknown: true)
                  }
            ");
            var result = GraphQLParser2.Definition.Token()(input);
            result.WasSuccessful.ShouldBeTrue();
            result.Value.ShouldBeType<FragmentDefinition>();

            var def = result.Value.As<FragmentDefinition>();
            def.Name.ShouldEqual("oneGoodArgOneInvalidArg");
            def.Type.Name.ShouldEqual("Dog");
        }

        [Fact]
        public void list_allows_nested_lists()
        {
            var input = new SourceInput(@"
                [[One]]
            ");
            var result = GraphQLParser2.ListType.Token()(input);
            result.WasSuccessful.ShouldBeTrue();
            result.Value.Type.ShouldBeType<ListType>();
            result.Value.Type.As<ListType>().Type.ShouldBeType<NamedType>();
            result.Value.Type.As<ListType>().Type.As<NamedType>().Name.ShouldEqual("One");
        }

        [Fact]
        public void type_allows_nested_nonnull()
        {
            var input = new SourceInput(@"
                One!!
            ");
            var result = GraphQLParser2.Type.Token()(input);
            result.WasSuccessful.ShouldBeTrue();
            var one = result.Value.As<NonNullType>();
            one.Type.ShouldBeType<NonNullType>();
            one.Type.As<NonNullType>().Type.ShouldBeType<NamedType>();
            one.Type.As<NonNullType>().Type.As<NamedType>().Name.ShouldEqual("One");
        }

        [Fact]
        public void type_allows_nested_mix_list_nonnull()
        {
            var input = new SourceInput(@"
                [[One!]!!]!
            ");
            var result = GraphQLParser2.Type.Token()(input);
            result.WasSuccessful.ShouldBeTrue();
        }
    }
}
