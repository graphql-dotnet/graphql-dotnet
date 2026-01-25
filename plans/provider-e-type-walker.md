# Provider E: Type Walker & Collector

## Purpose
Processes raw attribute data from Provider C to initialize collections, then orchestrates type discovery by walking the type graph using Providers A and B until all types are discovered.

## Inputs

### Primary Input
From Provider C (Attribute Data Collector):
- **Class metadata**: Name, namespace, base class, has constructor
- **Raw attribute data**: List of attributes with their types and parameters

### Providers Used
- **Provider A**: CLR Input Type Scanner
- **Provider B**: CLR Output Type Scanner

## Outputs

### Class Metadata
- Class name
- Namespace
- Base class name
- Whether class has existing constructor

### Root Types
- `queryRootGraphType`: GraphType or null
- `mutationRootGraphType`: GraphType or null
- `subscriptionRootGraphType`: GraphType or null

### Complete Type Discovery Collections
- **discoveredGraphTypes**: Set of all GraphTypes that need registration (complete)
- **outputCLRTypeMappings**: Dictionary mapping CLR types → GraphTypes for output (complete)
- **inputCLRTypeMappings**: Dictionary mapping CLR types → GraphTypes for input (complete)
- **All type mappings**: CLR type → GraphType for both input and output contexts

## Logic Flow

```
FUNCTION ProcessAttributesAndWalkTypes(rawAttributeData):
    // Step 1: Initialize all collection variables
    queryRootGraphType = null
    mutationRootGraphType = null
    subscriptionRootGraphType = null
    discoveredGraphTypes = empty set
    outputCLRTypeMappings = empty dictionary
    inputCLRTypeMappings = empty dictionary
    clrTypesToProcess = empty queue
    
    // Step 2: Process raw attribute data to populate initial data
    FOR EACH attribute in rawAttributeData.attributes:
        SWITCH attribute.attributeType:
            CASE AotQueryType<T>:
                IF T is a GraphType:
                    queryRootGraphType = T
                    AddGraphType(T, ignoreClrMapping: false)
                ELSE: // T is CLR type
                    queryRootGraphType = AddOutputClrType(T, isInterface: false)
            
            CASE AotMutationType<T>:
                IF T is a GraphType:
                    mutationRootGraphType = T
                    AddGraphType(T, ignoreClrMapping: false)
                ELSE: // T is CLR type
                    mutationRootGraphType = AddOutputClrType(T, isInterface: false)
            
            CASE AotSubscriptionType<T>:
                IF T is a GraphType:
                    subscriptionRootGraphType = T
                    AddGraphType(T, ignoreClrMapping: false)
                ELSE: // T is CLR type
                    subscriptionRootGraphType = AddOutputClrType(T, isInterface: false)
            
            CASE AotOutputType<T>:
                IF attribute.namedProperties contains "IsInterface":
                    isInterfaceFlag = attribute.namedProperties["IsInterface"]
                ELSE:
                    isInterfaceFlag = null  // Auto-detect
                
                wrappedGraphType = AddOutputClrType(T, isInterface: isInterfaceFlag)
            
            CASE AotInputType<T>:
                wrappedGraphType = AddInputClrType(T)
            
            CASE AotGraphType<T>:
                IF attribute.namedProperties contains "AutoRegisterClrMapping":
                    ignoreClrMapping = NOT attribute.namedProperties["AutoRegisterClrMapping"]
                ELSE:
                    ignoreClrMapping = false
                AddGraphType(T, ignoreClrMapping)
            
            CASE AotTypeMapping<TClr, TGraph>:
                IF TGraph is ScalarGraphType OR TGraph is IInputObjectGraphType:
                    SetInputTypeMapping(TClr, TGraph)
                IF TGraph is ScalarGraphType OR TGraph is not IInputObjectGraphType:
                    SetOutputTypeMapping(TClr, TGraph)
    
    // Step 3: Walk the type graph - process the queue until empty (breadth-first traversal)
    WHILE clrTypesToProcess is not empty:
        (currentClrType, isInputType) = dequeue from clrTypesToProcess
        
        // Skip if already processed (shouldn't happen but safety check)
        IF isInputType AND currentClrType in inputCLRTypeMappings:
            CONTINUE
        IF NOT isInputType AND currentClrType in outputCLRTypeMappings:
            CONTINUE
        
        IF isInputType:
            // Use Provider A to scan input type
            discoveredTypes, inputListTypes = ScanInputType(currentClrType)
            
            // Process discovered types
            FOR EACH discoveredType in discoveredTypes:
                IF discoveredType is GraphType:
                    // Already added by Provider A
                    CONTINUE
                ELSE: // discoveredType is CLR type
                    // Already added by Provider A via TryAddInputClrType
                    // Type is now in inputCLRTypeMappings and queue
                    CONTINUE
        
        ELSE: // Output type
            // Use Provider B to scan output type
            outputTypes, outputGraphTypes, 
            inputTypes, inputGraphTypes, inputListTypes = ScanOutputType(currentClrType)
            
            // Process discovered output types
            FOR EACH outputType in outputTypes:
                // Already added by Provider B via TryAddOutputClrType
                // Type is now in outputCLRTypeMappings and queue
                CONTINUE
            
            // Process discovered input types
            FOR EACH inputType in inputTypes:
                // Already added by Provider B via TryAddInputClrType
                // Type is now in inputCLRTypeMappings and queue
                CONTINUE
        
        // Note: inputListTypes collected but not processed further
        // (They're already unwrapped and processed as base types)
    
    // All types discovered and processed
    RETURN complete attributeData with filled collections
```

