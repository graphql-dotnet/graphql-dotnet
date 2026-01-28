# Provider D: Syntax Provider (Candidate Class Identification)

## Purpose
Identifies class declarations that are candidates for code generation by looking for classes with relevant attributes. This provider only captures the list of candidate classes; Provider C will extract the attributes from each candidate class.

## Strategy
Instead of manually iterating through all syntax trees and classes, create separate incremental providers for each target attribute. The compiler maintains internal indexes of attributes, allowing it to quickly locate only the relevant classes without examining every class in the compilation.

## Inputs

### Primary Input
- **Generator Context**: The incremental generator initialization context

### Target Attributes (Fully Qualified Names)
- `GraphQL.DI.AotQueryTypeAttribute`
- `GraphQL.DI.AotMutationTypeAttribute`
- `GraphQL.DI.AotSubscriptionTypeAttribute`
- `GraphQL.DI.AotOutputTypeAttribute`
- `GraphQL.DI.AotInputTypeAttribute`
- `GraphQL.DI.AotGraphTypeAttribute`
- `GraphQL.DI.AotTypeMappingAttribute`
- `GraphQL.DI.AotListTypeAttribute`
- `GraphQL.DI.AotRemapTypeAttribute`

## Outputs

### Primary Output
- **Candidate Classes**: List of class declarations that have one or more target attributes

## Logic Flow

```
FUNCTION CreateCandidateProviders(generatorContext):
    // Define all target attributes with fully qualified names
    targetAttributes = [
        "GraphQL.DI.AotQueryTypeAttribute",
        "GraphQL.DI.AotMutationTypeAttribute",
        "GraphQL.DI.AotSubscriptionTypeAttribute",
        "GraphQL.DI.AotOutputTypeAttribute",
        "GraphQL.DI.AotInputTypeAttribute",
        "GraphQL.DI.AotGraphTypeAttribute",
        "GraphQL.DI.AotTypeMappingAttribute",
        "GraphQL.DI.AotListTypeAttribute",
        "GraphQL.DI.AotRemapTypeAttribute"
    ]
    
    incrementalProviders = empty list
    
    // Create a separate provider for each attribute type
    // This leverages the compiler's attribute index for fast lookup
    FOR EACH attributeName in targetAttributes:
        provider = CREATE incremental provider with:
            ATTRIBUTE_FILTER:
                - Filter by fully qualified attribute name: attributeName
                
            SYNTAX_PREDICATE:
                - Only process nodes that are class declarations
                - This is a fast syntax-only check before semantic analysis
                
            TRANSFORM:
                - Capture the class declaration
        
        ADD provider to incrementalProviders
    
    // Combine all providers into a single list of candidate classes
    combinedProvider = COMBINE all incrementalProviders into single list
    
    RETURN combinedProvider as list of candidate classes
```

## Next Stage
Passes list of candidate classes to **Provider C: Attribute Data Collector** which will extract attributes from each candidate class.
