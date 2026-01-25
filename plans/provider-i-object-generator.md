# Provider I: Object Class Generator

## Purpose
Generates nested private classes for output object GraphQL types, implementing AOT-compatible object type handling with reflection-based field registration and resolver construction.

## Inputs

### Primary Input
From Provider F (Data Organizer):
- **ObjectTypeData** containing:
  - **CLR Type**: The output class type (e.g., `Droid`, `Human`)
  - **Members**: List of properties and methods to be exposed as GraphQL fields
  - **Member Metadata**: For each member:
    - Name
    - Type (property or method)
    - Return type (CLR type)
    - Parameters (for methods)
    - Nullability
    - Attributes (e.g., `[Ignore]`, `[Description]`)
    - Whether it requires service resolution

### Additional Context
- Schema namespace
- Required using statements
- Schema class name (for partial class generation)
- Service types referenced in method parameters

## Construction Instructions

The generated object type file should be constructed as follows:

1. **File Header**
   - Using directives (System.Reflection, GraphQL.Types, GraphQL.Types.Aot, and project-specific types)
   - Namespace declaration matching the schema class

2. **Partial Class Declaration**
   - Partial class matching the schema class name
   - Inherit from `AotSchema`

3. **Nested Private Class**
   - Private class named `AutoOutputGraphType_{TypeName}` where {TypeName} is the CLR type name
   - Inherit from `AotAutoRegisteringObjectGraphType<TClrType>`

4. **Members Dictionary Field**
   - `private readonly Dictionary<MemberInfo, Action<FieldType, MemberInfo>> _members;`
   - Maps reflected members to their field construction methods

5. **Constructor**
   - Initialize `_members` dictionary with reflected properties/methods using:
     - `typeof(TClrType).GetProperty(nameof(TClrType.PropertyName))!` for properties
     - `typeof(TClrType).GetMethod(nameof(TClrType.MethodName), [typeof(ParamType)])!` for methods
   - Map each member to its corresponding `ConstructField_*` method
   - Loop through `ProvideFields()` and call `AddField(fieldType)` for each
   - Add comments for ignored members (marked with `[Ignore]` attribute)

6. **GetRegisteredMembers Override**
   - Return `_members.Keys`
   - `protected override IEnumerable<MemberInfo> GetRegisteredMembers() => _members.Keys;`

7. **BuildFieldType Override**
   - Invoke the appropriate construction method from the dictionary
   - `protected override void BuildFieldType(FieldType fieldType, MemberInfo memberInfo) => _members[memberInfo](fieldType, memberInfo);`

8. **ConstructField Methods**
   - One method per field: `public void ConstructField0(FieldType fieldType, MemberInfo memberInfo)`, `ConstructField1`, etc.
   - Numbered sequentially (0, 1, 2...) in the order they appear in the members dictionary
   - For simple properties: Set `fieldType.Resolver` using `BuildFieldResolver(context => GetMemberInstance(context).PropertyName, false)`
   - For methods with parameters:
     - Build argument accessors using `BuildArgument<TParamType>(fieldType, parameters[i])`
     - Set `fieldType.Resolver` using `BuildFieldResolver` with a lambda that calls the method
     - Pass `true` for async support if method returns Task or similar

## Example Code

Below is the complete SampleAotSchema_Droid.cs showing the expected pattern:

```csharp
using System.Reflection;
using GraphQL.StarWars.TypeFirst;
using GraphQL.StarWars.TypeFirst.Types;
using GraphQL.Types;
using GraphQL.Types.Aot;

namespace AotSample;

public partial class SampleAotSchema : AotSchema
{
    private class AutoOutputGraphType_Droid : AotAutoRegisteringObjectGraphType<Droid>
    {
        private readonly Dictionary<MemberInfo, Action<FieldType, MemberInfo>> _members;
        public AutoOutputGraphType_Droid()
        {
            _members = new()
            {
                { typeof(Droid).GetProperty(nameof(Droid.Id))!, ConstructField0 },
                { typeof(Droid).GetProperty(nameof(Droid.Name))!, ConstructField1 },
                // Friends property is marked with [Ignore], so no field is generated
                { typeof(Droid).GetMethod(nameof(Droid.GetFriends), [typeof(StarWarsData)])!, ConstructField2 },
                { typeof(Droid).GetMethod(nameof(Droid.GetFriendsConnection), [typeof(StarWarsData)])!, ConstructField3 },
                { typeof(Droid).GetProperty(nameof(Droid.AppearsIn))!, ConstructField4 },
                // Cursor property is marked with [Ignore], so no field is generated
                { typeof(Droid).GetProperty(nameof(Droid.PrimaryFunction))!, ConstructField5 },
            };
            foreach (var fieldType in ProvideFields())
            {
                AddField(fieldType);
            }
        }

        protected override IEnumerable<MemberInfo> GetRegisteredMembers() => _members.Keys;
        protected override void BuildFieldType(FieldType fieldType, MemberInfo memberInfo) => _members[memberInfo](fieldType, memberInfo);

        public void ConstructField0(FieldType fieldType, MemberInfo memberInfo)
        {
            fieldType.Resolver = BuildFieldResolver(context => GetMemberInstance(context).Id, false);
        }

        public void ConstructField1(FieldType fieldType, MemberInfo memberInfo)
        {
            fieldType.Resolver = BuildFieldResolver(context => GetMemberInstance(context).Name, false);
        }

        public void ConstructField2(FieldType fieldType, MemberInfo memberInfo)
        {
            var parameters = ((MethodInfo)memberInfo).GetParameters();
            var param0 = BuildArgument<StarWarsData>(fieldType, parameters[0]);
            fieldType.Resolver = BuildFieldResolver(context =>
            {
                var source = GetMemberInstance(context);
                var arg0 = param0(context);
                return source.GetFriends(arg0);
            }, true);
        }

        public void ConstructField3(FieldType fieldType, MemberInfo memberInfo)
        {
            var parameters = ((MethodInfo)memberInfo).GetParameters();
            var param0 = BuildArgument<StarWarsData>(fieldType, parameters[0]);
            fieldType.Resolver = BuildFieldResolver(context =>
            {
                var source = GetMemberInstance(context);
                var arg0 = param0(context);
                return source.GetFriendsConnection(arg0);
            }, true);
        }

        public void ConstructField4(FieldType fieldType, MemberInfo memberInfo)
        {
            fieldType.Resolver = BuildFieldResolver(context => GetMemberInstance(context).AppearsIn, false);
        }

        public void ConstructField5(FieldType fieldType, MemberInfo memberInfo)
        {
            fieldType.Resolver = BuildFieldResolver(context => GetMemberInstance(context).PrimaryFunction, false);
        }
    }
}
```
