# Custom Validation Rules Documentation Guide

## Overview

This guide provides complete documentation for implementing custom validation rules in GraphQL.NET, including sample projects and integration instructions.

---

## 1. Sample Projects

### 1.1 No Deprecated Fields Rule

```csharp
using GraphQL.Validation;
using GraphQL.Language.AST;

namespace GraphQL.Validation.Samples
{
    public class NoDeprecatedFieldsValidationRule : ValidationRuleBase
    {
        public override ValueTask<INodeVisitor?> ValidateAsync(ValidationContext context)
        {
            return new ValueTask<INodeVisitor?>(new NoDeprecatedFieldsVisitor(context));
        }
    }

    internal class NoDeprecatedFieldsVisitor : INodeVisitor
    {
        private readonly ValidationContext _context;

        public NoDeprecatedFieldsVisitor(ValidationContext context)
        {
            _context = context;
        }

        public ValueTask EnterAsync(ASTNode node, ValidationContext context)
        {
            if (node is Field field)
            {
                var fieldDef = context.GetFieldDef(
                    context.GetParentType(context.GetTypeInfo(node)),
                    field);

                if (fieldDef?.DeprecationReason != null)
                {
                    _context.ReportError(new ValidationError(
                        context.OriginalQuery,
                        "5.4.2",
                        $"The field '{field.Name}' is deprecated: {fieldDef.DeprecationReason}",
                        field));
                }
            }

            return default;
        }

        public ValueTask LeaveAsync(ASTNode node, ValidationContext context) => default;
    }
}
```

### 1.2 Max Depth Rule

```csharp
public class MaxDepthValidationRule : ValidationRuleBase
{
    private readonly int _maxDepth;

    public MaxDepthValidationRule(int maxDepth = 10)
    {
        _maxDepth = maxDepth;
    }

    public override ValueTask<INodeVisitor?> ValidateAsync(ValidationContext context)
    {
        return new ValueTask<INodeVisitor?>(new MaxDepthVisitor(_maxDepth, context));
    }

    internal class MaxDepthVisitor : INodeVisitor
    {
        private readonly int _maxDepth;
        private readonly ValidationContext _context;
        private int _currentDepth;

        public MaxDepthVisitor(int maxDepth, ValidationContext context)
        {
            _maxDepth = maxDepth;
            _context = context;
        }

        public ValueTask EnterAsync(ASTNode node, ValidationContext context)
        {
            if (node is Operation || node is Field)
            {
                _currentDepth++;
                if (_currentDepth > _maxDepth)
                {
                    _context.ReportError(new ValidationError(
                        context.OriginalQuery,
                        "MAX_DEPTH",
                        $"Query depth {_currentDepth} exceeds maximum {_maxDepth}",
                        node));
                }
            }
            return default;
        }

        public ValueTask LeaveAsync(ASTNode node, ValidationContext context)
        {
            if (node is Operation || node is Field) _currentDepth--;
            return default;
        }
    }
}
```

### 1.3 Field Complexity Rule

```csharp
public class FieldComplexityValidationRule : ValidationRuleBase
{
    private readonly int _maxComplexity;
    private readonly Dictionary<string, int> _fieldWeights;

    public FieldComplexityValidationRule(int maxComplexity = 100, Dictionary<string, int>? fieldWeights = null)
    {
        _maxComplexity = maxComplexity;
        _fieldWeights = fieldWeights ?? new Dictionary<string, int>();
    }

    public override ValueTask<INodeVisitor?> ValidateAsync(ValidationContext context)
    {
        return new ValueTask<INodeVisitor?>(new FieldComplexityVisitor(_maxComplexity, _fieldWeights, context));
    }

    internal class FieldComplexityVisitor : INodeVisitor
    {
        private readonly int _maxComplexity;
        private readonly Dictionary<string, int> _fieldWeights;
        private readonly ValidationContext _context;
        private int _totalComplexity;

        public FieldComplexityVisitor(int maxComplexity, Dictionary<string, int> fieldWeights, ValidationContext context)
        {
            _maxComplexity = maxComplexity;
            _fieldWeights = fieldWeights;
            _context = context;
        }

        public ValueTask EnterAsync(ASTNode node, ValidationContext context)
        {
            if (node is Field field)
            {
                var weight = _fieldWeights.TryGetValue(field.Name, out var w) ? w : 1;
                _totalComplexity += weight;

                if (_totalComplexity > _maxComplexity)
                {
                    _context.ReportError(new ValidationError(
                        context.OriginalQuery,
                        "COMPLEXITY",
                        $"Query complexity {_totalComplexity} exceeds {_maxComplexity}",
                        node));
                }
            }
            return default;
        }

        public ValueTask LeaveAsync(ASTNode node, ValidationContext context) => default;
    }
}
```

---

## 2. Registration

```csharp
services.AddGraphQL(b => b
    .AddAutoSchema<Query>()
    .AddSystemTextJson()
    .AddValidationRule<NoDeprecatedFieldsValidationRule>()
    .AddValidationRule(new MaxDepthValidationRule(10))
    .AddValidationRule(new FieldComplexityValidationRule(100, new Dictionary<string, int>
    {
        { "expensiveField", 10 },
        { "complexResolver", 20 }
    })));
```

---

## 3. Testing

```csharp
[Fact]
public async Task Should_Reject_Query_Exceeding_Depth_Limit()
{
    var schema = Schema.For(@"
        type Query { user: User }
        type User { name: String posts: [Post] }
        type Post { comments: [Comment] }
        type Comment { author: User }
    ");

    var query = @"query { user { posts { comments { author { posts { comments { author { name } } } } } } }";

    var result = await schema.ExecuteAsync(_ =>
    {
        _.Query = query;
        _.ValidationRules = DocumentValidator.CoreRules.Append(new MaxDepthValidationRule(5));
    });

    result.Errors.ShouldNotBeNull();
    result.Errors.ShouldHaveSingleItem();
}
```

---

## 4. Best Practices

1. **Specific error messages** - Include field name and actionable guidance
2. **Lightweight validation** - Avoid expensive operations in visitors
3. **Composable rules** - Design rules to work together without conflicts

---

## 5. Files to Add

1. `src/GraphQL.Validation.Samples/` - Sample validation rules
2. `tests/GraphQL.Tests/Validation/CustomValidationTests.cs` - Tests
3. `docs2/guides/validation/custom-rules.md` - This documentation
4. Update `docs2/sitemap.yaml` - Include new page

---

**Bounty:** #2406 - $100
