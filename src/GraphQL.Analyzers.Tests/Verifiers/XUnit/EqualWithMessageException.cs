﻿using Xunit.Sdk;

namespace GraphQL.Analyzers.Tests.Verifiers.XUnit;

public class EqualWithMessageException : EqualException
{
    public EqualWithMessageException(object? expected, object? actual, string userMessage)
        : base(expected, actual)
    {
        UserMessage = userMessage;
    }

    public EqualWithMessageException(string? expected, string? actual, int expectedIndex, int actualIndex, string userMessage)
        : base(expected, actual, expectedIndex, actualIndex)
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