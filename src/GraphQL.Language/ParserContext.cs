using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Language.AST;

namespace GraphQL.Language
{
    public class ParserContext : IDisposable
    {
        private Token currentToken;
        private ILexer lexer;
        private ISource source;

        public ParserContext(ISource source, ILexer lexer)
        {
            this.source = source;
            this.lexer = lexer;

            this.currentToken = this.lexer.Lex(source);
        }

        public void Dispose()
        {
        }

        public GraphQLDocument Parse()
        {
            return this.ParseDocument();
        }

        private void Advance()
        {
            this.currentToken = this.lexer.Lex(this.source, this.currentToken.End);
        }

        private GraphQLType AdvanceThroughColonAndParseType()
        {
            this.Expect(TokenKind.COLON);
            return this.ParseType();
        }

        private IEnumerable<T> Any<T>(TokenKind open, Func<T> next, TokenKind close)
            where T : ASTNode
        {
            this.Expect(open);

            List<T> nodes = new List<T>();
            while (!this.Skip(close))
                nodes.Add(next());

            return nodes;
        }

        private GraphQLDocument CreateDocument(int start, List<ASTNode> definitions)
        {
            return new GraphQLDocument()
            {
                Location = new GraphQLLocation()
                {
                    Start = start,
                    End = this.currentToken.End
                },
                Definitions = definitions
            };
        }

        private GraphQLFieldSelection CreateFieldSelection(int start, GraphQLName name, GraphQLName alias)
        {
            return new GraphQLFieldSelection()
            {
                Alias = alias,
                Name = name,
                Arguments = this.ParseArguments(),
                Directives = this.ParseDirectives(),
                SelectionSet = this.Peek(TokenKind.BRACE_L) ? this.ParseSelectionSet() : null,
                Location = this.GetLocation(start)
            };
        }

        private ASTNode CreateGraphQLFragmentSpread(int start)
        {
            return new GraphQLFragmentSpread()
            {
                Name = this.ParseFragmentName(),
                Directives = this.ParseDirectives(),
                Location = this.GetLocation(start)
            };
        }

        private ASTNode CreateInlineFragment(int start)
        {
            return new GraphQLInlineFragment()
            {
                TypeCondition = this.GetTypeCondition(),
                Directives = this.ParseDirectives(),
                SelectionSet = this.ParseSelectionSet(),
                Location = this.GetLocation(start)
            };
        }

        private ASTNode CreateOperationDefinition(int start, OperationType operation, GraphQLName name)
        {
            return new GraphQLOperationDefinition()
            {
                Operation = operation,
                Name = name,
                VariableDefinitions = this.ParseVariableDefinitions(),
                Directives = this.ParseDirectives(),
                SelectionSet = this.ParseSelectionSet(),
                Location = this.GetLocation(start)
            };
        }

        private ASTNode CreateOperationDefinition(int start)
        {
            return new GraphQLOperationDefinition()
            {
                Operation = OperationType.Query,
                Directives = new GraphQLDirective[] { },
                SelectionSet = this.ParseSelectionSet(),
                Location = this.GetLocation(start)
            };
        }

        private void Expect(TokenKind kind)
        {
            if (this.currentToken.Kind == kind)
            {
                this.Advance();
            }
            else
            {
                throw new GraphQLSyntaxErrorException(
                    $"Expected {Token.GetTokenKindDescription(kind)}, found {this.currentToken}",
                    this.source,
                    this.currentToken.Start);
            }
        }

        private GraphQLValue ExpectColonAndParseValueLiteral(bool isConstant)
        {
            this.Expect(TokenKind.COLON);
            return this.ParseValueLiteral(isConstant);
        }

        private void ExpectKeyword(string keyword)
        {
            var token = this.currentToken;
            if (token.Kind == TokenKind.NAME && token.Value.Equals(keyword))
            {
                this.Advance();
                return;
            }

            throw new GraphQLSyntaxErrorException(
                    $"Expected \"{keyword}\", found Name \"{token.Value}\"", this.source, this.currentToken.Start);
        }

        private GraphQLNamedType ExpectOnKeywordAndParseNamedType()
        {
            this.ExpectKeyword("on");
            return this.ParseNamedType();
        }

