* Add known argument names validation rule
* Add known directives validation rule
* Add known fragment names validation rule
* Add unique variable names validation rule
* Add unique fragment names validation rule
* Add unique argument names validation rule
* Add no unused variables validation rule
* Add no unused fragments validation rule
* Add support for IEnumerable<T> in retrieving query arguments
* Add support for inline fragments without a type definition
* Update internal async code
* Fix bug where variable validation would fail for non-null and list variables
* Fix bug in DateGraphType where culture-specific dates would fail to parse
* Fix bug in DateGraphType where UTC information would not be retained when used as a query argument
* Fix bug in Antlr parser where double numbers could lose precision
