using System;
using System.Collections.Generic;
using System.Linq;
using GraphQLParser.AST;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a complex value within a document that has child fields (an object).
    /// </summary>
    public class ObjectValue : GraphQLObjectValue, IValue
    {
        /// <summary>
        /// Initializes a new instance that contains the specified field nodes.
        /// </summary>
        public ObjectValue(IEnumerable<GraphQLObjectField> fields)
        {
            Fields = (fields ?? throw new ArgumentNullException(nameof(fields))).ToList();
        }

        /// <summary>
        /// Initializes a new instance that contains the specified field nodes.
        /// </summary>
        public ObjectValue(List<GraphQLObjectField> fields)
        {
            Fields = fields ?? throw new ArgumentNullException(nameof(fields));
        }

        /// <summary>
        /// Returns a list of the names of the fields specified for this object value node.
        /// </summary>
        public IEnumerable<string> FieldNames
        {
            get
            {
                var list = new List<string>(Fields!.Count);
                foreach (var item in Fields)
                    list.Add((string)item.Name); //TODO:!!!!alloc
                return list;
            }
        }

        /// <summary>
        /// Returns a <see cref="Dictionary{TKey, TValue}">Dictionary&lt;string, object&gt;</see>
        /// containing the values of the field nodes that this object value node contains.
        /// </summary>
        public object ClrValue
        {
            get
            {
                var obj = new Dictionary<string, object?>(Fields!.Count);
                foreach (var item in Fields)
                    obj.Add((string)item.Name, ((IValue)item.Value).ClrValue); //TODO:!!!!!alloc
                return obj;
            }
        }

        /// <summary>
        /// Returns the first matching field node contained within this object value node that matches the specified name, or <see langword="null"/> otherwise.
        /// </summary>
        public GraphQLObjectField? Field(string name)
        {
            // DO NOT USE LINQ ON HOT PATH
            if (Fields != null)
            {
                foreach (var field in Fields)
                {
                    if (field.Name.Value == name)
                        return field;
                }
            }

            return null;
        }
    }
}
