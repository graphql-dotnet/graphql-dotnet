# Provider H: Input Class Generator

## Purpose
Generates nested private classes for input object GraphQL types, implementing AOT-compatible input type handling with reflection-based field registration and custom parsing logic.

## Inputs

### Primary Input
From Provider F (Data Organizer):
- **InputTypeData** containing:
  - **CLR Type**: The input class type (e.g., `HumanInput`)
  - **Properties**: List of properties to be exposed as GraphQL fields
  - **Property Metadata**: For each property:
    - Name
    - Type (CLR type)
    - Nullability
    - Default value (if any)
    - Attributes/metadata

### Additional Context
- Schema namespace
- Required using statements
- Schema class name (for partial class generation)

## Construction Instructions

The generated input type file should be constructed as follows:

1. **File Header**
   - Using directives (System.Reflection, GraphQL, GraphQL.Types, GraphQL.Types.Aot, GraphQLParser.AST, and project-specific types)
   - Namespace declaration matching the schema class

2. **Partial Class Declaration**
   - Partial class matching the schema class name
   - Inherit from `AotSchema`

3. **Nested Private Class**
   - Private class named `AutoInputGraphType_{TypeName}` where {TypeName} is the CLR type name
   - Inherit from `AotAutoRegisteringInputObjectGraphType<TClrType>`

4. **Fields**
   - `private readonly MemberInfo[] _members;` - Array of reflected property members
   - `private readonly FieldType[] _fields;` - Array of GraphQL field type definitions

5. **Constructor**
   - Initialize `_members` array with reflected properties using `typeof(TClrType).GetProperty(nameof(TClrType.PropertyName))!`
   - Call `_fields = ProvideFields().ToArray();` to get field definitions
   - Loop through `_fields` and call `AddField(fieldType)` for each

6. **GetRegisteredMembers Override**
   - Return `_members` array
   - `protected override IEnumerable<MemberInfo> GetRegisteredMembers() => _members;`

7. **ParseDictionary Override**
   - Implement custom parsing logic to convert dictionary to CLR instance
   - Use local `ParseField<TFieldType>` helper method to extract and convert field values
   - Create and return instance of the CLR type with all properties populated

8. **IsValidDefault Override**
   - Validate that the provided value is of the correct type
   - Check each field using `_fields[i].ResolvedType!.IsValidDefault()`
   - Return false if any field contains a default value (for required fields)

9. **ToAST Override**
   - Convert CLR instance to GraphQL AST representation
   - Return `GraphQLNullValue` for null input
   - Create `GraphQLObjectValue` with fields
   - Use local `ProcessField` helper to convert each property to AST

## Example Code

Below is the complete SampleAotSchema_HumanInput.cs showing the expected pattern:

```csharp
using System.Reflection;
using GraphQL;
using GraphQL.StarWars.TypeFirst.Types;
using GraphQL.Types;
using GraphQL.Types.Aot;
using GraphQLParser.AST;

namespace AotSample;

public partial class SampleAotSchema : AotSchema
{
    private class AutoInputGraphType_HumanInput : AotAutoRegisteringInputObjectGraphType<HumanInput>
    {
        private readonly MemberInfo[] _members;
        private readonly FieldType[] _fields;

        public AutoInputGraphType_HumanInput()
        {
            _members = [
                typeof(HumanInput).GetProperty(nameof(HumanInput.Name))!,
                typeof(HumanInput).GetProperty(nameof(HumanInput.HomePlanet))!,
            ];
            _fields = ProvideFields().ToArray();
            foreach (var fieldType in _fields)
            {
                AddField(fieldType);
            }
        }

        protected override IEnumerable<MemberInfo> GetRegisteredMembers() => _members;

        public override object ParseDictionary(IDictionary<string, object?> value, IValueConverter valueConverter)
        {
            // supports arguments in constructors, required and init
            // exactly same semantics as non-aot input object types
            return new HumanInput
            {
                Name = ParseField<string>(_fields[0]),
                HomePlanet = ParseField<string?>(_fields[1]),
            };

            TFieldType ParseField<TFieldType>(FieldType fieldType)
            {
                if (value.TryGetValue(fieldType.Name, out var fieldValue))
                {
                    return (TFieldType)valueConverter.GetPropertyValue(fieldValue, typeof(string), fieldType.ResolvedType!)!;
                }
                return default!;
            }
        }

        public override bool IsValidDefault(object value)
        {
            if (value is not HumanInput obj)
                return false;

            if (_fields[0].ResolvedType!.IsValidDefault(obj.Name))
                return false;
            if (_fields[1].ResolvedType!.IsValidDefault(obj.HomePlanet))
                return false;

            return true;
        }

        public override GraphQLValue ToAST(object? value)
        {
            if (value == null)
                return new GraphQLNullValue();

            if (value is not HumanInput obj)
                return null!; //allowed return to indicate failure

            var objectValue = new GraphQLObjectValue
            {
                Fields = new(_fields.Length)
            };

            ProcessField(_fields[0], obj.Name);
            ProcessField(_fields[1], obj.HomePlanet);

            return objectValue;

            void ProcessField(FieldType fieldType, object? value)
            {
                var ast = fieldType.ResolvedType!.ToAST(value)
                    ?? throw new InvalidOperationException("Could not convert value in HumanInput.Name to AST");
                if (ast is not GraphQLNullValue || fieldType.DefaultValue != null)
                    objectValue.Fields.Add(new(new(fieldType.Name), ast));
            }
        }
    }
}
```
