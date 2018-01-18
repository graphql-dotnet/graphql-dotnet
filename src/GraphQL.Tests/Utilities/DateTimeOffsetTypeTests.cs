// <copyright file="DateTimeOffsetTypeTests.cs" company="eVote">
//   Copyright © eVote
// </copyright>

using System;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Utilities
{
    public class DateTimeOffsetTypeTests : SchemaBuilderTestBase
    {
        [Fact]
        public void can_use_DateTimeOffset_type()
        {
            var schema = Schema.For(@"
                input DateTimeOffsetInput{
                    value: Date
                }
                type Query {
                  five(model: DateTimeOffsetInput): Date
                }
            ", _ =>
            {
                _.Types.Include<ParametersType>();
            });

            var utcNow = DateTimeOffset.UtcNow;

            var result = schema.Execute(_ =>
            {
                _.Query = $"{{ five(model:{{ value:\"{utcNow}\"}}) }}";
            });

            var expectedResult = CreateQueryResult($"{{ 'five': \"{utcNow.AddDays(1):yyyy-MM-ddTHH:mm:ssZ}\" }}");
            var serializedExpectedResult = Writer.Write(expectedResult);

            result.ShouldBe(serializedExpectedResult);
        }

        [GraphQLMetadata("Query")]
        class ParametersType
        {
            public DateTimeOffset Five(DateTimeOffsetInput model)
            {
                return model.Value.AddDays(1);
            }
        }
    }

    class DateTimeOffsetInput
    {
        public DateTimeOffset Value { get; set; }
    }
}
