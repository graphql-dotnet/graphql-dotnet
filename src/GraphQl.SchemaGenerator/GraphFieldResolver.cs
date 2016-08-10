using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using GraphQl.SchemaGenerator.Extensions;
using GraphQl.SchemaGenerator.Models;
using GraphQl.SchemaGenerator.Wrappers;
using GraphQL.Types;
using Newtonsoft.Json;

namespace GraphQl.SchemaGenerator
{
    /// <summary>
    ///     Default implementation
    /// </summary>
    public class GraphFieldResolver : IGraphFieldResolver
    {
        private readonly IServiceProvider _serviceProvider;

        public GraphFieldResolver(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public object ResolveField(ResolveFieldContext context, FieldInformation field)
        {
            var classObject = _serviceProvider.GetService(field.Method.DeclaringType);
            var result = field.Method.Invoke(classObject, context.Parameters(field));

            if (result is HttpResponseMessage)
            {
                return ExtractResult(field, result as HttpResponseMessage);
            }

            return result;
        }

        /// <summary>
        ///     Extract result.
        /// </summary>
        /// <param name="field">Field information.</param>
        /// <param name="result">HttpResponseMessage to parse.</param>
        /// <returns>object.</returns>
        private object ExtractResult(FieldInformation field, HttpResponseMessage result)
        {
            if (result == null)
            {
                return null;
            }

            if (field.Response == null)
            {
                return result.StatusCode.ToString();
            }

            return result.Content.ReadAsAsync(field.Response);
        }
    }
}

