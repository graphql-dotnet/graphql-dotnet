using GraphQL.Types;

namespace GraphQL.Tests.Bugs.Configuration
{
    public class ResultType : ObjectGraphType
    {
        public ResultType()
        {
            Name = "Result";

            Field<AdvancedEnumType>("episodeSet", "Which episode.");
           
            IsTypeOf = value => value is Result;
        }
    }

    public class Result
    {
        public AdvancedEnum EpisodeSet { get; set; }
    }
}