        private GraphQLValue GetDefaultConstantValue()
        {
            GraphQLValue defaultValue = null;
            if (this.Skip(TokenKind.EQUALS))
            {
                defaultValue = this.ParseConstantValue();
            }

            return defaultValue;
        }

        private GraphQLLocation GetLocation(int start)
        {
            return new GraphQLLocation()
            {
                Start = start,
                End = this.currentToken.End
            };
        }

        private GraphQLName GetName()
        {
            return this.Peek(TokenKind.NAME) ? this.ParseName() : null;
        }

        private GraphQLNamedType GetTypeCondition()
        {
            GraphQLNamedType typeCondition = null;
            if (this.currentToken.Value != null && this.currentToken.Value.Equals("on"))
            {
                this.Advance();
                typeCondition = this.ParseNamedType();
            }

            return typeCondition;
        }

        private IEnumerable<T> Many<T>(TokenKind open, Func<T> next, TokenKind close)
        {
            this.Expect(open);

            List<T> nodes = new List<T>() { next() };
            while (!this.Skip(close))
                nodes.Add(next());

            return nodes;
        }

        private GraphQLArgument ParseArgument()
        {
            var start = this.currentToken.Start;

            return new GraphQLArgument()
            {
                Name = this.ParseName(),
                Value = this.ExpectColonAndParseValueLiteral(false),
                Location = this.GetLocation(start)
            };
        }

        private IEnumerable<GraphQLInputValueDefinition> ParseArgumentDefs()
        {
            if (!this.Peek(TokenKind.PAREN_L))
            {
                return new GraphQLInputValueDefinition[] { };
            }

            return this.Many(TokenKind.PAREN_L, () => this.ParseInputValueDef(), TokenKind.PAREN_R);
        }

        private IEnumerable<GraphQLArgument> ParseArguments()
        {
            return this.Peek(TokenKind.PAREN_L) ?
                this.Many(TokenKind.PAREN_L, () => this.ParseArgument(), TokenKind.PAREN_R) :
                new GraphQLArgument[] { };
        }

        private GraphQLValue ParseBooleanValue(Token token)
        {
            this.Advance();
            return new GraphQLScalarValue(ASTNodeKind.BooleanValue)
            {
                Value = token.Value,
                Location = this.GetLocation(token.Start)
            };
        }

        private GraphQLValue ParseConstantValue()
        {
            return this.ParseValueLiteral(true);
        }

        private ASTNode ParseDefinition()
        {
            if (this.Peek(TokenKind.BRACE_L))
            {
                return this.ParseOperationDefinition();
            }

            if (this.Peek(TokenKind.NAME))
            {
                ASTNode definition = null;
                if ((definition = this.ParseNamedDefinition()) != null)
                    return definition;
            }

            throw new GraphQLSyntaxErrorException(
                    $"Unexpected {this.currentToken}", this.source, this.currentToken.Start);
        }

        private IEnumerable<ASTNode> ParseDefinitionsIfNotEOF()
        {
            if (this.currentToken.Kind != TokenKind.EOF)
            {
                do
                {
                    yield return this.ParseDefinition();
                }
                while (!this.Skip(TokenKind.EOF));
            }
        }

        private GraphQLDirective ParseDirective()
        {
            var start = this.currentToken.Start;
            this.Expect(TokenKind.AT);
            return new GraphQLDirective()
            {
                Name = this.ParseName(),
                Arguments = this.ParseArguments(),
                Location = this.GetLocation(start)
            };
        }

        private GraphQLDirectiveDefinition ParseDirectiveDefinition()
        {
            var start = this.currentToken.Start;
            this.ExpectKeyword("directive");
            this.Expect(TokenKind.AT);

            var name = this.ParseName();
            var args = this.ParseArgumentDefs();

            this.ExpectKeyword("on");
            var locations = this.ParseDirectiveLocations();

            return new GraphQLDirectiveDefinition()
            {
                Name = name,
                Arguments = args,
                Locations = locations,
                Location = this.GetLocation(start)
            };
        }

        private IEnumerable<GraphQLName> ParseDirectiveLocations()
        {
            var locations = new List<GraphQLName>();

            do
            {
                locations.Add(this.ParseName());
            }
            while (this.Skip(TokenKind.PIPE));

            return locations;
        }

