# Global Helper Functions Reference

This document contains all the global/static helper functions used across the AOT source generator providers. These functions do not reference provider-specific state and can be used across all providers.

## Type Unwrapping Functions

### Helper Function: UnwrapGraphType
```
FUNCTION UnwrapGraphType(graphType):
    // Recursively unwrap ListGraphType and NonNullGraphType wrappers to find the underlying GraphType
    // Parameters:
    //   - graphType: The GraphType to unwrap (e.g., NonNullGraphType<ListGraphType<DroidType>>)
    // Returns: The unwrapped GraphType (e.g., DroidType)
    
    // Handle NonNullGraphType<T>
    IF graphType is NonNullGraphType<T>:
        RETURN UnwrapGraphType(T)
    
    // Handle ListGraphType<T>
    IF graphType is ListGraphType<T>:
        RETURN UnwrapGraphType(T)
    
    // Base case: return the unwrapped type
    RETURN graphType
```

### Helper Function: UnwrapClrType
```
FUNCTION UnwrapClrType(type):
    // Recursively unwrap nested generic wrappers to find the underlying CLR type
    // Parameters:
    //   - type: The CLR type to unwrap
    // Returns: The unwrapped CLR type
    
    // Handle Nullable<T>
    IF type is Nullable<T>:
        RETURN UnwrapClrType(T)
    
    // Handle Task<T>
    IF type is Task<T>:
        RETURN UnwrapClrType(T)
    
    // Handle ValueTask<T>
    IF type is ValueTask<T>:
        RETURN UnwrapClrType(T)
    
    // Handle IDataLoaderResult<T>
    IF type is IDataLoaderResult<T>:
        RETURN UnwrapClrType(T)
    
    // Handle recognized list types
    IF type is IEnumerable<T> OR
       type is IList<T> OR
       type is List<T> OR
       type is IReadOnlyList<T> OR
       type is ICollection<T> OR
       type is IReadOnlyCollection<T> OR
       type is T[]:
        RETURN UnwrapClrType(T)
    
    // Base case: return the unwrapped type
    RETURN type
```

## Member Scanning Functions

### Helper Function: ShouldSkipMember
```
FUNCTION ShouldSkipMember(member):
    // Determines if a property or method should be skipped during type discovery
    // Parameters:
    //   - member: The property or method to check
    // Returns: true if member should be skipped, false otherwise
    
    // Skip if member has [Ignore] attribute
    IF member has Ignore attribute:
        RETURN true
    
    RETURN false
```

### Helper Function: GetMembersToScan
```
FUNCTION GetMembersToScan(clrType, isInputType):
    // Collects members (fields, properties, methods) that should be scanned for a CLR type
    // Parameters:
    //   - clrType: The CLR type to scan
    //   - isInputType: boolean - true if this is an input type, false if output type
    // Returns: List of members to scan
    
    // Check for MemberScan attribute on the type
    memberScanAttribute = Find MemberScan attribute on clrType
    
    // Determine which member types to scan
    IF memberScanAttribute is found:
        scanFlags = memberScanAttribute.ScanFlags value
    ELSE:
        // Default: properties and methods
        scanFlags = Properties | Methods
    
    membersToScan = empty list
    
    // Collect fields if requested
    IF scanFlags includes Fields:
        FOR EACH field in clrType:
            // Skip if member should be skipped (has Ignore attribute, etc.)
            IF ShouldSkipMember(field):
                CONTINUE to next field
            
            // Skip readonly fields for input types (can't be set)
            IF isInputType AND field is readonly:
                CONTINUE to next field
            
            Add field to membersToScan
    
    // Collect properties if requested
    IF scanFlags includes Properties:
        FOR EACH property in clrType:
            // Skip if member should be skipped (has Ignore attribute, etc.)
            IF ShouldSkipMember(property):
                CONTINUE to next property
            
            // Skip write-only properties for output types (can't be read)
            IF NOT isInputType AND property is write-only:
                CONTINUE to next property
            
            // Skip read-only properties for input types (can't be set)
            IF isInputType AND property is read-only:
                CONTINUE to next property
            
            Add property to membersToScan
    
    // Collect methods if requested
    IF scanFlags includes Methods:
        // Methods are only valid for output types
        IF NOT isInputType:
            FOR EACH method in clrType:
                // Skip if member should be skipped (has Ignore attribute, etc.)
                IF ShouldSkipMember(method):
                    CONTINUE to next method
                
                Add method to membersToScan
    
    RETURN membersToScan
```

## Attribute Extraction Functions

