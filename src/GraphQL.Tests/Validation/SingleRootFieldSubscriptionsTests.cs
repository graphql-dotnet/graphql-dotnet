using GraphQL.Validation.Errors;
using GraphQL.Validation.Rules;
using GraphQLParser.AST;

namespace GraphQL.Tests.Validation;

public class SingleRootFieldSubscriptionsTests
    : ValidationTestBase<SingleRootFieldSubscriptions, ValidationSchema>
{
    [Fact]
    public void No_operations_should_pass()
    {
        ShouldPassRule("fragment fragA on Type { field }");
    }

    [Fact]
    public void Anonymous_query_operation_should_pass()
    {
        ShouldPassRule("{ field1 }");
    }

    [Fact]
    public void Anonymous_subscription_operation_with_single_root_field_should_pass()
    {
        ShouldPassRule("subscription { field }");
    }

    [Fact]
    public void One_named_subscription_operation_with_single_root_field_should_pass()
    {
        ShouldPassRule("subscription { field }");
    }

    [Fact]
    public void Fails_with_more_than_one_root_field_in_anonymous_subscription()
    {
        string query = @"
                subscription {
                    field
                    field2
                }
            ";

        ShouldFailRule(config =>
        {
            config.Query = query;
            config.Error(SingleRootFieldSubscriptionsError.InvalidNumberOfRootFieldMessage(default), 4, 21);
        });
    }

    [Fact]
    public void Fails_with_more_than_one_root_field_including_introspection_in_anonymous_subscription()
    {
        string query = @"
                subscription {
                    field
                    __typename
                }
            ";

        ShouldFailRule(config =>
        {
            config.Query = query;
            config.Error(SingleRootFieldSubscriptionsError.InvalidNumberOfRootFieldMessage(default), 4, 21);
        });
    }

    [Fact]
    public void Fails_with_more_than_one_root_field()
    {
        const string subscriptionName = "NamedSubscription";
        const string query = @"
                subscription NamedSubscription {
                    field
                    field2
                }
            ";

        ShouldFailRule(config =>
        {
            config.Query = query;
            config.Error(SingleRootFieldSubscriptionsError.InvalidNumberOfRootFieldMessage(new GraphQLName(subscriptionName)), 4, 21);
        });
    }

    [Fact]
    public void Fails_with_more_than_one_root_field_including_introspection()
    {
        const string subscriptionName = "NamedSubscription";
        const string query = @"
                subscription NamedSubscription {
                    field
                    __typename
                }
            ";

        ShouldFailRule(config =>
        {
            config.Query = query;
            config.Error(SingleRootFieldSubscriptionsError.InvalidNumberOfRootFieldMessage(new GraphQLName(subscriptionName)), 4, 21);
        });
    }

    [Fact]
    public void Fails_with_more_than_one_root_field_in_fragment_spead()
    {
        const string subscriptionName = "NamedSubscription";
        const string query = @"
                subscription NamedSubscription {
                    ...newMessageFields
                }

                fragment newMessageFields on Subscription {
                    newMessage {
                        body
                        sender
                    }
                    disallowedSecondRootField
                }
            ";

        ShouldFailRule(config =>
        {
            config.Query = query;
            config.Error(SingleRootFieldSubscriptionsError.InvalidNumberOfRootFieldMessage(new GraphQLName(subscriptionName)), 3, 21);
        });
    }

    [Fact]
    public void Fails_with_more_than_one_root_field_in_inline_fragment()
    {
        const string subscriptionName = "NamedSubscription";
        const string query = @"
                subscription NamedSubscription {
                    ...on Subscription {
                        newMessage {
                            body
                            sender
                        }
                        disallowedSecondRootField
                    }
                }
            ";

        ShouldFailRule(config =>
        {
            config.Query = query;
            config.Error(SingleRootFieldSubscriptionsError.InvalidNumberOfRootFieldMessage(new GraphQLName(subscriptionName)), 3, 21);
        });
    }

    [Fact]
    public void Pass_with_one_root_field_in_fragment_spead()
    {
        const string query = @"
                subscription NamedSubscription {
                    ...newMessageFields
                }
                
                fragment newMessageFields on Subscription {
                    newMessage {
                        body
                        sender
                    }
                }
            ";

        ShouldPassRule(query);
    }

    [Fact]
    public void Pass_with_one_root_field_in_inline_fragment()
    {
        const string query = @"
                subscription NamedSubscription {
                    ...on Subscription {
                        newMessage {
                            body
                            sender
                        }
                    }
                }
            ";

        ShouldPassRule(query);
    }
}