        private IEnumerable<GraphQLDirective> ParseDirectives()
        {
            var directives = new List<GraphQLDirective>();
            while (this.Peek(TokenKind.AT))
                directives.Add(this.ParseDirective());

            return directives;
        }

        private GraphQLDocument ParseDocument()
        {
            int start = this.currentToken.Start;
            var definitions = this.ParseDefinitionsIfNotEOF().ToList();

            return this.CreateDocument(start, definitions);
        }

        private GraphQLEnumTypeDefinition ParseEnumTypeDefinition()
        {
            var start = this.currentToken.Start;
            this.ExpectKeyword("enum");

            return new GraphQLEnumTypeDefinition()
            {
                Name = this.ParseName(),
                Directives = this.ParseDirectives(),
                Values = this.Many(TokenKind.BRACE_L, () => this.ParseEnumValueDefinition(), TokenKind.BRACE_R),
                Location = this.GetLocation(start)
            };
        }

        private GraphQLValue ParseEnumValue(Token token)
        {
            this.Advance();
            return new GraphQLScalarValue(ASTNodeKind.EnumValue)
            {
                Value = token.Value.ToString(),
                Location = this.GetLocation(token.Start)
            };
        }

        private GraphQLEnumValueDefinition ParseEnumValueDefinition()
        {
            var start = this.currentToken.Start;
            return new GraphQLEnumValueDefinition()
            {
                Name = this.ParseName(),
                Directives = this.ParseDirectives(),
                Location = this.GetLocation(start)
            };
        }

        private GraphQLFieldDefinition ParseFieldDefinition()
        {
            var start = this.currentToken.Start;
            var name = this.ParseName();
            var args = this.ParseArgumentDefs();
            this.Expect(TokenKind.COLON);

            return new GraphQLFieldDefinition()
            {
                Name = name,
                Arguments = args,
                Type = this.ParseType(),
                Directives = this.ParseDirectives(),
                Location = this.GetLocation(start)
            };
        }

        private GraphQLFieldSelection ParseFieldSelection()
        {
            var start = this.currentToken.Start;
            var nameOrAlias = this.ParseName();
            GraphQLName name = null;
            GraphQLName alias = null;

            if (this.Skip(TokenKind.COLON))
            {
                name = this.ParseName();
                alias = nameOrAlias;
            }
            else
            {
                alias = null;
                name = nameOrAlias;
            }

            return this.CreateFieldSelection(start, name, alias);
        }

        private GraphQLValue ParseFloat(bool isConstant)
        {
            var token = this.currentToken;
            this.Advance();
            return new GraphQLScalarValue(ASTNodeKind.FloatValue)
            {
                Value = token.Value,
                Location = this.GetLocation(token.Start)
            };
        }

        private ASTNode ParseFragment()
        {
            var start = this.currentToken.Start;
            this.Expect(TokenKind.SPREAD);

            if (this.Peek(TokenKind.NAME) && !this.currentToken.Value.Equals("on"))
            {
                return this.CreateGraphQLFragmentSpread(start);
            }

            return this.CreateInlineFragment(start);
        }

        private GraphQLFragmentDefinition ParseFragmentDefinition()
        {
            var start = this.currentToken.Start;
            this.ExpectKeyword("fragment");

            return new GraphQLFragmentDefinition()
            {
                Name = this.ParseFragmentName(),
                TypeCondition = this.ExpectOnKeywordAndParseNamedType(),
                Directives = this.ParseDirectives(),
                SelectionSet = this.ParseSelectionSet(),
                Location = this.GetLocation(start)
            };
        }

        private GraphQLName ParseFragmentName()
        {
            if (this.currentToken.Value.Equals("on"))
            {
                throw new GraphQLSyntaxErrorException(
                    $"Unexpected {this.currentToken}", this.source, this.currentToken.Start);
            }

            return this.ParseName();
        }

        private IEnumerable<GraphQLNamedType> ParseImplementsInterfaces()
        {
            var types = new List<GraphQLNamedType>();
            if (this.currentToken.Value?.Equals("implements") == true)
            {
                this.Advance();

                do
                {
                    types.Add(this.ParseNamedType());
                }
                while (this.Peek(TokenKind.NAME));
            }

            return types;
        }

