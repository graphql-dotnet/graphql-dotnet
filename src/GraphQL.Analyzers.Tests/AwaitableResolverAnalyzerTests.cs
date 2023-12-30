using GraphQL.Analyzers.Helpers;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = GraphQL.Analyzers.Tests.VerifiersExtensions.CSharpCodeFixVerifier<
    GraphQL.Analyzers.AwaitableResolverAnalyzer,
    GraphQL.Analyzers.AwaitableResolverCodeFixProvider>;

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
    // Resolve - return type defined by Field method
    [InlineData("Resolve", "Field<StringGraphType, string>(\"Test\")")]
    [InlineData("Resolve", "Field<StringGraphType, dynamic>(\"Test\")")]
    [InlineData("Resolve", "Field<StringGraphType, object>(\"Test\")")]
    [InlineData("Resolve", "Field<StringGraphType, System.Object>(\"Test\")")]
    // Resolve - return type defined by Returns method
    [InlineData("Resolve", "Field<StringGraphType>(\"Test\").Returns<string>()")]
    [InlineData("Resolve", "Field<StringGraphType>(\"Test\").Returns<dynamic>()")]
    [InlineData("Resolve", "Field<StringGraphType>(\"Test\").Returns<object>()")]
    [InlineData("Resolve", "Field<StringGraphType>(\"Test\").Returns<System.Object>()")]
    // ResolveScoped - no return type
    [InlineData("ResolveScoped", "Field<StringGraphType>(\"Test\")")]
    // ResolveScoped - return type defined by Field method
    [InlineData("ResolveScoped", "Field<StringGraphType, string>(\"Test\")")]
    [InlineData("ResolveScoped", "Field<StringGraphType, dynamic>(\"Test\")")]
    [InlineData("ResolveScoped", "Field<StringGraphType, object>(\"Test\")")]
    [InlineData("ResolveScoped", "Field<StringGraphType, System.Object>(\"Test\")")]
    // ResolveScoped - return type defined by Returns method
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
    [InlineData("Task", "dynamic")]
    [InlineData("Task", "object")]
    [InlineData("Task", "System.Object")]
    [InlineData("ValueTask", "dynamic")]
    [InlineData("ValueTask", "object")]
    [InlineData("ValueTask", "System.Object")]
    [InlineData("CustomAwaitable", "dynamic")]
    [InlineData("CustomAwaitable", "object")]
    [InlineData("CustomAwaitable", "System.Object")]
    public async Task SyncResolve_AwaitableResolver_NotAwaitableReturnType_GQL009_Fixed(string awaitableType, string returnType)
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
                          .{|#0:Resolve|}(ctx => {{awaitableType}}.FromResult("text"));
                  }
              }

              {{CUSTOM_AWAITABLE_SOURCE}}
              """;

        string fix =
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
                          .ResolveAsync(async ctx => await {{awaitableType}}.FromResult("text"));
                  }
              }

              {{CUSTOM_AWAITABLE_SOURCE}}
              """;

        var expected = VerifyCS.Diagnostic().WithLocation(0).WithArguments(Constants.MethodNames.ResolveAsync);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
    }

    [Theory]
    [InlineData("Task", "Task<string>")]
    [InlineData("ValueTask", "ValueTask<string>")]
    [InlineData("CustomAwaitable", "CustomAwaitable<string>")]
    public async Task SyncResolve_AwaitableResolver_AwaitableReturnType_GQL009(string awaitableType, string returnType)
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
                          .{|#0:Resolve|}(ctx => {{awaitableType}}.FromResult("text"));
                  }
              }

              {{CUSTOM_AWAITABLE_SOURCE}}
              """;

        var expected = VerifyCS.Diagnostic().WithLocation(0).WithArguments(Constants.MethodNames.ResolveAsync);
        await VerifyCS.VerifyCodeFixAsync(source, expected, source);
    }

    [Theory]
    [InlineData("Task", "dynamic")]
    [InlineData("Task", "object")]
    [InlineData("Task", "System.Object")]
    [InlineData("ValueTask", "dynamic")]
    [InlineData("ValueTask", "object")]
    [InlineData("ValueTask", "System.Object")]
    [InlineData("CustomAwaitable", "dynamic")]
    [InlineData("CustomAwaitable", "object")]
    [InlineData("CustomAwaitable", "System.Object")]
    public async Task SyncResolveScoped_AwaitableResolver_NotAwaitableReturnType_GQL009(string awaitableType, string returnType)
    {
        string source =
            $$"""
              using System;
              using System.Threading.Tasks;
              using GraphQL.MicrosoftDI;
              using GraphQL.Types;

              namespace Sample.Server;

              public class MyGraphType : ObjectGraphType<MySource>
              {
                  public MyGraphType()
                  {
                      Field<StringGraphType, {{returnType}}>("Test")
                          .{|#0:ResolveScoped|}(ctx => {{awaitableType}}.FromResult("text"));
                  }
              }

              public class MySource { }

              {{CUSTOM_AWAITABLE_SOURCE}}
              """;

        var expected = VerifyCS.Diagnostic().WithLocation(0).WithArguments(Constants.MethodNames.ResolveScopedAsync);
        await VerifyCS.VerifyCodeFixAsync(source, expected, source);
    }

    [Theory]
    [InlineData("Task", "Task<string>")]
    [InlineData("ValueTask", "ValueTask<string>")]
    [InlineData("CustomAwaitable", "CustomAwaitable<string>")]
    public async Task SyncResolveScoped_AwaitableResolver_AwaitableReturnType_GQL009(string awaitableType, string returnType)
    {
        string source =
            $$"""
              using System;
              using System.Threading.Tasks;
              using GraphQL.MicrosoftDI;
              using GraphQL.Types;

              namespace Sample.Server;

              public class MyGraphType : ObjectGraphType<MySource>
              {
                  public MyGraphType()
                  {
                      Field<StringGraphType, {{returnType}}>("Test")
                          .{|#0:ResolveScoped|}(ctx => {{awaitableType}}.FromResult("text"));
                  }
              }

              public class MySource { }

              {{CUSTOM_AWAITABLE_SOURCE}}
              """;

        var expected = VerifyCS.Diagnostic().WithLocation(0).WithArguments(Constants.MethodNames.ResolveScopedAsync);
        await VerifyCS.VerifyCodeFixAsync(source, expected, source);
    }

    [Theory]
    [InlineData("\"text\"", false)]
    [InlineData("Resolve(ctx)", false)]
    [InlineData("Task.FromResult(\"text\")", true)]
    [InlineData("ValueTask.FromResult(\"text\")", true)]
    [InlineData("ResolveAsync(ctx)", true)]
    [InlineData("ResolveValueAsync(ctx)", true)]
    public async Task SyncResolve_AwaitableLambdaResolver_GQL009_Fixed(string resolver, bool report)
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
                      Field<StringGraphType>("Test").{|#0:Resolve|}(ctx => {{resolver}});
                  }

                  private string Resolve(IResolveFieldContext<object> ctx) => throw new NotImplementedException();
                  private Task<string> ResolveAsync(IResolveFieldContext<object> ctx) => throw new NotImplementedException();
                  private ValueTask<string> ResolveValueAsync(IResolveFieldContext<object> ctx) => throw new NotImplementedException();
              }
              """;

        string fix =
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
                      Field<StringGraphType>("Test").ResolveAsync(async ctx => await {{resolver}});
                  }

                  private string Resolve(IResolveFieldContext<object> ctx) => throw new NotImplementedException();
                  private Task<string> ResolveAsync(IResolveFieldContext<object> ctx) => throw new NotImplementedException();
                  private ValueTask<string> ResolveValueAsync(IResolveFieldContext<object> ctx) => throw new NotImplementedException();
              }
              """;

        var expected = report
            ? new[] { VerifyCS.Diagnostic().WithLocation(0).WithArguments(Constants.MethodNames.ResolveAsync) }
            : DiagnosticResult.EmptyDiagnosticResults;

        string expectedFix = report ? fix : source;

        await VerifyCS.VerifyCodeFixAsync(source, expected, expectedFix);
    }

    [Theory]
    [InlineData("\"text\"", false)]
    [InlineData("Resolve(ctx)", false)]
    [InlineData("Task.FromResult(\"text\")", true)]
    [InlineData("ValueTask.FromResult(\"text\")", true)]
    [InlineData("ResolveAsync(ctx)", true)]
    [InlineData("ResolveValueAsync(ctx)", true)]
    public async Task SyncResolve_AwaitableBlockResolver_GQL009_Fixed(string resolver, bool report)
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
                      Field<StringGraphType>("Test").{|#0:Resolve|}(ctx =>
                      {
                          if (true)
                              return {{resolver}};
                          else
                              return {{resolver}};
                      });
                  }

                  private string Resolve(IResolveFieldContext<object> ctx) => throw new NotImplementedException();
                  private Task<string> ResolveAsync(IResolveFieldContext<object> ctx) => throw new NotImplementedException();
                  private ValueTask<string> ResolveValueAsync(IResolveFieldContext<object> ctx) => throw new NotImplementedException();
              }
              """;

        string fix =
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
                      Field<StringGraphType>("Test").ResolveAsync(async ctx =>
                      {
                          if (true)
                              return await {{resolver}};
                          else
                              return await {{resolver}};
                      });
                  }

                  private string Resolve(IResolveFieldContext<object> ctx) => throw new NotImplementedException();
                  private Task<string> ResolveAsync(IResolveFieldContext<object> ctx) => throw new NotImplementedException();
                  private ValueTask<string> ResolveValueAsync(IResolveFieldContext<object> ctx) => throw new NotImplementedException();
              }
              """;

        var expected = report
            ? new[] { VerifyCS.Diagnostic().WithLocation(0).WithArguments(Constants.MethodNames.ResolveAsync) }
            : DiagnosticResult.EmptyDiagnosticResults;

        string expectedFix = report ? fix : source;

        await VerifyCS.VerifyCodeFixAsync(source, expected, expectedFix);
    }

    [Theory]
    [InlineData("Resolve", false)]
    [InlineData("ResolveAsync", true)]
    public async Task SyncResolve_AwaitableMethodGroupResolver_GQL009_Fixed(string resolver, bool report)
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
                      Field<StringGraphType>("Test").{|#0:Resolve|}({{resolver}});
                  }

                  private string Resolve(IResolveFieldContext<object> ctx) => throw new NotImplementedException();
                  private Task<string> ResolveAsync(IResolveFieldContext<object> ctx) => throw new NotImplementedException();
              }
              """;

        string fix =
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
                      Field<StringGraphType>("Test").ResolveAsync(async context => await {{resolver}}(context));
                  }

                  private string Resolve(IResolveFieldContext<object> ctx) => throw new NotImplementedException();
                  private Task<string> ResolveAsync(IResolveFieldContext<object> ctx) => throw new NotImplementedException();
              }
              """;

        var expected = report
            ? new[] { VerifyCS.Diagnostic().WithLocation(0).WithArguments(Constants.MethodNames.ResolveAsync) }
            : DiagnosticResult.EmptyDiagnosticResults;

        string expectedFix = report ? fix : source;

        await VerifyCS.VerifyCodeFixAsync(source, expected, expectedFix);
    }

    [Fact]
    public async Task SyncResolve_AwaitableLambdaResolver_GQL009_FormatPreserved()
    {
        const string source =
            """
            using System;
            using System.Threading.Tasks;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Test")
                        .{|#0:Resolve|}(
                            ctx => Task.FromResult("text")
                        );
                }
            }

            """;

        const string fix =
            """
            using System;
            using System.Threading.Tasks;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Test")
                        .ResolveAsync(
                            async ctx => await Task.FromResult("text")
                        );
                }
            }

            """;

        var expected = VerifyCS.Diagnostic().WithLocation(0).WithArguments(Constants.MethodNames.ResolveAsync);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
    }

    [Fact]
    public async Task SyncResolve_AwaitableLambdaResolver_GQL009_FormatPreserved2()
    {
        const string source =
            """
            using System;
            using System.Threading.Tasks;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Test")
                        .{|#0:Resolve|}(ctx =>
                            Task.FromResult("text")
                    );
                }
            }

            """;

        const string fix =
            """
            using System;
            using System.Threading.Tasks;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Test")
                        .ResolveAsync(async ctx =>
                            await Task.FromResult("text")
                    );
                }
            }

            """;

        var expected = VerifyCS.Diagnostic().WithLocation(0).WithArguments(Constants.MethodNames.ResolveAsync);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
    }
}
