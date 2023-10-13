using Xunit.Sdk;

namespace GraphQL.Analyzers.Tests.Verifiers.XUnit;

public class NotEmptyWithMessageException : NotEmptyException
{
    public NotEmptyWithMessageException(string userMessage)
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