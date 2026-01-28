# Provider B: CLR Output Type Scanner

## Purpose
Scans a specific CLR output type and discovers its dependencies by examining properties, methods, and their parameters.

## Inputs

### Primary Input
- **CLR Type**: A CLR type to be used as an output type in GraphQL schema

## Outputs

Returns discovered type information:

1. **Other referenced output CLR types**: CLR types from property/method return types
2. **Other referenced output GraphTypes**: GraphTypes from explicit attribute overrides on members
3. **Referenced CLR input types**: CLR types from method parameters
4. **Referenced input GraphTypes**: GraphTypes from explicit attribute overrides on parameters
5. **Input list CLR types**: List types (`T[]`, `IEnumerable<T>`, etc.) from method parameters before unwrapping

## Logic Flow

```
FUNCTION ScanOutputType(currentClrType):
    // Get members to scan based on MemberScan attribute
    membersToScan = GetMembersToScan(currentClrType, isInputType: false)
    
    outputDiscoveredClrTypes = empty list
    outputDiscoveredGraphTypes = empty list
    inputDiscoveredClrTypes = empty list
    inputDiscoveredGraphTypes = empty list
    inputListTypes = empty list
    
    // Inspect members to discover return types
    FOR EACH member in membersToScan:
        // Check if member has explicit GraphType override
        memberGraphType = GetMemberGraphType(member, isInputType: false)
        IF memberGraphType is not null:
            // Check if it's a GraphQLClrOutputTypeReference<T>
            IF memberGraphType is GraphQLClrOutputTypeReference<T>:
                // Extract T and add to outputDiscoveredClrTypes
                Add T to outputDiscoveredClrTypes
            ELSE:
                // Add unwrapped GraphType to outputDiscoveredGraphTypes
                Add memberGraphType to outputDiscoveredGraphTypes
            
            CONTINUE to next member
        
        memberClrType = member.Type  // Return type for property/method
        
        // Unwrap nested generic wrappers (recursively)
        unwrappedClrType = UnwrapClrType(memberClrType)
        
        // Discover nested output types
        Add unwrappedClrType to outputDiscoveredClrTypes
    
    // Inspect methods to discover input types from parameters
    methodsToScan = Filter membersToScan to only methods
    FOR EACH method in methodsToScan:
        FOR EACH parameter in method:
            // Skip parameters that are injected from context (not GraphQL arguments)
            IF ShouldSkipParameterProcessing(parameter):
                CONTINUE to next parameter
            
            parameterClrType = parameter.Type
            
            // Check if parameter type is a list type BEFORE unwrapping
            IF parameterClrType is T[] OR IEnumerable<T> OR IList<T> OR List<T> OR
               IReadOnlyList<T> OR ICollection<T> OR IReadOnlyCollection<T>:
                Add parameterClrType to inputListTypes
            
            // Check if parameter has explicit GraphType override
            parameterGraphType = GetParameterGraphType(parameter)
            IF parameterGraphType is not null:
                // Check if it's a GraphQLClrInputTypeReference<T>
                IF parameterGraphType is GraphQLClrInputTypeReference<T>:
                    // Extract T and add to inputDiscoveredClrTypes
                    Add T to inputDiscoveredClrTypes
                ELSE:
                    // Add unwrapped GraphType to inputDiscoveredGraphTypes
                    Add parameterGraphType to inputDiscoveredGraphTypes
                
                CONTINUE to next parameter
            
            // Unwrap nested generic wrappers
            unwrappedParamType = UnwrapClrType(parameterClrType)
            
            // Method parameters are input types
            Add unwrappedParamType to inputDiscoveredClrTypes
    
    RETURN outputDiscoveredClrTypes, outputDiscoveredGraphTypes,
           inputDiscoveredClrTypes, inputDiscoveredGraphTypes, inputListTypes
```

**Note:** This provider uses global helper functions defined in [`helper-functions.md`](helper-functions.md):
- [`GetMemberGraphType()`](helper-functions.md) - Extracts and unwraps GraphType from member attributes
- [`GetParameterGraphType()`](helper-functions.md) - Extracts and unwraps GraphType from parameter attributes
- [`UnwrapGraphType()`](helper-functions.md) - Unwraps ListGraphType and NonNullGraphType wrappers
- [`GetMembersToScan()`](helper-functions.md) - Collects members to scan based on MemberScan attribute
- [`UnwrapClrType()`](helper-functions.md) - Unwraps CLR type wrappers (Nullable, collections, Task, etc.)
- [`ShouldSkipParameterProcessing()`](helper-functions.md) - Checks if parameter should be skipped
