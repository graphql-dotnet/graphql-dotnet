using GraphQL.Types;
using GraphQL.Validation;
using GraphQL.Validation.Rules.Custom;
using GraphQLParser.AST;

namespace GraphQL.Tests.Validation;

public class DeprecatedElementsValidationRuleTests : ValidationTestBase<DeprecatedElementsValidationRuleTests.TestDeprecatedElementsValidationRule, DeprecatedElementsValidationRuleTests.DeprecatedElementsTestSchema>
{
    [Fact]
    public void should_detect_deprecated_field()
    {
        var rule = new TestDeprecatedElementsValidationRule();

        ShouldPassRule(config =>
        {
            config.Query = """
                {
                  human {
                    name
                    deprecatedField
                  }
                }
                """;
            config.Rule(rule);
        });

        rule.DeprecatedFieldCalls.Count.ShouldBe(1);
        var call = rule.DeprecatedFieldCalls[0];
        call.FieldNode.Name.StringValue.ShouldBe("deprecatedField");
        call.FieldDefinition.Name.ShouldBe("deprecatedField");
        call.FieldDefinition.DeprecationReason.ShouldBe("This field is deprecated");
        call.ParentType.Name.ShouldBe("Human");
    }

    [Fact]
    public void should_detect_multiple_deprecated_fields()
    {
        var rule = new TestDeprecatedElementsValidationRule();

        ShouldPassRule(config =>
        {
            config.Query = """
                {
                  human {
                    deprecatedField
                    anotherDeprecatedField
                  }
                }
                """;
            config.Rule(rule);
        });

        rule.DeprecatedFieldCalls.Count.ShouldBe(2);
        rule.DeprecatedFieldCalls[0].FieldNode.Name.StringValue.ShouldBe("deprecatedField");
        rule.DeprecatedFieldCalls[1].FieldNode.Name.StringValue.ShouldBe("anotherDeprecatedField");
    }

    [Fact]
    public void should_not_detect_non_deprecated_fields()
    {
        var rule = new TestDeprecatedElementsValidationRule();

        ShouldPassRule(config =>
        {
            config.Query = """
                {
                  human {
                    name
                    id
                  }
                }
                """;
            config.Rule(rule);
        });

        rule.DeprecatedFieldCalls.Count.ShouldBe(0);
    }

    [Fact]
    public void should_detect_deprecated_field_argument()
    {
        var rule = new TestDeprecatedElementsValidationRule();

        ShouldPassRule(config =>
        {
            config.Query = """
                {
                  human {
                    fieldWithDeprecatedArg(deprecatedArg: "test")
                  }
                }
                """;
            config.Rule(rule);
        });

        rule.DeprecatedFieldArgumentCalls.Count.ShouldBe(1);
        var call = rule.DeprecatedFieldArgumentCalls[0];
        call.ArgumentNode.Name.StringValue.ShouldBe("deprecatedArg");
        call.ArgumentDefinition.Name.ShouldBe("deprecatedArg");
        call.ArgumentDefinition.DeprecationReason.ShouldBe("This argument is deprecated");
        call.FieldDefinition.Name.ShouldBe("fieldWithDeprecatedArg");
        call.ParentType.Name.ShouldBe("Human");
    }

    [Fact]
    public void should_detect_multiple_deprecated_field_arguments()
    {
        var rule = new TestDeprecatedElementsValidationRule();

        ShouldPassRule(config =>
        {
            config.Query = """
                {
                  human {
                    fieldWithMultipleDeprecatedArgs(deprecatedArg: "test", anotherDeprecatedArg: 42)
                  }
                }
                """;
            config.Rule(rule);
        });

        rule.DeprecatedFieldArgumentCalls.Count.ShouldBe(2);
        rule.DeprecatedFieldArgumentCalls[0].ArgumentNode.Name.StringValue.ShouldBe("deprecatedArg");
        rule.DeprecatedFieldArgumentCalls[1].ArgumentNode.Name.StringValue.ShouldBe("anotherDeprecatedArg");
    }

    [Fact]
    public void should_not_detect_non_deprecated_field_arguments()
    {
        var rule = new TestDeprecatedElementsValidationRule();

        ShouldPassRule(config =>
        {
            config.Query = """
                {
                  human {
                    fieldWithDeprecatedArg(normalArg: "test")
                  }
                }
                """;
            config.Rule(rule);
        });

        rule.DeprecatedFieldArgumentCalls.Count.ShouldBe(0);
    }