## Helper Functions Used

### From Global Helper Functions
Provider E uses the following stateless helper functions defined in [`helper-functions.md`](helper-functions.md):
- [`UnwrapGraphType()`](helper-functions.md:9) - Unwraps ListGraphType and NonNullGraphType wrappers
- [`UnwrapClrType()`](helper-functions.md:28) - Unwraps CLR type wrappers (Nullable, Task, collections, etc.)

### Provider E-Specific Functions
The following functions are specific to Provider E's type walking logic and maintain state during type discovery:

## Type Walker Processing Functions

### AddOutputClrType(clrType, isInterfaceFlag)
```
FUNCTION AddOutputClrType(clrType, isInterfaceFlag):
    // Adds an output type by wrapping it in the appropriate auto-registering graph type
    // Note: SetOutputTypeMapping will automatically add to discoveredGraphTypes
    // ASSUMES the type is not already in outputCLRTypeMappings
    // Parameters:
    //   - clrType: The CLR type to wrap
    //   - isInterfaceFlag: nullable boolean - true if should be treated as interface, false if not, null if auto-detect
    // Returns: The wrapped GraphType, or null if type doesn't need wrapping (e.g., primitives)
    
    // Check if type is in known scalars list first
    IF clrType in knownPrimitives:
        scalarGraphType = knownPrimitives[clrType]
        RETURN AddScalar(clrType, scalarGraphType)
    
    // Check for enum
    IF clrType is enum:
        wrappedGraphType = EnumerationGraphType<clrType>
        RETURN AddScalar(clrType, wrappedGraphType)
    
    // Check for interface
    IF (isInterfaceFlag is true) OR (isInterfaceFlag is null AND clrType is interface):
        wrappedGraphType = AutoRegisteringInterfaceGraphType<clrType>
        SetOutputTypeMapping(clrType, wrappedGraphType)
        RETURN wrappedGraphType
    
    // Create AutoRegisteringObjectGraphType
    wrappedGraphType = AutoRegisteringObjectGraphType<clrType>
    SetOutputTypeMapping(clrType, wrappedGraphType)
    RETURN wrappedGraphType
```

