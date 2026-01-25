# AOT Source Generator Pseudocode

## Overview
This incremental source generator processes attribute-decorated schema classes to generate partial class implementations for AOT (Ahead-of-Time) compilation scenarios in GraphQL.NET.

## Incremental Source Generator Architecture

This source generator uses C# incremental generators with a pipeline architecture to efficiently process syntax trees and generate code only when necessary. The pipeline consists of several stages:

### Pipeline Stages

1. **Syntax Provider (D)** - Identifies candidate class declarations
2. **Attribute Data Provider (C)** - Collects attribute data from candidate classes
3. **CLR Type Scanners (A, B)** - Analyze CLR types to discover dependencies
4. **Type Walker & Collector (E)** - Walks type graph and collects all related types
5. **Data Organizer (F)** - Splits and organizes collected data by category
6. **Code Generators (G, H, I, J)** - Generate partial class implementations

### Incremental Generator Pipeline Flow

```
Syntax Trees
    ↓
[Provider D: Syntax Provider]
    ↓ (Candidate class declarations)
[Provider C: Attribute Collector]
    ↓ (Attribute data)
[Provider E: Type Walker]
    ├→ [Provider A: Input Scanner] ←┐
    └→ [Provider B: Output Scanner] ←┘
    ↓ (Complete type discovery)
[Provider F: Data Organizer]
    ↓ (Organized by category)
    ├→ [Provider G: Candidate Class Generator] → Generated Schema Class
    ├→ [Provider H: Input Class Generator] → Generated Input Helpers
    ├→ [Provider I: Object Class Generator] → Generated Object Helpers
    └→ [Provider J: Interface Class Generator] → Generated Interface Helpers
```

## Incremental Pipeline Steps

### Step 1: Identify Candidate Classes
```
FOR EACH syntax tree in compilation:
    FOR EACH class declaration in syntax tree:
        IF class has ANY of these attributes:
            - AotQueryType
            - AotMutationType
            - AotSubscriptionType
            - AotOutputType
            - AotInputType
            - AotGraphType
            - AotTypeMapping
            - AotListType
            - AotRemapType
        THEN:
            Mark this class as a candidate for generation
            Capture:
                - Class name
                - Namespace
                - All attribute data
                - Whether class already has a constructor
                - Base class name
```

### Step 2: Parse Attributes and Initialize Collections
```
FOR EACH candidate class:
    // Initialize all collection variables that will be used throughout generation
    
    // Root types (single value each, can be null) - all are GraphTypes (either explicit or auto-wrapped)
    queryRootGraphType = null
    mutationRootGraphType = null
    subscriptionRootGraphType = null
    
    // GraphTypes that need to be registered
    discoveredGraphTypes = empty set  // holds: GraphType
    
    // CLR type mappings (CLR type -> GraphType, with GraphType auto-wrapped if needed)
    outputCLRTypeMappings = empty dictionary  // key: CLR type, value: GraphType
    inputCLRTypeMappings = empty dictionary   // key: CLR type, value: GraphType
    
    // CLR types to process for type discovery - holds tuples of (clrType, isInputType)
    clrTypesToProcess = empty queue  // holds: (CLR type, boolean isInputType)
    
    // Parse attributes and populate initial data
    FOR EACH attribute on the class:
        SWITCH attribute type:
            CASE AotQueryType<T>:
                // T can be either a CLR type or a GraphType
                IF T is a GraphType (inherits from IGraphType):
                    queryRootGraphType = T  // T is a GraphType
                    AddGraphType(T, ignoreClrMapping: false)
                ELSE: // T is a CLR type - wrap it in AutoRegisteringObjectGraphType
                    queryRootGraphType = AddOutputClrType(T, isInterface: false)
            
            CASE AotMutationType<T>:
                // T can be either a CLR type or a GraphType
                IF T is a GraphType:
                    mutationRootGraphType = T  // T is a GraphType
                    AddGraphType(T, ignoreClrMapping: false)
                ELSE: // T is a CLR type - wrap it in AutoRegisteringObjectGraphType
                    mutationRootGraphType = AddOutputClrType(T, isInterface: false)
            
            CASE AotSubscriptionType<T>:
                // T can be either a CLR type or a GraphType
                IF T is a GraphType:
                    subscriptionRootGraphType = T  // T is a GraphType
                    AddGraphType(T, ignoreClrMapping: false)
                ELSE: // T is a CLR type - wrap it in AutoRegisteringObjectGraphType
                    subscriptionRootGraphType = AddOutputClrType(T, isInterface: false)
            
            CASE AotOutputType<T>:
                // T is a CLR type - determine wrapper type and wrap immediately
                // Check if attribute has IsInterface property set
                IF attribute defines IsInterface property:
                    isInterfaceFlag = attribute.IsInterface value (null, true, or false)
                ELSE:
                    isInterfaceFlag = null  // Auto-detect based on CLR type
                
                // Add the output type using the helper method
                wrappedGraphType = AddOutputClrType(T, isInterface: isInterfaceFlag)
            
            CASE AotInputType<T>:
                // T is a CLR type - wrap it in AutoRegisteringInputObjectGraphType
                wrappedGraphType = AddInputClrType(T)
            
            CASE AotGraphType<T>:
                // T is a GraphType
                ignoreClrMapping = if attribute's AutoRegisterClrMapping property is FALSE
                AddGraphType(T, ignoreClrMapping)
            
            CASE AotTypeMapping<TClr, TGraph>:
                // TClr is a CLR type, TGraph is a GraphType
                IF TGraph is ScalarGraphType OR TGraph is IInputObjectGraphType:
                    SetInputTypeMapping(TClr, TGraph)
                IF TGraph is ScalarGraphType OR TGraph is not IInputObjectGraphType:
                    SetOutputTypeMapping(TClr, TGraph)
```

