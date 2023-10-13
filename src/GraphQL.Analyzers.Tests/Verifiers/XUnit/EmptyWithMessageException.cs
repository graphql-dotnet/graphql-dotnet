using System.Collections;
using Xunit.Sdk;

namespace GraphQL.Analyzers.Tests.Verifiers.XUnit;

public class EmptyWithMessageException : EmptyException
{
    public EmptyWithMessageException(IEnumerable collection, string userMessage)
        : base(collection)
    {
        UserMessage = userMessage;
    }

    public override string Message
    {
        get
        {
            if (string.IsNullOrEmpty(UserMessage))
                return base.Message;

            return UserMessage + Environment.NewLine + base.Message;
        }
    }
}