### AddInputClrType(clrType)
```
FUNCTION AddInputClrType(clrType):
    // Adds an input type by wrapping it in the appropriate auto-registering graph type
    // Note: SetInputTypeMapping will automatically add to discoveredGraphTypes
    // ASSUMES the type is not already in inputCLRTypeMappings
    // Parameters:
    //   - clrType: The CLR type to wrap
    // Returns: The wrapped GraphType, or null if type doesn't need wrapping (e.g., primitives)
    
    // Check if type is in known scalars list first
    IF clrType in knownPrimitives:
        scalarGraphType = knownPrimitives[clrType]
        RETURN AddScalar(clrType, scalarGraphType)
    
    // Check for enum
    IF clrType is enum:
        wrappedGraphType = EnumerationGraphType<clrType>
        RETURN AddScalar(clrType, wrappedGraphType)
    
    // Create AutoRegisteringInputObjectGraphType
    wrappedGraphType = AutoRegisteringInputObjectGraphType<clrType>
    SetInputTypeMapping(clrType, wrappedGraphType)
    RETURN wrappedGraphType
```

### TryAddOutputClrType(clrType)
```
FUNCTION TryAddOutputClrType(clrType):
    // Tries to add an output type if not already in mappings
    // Parameters:
    //   - clrType: The CLR type to wrap
    // Returns: true if type was added, false if already exists
    
    // Check if already in mappings
    IF clrType in outputCLRTypeMappings:
        RETURN false
    
    // Add the type
    AddOutputClrType(clrType, isInterfaceFlag: null)
    RETURN true
```

### TryAddInputClrType(clrType)
```
FUNCTION TryAddInputClrType(clrType):
    // Tries to add an input type if not already in mappings
    // Parameters:
    //   - clrType: The CLR type to wrap
    // Returns: true if type was added, false if already exists
    
    // Check if already in mappings
    IF clrType in inputCLRTypeMappings:
        RETURN false
    
    // Add the type
    AddInputClrType(clrType)
    RETURN true
```

### AddGraphType(graphType, ignoreClrMapping)
```
FUNCTION AddGraphType(graphType, ignoreClrMapping):
    // Adds an explicit GraphType to the discovered types and optionally creates a CLR type mapping
    // Also populates the CLR type processing queues when adding auto-registering types
    // Parameters:
    //   - graphType: The GraphType to add (e.g., DroidType, EnumerationGraphType<Episode>)
    //   - ignoreClrMapping: boolean - if true, skip automatic CLR type mapping
    
    // Check if this is a new addition to discovered types
    isNewAddition = graphType NOT in discoveredGraphTypes
    
    // Fast-quit if already added
    IF NOT isNewAddition, RETURN
    
    // Add to discovered types
    Add graphType to discoveredGraphTypes
    
    // If this is a new auto-registering type, add its CLR type to the processing queue
    IF graphType is AutoRegisteringObjectGraphType<TClr> OR
       graphType is AutoRegisteringInterfaceGraphType<TClr>:
        // This is an auto-registering output type
        Add (TClr, false) to clrTypesToProcess
    ELSE IF graphType is AutoRegisteringInputObjectGraphType<TClr>:
        // This is an auto-registering input type
        Add (TClr, true) to clrTypesToProcess
    
    // Check if we should create a CLR type mapping
    IF NOT ignoreClrMapping:
        // Determine if this GraphType should be automatically mapped to a CLR type
        clrTypeToMap = null
        isInputType = false
        
        // Check if graphType inherits from ComplexGraphType<TSourceType>
        IF graphType inherits from ComplexGraphType<TSourceType>:
            clrTypeToMap = TSourceType
            // Determine if it's an input or output type
            IF graphType is IInputObjectGraphType:
                isInputType = true
            ELSE:
                isInputType = false
        
        // Check if graphType inherits from EnumerationGraphType<TEnum>
        ELSE IF graphType inherits from EnumerationGraphType<TEnum>:
            clrTypeToMap = TEnum
            isInputType = true  // Enums are used for both input and output
        
        // No mapping required
        ELSE RETURN

        // Apply skip conditions if we found a CLR type to map
        
        // Skip if the generic type is object
        IF clrTypeToMap is System.Object:
            RETURN

        // Skip if the CLR type is marked with DoNotMapClrTypeAttribute
        IF clrTypeToMap has DoNotMapClrTypeAttribute:
            RETURN
        
        // Skip if the CLR type is marked with InstanceSourceAttribute with value other than ContextSource
        IF clrTypeToMap has InstanceSourceAttribute:
            instanceSourceValue = attribute.Source value
            IF instanceSourceValue is not InstanceSource.ContextSource:
                RETURN
        
        // Create the mapping if appropriate
        IF graphType is EnumerationGraphType<TEnum>:
            // Enums are scalars - set both input and output mappings
            AddScalar(clrTypeToMap, graphType)
        ELSE IF isInputType:
            SetInputTypeMapping(clrTypeToMap, graphType)
        ELSE:
            SetOutputTypeMapping(clrTypeToMap, graphType)
```

