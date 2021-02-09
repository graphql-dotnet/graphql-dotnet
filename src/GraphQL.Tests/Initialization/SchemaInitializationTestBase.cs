using System;
using GraphQL.Types;
using Shouldly;

namespace GraphQL.Tests.Initialization
{
    public abstract class SchemaInitializationTestBase
    {
        public void ShouldThrow<TSchema, TException>(Action<TException> assert = null)
            where TSchema : Schema, new()
            where TException : Exception
        {
            var ex = Should.Throw<TException>(() => new TSchema().Initialize());
            assert?.Invoke(ex);
        }

        public void ShouldThrow<TSchema, TException>(string errorMessage)
            where TSchema : Schema, new()
            where TException : Exception => ShouldThrow<TSchema, TException>(ex => ex.Message.ShouldBe(errorMessage));
    }
}