        private GraphQLInputObjectTypeDefinition ParseInputObjectTypeDefinition()
        {
            var start = this.currentToken.Start;
            this.ExpectKeyword("input");

            return new GraphQLInputObjectTypeDefinition()
            {
                Name = this.ParseName(),
                Directives = this.ParseDirectives(),
                Fields = this.Any(TokenKind.BRACE_L, () => this.ParseInputValueDef(), TokenKind.BRACE_R),
                Location = this.GetLocation(start)
            };
        }

        private GraphQLInputValueDefinition ParseInputValueDef()
        {
            var start = this.currentToken.Start;
            var name = this.ParseName();
            this.Expect(TokenKind.COLON);

            return new GraphQLInputValueDefinition()
            {
                Name = name,
                Type = this.ParseType(),
                DefaultValue = this.GetDefaultConstantValue(),
                Directives = this.ParseDirectives(),
                Location = this.GetLocation(start)
            };
        }

        private GraphQLValue ParseInt(bool isConstant)
        {
            var token = this.currentToken;
            this.Advance();

            return new GraphQLScalarValue(ASTNodeKind.IntValue)
            {
                Value = token.Value,
                Location = this.GetLocation(token.Start)
            };
        }

        private GraphQLInterfaceTypeDefinition ParseInterfaceTypeDefinition()
        {
            var start = this.currentToken.Start;
            this.ExpectKeyword("interface");

            return new GraphQLInterfaceTypeDefinition()
            {
                Name = this.ParseName(),
                Directives = this.ParseDirectives(),
                Fields = this.Any(TokenKind.BRACE_L, () => this.ParseFieldDefinition(), TokenKind.BRACE_R),
                Location = this.GetLocation(start)
            };
        }

        private GraphQLValue ParseList(bool isConstant)
        {
            var start = this.currentToken.Start;
            Func<GraphQLValue> constant = () => this.ParseConstantValue();
            Func<GraphQLValue> value = () => this.ParseValueValue();

            return new GraphQLListValue(ASTNodeKind.ListValue)
            {
                Values = this.Any(TokenKind.BRACKET_L, isConstant ? constant : value, TokenKind.BRACKET_R),
                Location = this.GetLocation(start),
                AstValue = this.source.Body.Substring(start, this.currentToken.End - start - 1)
            };
        }

        private GraphQLName ParseName()
        {
            int start = this.currentToken.Start;
            var value = this.currentToken.Value;

            this.Expect(TokenKind.NAME);

            return new GraphQLName()
            {
                Location = this.GetLocation(start),
                Value = value as string
            };
        }

        private ASTNode ParseNamedDefinition()
        {
            switch (this.currentToken.Value as string)
            {
                // Note: subscription is an experimental non-spec addition.
                case "query":
                case "mutation":
                case "subscription":
                    return this.ParseOperationDefinition();

                case "fragment": return this.ParseFragmentDefinition();

                // Note: the Type System IDL is an experimental non-spec addition.
                case "schema": return this.ParseSchemaDefinition();
                case "scalar": return this.ParseScalarTypeDefinition();
                case "type": return this.ParseObjectTypeDefinition();
                case "interface": return this.ParseInterfaceTypeDefinition();
                case "union": return this.ParseUnionTypeDefinition();
                case "enum": return this.ParseEnumTypeDefinition();
                case "input": return this.ParseInputObjectTypeDefinition();
                case "extend": return this.ParseTypeExtensionDefinition();
                case "directive": return this.ParseDirectiveDefinition();
            }

            return null;
        }

        private GraphQLNamedType ParseNamedType()
        {
            var start = this.currentToken.Start;
            return new GraphQLNamedType()
            {
                Name = this.ParseName(),
                Location = this.GetLocation(start)
            };
        }

        private GraphQLValue ParseNameValue(bool isConstant)
        {
            var token = this.currentToken;

            if (token.Value.Equals("true") || token.Value.Equals("false"))
                return this.ParseBooleanValue(token);
            else if (token.Value != null && !token.Value.Equals("null"))
                return this.ParseEnumValue(token);

            throw new GraphQLSyntaxErrorException(
                    $"Unexpected {this.currentToken}", this.source, this.currentToken.Start);
        }

