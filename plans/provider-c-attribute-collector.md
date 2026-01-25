# Provider C: Attribute Data Collector

## Purpose
Extracts raw attribute data from candidate classes. This provider only collects and structures the attribute information; all processing logic is deferred to Provider E for better incremental compilation caching.

## Inputs

### Primary Input
- **Candidate Classes**: List of class declarations from Provider D

## Outputs

### Class Metadata
- Class name
- Namespace
- Base class name
- Whether class has existing constructor

### Raw Attribute Data
A list of attribute declarations with their types and parameters:
- Attribute type (AotQueryType, AotMutationType, etc.)
- Generic type argument(s)
- Named properties (e.g., IsInterface, AutoRegisterClrMapping)

## Logic Flow

```
FOR EACH candidate class from Provider D:
    // Capture class metadata
    className = class name
    namespace = class namespace
    baseClass = class base class
    hasConstructor = whether class has existing constructor
    
    // Extract raw attribute data
    attributeList = empty list
    
    FOR EACH attribute on the class:
        attributeData = {
            attributeType: attribute's type name,
            genericArguments: list of type arguments,
            namedProperties: dictionary of property names and values
        }
        
        ADD attributeData to attributeList
    
    // Return structured data
    RETURN {
        classMetadata: { className, namespace, baseClass, hasConstructor },
        attributes: attributeList
    }
```

## Output Structure

The output contains only raw, unprocessed data:
- **Class metadata**: Simple strings and flags
- **Attributes**: List of attribute type information with parameters

No collections are initialized, no types are wrapped, no mappings are created. This is purely data extraction.

## Incremental Compilation Benefits

By keeping Provider C simple and data-focused:
- If attribute data hasn't changed, Provider C output is cached
- Provider E won't re-run unless Provider C produces new output
- Better compilation performance for unchanged classes

## Next Stage
Passes raw attribute data to **Provider E: Type Walker & Collector** which will process attributes, initialize collections, and perform type discovery.
