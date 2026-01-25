# Provider J: Interface Class Generator

## Purpose
Generates nested private classes for interface GraphQL types, implementing AOT-compatible interface type handling with reflection-based field registration and metadata configuration.

## Inputs

### Primary Input
From Provider F (Data Organizer):
- **InterfaceTypeData** containing:
  - **CLR Type**: The interface type (e.g., `IStarWarsCharacter`)
  - **Members**: List of properties and methods to be exposed as GraphQL fields
  - **Member Metadata**: For each member:
    - Name
    - Type (property or method)
    - Return type (CLR type)
    - Parameters (for methods)
    - Nullability
    - Attributes (e.g., `[Ignore]`, `[Description]`)
    - Description text

### Additional Context
- Schema namespace
- Required using statements
- Schema class name (for partial class generation)
- Service types referenced in method parameters

## Construction Instructions

The generated interface type file should be constructed as follows:

1. **File Header**
   - Using directives (System.Reflection, GraphQL.Types, GraphQL.Types.Aot, and project-specific types)
   - Namespace declaration matching the schema class

2. **Partial Class Declaration**
   - Partial class matching the schema class name
   - Inherit from `AotSchema`

3. **Nested Private Class**
   - Private class named `AutoOutputGraphType_{TypeName}` where {TypeName} is the interface name
   - Inherit from `AotAutoRegisteringInterfaceGraphType<TInterface>`

4. **Members Dictionary Field**
   - `private readonly Dictionary<MemberInfo, Action<FieldType, MemberInfo>> _members;`
   - Maps reflected members to their field construction methods

5. **Constructor**
   - Initialize `_members` dictionary with reflected properties/methods using:
     - `typeof(TInterface).GetProperty(nameof(TInterface.PropertyName))!` for properties
     - `typeof(TInterface).GetMethod(nameof(TInterface.MethodName), [typeof(ParamType)])!` for methods
   - Map each member to its corresponding `ConstructField{N}` method
   - Loop through `ProvideFields()` and call `AddField(fieldType)` for each
   - Add comments for ignored members (marked with `[Ignore]` attribute)

6. **GetRegisteredMembers Override**
   - Return `_members.Keys`
   - `protected override IEnumerable<MemberInfo> GetRegisteredMembers() => _members.Keys;`

7. **BuildFieldType Override**
   - Invoke the appropriate construction method from the dictionary
   - Clear resolvers for interface fields (interfaces don't provide implementations)
   - `protected override void BuildFieldType(FieldType fieldType, MemberInfo memberInfo)`

8. **ConstructField Methods**
   - One method per field: `public void ConstructField0(FieldType fieldType, MemberInfo memberInfo)`, `ConstructField1`, etc.
   - Numbered sequentially (0, 1, 2...) in the order they appear in the members dictionary
   - Set field metadata (Description, etc.)
   - For methods with parameters: Call `BuildArgument<TParamType>(fieldType, parameters[i])` to register arguments
   - **Do NOT set resolvers** - interface fields are resolved by implementing types

## Example Code

Below is the complete SampleAotSchema_IStarWarsCharacter.cs showing the expected pattern:

```csharp
using System.Reflection;
using GraphQL.StarWars.TypeFirst;
using GraphQL.StarWars.TypeFirst.Types;
using GraphQL.Types;
using GraphQL.Types.Aot;

namespace AotSample;

public partial class SampleAotSchema : AotSchema
{
    private class AutoOutputGraphType_IStarWarsCharacter : AotAutoRegisteringInterfaceGraphType<IStarWarsCharacter>
    {
        private readonly Dictionary<MemberInfo, Action<FieldType, MemberInfo>> _members;
        public AutoOutputGraphType_IStarWarsCharacter()
        {
            _members = new()
            {
                { typeof(IStarWarsCharacter).GetProperty(nameof(IStarWarsCharacter.Id))!, ConstructField0 },
                { typeof(IStarWarsCharacter).GetProperty(nameof(IStarWarsCharacter.Name))!, ConstructField1 },
                // Friends property is marked with [Ignore], so no field is generated
                { typeof(IStarWarsCharacter).GetMethod(nameof(IStarWarsCharacter.GetFriends), [typeof(StarWarsData)])!, ConstructField2 },
                { typeof(IStarWarsCharacter).GetMethod(nameof(IStarWarsCharacter.GetFriendsConnection), [typeof(StarWarsData)])!, ConstructField3 },
                { typeof(IStarWarsCharacter).GetProperty(nameof(IStarWarsCharacter.AppearsIn))!, ConstructField4 },
                // Cursor property is marked with [Ignore], so no field is generated
            };
            foreach (var fieldType in ProvideFields())
            {
                AddField(fieldType);
            }
        }

        protected override IEnumerable<MemberInfo> GetRegisteredMembers() => _members.Keys;
        protected override void BuildFieldType(FieldType fieldType, MemberInfo memberInfo)
        {
            _members[memberInfo](fieldType, memberInfo);
            if (!fieldType.IsPrivate)
            {
                fieldType.Resolver = null;
                fieldType.StreamResolver = null;
            }
        }

        public void ConstructField0(FieldType fieldType, MemberInfo memberInfo)
        {
            fieldType.Description = "The id of the character.";
        }

        public void ConstructField1(FieldType fieldType, MemberInfo memberInfo)
        {
            fieldType.Description = "The name of the character.";
        }

        public void ConstructField2(FieldType fieldType, MemberInfo memberInfo)
        {
            var parameters = ((MethodInfo)memberInfo).GetParameters();
            var param0 = BuildArgument<StarWarsData>(fieldType, parameters[0]);
        }

        public void ConstructField3(FieldType fieldType, MemberInfo memberInfo)
        {
            var parameters = ((MethodInfo)memberInfo).GetParameters();
            var param0 = BuildArgument<StarWarsData>(fieldType, parameters[0]);
        }

        public void ConstructField4(FieldType fieldType, MemberInfo memberInfo)
        {
            fieldType.Description = "Which movie they appear in.";
        }
    }
}
```