    [Fact]
    public void should_detect_deprecated_directive_argument()
    {
        var rule = new TestDeprecatedElementsValidationRule();

        ShouldPassRule(config =>
        {
            config.Query = """
                {
                  human {
                    name @testDirective(deprecatedDirectiveArg: "test")
                  }
                }
                """;
            config.Rule(rule);
        });

        rule.DeprecatedDirectiveArgumentCalls.Count.ShouldBe(1);
        var call = rule.DeprecatedDirectiveArgumentCalls[0];
        call.ArgumentNode.Name.StringValue.ShouldBe("deprecatedDirectiveArg");
        call.ArgumentDefinition.Name.ShouldBe("deprecatedDirectiveArg");
        call.ArgumentDefinition.DeprecationReason.ShouldBe("This directive argument is deprecated");
        call.DirectiveDefinition.Name.ShouldBe("testDirective");
    }

    [Fact]
    public void should_not_detect_non_deprecated_directive_arguments()
    {
        var rule = new TestDeprecatedElementsValidationRule();

        ShouldPassRule(config =>
        {
            config.Query = """
                {
                  human {
                    name @testDirective(normalDirectiveArg: "test")
                  }
                }
                """;
            config.Rule(rule);
        });

        rule.DeprecatedDirectiveArgumentCalls.Count.ShouldBe(0);
    }

    [Fact]
    public void should_detect_deprecated_type_in_fragment_definition()
    {
        var rule = new TestDeprecatedElementsValidationRule();

        ShouldPassRule(config =>
        {
            config.Query = """
                fragment TestFragment on DeprecatedType {
                  name
                }
                
                {
                  human {
                    name
                  }
                }
                """;
            config.Rule(rule);
        });

        rule.DeprecatedTypeCalls.Count.ShouldBe(1);
        var call = rule.DeprecatedTypeCalls[0];
        call.TypeConditionNode.Name.StringValue.ShouldBe("DeprecatedType");
        call.TypeDefinition.Name.ShouldBe("DeprecatedType");
        call.TypeDefinition.DeprecationReason.ShouldBe("This type is deprecated");
    }

    [Fact]
    public void should_detect_deprecated_type_in_inline_fragment()
    {
        var rule = new TestDeprecatedElementsValidationRule();

        ShouldPassRule(config =>
        {
            config.Query = """
                {
                  human {
                    name
                    ... on DeprecatedType {
                      name
                    }
                  }
                }
                """;
            config.Rule(rule);
        });

        rule.DeprecatedTypeCalls.Count.ShouldBe(1);
        var call = rule.DeprecatedTypeCalls[0];
        call.TypeConditionNode.Name.StringValue.ShouldBe("DeprecatedType");
        call.TypeDefinition.Name.ShouldBe("DeprecatedType");
        call.TypeDefinition.DeprecationReason.ShouldBe("This type is deprecated");
    }

    [Fact]
    public void should_not_detect_deprecated_type_in_inline_fragment_without_type_condition()
    {
        var rule = new TestDeprecatedElementsValidationRule();

        ShouldPassRule(config =>
        {
            config.Query = """
                {
                  human {
                    name
                    ... {
                      id
                    }
                  }
                }
                """;
            config.Rule(rule);
        });

        rule.DeprecatedTypeCalls.Count.ShouldBe(0);
    }

    [Fact]
    public void should_not_detect_non_deprecated_types()
    {
        var rule = new TestDeprecatedElementsValidationRule();

        ShouldPassRule(config =>
        {
            config.Query = """
                fragment TestFragment on Human {
                  name
                }
                
                {
                  human {
                    name
                    ... on Human {
                      id
                    }
                  }
                }
                """;
            config.Rule(rule);
        });

        rule.DeprecatedTypeCalls.Count.ShouldBe(0);
    }

