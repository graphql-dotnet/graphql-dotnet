using System;
using System.Collections.Generic;
using GraphQl.SchemaGenerator.Models;

namespace GraphQl.SchemaGenerator
{
    /// <summary>
    ///     Generates a schema from model definitions.
    /// </summary>
    public static class ModelSchemaGenerator
    {
        /// <summary>
        ///     Generate a schema from a model.
        /// </summary>
        /// <param name="objects"></param>
        /// <returns></returns>
        public static GraphQL.Types.Schema CreateSchema(params object[] objects)
        {
            var graphTypes = new List<FieldDefinition>();

            foreach (var o in objects)
            {
                var definition = new FieldDefinition(
                    o.GetType(),
                    o.GetType().Name,
                    null,
                    false,
                    (obj) => o
                );

                graphTypes.Add(definition);
            }

            return SchemaGenerator.CreateSchema(graphTypes);
        }
    }
}