### SetInputTypeMapping(clrType, graphType)
```
FUNCTION SetInputTypeMapping(clrType, graphType):
    // Sets an input type mapping with conflict detection
    // Automatically adds the graphType to discoveredGraphTypes
    // Parameters:
    //   - clrType: The CLR type to map
    //   - graphType: The GraphType to map to (must not be null)
    // Returns: true if mapping was set successfully, false if conflict detected
    
    IF clrType in inputCLRTypeMappings:
        existingMapping = inputCLRTypeMappings[clrType]
        IF existingMapping is not null AND existingMapping != graphType:
            // CONFLICT: Same CLR type mapped to different GraphTypes
            Report diagnostic error:
                "Conflicting input type mapping for '{clrType}': already mapped to '{existingMapping}', cannot map to '{graphType}'"
            RETURN false
        ELSE IF existingMapping == graphType:
            // Same mapping - redundant but harmless
            RETURN true
        // ELSE existingMapping is null and graphType is not null - update the mapping
    
    // Add the graphType to discoveredGraphTypes (with ignoreClrMapping=true to avoid recursion)
    AddGraphType(graphType, ignoreClrMapping: true)
    
    // Set the mapping
    inputCLRTypeMappings[clrType] = graphType
    RETURN true
```

### SetOutputTypeMapping(clrType, graphType)
```
FUNCTION SetOutputTypeMapping(clrType, graphType):
    // Sets an output type mapping with conflict detection
    // Automatically adds the graphType to discoveredGraphTypes
    // Parameters:
    //   - clrType: The CLR type to map
    //   - graphType: The GraphType to map to (must not be null)
    // Returns: true if mapping was set successfully, false if conflict detected
    
    IF clrType in outputCLRTypeMappings:
        existingMapping = outputCLRTypeMappings[clrType]
        
        IF existingMapping is not null AND existingMapping != graphType:
            // CONFLICT: Same CLR type mapped to different GraphTypes
            Report diagnostic error:
                "Conflicting output type mapping for '{clrType}': already mapped to '{existingMapping}', cannot map to '{graphType}'"
            RETURN false
        ELSE IF existingMapping == graphType:
            // Same mapping - redundant but harmless
            RETURN true
        // ELSE existingMapping is null and graphType is not null - update the mapping
    
    // Add the graphType to discoveredGraphTypes (with ignoreClrMapping=true to avoid recursion)
    AddGraphType(graphType, ignoreClrMapping: true)
    
    // Set the mapping
    outputCLRTypeMappings[clrType] = graphType
    RETURN true
```

### AddScalar(clrType, scalarGraphType)
```
FUNCTION AddScalar(clrType, scalarGraphType):
    // Adds a scalar type (like enum) that is used for both input and output
    // Note: SetInputTypeMapping and SetOutputTypeMapping will automatically add to discoveredGraphTypes
    // Parameters:
    //   - clrType: The CLR type (e.g., an enum)
    //   - scalarGraphType: The GraphType to use (e.g., EnumerationGraphType<T>)
    // Returns: The scalarGraphType
    
    // Set both input and output mappings to the same scalar type
    // These calls will automatically add scalarGraphType to discoveredGraphTypes
    SetInputTypeMapping(clrType, scalarGraphType)
    SetOutputTypeMapping(clrType, scalarGraphType)
    
    RETURN scalarGraphType
```
