#nullable enable

using GraphQL.Types;
using GraphQL.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests.Validation;

public class RequestServicesTests
{
    [Fact]
    public async Task ValidationRuleUsesServiceProviderFromDocumentExecuter()
    {
        // prepare service provider
        var services = new ServiceCollection();
        services.AddScoped<Class1>();
        services.AddGraphQL(b => b
            .AddAutoSchema<Query>()
            .AddValidationRule<MyValidationRule>()
            .AddSystemTextJson());
        using var provider = services.BuildServiceProvider();

        // test class1 with root service provider
        var class1 = provider.GetRequiredService<Class1>();
        class1.GetNum.ShouldBe(1);

        // should be same instance as class1
        var class1b = provider.GetRequiredService<Class1>();
        class1b.GetNum.ShouldBe(2);

        // execute a request within a service scope
        var executer = provider.GetRequiredService<IDocumentExecuter<ISchema>>();
        using var scope = provider.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var result = await executer.ExecuteAsync(new ExecutionOptions
        {
            Query = "{hero}",
            RequestServices = scope.ServiceProvider,
        }).ConfigureAwait(false);

        // MyValidationRule should always add an error message
        result.Executed.ShouldBeFalse();

        // verify that class1.GetNum returned "1" because it is a scoped instance
        result.Errors.ShouldHaveSingleItem().Message.ShouldBe("Num is 1");

        // serialize to json to be sure no issues with a validation error without a number
        var serializer = provider.GetRequiredService<IGraphQLTextSerializer>();
        var resultString = serializer.Serialize(result);
        resultString.ShouldBe(@"{""errors"":[{""message"":""Num is 1"",""extensions"":{""code"":""VALIDATION_ERROR"",""codes"":[""VALIDATION_ERROR""]}}]}");
    }

    private class MyValidationRule : IValidationRule
    {
        public ValueTask<INodeVisitor?> ValidateAsync(ValidationContext context)
        {
            var num = context.RequestServices.GetRequiredService<Class1>().GetNum;
            context.ReportError(new ValidationError($"Num is {num}"));
            return default;
        }
    }

    private class Query
    {
        public static string Hero => "hello";
    }

    private class Class1
    {
        private int _num;

        public int GetNum => Interlocked.Increment(ref _num);
    }
}
