# Provider F: Data Organizer

## Purpose
Splits and categorizes the collected type discovery data into organized structures for each code generator.

## Inputs

### Primary Input
From Provider E (Type Walker & Collector):
- **Class metadata**: Name, namespace, base class, has constructor
- **Root types**: queryRootGraphType, mutationRootGraphType, subscriptionRootGraphType
- **Complete collections**:
  - `discoveredGraphTypes`: Set of all GraphTypes
  - `outputCLRTypeMappings`: Dictionary (CLR type → GraphType)
  - `inputCLRTypeMappings`: Dictionary (CLR type → GraphType)

## Outputs

Organized data structures for code generation:

### 1. Candidate Class Data (for Provider G)
- Class name
- Namespace
- Base class
- Has existing constructor
- Root types (Query, Mutation, Subscription GraphTypes)
- All GraphTypes to register
- All type mappings (input and output)

### 2. Input Class Data (for Provider H)
- List of AutoRegisteringInputObjectGraphType<T> instances
- Corresponding CLR types
- Registration metadata

### 3. Object Class Data (for Provider I)
- List of AutoRegisteringObjectGraphType<T> instances
- Corresponding CLR types
- Registration metadata

### 4. Interface Class Data (for Provider J)
- List of AutoRegisteringInterfaceGraphType<T> instances
- Corresponding CLR types
- Registration metadata

## Logic Flow

```
FUNCTION OrganizeData(discoveryData):
    // Extract complete collections
    allGraphTypes = discoveryData.discoveredGraphTypes
    inputMappings = discoveryData.inputCLRTypeMappings
    outputMappings = discoveryData.outputCLRTypeMappings
    
    // Initialize categorized lists
    inputGraphTypes = empty list
    objectGraphTypes = empty list
    interfaceGraphTypes = empty list
    explicitGraphTypes = empty list
    scalarGraphTypes = empty list
    
    // Categorize all discovered GraphTypes
    FOR EACH graphType in allGraphTypes:
        IF graphType is AutoRegisteringInputObjectGraphType<T>:
            Add graphType to inputGraphTypes
        
        ELSE IF graphType is AutoRegisteringInterfaceGraphType<T>:
            Add graphType to interfaceGraphTypes
        
        ELSE IF graphType is AutoRegisteringObjectGraphType<T>:
            Add graphType to objectGraphTypes
        
        ELSE IF graphType is ScalarGraphType OR EnumerationGraphType<T>:
            Add graphType to scalarGraphTypes
        
        ELSE:
            // Custom/explicit GraphType (e.g., DroidType, QueryType)
            Add graphType to explicitGraphTypes
    
    // Build candidate class data
    candidateClassData = {
        ClassName: discoveryData.className,
        Namespace: discoveryData.namespace,
        BaseClass: discoveryData.baseClass,
        HasExistingConstructor: discoveryData.hasExistingConstructor,
        QueryRootType: discoveryData.queryRootGraphType,
        MutationRootType: discoveryData.mutationRootGraphType,
        SubscriptionRootType: discoveryData.subscriptionRootGraphType,
        AllGraphTypes: allGraphTypes,
        InputTypeMappings: inputMappings,
        OutputTypeMappings: outputMappings,
        ScalarGraphTypes: scalarGraphTypes,
        ExplicitGraphTypes: explicitGraphTypes
    }
    
    // Build input class data
    inputClassData = {
        InputGraphTypes: inputGraphTypes,
        // Extract CLR types from AutoRegisteringInputObjectGraphType<T>
        InputClrTypes: [T for each AutoRegisteringInputObjectGraphType<T>]
    }
    
    // Build object class data
    objectClassData = {
        ObjectGraphTypes: objectGraphTypes,
        // Extract CLR types from AutoRegisteringObjectGraphType<T>
        ObjectClrTypes: [T for each AutoRegisteringObjectGraphType<T>]
    }
    
    // Build interface class data
    interfaceClassData = {
        InterfaceGraphTypes: interfaceGraphTypes,
        // Extract CLR types from AutoRegisteringInterfaceGraphType<T>
        InterfaceClrTypes: [T for each AutoRegisteringInterfaceGraphType<T>]
    }
    
    RETURN candidateClassData, inputClassData, objectClassData, interfaceClassData
```

## Categorization Rules

### AutoRegisteringInputObjectGraphType<T>
- CLR input types that were auto-wrapped
- Extracted to inputClassData
- Used by Provider H (if separate generation needed)

### AutoRegisteringObjectGraphType<T>
- CLR output object types that were auto-wrapped
- Extracted to objectClassData
- Used by Provider I (if separate generation needed)

### AutoRegisteringInterfaceGraphType<T>
- CLR interface types that were auto-wrapped
- Extracted to interfaceClassData
- Used by Provider J (if separate generation needed)

### ScalarGraphType / EnumerationGraphType<T>
- Primitive scalars, enums
- Often built-in or simple types
- Included in candidateClassData for registration

### Explicit GraphTypes
- Custom GraphType classes (e.g., DroidType, HumanType, QueryType)
- Not auto-registering wrappers
- Included in candidateClassData for registration
