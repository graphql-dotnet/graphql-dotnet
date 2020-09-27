using System;
using System.Collections.Generic;
using GraphQL.Instrumentation;
using Shouldly;
using Xunit;

namespace GraphQL.ApolloStudio.Tests
{
    public class MetricsToTraceConverterTests
    {
        private readonly MetricsToTraceConverter _converter;

        public MetricsToTraceConverterTests()
        {
            _converter = new MetricsToTraceConverter();
        }

        [Fact]
        public void NoTracing()
        {
            var executionResult = new ExecutionResult
            {
                Errors = new ExecutionErrors()
            };

            var trace = _converter.CreateTrace(executionResult, DateTime.UtcNow);
            trace.ShouldNotBeNull();
        }

        [Fact]
        public void EmptyTrace()
        {
            var executionResult = new ExecutionResult
            {
                Extensions = new Dictionary<string, object>
                {
                    {"tracing", new ApolloTrace(DateTime.Now, 1000)}
                },
                Errors = new ExecutionErrors()
            };

            var trace = _converter.CreateTrace(executionResult, DateTime.UtcNow);
            trace.ShouldNotBeNull();
        }

        [Fact]
        public void ErrorsOnly()
        {
            var executionResult = new ExecutionResult
            {
                Errors = new ExecutionErrors
                {
                    new ExecutionError("BORKED")
                }
            };

            var trace = _converter.CreateTrace(executionResult, DateTime.UtcNow);
            trace.ShouldNotBeNull();
            trace.Root.ShouldNotBeNull();
            trace.Root.Errors.ShouldHaveSingleItem().Message.ShouldBe("BORKED");
        }

        [Fact]
        public void SimpleTrace()
        {
            var executionResult = new ExecutionResult
            {
                Extensions = new Dictionary<string, object>
                {
                    {
                        "tracing", new ApolloTrace(DateTime.Now, 1000)
                        {
                            Execution =
                            {
                                Resolvers =
                                {
                                    new ApolloTrace.ResolverTrace
                                    {
                                        StartOffset = 0,
                                        Duration = 1000,
                                        FieldName = "TestGroup",
                                        Path = {"TestGroup"},
                                        ReturnType = "TestGroupType"
                                    },
                                    new ApolloTrace.ResolverTrace
                                    {
                                        StartOffset = 0,
                                        Duration = 500,
                                        FieldName = "FieldA",
                                        ReturnType = "String!",
                                        Path = {"TestGroup", "FieldA" }
                                    },
                                    new ApolloTrace.ResolverTrace
                                    {
                                        StartOffset = 500,
                                        Duration = 500,
                                        FieldName = "FieldB",
                                        ReturnType = "Int",
                                        Path = {"TestGroup", "FieldB" }
                                    },
                                    new ApolloTrace.ResolverTrace
                                    {
                                        StartOffset = 500,
                                        Duration = 500,
                                        FieldName = "FieldC",
                                        ReturnType = "Int",
                                        Path = {"TestGroup", "FieldC" }
                                    }
                                }
                            }
                        }
                    }
                },
                Errors = new ExecutionErrors
                {
                    new ExecutionError("Failed to get FieldC")
                    {

                        Path = new object[] {"TestGroup", "FieldC" }
                    }
                }
            };

            var trace = _converter.CreateTrace(executionResult, DateTime.UtcNow);
            trace.ShouldNotBeNull();
            trace.Root.ShouldNotBeNull();
            trace.Root.Childs.Count.ShouldBe(3);
            var errorField = trace.Root.Childs[2];
            errorField.Childs.ShouldBeEmpty();
            errorField.Errors.ShouldHaveSingleItem().Message.ShouldBe("Failed to get FieldC");
        }

