using System;
using Should;

namespace GraphQL.Tests
{
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
