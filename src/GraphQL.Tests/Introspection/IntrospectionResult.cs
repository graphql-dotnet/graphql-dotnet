using System;
using System.IO;

namespace GraphQL.Tests.Introspection
{
    public class IntrospectionResult
    {
        public static readonly string Data = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Introspection", "IntrospectionResult.json"));
    }
}