### Helper Function: GetMemberGraphType
```
FUNCTION GetMemberGraphType(member, isInputType):
    // Gets the explicit GraphType from a member's attributes and unwraps it
    // Handles both input and output type attributes based on context
    // Parameters:
    //   - member: The property or method to check
    //   - isInputType: boolean - true if scanning for input types, false for output types
    // Returns: The unwrapped GraphType if found, null otherwise
    
    memberGraphType = null
    
    // Check for explicit type override attributes based on context
    IF isInputType:
        // For input types, check input-specific attributes first
        IF member has InputType attribute:
            memberGraphType = attribute's GraphType parameter
        ELSE IF member has InputBaseType attribute:
            memberGraphType = attribute's GraphType parameter
        ELSE IF member has BaseGraphType attribute:
            memberGraphType = attribute's GraphType parameter
        ELSE:
            RETURN null
    ELSE:
        // For output types, check output-specific attributes first
        IF member has OutputType attribute:
            memberGraphType = attribute's GraphType parameter
        ELSE IF member has OutputBaseType attribute:
            memberGraphType = attribute's GraphType parameter
        ELSE IF member has BaseGraphType attribute:
            memberGraphType = attribute's GraphType parameter
        ELSE:
            RETURN null
    
    // Unwrap the GraphType to get the base type
    RETURN UnwrapGraphType(memberGraphType)
```

### Helper Function: GetParameterGraphType
```
FUNCTION GetParameterGraphType(parameter):
    // Gets the explicit GraphType from a parameter's attributes and unwraps it
    // For input types: checks InputType, InputBaseType, BaseGraphType
    // Parameters:
    //   - parameter: The method parameter to check
    // Returns: The unwrapped GraphType if found, null otherwise
    
    parameterGraphType = null
    
    // Check for explicit type override attributes
    IF parameter has InputType attribute:
        parameterGraphType = attribute's GraphType parameter
    ELSE IF parameter has InputBaseType attribute:
        parameterGraphType = attribute's GraphType parameter
    ELSE IF parameter has BaseGraphType attribute:
        parameterGraphType = attribute's GraphType parameter
    ELSE:
        RETURN null
    
    // Unwrap the GraphType to get the base type
    RETURN UnwrapGraphType(parameterGraphType)
```

### Helper Function: ShouldSkipParameterProcessing
```
FUNCTION ShouldSkipParameterProcessing(parameter):
    // Determines if a method parameter should skip CLR type processing
    // Parameters with any ParameterAttribute are injected from the resolution context
    // and are not GraphQL arguments. IResolveFieldContext and CancellationToken
    // are also injected types that should be skipped.
    // Parameters:
    //   - parameter: The method parameter to check
    // Returns: true if parameter processing should be skipped, false otherwise
    
    // Check if parameter type is IResolveFieldContext or CancellationToken
    IF parameter type is IResolveFieldContext:
        RETURN true
    
    IF parameter type is CancellationToken:
        RETURN true
    
    // Check each attribute on the parameter
    FOR EACH attribute on parameter:
        // Check if attribute inherits from ParameterAttribute<T> (generic version)
        IF attribute inherits from ParameterAttribute<T>:
            typeMatches = T is parameter type
            IF typeMatches:
                RETURN true
            ELSE:
                CONTINUE to next attribute
        
        // Check if attribute inherits from ParameterAttribute
        IF attribute inherits from ParameterAttribute:
            RETURN true
    
    // No ParameterAttribute found on this parameter
    RETURN false
```

## Usage Notes

### Type Reference Processing
When using `GetMemberGraphType` or `GetParameterGraphType`, the returned GraphType may be a type reference:
- `GraphQLClrInputTypeReference<T>` for input types
- `GraphQLClrOutputTypeReference<T>` for output types

The calling code should check for these type references and handle them appropriately:
```
memberGraphType = GetMemberGraphType(member, isInputType)
IF memberGraphType is not null:
    IF isInputType AND memberGraphType is GraphQLClrInputTypeReference<T>:
        // Extract T and add to CLR types collection
        Add T to discoveredClrTypes
    ELSE IF NOT isInputType AND memberGraphType is GraphQLClrOutputTypeReference<T>:
        // Extract T and add to CLR types collection
        Add T to discoveredClrTypes
    ELSE:
        // Add the GraphType to discovered GraphTypes
        Add memberGraphType to discoveredGraphTypes
```

### Unwrapping Order
Functions like `GetMemberGraphType` and `GetParameterGraphType` automatically unwrap `ListGraphType` and `NonNullGraphType` wrappers before returning. The caller should then check if the result is a type reference.
