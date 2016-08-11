using System.Collections.Generic;
using System.Linq;
using GraphQL.SchemaGenerator.Models;
using GraphQL.Types;
using Newtonsoft.Json;

namespace GraphQL.SchemaGenerator.Extensions
{
    /// <summary>
    ///     Extensions for resolve field context.
    /// </summary>
    internal static class ResolveFieldContextExtensions
    {
        /// <summary>
        ///     Generate the parameters for the field.
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
                if (!type.Arguments.ContainsKey(parameter.Name))
                {
                    continue;
                }

                var arg = type.Arguments[parameter.Name];

                if (typeof(IDictionary<string, object>).IsAssignableFrom(arg.GetType()))
                {
                    var json = JsonConvert.SerializeObject(arg);
                    arg = JsonConvert.DeserializeObject(json, parameter.ParameterType);
                }
                else if (parameter.ParameterType == typeof(char))
                {
                    arg = arg.ToString()[0];
                }

                routeArguments.Add(arg);
            }

            return routeArguments.Any() ? routeArguments.ToArray() : null;
        }

    }
}