### Step 3: Scan and Discover Related CLR Types
```
// Scan CLR types to discover nested types
WHILE clrTypesToProcess is not empty:
    (currentClrType, isInputType) = dequeue from clrTypesToProcess
    
    // Get members to scan for this type (properties, methods, fields based on MemberScan attribute)
    membersToScan = GetMembersToScan(currentClrType, isInputType)
    
    // Inspect members to discover return types
    FOR EACH member in membersToScan:
        // Check if member has explicit GraphType override
        memberGraphType = GetMemberGraphType(member)
        IF memberGraphType is not null:
            // Member has explicit GraphType - add it and skip CLR type processing
            AddGraphType(memberGraphType, ignoreClrMapping: false)
            CONTINUE to next member
        
        memberClrType = member.Type  // memberClrType is a CLR type
        
        // Unwrap nested generic wrappers (recursively)
        memberClrType = UnwrapClrType(memberClrType)
        
        // Discover nested types
        IF isInputType:
            TryAddInputClrType(memberClrType)
        ELSE:
            TryAddOutputClrType(memberClrType)
    
    // Inspect methods to discover input types from parameters
    methodsToScan = Filter membersToScan to only methods
    FOR EACH method in methodsToScan:
        FOR EACH parameter in method:
            // Skip parameters that are injected from context (not GraphQL arguments)
            IF ShouldSkipParameterProcessing(parameter):
                CONTINUE to next parameter
            
            // Check if parameter has explicit GraphType override
            parameterGraphType = GetParameterGraphType(parameter)
            IF parameterGraphType is not null:
                // Parameter has explicit GraphType - add it and skip CLR type processing
                AddGraphType(parameterGraphType, ignoreClrMapping: false)
                CONTINUE to next parameter
            
            parameterClrType = parameter.Type  // parameterClrType is a CLR type
            
            // Unwrap nested generic wrappers
            parameterClrType = UnwrapClrType(parameterClrType)
            
            // Method parameters are input types
            TryAddInputClrType(parameterClrType)

```

## Type Walker Helper Functions

The following helper functions are specific to Provider E (Type Walker) and are used during type discovery. See [`provider-e-type-walker.md`](provider-e-type-walker.md) for detailed definitions:

- **Type Management**
  - [`AddOutputClrType()`](provider-e-type-walker.md:157) - Wraps CLR type in auto-registering output GraphType
  - [`AddInputClrType()`](provider-e-type-walker.md:190) - Wraps CLR type in auto-registering input GraphType
  - [`TryAddOutputClrType()`](provider-e-type-walker.md:216) - Conditionally adds output type if not present
  - [`TryAddInputClrType()`](provider-e-type-walker.md:233) - Conditionally adds input type if not present
  - [`AddGraphType()`](provider-e-type-walker.md:250) - Adds explicit GraphType with optional CLR mapping
  - [`SetInputTypeMapping()`](provider-e-type-walker.md:326) - Sets input type mapping with conflict detection
  - [`SetOutputTypeMapping()`](provider-e-type-walker.md:356) - Sets output type mapping with conflict detection
  - [`AddScalar()`](provider-e-type-walker.md:387) - Adds scalar type for both input and output

## Global Helper Functions

The following global/static helper functions are used throughout the generator. See [`helper-functions.md`](helper-functions.md) for detailed definitions:

- **Type Unwrapping**
  - [`UnwrapGraphType()`](helper-functions.md) - Unwraps ListGraphType and NonNullGraphType wrappers
  - [`UnwrapClrType()`](helper-functions.md) - Unwraps CLR type wrappers (Nullable, Task, collections, etc.)

- **Member Scanning**
  - [`ShouldSkipMember()`](helper-functions.md) - Checks if a member should be skipped
  - [`GetMembersToScan()`](helper-functions.md) - Collects members to scan based on MemberScan attribute

- **Attribute Extraction**
  - [`GetMemberGraphType()`](helper-functions.md) - Extracts and unwraps GraphType from member attributes
  - [`GetParameterGraphType()`](helper-functions.md) - Extracts and unwraps GraphType from parameter attributes
  - [`ShouldSkipParameterProcessing()`](helper-functions.md) - Checks if parameter should be skipped
