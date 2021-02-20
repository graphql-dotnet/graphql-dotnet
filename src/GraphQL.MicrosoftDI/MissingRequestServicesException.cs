using System;

namespace GraphQL.MicrosoftDI
{
    internal class MissingRequestServicesException : InvalidOperationException
    {
        public MissingRequestServicesException() : base("No service provider specified. Please set the value of the ExecutionOptions.RequestServices to a valid service provider. Typically, this would be a scoped service provider from your dependency injection framework.")
        {
        }
    }
}
