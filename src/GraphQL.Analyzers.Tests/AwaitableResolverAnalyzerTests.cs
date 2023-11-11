using Microsoft.CodeAnalysis.Testing;
using VerifyCS = GraphQL.Analyzers.Tests.VerifiersExtensions.CSharpAnalyzerVerifier<
    GraphQL.Analyzers.AwaitableResolverAnalyzer>;

namespace GraphQL.Analyzers.Tests;

public class AwaitableResolverAnalyzerTests
{
    private const string CUSTOM_AWAITABLE_SOURCE =
        """
        public class CustomAwaitable
        {
            public static CustomAwaitable<TResult> FromResult<TResult>(TResult result) => throw new NotImplementedException();
        }

        public class CustomAwaitable<TResult>
        {
            public static CustomAwaitable<TResult> FromResult(TResult result) => throw new NotImplementedException();
            public CustomAwaiter<TResult> GetAwaiter() => throw new NotImplementedException();
        }

        public class CustomAwaiter<TResult> : System.Runtime.CompilerServices.INotifyCompletion
        {
            public void OnCompleted(Action continuation) => throw new NotImplementedException();
            public bool IsCompleted => throw new NotImplementedException();
            public TResult GetResult() => throw new NotImplementedException();
        }
        """;

    [Fact]
    public async Task Sanity_NoDiagnostics()
    {
        const string source = "";
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Theory]
    [InlineData("ResolveAsync", "Task", "dynamic")]
    [InlineData("ResolveAsync", "Task", "object")]
    [InlineData("ResolveAsync", "Task", "System.Object")]
    [InlineData("ResolveAsync", "Task", "string")]
    [InlineData("ResolveAsync", "ValueTask", "dynamic")]
    [InlineData("ResolveAsync", "ValueTask", "object")]
    [InlineData("ResolveAsync", "ValueTask", "System.Object")]
    [InlineData("ResolveAsync", "ValueTask", "string")]
    [InlineData("ResolveAsync", "CustomAwaitable", "dynamic")]
    [InlineData("ResolveAsync", "CustomAwaitable", "object")]
    [InlineData("ResolveAsync", "CustomAwaitable", "System.Object")]
    [InlineData("ResolveAsync", "CustomAwaitable", "string")]
    [InlineData("ResolveScopedAsync<object, dynamic>", "Task", "dynamic")]
    [InlineData("ResolveScopedAsync<object, object>", "Task", "object")]
    [InlineData("ResolveScopedAsync<object, System.Object>", "Task", "System.Object")]
    [InlineData("ResolveScopedAsync<object, string>", "Task", "string")]
    [InlineData("ResolveScopedAsync<object, dynamic>", "ValueTask", "dynamic")]
    [InlineData("ResolveScopedAsync<object, object>", "ValueTask", "object")]
    [InlineData("ResolveScopedAsync<object, System.Object>", "ValueTask", "System.Object")]
    [InlineData("ResolveScopedAsync<object, string>", "ValueTask", "string")]
    [InlineData("ResolveScopedAsync<object, dynamic>", "CustomAwaitable", "dynamic")]
    [InlineData("ResolveScopedAsync<object, object>", "CustomAwaitable", "object")]
    [InlineData("ResolveScopedAsync<object, System.Object>", "CustomAwaitable", "System.Object")]
    [InlineData("ResolveScopedAsync<object, string>", "CustomAwaitable", "string")]
    public async Task AsyncResolve_NoDiagnostics(string resolveMethod, string awaitableType, string returnType)
    {
        string source =
            $$"""
              using System;
              using System.Threading.Tasks;
              using GraphQL.MicrosoftDI;
              using GraphQL.Types;

              namespace Sample.Server;

              public class MyGraphType : ObjectGraphType
              {
                  public MyGraphType()
                  {
                      Field<StringGraphType, {{returnType}}>("Test")
                          .{{resolveMethod}}(async ctx => await {{awaitableType}}.FromResult("text"));
                  }
              }

              {{CUSTOM_AWAITABLE_SOURCE}}
              """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Theory]
    // Resolve - no return type
    [InlineData("Resolve", "Field<StringGraphType>(\"Test\")")]
    // Resolve - no return type defined by Field method
    [InlineData("Resolve", "Field<StringGraphType, string>(\"Test\")")]
    [InlineData("Resolve", "Field<StringGraphType, dynamic>(\"Test\")")]
    [InlineData("Resolve", "Field<StringGraphType, object>(\"Test\")")]
    [InlineData("Resolve", "Field<StringGraphType, System.Object>(\"Test\")")]
    // Resolve - no return type defined by Returns method
    [InlineData("Resolve", "Field<StringGraphType>(\"Test\").Returns<string>()")]
    [InlineData("Resolve", "Field<StringGraphType>(\"Test\").Returns<dynamic>()")]
    [InlineData("Resolve", "Field<StringGraphType>(\"Test\").Returns<object>()")]
    [InlineData("Resolve", "Field<StringGraphType>(\"Test\").Returns<System.Object>()")]
    // ResolveScoped - no return type
    [InlineData("ResolveScoped", "Field<StringGraphType>(\"Test\")")]
    // ResolveScoped - no return type defined by Field method
    [InlineData("ResolveScoped", "Field<StringGraphType, string>(\"Test\")")]
    [InlineData("ResolveScoped", "Field<StringGraphType, dynamic>(\"Test\")")]
    [InlineData("ResolveScoped", "Field<StringGraphType, object>(\"Test\")")]
    [InlineData("ResolveScoped", "Field<StringGraphType, System.Object>(\"Test\")")]
    // ResolveScoped - no return type defined by Returns method
    [InlineData("ResolveScoped", "Field<StringGraphType>(\"Test\").Returns<string>()")]
    [InlineData("ResolveScoped", "Field<StringGraphType>(\"Test\").Returns<dynamic>()")]
    [InlineData("ResolveScoped", "Field<StringGraphType>(\"Test\").Returns<object>()")]
    [InlineData("ResolveScoped", "Field<StringGraphType>(\"Test\").Returns<System.Object>()")]
    public async Task SyncResolve_NotAwaitableResolver_NoDiagnostics(string resolveMethod, string fieldDefinition)
    {
        string source =
            $$"""
              using GraphQL.MicrosoftDI;
              using GraphQL.Types;

              namespace Sample.Server;

              public class MyGraphType : ObjectGraphType
              {
                  public MyGraphType()
                  {
                      {{fieldDefinition}}
                          .{{resolveMethod}}(ctx => "text");
                  }
              }
              """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Theory]
    [InlineData("Resolve", "Task", "dynamic")]
    [InlineData("Resolve", "Task", "object")]
    [InlineData("Resolve", "Task", "System.Object")]
    [InlineData("Resolve", "Task", "Task<string>")]
    [InlineData("Resolve", "ValueTask", "dynamic")]
    [InlineData("Resolve", "ValueTask", "object")]
    [InlineData("Resolve", "ValueTask", "System.Object")]
    [InlineData("Resolve", "ValueTask", "ValueTask<string>")]
    [InlineData("Resolve", "CustomAwaitable", "dynamic")]
    [InlineData("Resolve", "CustomAwaitable", "object")]
    [InlineData("Resolve", "CustomAwaitable", "System.Object")]
    [InlineData("Resolve", "CustomAwaitable", "CustomAwaitable<string>")]
    [InlineData("ResolveScoped", "Task", "dynamic")]
    [InlineData("ResolveScoped", "Task", "object")]
    [InlineData("ResolveScoped", "Task", "System.Object")]
    [InlineData("ResolveScoped", "Task", "Task<string>")]
    [InlineData("ResolveScoped", "ValueTask", "dynamic")]
    [InlineData("ResolveScoped", "ValueTask", "object")]
    [InlineData("ResolveScoped", "ValueTask", "System.Object")]
    [InlineData("ResolveScoped", "ValueTask", "ValueTask<string>")]
    [InlineData("ResolveScoped", "CustomAwaitable", "dynamic")]
    [InlineData("ResolveScoped", "CustomAwaitable", "object")]
    [InlineData("ResolveScoped", "CustomAwaitable", "System.Object")]
    [InlineData("ResolveScoped", "CustomAwaitable", "CustomAwaitable<string>")]
    public async Task SyncResolve_AwaitableResolver_GQL009(string resolveMethod, string awaitableType, string returnType)
    {
        string source =
            $$"""
              using System;
              using System.Threading.Tasks;
              using GraphQL.MicrosoftDI;
              using GraphQL.Types;

              namespace Sample.Server;

              public class MyGraphType : ObjectGraphType
              {
                  public MyGraphType()
                  {
                      Field<StringGraphType, {{returnType}}>("Test")
                          .{{resolveMethod}}(ctx => {{awaitableType}}.FromResult("text"));
                  }
              }

              {{CUSTOM_AWAITABLE_SOURCE}}
              """;

        const int startCol = 14;
        int endCol = startCol + resolveMethod.Length;

        var expected = VerifyCS.Diagnostic().WithSpan(13, startCol, 13, endCol).WithArguments(resolveMethod + "Async");
        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Theory]
    [InlineData("ctx => \"text\"", false)]
    [InlineData("ctx => Resolve(ctx)", false)]
    [InlineData("ctx => Task.FromResult(\"text\")", true)]
    [InlineData("ctx => ResolveAsync(ctx)", true)]
    public async Task SyncResolve_AwaitableLambdaResolver_GQL009(string resolver, bool report)
    {
        string source =
            $$"""
              using System;
              using System.Threading.Tasks;
              using GraphQL;
              using GraphQL.Types;

              namespace Sample.Server;

              public class MyGraphType : ObjectGraphType
              {
                  public MyGraphType()
                  {
                      Field<StringGraphType>("Test").Resolve({{resolver}});
                  }

                  private string Resolve(IResolveFieldContext<object> ctx) => throw new NotImplementedException();
                  private Task<string> ResolveAsync(IResolveFieldContext<object> ctx) => throw new NotImplementedException();
              }
              """;

        var expected = report
            ? new[] { VerifyCS.Diagnostic().WithSpan(12, 40, 12, 47).WithArguments("ResolveAsync") }
            : DiagnosticResult.EmptyDiagnosticResults;

        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Theory]
    [InlineData("\"text\"", false)]
    [InlineData("Resolve(ctx)", false)]
    [InlineData("Task.FromResult(\"text\")", true)]
    [InlineData("ResolveAsync(ctx)", true)]
    public async Task SyncResolve_AwaitableBlockResolver_GQL009(string resolver, bool report)
    {
        string source =
            $$"""
              using System;
              using System.Threading.Tasks;
              using GraphQL;
              using GraphQL.Types;

              namespace Sample.Server;

              public class MyGraphType : ObjectGraphType
              {
                  public MyGraphType()
                  {
                      Field<StringGraphType>("Test").Resolve(ctx =>
                      {
                          return {{resolver}};
                      });
                  }

                  private string Resolve(IResolveFieldContext<object> ctx) => throw new NotImplementedException();
                  private Task<string> ResolveAsync(IResolveFieldContext<object> ctx) => throw new NotImplementedException();
              }
              """;

        var expected = report
            ? new[] { VerifyCS.Diagnostic().WithSpan(12, 40, 12, 47).WithArguments("ResolveAsync") }
            : DiagnosticResult.EmptyDiagnosticResults;

        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }
    [Theory]
    [InlineData("Resolve", false)]
    [InlineData("ResolveAsync", true)]
    public async Task SyncResolve_AwaitableMethodGroupResolver_GQL009(string resolver, bool report)
    {
        string source =
            $$"""
              using System;
              using System.Threading.Tasks;
              using GraphQL;
              using GraphQL.Types;

              namespace Sample.Server;

              public class MyGraphType : ObjectGraphType
              {
                  public MyGraphType()
                  {
                      Field<StringGraphType>("Test").Resolve({{resolver}});
                  }

                  private string Resolve(IResolveFieldContext<object> ctx) => throw new NotImplementedException();
                  private Task<string> ResolveAsync(IResolveFieldContext<object> ctx) => throw new NotImplementedException();
              }
              """;

        var expected = report
            ? new[] { VerifyCS.Diagnostic().WithSpan(12, 40, 12, 47).WithArguments("ResolveAsync") }
            : DiagnosticResult.EmptyDiagnosticResults;

        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }
}