    [Fact]
    public void should_detect_multiple_deprecated_elements_in_single_query()
    {
        var rule = new TestDeprecatedElementsValidationRule();

        ShouldPassRule(config =>
        {
            config.Query = """
                fragment TestFragment on DeprecatedType {
                  name
                }
                
                {
                  human {
                    deprecatedField
                    fieldWithDeprecatedArg(deprecatedArg: "test")
                    name @testDirective(deprecatedDirectiveArg: "test")
                    ... on DeprecatedType {
                      name
                    }
                  }
                }
                """;
            config.Rule(rule);
        });

        rule.DeprecatedFieldCalls.Count.ShouldBe(1);
        rule.DeprecatedFieldCalls[0].FieldNode.Name.StringValue.ShouldBe("deprecatedField");
        rule.DeprecatedFieldCalls[0].FieldDefinition.DeprecationReason.ShouldBe("This field is deprecated");
        rule.DeprecatedFieldCalls[0].ParentType.Name.ShouldBe("Human");

        rule.DeprecatedFieldArgumentCalls.Count.ShouldBe(1);
        rule.DeprecatedFieldArgumentCalls[0].ArgumentNode.Name.StringValue.ShouldBe("deprecatedArg");
        rule.DeprecatedFieldArgumentCalls[0].ArgumentDefinition.DeprecationReason.ShouldBe("This argument is deprecated");
        rule.DeprecatedFieldArgumentCalls[0].FieldDefinition.Name.ShouldBe("fieldWithDeprecatedArg");

        rule.DeprecatedDirectiveArgumentCalls.Count.ShouldBe(1);
        rule.DeprecatedDirectiveArgumentCalls[0].ArgumentNode.Name.StringValue.ShouldBe("deprecatedDirectiveArg");
        rule.DeprecatedDirectiveArgumentCalls[0].ArgumentDefinition.DeprecationReason.ShouldBe("This directive argument is deprecated");
        rule.DeprecatedDirectiveArgumentCalls[0].DirectiveDefinition.Name.ShouldBe("testDirective");

        rule.DeprecatedTypeCalls.Count.ShouldBe(2); // One from fragment definition, one from inline fragment
        rule.DeprecatedTypeCalls[0].TypeConditionNode.Name.StringValue.ShouldBe("DeprecatedType");
        rule.DeprecatedTypeCalls[0].TypeDefinition.DeprecationReason.ShouldBe("This type is deprecated");
        rule.DeprecatedTypeCalls[1].TypeConditionNode.Name.StringValue.ShouldBe("DeprecatedType");
        rule.DeprecatedTypeCalls[1].TypeDefinition.DeprecationReason.ShouldBe("This type is deprecated");
    }

    [Fact]
    public void should_handle_nested_deprecated_fields()
    {
        var rule = new TestDeprecatedElementsValidationRule();

        ShouldPassRule(config =>
        {
            config.Query = """
                {
                  human {
                    deprecatedField {
                      nestedDeprecatedField
                    }
                  }
                }
                """;
            config.Rule(rule);
        });

        rule.DeprecatedFieldCalls.Count.ShouldBe(2);
        rule.DeprecatedFieldCalls[0].FieldNode.Name.StringValue.ShouldBe("deprecatedField");
        rule.DeprecatedFieldCalls[1].FieldNode.Name.StringValue.ShouldBe("nestedDeprecatedField");
    }




    public class TestDeprecatedElementsValidationRule : DeprecatedElementsValidationRule
    {
        public List<DeprecatedFieldCall> DeprecatedFieldCalls { get; } = [];
        public List<DeprecatedFieldArgumentCall> DeprecatedFieldArgumentCalls { get; } = [];
        public List<DeprecatedDirectiveArgumentCall> DeprecatedDirectiveArgumentCalls { get; } = [];
        public List<DeprecatedTypeCall> DeprecatedTypeCalls { get; } = [];

        protected override ValueTask OnDeprecatedFieldReferencedAsync(
            ValidationContext context,
            GraphQLField fieldNode,
            FieldType fieldDefinition,
            IGraphType parentType)
        {
            DeprecatedFieldCalls.Add(new DeprecatedFieldCall(fieldNode, fieldDefinition, parentType));
            return default;
        }

        protected override ValueTask OnDeprecatedFieldArgumentReferencedAsync(
            ValidationContext context,
            GraphQLArgument argumentNode,
            QueryArgument argumentDefinition,
            FieldType fieldDefinition,
            IGraphType parentType)
        {
            DeprecatedFieldArgumentCalls.Add(new DeprecatedFieldArgumentCall(argumentNode, argumentDefinition, fieldDefinition, parentType));
            return default;
        }

