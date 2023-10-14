using GraphQLParser.AST;
using static GraphQL.Validation.Rules.OverlappingFieldsCanBeMerged;

namespace GraphQL.Validation.Errors
{
    /// <inheritdoc cref="Rules.OverlappingFieldsCanBeMerged"/>
    [Serializable]
    public class OverlappingFieldsCanBeMergedError : ValidationError
    {
        internal const string NUMBER = "5.3.2";

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public OverlappingFieldsCanBeMergedError(ValidationContext context, Conflict conflict)
            : base(context.Document.Source, NUMBER, FieldsConflictMessage(conflict.Reason.Name, conflict.Reason),
                  conflict.FieldsLeft.Concat(conflict.FieldsRight).Cast<ASTNode>().ToArray())
        {
        }

        internal static string FieldsConflictMessage(string responseName, ConflictReason reason) =>
            $"Fields {responseName} conflicts because {ReasonMessage(reason.Message)}. " +
            "Use different aliases on the fields to fetch both if this was intentional.";

        private static string ReasonMessage(Message reasonMessage)
        {
            if (reasonMessage.Msgs?.Count > 0)
            {
                return string.Join(
                    " and ",
                    reasonMessage.Msgs.Select(x => $"subfields '{x.Name}' conflict because {ReasonMessage(x.Message)}").ToArray()
                );
            }
            else
            {
                return reasonMessage.Msg!;
            }
        }
    }
}
