using System;
using System.Collections.Generic;
using GraphQL.Execution;
using GraphQL.Types;
using Moq;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.MicrosoftDI
{
    public class AdapterTests
    {
        [Fact]
        public void Passthrough()
        {
            var rfc = new ResolveFieldContext<string>
            {
                Arguments = new Dictionary<string, ArgumentValue>() { { "6", default } },
                ArrayPool = Mock.Of<IExecutionArrayPool>(),
                CancellationToken = default,
                Document = new GraphQL.Language.AST.Document(),
                Errors = new ExecutionErrors(),
                Extensions = new Dictionary<string, object>() { { "1", new object() } },
                FieldAst = new GraphQL.Language.AST.Field(),
                FieldDefinition = new FieldType(),
                FieldName = "test",
                Fragments = new GraphQL.Language.AST.Fragments(),
                Metrics = new GraphQL.Instrumentation.Metrics(),
                Operation = new GraphQL.Language.AST.Operation(new GraphQL.Language.AST.NameNode()),
                ParentType = Mock.Of<IObjectGraphType>(),
                Path = new object[] { "5" },
                RequestServices = Mock.Of<IServiceProvider>(),
                ResponsePath = new object[] { "4" },
                ReturnType = Mock.Of<IGraphType>(),
                RootValue = new object(),
                Schema = Mock.Of<ISchema>(),
                Source = "hello",
                SubFields = new Dictionary<string, GraphQL.Language.AST.Field>() { { "2", null } },
                UserContext = new Dictionary<string, object>() { { "3", new object() } },
                Variables = new GraphQL.Language.AST.Variables(),
            };
            var rs = Mock.Of<IServiceProvider>();
            var mocked = new GraphQL.MicrosoftDI.ScopedResolveFieldContextAdapter(rfc, rs);
            mocked.Arguments.ShouldBe(rfc.Arguments);
            mocked.ArrayPool.ShouldBe(rfc.ArrayPool);
            mocked.CancellationToken.ShouldBe(rfc.CancellationToken);
            mocked.Document.ShouldBe(rfc.Document);
            mocked.Errors.ShouldBe(rfc.Errors);
            mocked.Extensions.ShouldBe(rfc.Extensions);
            mocked.FieldAst.ShouldBe(rfc.FieldAst);
            mocked.FieldDefinition.ShouldBe(rfc.FieldDefinition);
            mocked.FieldName.ShouldBe(rfc.FieldName);
            mocked.Fragments.ShouldBe(rfc.Fragments);
            mocked.Metrics.ShouldBe(rfc.Metrics);
            mocked.Operation.ShouldBe(rfc.Operation);
            mocked.ParentType.ShouldBe(rfc.ParentType);
            mocked.Path.ShouldBe(rfc.Path);
            mocked.RequestServices.ShouldBe(rs);
            mocked.ResponsePath.ShouldBe(rfc.ResponsePath);
            mocked.ReturnType.ShouldBe(rfc.ReturnType);
            mocked.RootValue.ShouldBe(rfc.RootValue);
            mocked.Schema.ShouldBe(rfc.Schema);
            mocked.Source.ShouldBe(rfc.Source);
            ((IResolveFieldContext)mocked).Source.ShouldBe(((IResolveFieldContext)rfc).Source);
            mocked.SubFields.ShouldBe(rfc.SubFields);
            mocked.UserContext.ShouldBe(rfc.UserContext);
            mocked.Variables.ShouldBe(rfc.Variables);
        }
    }
}
