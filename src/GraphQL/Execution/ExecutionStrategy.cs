using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Introspection;
using GraphQL.Language.AST;
using GraphQL.Resolvers;
using GraphQL.Types;
using static GraphQL.Execution.ExecutionHelper;

namespace GraphQL.Execution
{
    public abstract class ExecutionStrategy : IExecutionStrategy
    {
        public abstract Task<ExecutionResult> ExecuteAsync(ExecutionContext context, IObjectGraphType rootType, object source, Dictionary<string, Field> fields);

        public static FieldType GetFieldDefinition(Document document, ISchema schema, IObjectGraphType parentType, Field field)
        {
            if (field.Name == SchemaIntrospection.SchemaMeta.Name && schema.Query == parentType)
            {
                return SchemaIntrospection.SchemaMeta;
            }
            if (field.Name == SchemaIntrospection.TypeMeta.Name && schema.Query == parentType)
            {
                return SchemaIntrospection.TypeMeta;
            }
            if (field.Name == SchemaIntrospection.TypeNameMeta.Name)
            {
                return SchemaIntrospection.TypeNameMeta;
            }

            if (parentType == null)
            {
                var error = new ExecutionError($"Schema is not configured correctly to fetch {field.Name}.  Are you missing a root type?");
                error.AddLocation(field, document);
                throw error;
            }

            return parentType.Fields.FirstOrDefault(f => f.Name == field.Name);
        }
    }
}