        private GraphQLValue ParseObject(bool isConstant)
        {
            var start = this.currentToken.Start;

            return new GraphQLObjectValue()
            {
                Fields = this.ParseObjectFields(isConstant),
                Location = this.GetLocation(start)
            };
        }

        private GraphQLObjectField ParseObjectField(bool isConstant)
        {
            var start = this.currentToken.Start;
            return new GraphQLObjectField()
            {
                Name = this.ParseName(),
                Value = this.ExpectColonAndParseValueLiteral(isConstant),
                Location = this.GetLocation(start)
            };
        }

        private List<GraphQLObjectField> ParseObjectFields(bool isConstant)
        {
            List<GraphQLObjectField> fields = new List<GraphQLObjectField>();

            this.Expect(TokenKind.BRACE_L);
            while (!this.Skip(TokenKind.BRACE_R))
                fields.Add(this.ParseObjectField(isConstant));

            return fields;
        }

        private GraphQLObjectTypeDefinition ParseObjectTypeDefinition()
        {
            var start = this.currentToken.Start;
            this.ExpectKeyword("type");

            return new GraphQLObjectTypeDefinition()
            {
                Name = this.ParseName(),
                Interfaces = this.ParseImplementsInterfaces(),
                Directives = this.ParseDirectives(),
                Fields = this.Any(TokenKind.BRACE_L, () => this.ParseFieldDefinition(), TokenKind.BRACE_R),
                Location = this.GetLocation(start)
            };
        }

        private ASTNode ParseOperationDefinition()
        {
            var start = this.currentToken.Start;

            if (this.Peek(TokenKind.BRACE_L))
            {
                return this.CreateOperationDefinition(start);
            }

            return this.CreateOperationDefinition(start, this.ParseOperationType(), this.GetName());
        }

        private OperationType ParseOperationType()
        {
            var token = this.currentToken;
            this.Expect(TokenKind.NAME);

            switch (token.Value as string)
            {
                case "query": return OperationType.Query;
                case "mutation": return OperationType.Mutation;
                case "subscription": return OperationType.Subscription;
            }

            return OperationType.Query;
        }

        private GraphQLOperationTypeDefinition ParseOperationTypeDefinition()
        {
            var start = this.currentToken.Start;
            var operation = this.ParseOperationType();
            this.Expect(TokenKind.COLON);
            var type = this.ParseNamedType();

            return new GraphQLOperationTypeDefinition()
            {
                Operation = operation,
                Type = type,
                Location = this.GetLocation(start)
            };
        }

        private GraphQLScalarTypeDefinition ParseScalarTypeDefinition()
        {
            var start = this.currentToken.Start;
            this.ExpectKeyword("scalar");
            var name = this.ParseName();
            var directives = this.ParseDirectives();

            return new GraphQLScalarTypeDefinition()
            {
                Name = name,
                Directives = directives,
                Location = this.GetLocation(start)
            };
        }

        private GraphQLSchemaDefinition ParseSchemaDefinition()
        {
            var start = this.currentToken.Start;
            this.ExpectKeyword("schema");
            var directives = this.ParseDirectives();
            var operationTypes = this.Many(TokenKind.BRACE_L, () => this.ParseOperationTypeDefinition(), TokenKind.BRACE_R);

            return new GraphQLSchemaDefinition()
            {
                Directives = directives,
                OperationTypes = operationTypes,
                Location = this.GetLocation(start)
            };
        }

        private ASTNode ParseSelection()
        {
            return this.Peek(TokenKind.SPREAD) ?
                this.ParseFragment() :
                this.ParseFieldSelection();
        }

        private GraphQLSelectionSet ParseSelectionSet()
        {
            var start = this.currentToken.Start;
            return new GraphQLSelectionSet()
            {
                Selections = this.Many(TokenKind.BRACE_L, () => this.ParseSelection(), TokenKind.BRACE_R),
                Location = this.GetLocation(start)
            };
        }

        private GraphQLValue ParseString(bool isConstant)
        {
            var token = this.currentToken;
            this.Advance();
            return new GraphQLScalarValue(ASTNodeKind.StringValue)
            {
                Value = token.Value as string,
                Location = this.GetLocation(token.Start)
            };
        }