        protected override ValueTask OnDeprecatedDirectiveArgumentReferencedAsync(
            ValidationContext context,
            GraphQLArgument argumentNode,
            QueryArgument argumentDefinition,
            Directive directiveDefinition)
        {
            DeprecatedDirectiveArgumentCalls.Add(new DeprecatedDirectiveArgumentCall(argumentNode, argumentDefinition, directiveDefinition));
            return default;
        }

        protected override ValueTask OnDeprecatedTypeReferencedAsync(
            ValidationContext context,
            GraphQLNamedType typeConditionNode,
            IGraphType typeDefinition)
        {
            DeprecatedTypeCalls.Add(new DeprecatedTypeCall(typeConditionNode, typeDefinition));
            return default;
        }
    }

    public record DeprecatedFieldCall(GraphQLField FieldNode, FieldType FieldDefinition, IGraphType ParentType);
    public record DeprecatedFieldArgumentCall(GraphQLArgument ArgumentNode, QueryArgument ArgumentDefinition, FieldType FieldDefinition, IGraphType ParentType);
    public record DeprecatedDirectiveArgumentCall(GraphQLArgument ArgumentNode, QueryArgument ArgumentDefinition, Directive DirectiveDefinition);
    public record DeprecatedTypeCall(GraphQLNamedType TypeConditionNode, IGraphType TypeDefinition);

    public class DeprecatedElementsTestSchema : Schema
    {
        public DeprecatedElementsTestSchema()
        {
            Query = new DeprecatedElementsQueryRoot();
            this.RegisterType<DeprecatedType>();
            this.RegisterType<NestedDeprecatedType>();

            Directives.Register(new TestDirective());
        }
    }

    public class DeprecatedElementsQueryRoot : ObjectGraphType
    {
        public DeprecatedElementsQueryRoot()
        {
            Name = "Query";
            Field<HumanWithDeprecatedElements>("human");
        }
    }

    public class HumanWithDeprecatedElements : ObjectGraphType
    {
        public HumanWithDeprecatedElements()
        {
            Name = "Human";
            Field<StringGraphType>("name");
            Field<IntGraphType>("id");

            // Deprecated field
            Field<NestedDeprecatedType>("deprecatedField")
                .DeprecationReason("This field is deprecated");

            // Another deprecated field
            Field<StringGraphType>("anotherDeprecatedField")
                .DeprecationReason("Another deprecated field");

            // Field with deprecated argument
            Field<StringGraphType>("fieldWithDeprecatedArg")
                .Argument<StringGraphType>("deprecatedArg", arg => arg.DeprecationReason = "This argument is deprecated")
                .Argument<StringGraphType>("normalArg");

            // Field with multiple deprecated arguments
            Field<StringGraphType>("fieldWithMultipleDeprecatedArgs")
                .Argument<StringGraphType>("deprecatedArg", arg => arg.DeprecationReason = "This argument is deprecated")
                .Argument<IntGraphType>("anotherDeprecatedArg", arg => arg.DeprecationReason = "Another deprecated argument")
                .Argument<StringGraphType>("normalArg");

            IsTypeOf = _ => true;
        }
    }

    public class NestedDeprecatedType : ObjectGraphType
    {
        public NestedDeprecatedType()
        {
            Name = "NestedDeprecatedType";
            Field<StringGraphType>("name");

            // Nested deprecated field
            Field<StringGraphType>("nestedDeprecatedField")
                .DeprecationReason("This nested field is deprecated");

            IsTypeOf = _ => true;
        }
    }

    public class DeprecatedType : ObjectGraphType
    {
        public DeprecatedType()
        {
            Name = "DeprecatedType";
            DeprecationReason = "This type is deprecated";
            Field<StringGraphType>("name");

            IsTypeOf = _ => true;
        }
    }

    public class TestDirective : Directive
    {
        public TestDirective() : base("testDirective", DirectiveLocation.Field)
        {
            Arguments = new QueryArguments(
                new QueryArgument<StringGraphType>
                {
                    Name = "deprecatedDirectiveArg",
                    DeprecationReason = "This directive argument is deprecated"
                },
                new QueryArgument<StringGraphType>
                {
                    Name = "normalDirectiveArg"
                }
            );
        }
    }
}
