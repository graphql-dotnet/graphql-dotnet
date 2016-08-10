using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using GraphQl.SchemaGenerator.Models;
using GraphQL;
using GraphQL.Types;
using System.Linq;

namespace GraphQl.SchemaGenerator.Extensions
{
    /// <summary>
    ///     Extensions for resolve field context.
    /// </summary>
    internal static class ResolveFieldContextExtensions
    {
        /// <summary>
        ///     Generate the paramets for the field.
        /// </summary>
        /// <param name="type">Extension.</param>
        /// <param name="field">Field information.</param>
        /// <returns></returns>
        public static object[] Parameters(this ResolveFieldContext type, FieldInformation field)
        {
            if (field == null)
            {
                return null;
            }

            var routeArguments = new List<object>();
            foreach (var parameter in field.Method.GetParameters())
            {
                if (type.Arguments.ContainsKey(parameter.Name))
                {
                    continue;
                }

                var arg = type.Arguments[parameter.Name];

                if (parameter.ParameterType == typeof(char))
                {
                    routeArguments.Add(arg.ToString()[0]);
                    continue;
                }

                routeArguments.Add(arg);
            }

            return routeArguments.Any() ? routeArguments.ToArray() : null;
        }

    }
}
