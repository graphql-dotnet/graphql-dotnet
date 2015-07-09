using System;
using Should;

namespace GraphQL.Tests.Types
{
    public class FieldRegistrationTests
    {
        [Test]
        public void throws_error_when_trying_to_register_type_with_same_name()
        {
            var graphType = new ObjectGraphType();
            graphType.Field<StringGraphType>("id");

            Exception exc = null;

            try
            {
                graphType.Field<StringGraphType>("id");
            }
            catch (Exception ex)
            {
                exc = ex;
            }
            finally
            {
                exc.ShouldNotBeNull();
                exc.ShouldBeType<ArgumentOutOfRangeException>();
            }
        }
    }

    public static class Expect
    {
        public static void Throws<TException>(Action action)
            where TException : Exception
        {
            var threwException = false;

            try
            {
                action();
            }
            catch (Exception exc)
            {
                threwException = true;
                exc.ShouldBeType<TException>();
            }
            finally
            {
                threwException.ShouldBeTrue("Expected exception '{0}' was not thrown.".ToFormat(typeof(TException).Name));
            }
        }
    }
}