        private GraphQLType ParseType()
        {
            GraphQLType type = null;
            var start = this.currentToken.Start;
            if (this.Skip(TokenKind.BRACKET_L))
            {
                type = this.ParseType();
                this.Expect(TokenKind.BRACKET_R);
                type = new GraphQLListType()
                {
                    Type = type,
                    Location = this.GetLocation(start)
                };
            }
            else
            {
                type = this.ParseNamedType();
            }

            if (this.Skip(TokenKind.BANG))
            {
                return new GraphQLNonNullType()
                {
                    Type = type,
                    Location = this.GetLocation(start)
                };
            }

            return type;
        }

        private GraphQLTypeExtensionDefinition ParseTypeExtensionDefinition()
        {
            var start = this.currentToken.Start;
            this.ExpectKeyword("extend");
            var definition = this.ParseObjectTypeDefinition();

            return new GraphQLTypeExtensionDefinition()
            {
                Definition = definition,
                Location = this.GetLocation(start)
            };
        }

        private IEnumerable<GraphQLNamedType> ParseUnionMembers()
        {
            var members = new List<GraphQLNamedType>();

            do
            {
                members.Add(this.ParseNamedType());
            }
            while (this.Skip(TokenKind.PIPE));

            return members;
        }

        private GraphQLUnionTypeDefinition ParseUnionTypeDefinition()
        {
            var start = this.currentToken.Start;
            this.ExpectKeyword("union");
            var name = this.ParseName();
            var directives = this.ParseDirectives();
            this.Expect(TokenKind.EQUALS);
            var types = this.ParseUnionMembers();

            return new GraphQLUnionTypeDefinition()
            {
                Name = name,
                Directives = directives,
                Types = types,
                Location = this.GetLocation(start)
            };
        }

        private GraphQLValue ParseValueLiteral(bool isConstant)
        {
            var token = this.currentToken;

            switch (token.Kind)
            {
                case TokenKind.BRACKET_L: return this.ParseList(isConstant);
                case TokenKind.BRACE_L: return this.ParseObject(isConstant);
                case TokenKind.INT: return this.ParseInt(isConstant);
                case TokenKind.FLOAT: return this.ParseFloat(isConstant);
                case TokenKind.STRING: return this.ParseString(isConstant);
                case TokenKind.NAME: return this.ParseNameValue(isConstant);
                case TokenKind.DOLLAR: if (!isConstant) return this.ParseVariable(); break;
            }

            throw new GraphQLSyntaxErrorException(
                    $"Unexpected {this.currentToken}", this.source, this.currentToken.Start);
        }

        private GraphQLValue ParseValueValue()
        {
            return this.ParseValueLiteral(false);
        }

        private GraphQLVariable ParseVariable()
        {
            var start = this.currentToken.Start;
            this.Expect(TokenKind.DOLLAR);

            return new GraphQLVariable()
            {
                Name = this.GetName(),
                Location = this.GetLocation(start)
            };
        }

        private GraphQLVariableDefinition ParseVariableDefinition()
        {
            int start = this.currentToken.Start;
            return new GraphQLVariableDefinition()
            {
                Variable = this.ParseVariable(),
                Type = this.AdvanceThroughColonAndParseType(),
                DefaultValue = this.SkipEqualsAndParseValueLiteral(),
                Location = this.GetLocation(start)
            };
        }

        private IEnumerable<GraphQLVariableDefinition> ParseVariableDefinitions()
        {
            return this.Peek(TokenKind.PAREN_L) ?
                this.Many(TokenKind.PAREN_L, () => this.ParseVariableDefinition(), TokenKind.PAREN_R) :
                new GraphQLVariableDefinition[] { };
        }

        private bool Peek(TokenKind kind)
        {
            return this.currentToken.Kind == kind;
        }

        private bool Skip(TokenKind kind)
        {
            var isCurrentTokenMatching = this.currentToken.Kind == kind;

            if (isCurrentTokenMatching)
            {
                this.Advance();
            }

            return isCurrentTokenMatching;
        }

        private object SkipEqualsAndParseValueLiteral()
        {
            return this.Skip(TokenKind.EQUALS) ? this.ParseValueLiteral(true) : null;
        }
    }
}
