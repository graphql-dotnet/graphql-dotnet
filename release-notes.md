* A few minor updates to introspection
* The language AST types have been moved from GraphQL.Language to GraphQL.Language.AST.
* This release adds a new lexer and parser for the GraphQL language.  The AntlrDocumentBuilder is now deprecated in favor of the GraphQLDocumentBuilder.  This new lexer and parser are written in C# and have no other dependencies.  It is also slightly faster than the AntlrDocumentBuilder.  The AntlrDocumentBuilder will be removed in the next major release.
* The new parser can be found at [graphql-dotnet/parser](http://github.com/graphql-dotnet/parser).