        [Fact]
        public void TraceWithArray()
        {
            var executionResult = new ExecutionResult
            {
                Extensions = new Dictionary<string, object>
                {
                    {
                        "tracing", new ApolloTrace(DateTime.Now, 1000)
                        {
                            Execution =
                            {
                                Resolvers =
                                {
                                    new ApolloTrace.ResolverTrace
                                    {
                                        StartOffset = 0,
                                        Duration = 1000,
                                        FieldName = "TestGroup",
                                        Path = {"TestGroup"},
                                        ReturnType = "[TestGroupType]!"
                                    },
                                    new ApolloTrace.ResolverTrace
                                    {
                                        StartOffset = 0,
                                        Duration = 500,
                                        FieldName = "FieldA",
                                        ReturnType = "String!",
                                        Path = {"TestGroup", 0, "FieldA" }
                                    },
                                    new ApolloTrace.ResolverTrace
                                    {
                                        StartOffset = 500,
                                        Duration = 500,
                                        FieldName = "FieldB",
                                        ReturnType = "Int",
                                        Path = {"TestGroup", 0, "FieldB" }
                                    },
                                    new ApolloTrace.ResolverTrace
                                    {
                                        StartOffset = 500,
                                        Duration = 500,
                                        FieldName = "FieldC",
                                        ReturnType = "Int",
                                        Path = {"TestGroup", 0, "FieldC" }
                                    },
                                    new ApolloTrace.ResolverTrace
                                    {
                                        StartOffset = 0,
                                        Duration = 500,
                                        FieldName = "FieldA",
                                        ReturnType = "String!",
                                        Path = {"TestGroup", 1, "FieldA" }
                                    },
                                    new ApolloTrace.ResolverTrace
                                    {
                                        StartOffset = 500,
                                        Duration = 500,
                                        FieldName = "FieldB",
                                        ReturnType = "Int",
                                        Path = {"TestGroup", 1, "FieldB" }
                                    },
                                    new ApolloTrace.ResolverTrace
                                    {
                                        StartOffset = 500,
                                        Duration = 500,
                                        FieldName = "FieldC",
                                        ReturnType = "Int",
                                        Path = {"TestGroup", 1, "FieldC" }
                                    }
                                }
                            }
                        }
                    }
                },
                Errors = new ExecutionErrors
                {
                    new ExecutionError("Failed to get FieldC")
                    {

                        Path = new object[] {"TestGroup", 1, "FieldC" }
                    }
                }
            };

            var trace = _converter.CreateTrace(executionResult, DateTime.UtcNow);
            trace.ShouldNotBeNull();
            trace.Root.ShouldNotBeNull();
            trace.Root.Childs.Count.ShouldBe(2);
            var currentItem = trace.Root.Childs[0];
            currentItem.Index.ShouldBe((uint)0);
            var secondItem = trace.Root.Childs[1];
            secondItem.Index.ShouldBe((uint)1);
            var errorField = currentItem.Childs[2];
            errorField.Childs.ShouldBeEmpty();
            errorField.Errors.ShouldBeEmpty();
            errorField = secondItem.Childs[2];
            errorField.Childs.ShouldBeEmpty();
            errorField.Errors.ShouldHaveSingleItem().Message.ShouldBe("Failed to get FieldC");
        }

        [Fact]
        public void ComplexArray()
        {
            var executionResult = new ExecutionResult
            {
                Extensions = new Dictionary<string, object>
                {
                    {
                        "tracing", new ApolloTrace(DateTime.Now, 1000)
                        {
                            Execution =
                            {
                                Resolvers =
                                {
                                    new ApolloTrace.ResolverTrace
                                    {
                                        StartOffset = 0,
                                        Duration = 1000,
                                        FieldName = "TestGroup",
                                        Path = {"TestGroup"},
                                        ReturnType = "[TestGroupType]"
                                    },
                                    new ApolloTrace.ResolverTrace
                                    {
                                        StartOffset = 0,
                                        Duration = 500,
                                        FieldName = "FieldA",
                                        ReturnType = "FieldAType",
                                        Path = {"TestGroup", 0, "FieldA" }
                                    },
                                    new ApolloTrace.ResolverTrace
                                    {
                                        StartOffset = 0,
                                        Duration = 500,
                                        FieldName = "FieldB",
                                        ReturnType = "[FieldBType]",
                                        Path = {"TestGroup", 0, "FieldA", "FieldB"}
                                    }
                                    ,
                                    new ApolloTrace.ResolverTrace
                                    {
                                        StartOffset = 0,
                                        Duration = 500,
                                        FieldName = "FieldC",
                                        ReturnType = "String!",
                                        Path = {"TestGroup", 0, "FieldA", "FieldB", 0, "FieldC"}
                                    }
                                }
                            }
                        }
                    }
                },
                Errors = new ExecutionErrors
                {
                    new ExecutionError("Failed to get FieldC")
                    {

                        Path = new object[] { "TestGroup", 0, "FieldA", "FieldB", 0, "FieldC" }
                    }
                }
            };

            var trace = _converter.CreateTrace(executionResult, DateTime.UtcNow);
            trace.ShouldNotBeNull();
            trace.Root.ShouldNotBeNull();
            var currentItem = trace.Root.Childs.ShouldHaveSingleItem();
            currentItem.Index.ShouldBe((uint)0);
            currentItem = currentItem.Childs.ShouldHaveSingleItem();
            currentItem.ResponseName.ShouldBe("FieldA");
            currentItem = currentItem.Childs.ShouldHaveSingleItem();
            currentItem.ResponseName.ShouldBe("FieldB");
            currentItem = currentItem.Childs.ShouldHaveSingleItem();
            currentItem.Index.ShouldBe((uint)0);
            currentItem = currentItem.Childs.ShouldHaveSingleItem();
            currentItem.ResponseName.ShouldBe("FieldC");
            currentItem.Errors.ShouldHaveSingleItem().Message.ShouldBe("Failed to get FieldC");
        }
    }
}
