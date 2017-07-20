using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Types;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    public class Bug376GuidInputTests : QueryTestBase<Bug376Schema>
    {
        [Fact]
        public void can_pass_guids_as_strings()
        {
            var query = @"query someQuery($ids: [String]) { inspectionReportInspections(inspectionIds: $ids) { id name } }";

            var expected = @"{ inspectionReportInspections: [{ 'id': 'b6cca29a-1457-46a1-a5c7-26f599bb20b4', 'name': 'Inspection b6cca29a-1457-46a1-a5c7-26f599bb20b4' }] }";

            var inputs = "{ 'ids': ['b6cca29a-1457-46a1-a5c7-26f599bb20b4'] }".ToInputs();

            AssertQuerySuccess(query, expected, inputs);
        }
    }

    public class InspectionType : ObjectGraphType
    {
        public InspectionType()
        {
            Name = "Inspection";
            Field<IdGraphType>("id");
            Field<StringGraphType>("name");
        }
    }

    public class Inspection
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }

    public class Bug376QueryType : ObjectGraphType
    {
        public Bug376QueryType()
        {
            Name = "Query";

            Field<ListGraphType<InspectionType>>(
                "inspectionReportInspections",
                arguments: new QueryArguments(new QueryArgument[] {
                    new QueryArgument<ListGraphType<StringGraphType>> {
                        Name = "inspectionIds"
                    }
                }),
                resolve: context =>
                {
                    var ids = context.GetArgument<List<string>>("inspectionIds");

                    return ids.Select(id => new Inspection
                    {
                        Id = Guid.Parse(id),
                        Name = "Inspection " + id
                    });
                }
            );
        }
    }

    public class Bug376Schema : Schema
    {
        public Bug376Schema()
        {
            Query = new Bug376QueryType();
        }
    }
}
