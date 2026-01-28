# Provider A: CLR Input Type Scanner

## Purpose
Scans a specific CLR input type and discovers its dependencies by examining properties and fields.

## Inputs

### Primary Input
- **CLR Type**: A CLR type to be used as an input type in GraphQL schema

## Outputs

Returns discovered type information:

1. **Other referenced input CLR types**: CLR types found in properties/fields
2. **Other referenced input GraphTypes**: GraphTypes from explicit attribute overrides
3. **Input list CLR types**: List types (`T[]`, `IEnumerable<T>`, etc.) before unwrapping

## Logic Flow

```
FUNCTION ScanInputType(currentClrType):
    // Get members to scan based on MemberScan attribute
    membersToScan = GetMembersToScan(currentClrType, isInputType: true)
    
    discoveredClrTypes = empty list
    discoveredGraphTypes = empty list
    inputListTypes = empty list
    
    // Inspect members to discover types
    FOR EACH member in membersToScan:
        memberClrType = member.Type
        
        // Check if member type is a list type BEFORE unwrapping
        IF memberClrType is T[] OR IEnumerable<T> OR IList<T> OR List<T> OR
           IReadOnlyList<T> OR ICollection<T> OR IReadOnlyCollection<T>:
            Add memberClrType to inputListTypes
        
        // Check if member has explicit GraphType override
        memberGraphType = GetMemberGraphType(member)
        IF memberGraphType is not null:
            // Check if it's a GraphQLClrInputTypeReference<T>
            IF memberGraphType is GraphQLClrInputTypeReference<T>:
                // Extract T and add to discoveredClrTypes
                Add T to discoveredClrTypes
            ELSE:
                // Add unwrapped GraphType to discoveredGraphTypes
                Add memberGraphType to discoveredGraphTypes
            
            CONTINUE to next member
        
        // Unwrap nested generic wrappers (recursively)
        unwrappedClrType = UnwrapClrType(memberClrType)
        
        // Discover nested input types
        Add unwrappedClrType to discoveredClrTypes
    
    RETURN discoveredTypes, inputListTypes
```

**Note:** This provider uses global helper functions defined in [`helper-functions.md`](helper-functions.md):
- [`GetMemberGraphType()`](helper-functions.md) - Extracts and unwraps GraphType from member attributes
- [`UnwrapGraphType()`](helper-functions.md) - Unwraps ListGraphType and NonNullGraphType wrappers
- [`GetMembersToScan()`](helper-functions.md) - Collects members to scan based on MemberScan attribute
- [`UnwrapClrType()`](helper-functions.md) - Unwraps CLR type wrappers (Nullable, collections, etc.)
