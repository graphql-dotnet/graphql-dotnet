using System;
using GraphQL.Types;
using System.Collections.Generic;

namespace GraphQL.DotNet.Examples.SchemaFirst
{
    /// <summary>
    /// Simple example demonstrating schema-first approach
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            // Example: Define a schema-first GraphQL type
            var userType = new ObjectGraphType<User>();
            userType.Field("id", typeof(int));
            userType.Field("name", typeof(string));
            userType.Field("email", typeof(string));

            // Example query
            var query = new SchemaFirstQuery(userType);
            query.Field("user", new QueryArguments(new QueryArgument("id", typeof(int))), userType);

            Console.WriteLine("Schema-First Example: User query defined successfully!");
        }
    }

    // Sample data class
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }
}
