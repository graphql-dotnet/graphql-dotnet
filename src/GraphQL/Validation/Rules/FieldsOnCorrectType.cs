using System.Collections.Generic;
using System.Linq;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.Validation.Rules
{
  /// <summary>
  /// Fields on correct type
  ///
  /// A GraphQL document is only valid if all fields selected are defined by the
  /// parent type, or are an allowed meta field such as __typename
  /// </summary>
  public class FieldsOnCorrectType : IValidationRule
    {
        public string UndefinedFieldMessage(
            string fieldName,
            string type,
            IEnumerable<string> suggestedTypeNames,
            IEnumerable<string> suggestedFieldNames)
        {
            var message = $"Cannot query field \"{fieldName}\" on type \"{type}\".";

            if (suggestedTypeNames != null && suggestedTypeNames.Any())
            {
                var suggestions = StringUtils.QuotedOrList(suggestedTypeNames);
                message += $" Did you mean to use an inline fragment on {suggestions}?";
            }
            else if (suggestedFieldNames != null && suggestedFieldNames.Any())
            {
                message += $" Did you mean {StringUtils.QuotedOrList(suggestedFieldNames)}?";
            }

            return message;
        }

        public INodeVisitor Validate(ValidationContext context)
        {
            return new EnterLeaveListener(_ =>
            {
                _.Match<Field>(node =>
                {
                    var type = context.TypeInfo.GetParentType().GetNamedType();

                    if (type != null)
                    {
                        var fieldDef = context.TypeInfo.GetFieldDef();
                        if (fieldDef == null)
                        {
                            // This field doesn't exist, lets look for suggestions.
                            var fieldName = node.Name;

                            // First determine if there are any suggested types to condition on.
                            var suggestedTypeNames = getSuggestedTypeNames(context.Schema, type, fieldName).ToList();

                            // If there are no suggested types, then perhaps this was a typo?
                            var suggestedFieldNames = suggestedTypeNames.Any()
                                ? new string[] {}
                                : getSuggestedFieldNames(type, fieldName);

                            // Retport an error, including helpful suggestions.
                            context.ReportError(new ValidationError(
                                context.OriginalQuery,
                                "5.2.1",
                                UndefinedFieldMessage(fieldName, type.Name, suggestedTypeNames, suggestedFieldNames),
                                node
                                ));
                        }
                    }
                });
            });
        }
        /// <summary>
        /// Go through all of the implementations of type, as well as the interfaces
        /// that they implement. If any of those types include the provided field,
        /// suggest them, sorted by how often the type is referenced,  starting
        /// with Interfaces.
        /// </summary>
        private IEnumerable<string> getSuggestedTypeNames(
          ISchema schema,
          IGraphType type,
          string fieldName)
        {
            if (type is IAbstractGraphType)
            {
                var suggestedObjectTypes = new List<string>();
                var interfaceUsageCount = new LightweightCache<string, int>(key => 0);

                var absType = type as IAbstractGraphType;
                absType.PossibleTypes.Apply(possibleType =>
                {
                    if (!possibleType.HasField(fieldName))
                    {
                        return;
                    }

                    // This object defines this field.
                    suggestedObjectTypes.Add(possibleType.Name);

                    possibleType.ResolvedInterfaces.Apply(possibleInterface =>
                    {
                        if (possibleInterface.HasField(fieldName))
                        {
                            // This interface type defines this field.
                            interfaceUsageCount[possibleInterface.Name] = interfaceUsageCount[possibleInterface.Name] + 1;
                        }
                    });
                });

                var suggestedInterfaceTypes = interfaceUsageCount.Keys.OrderBy(x => interfaceUsageCount[x]);
                return suggestedInterfaceTypes.Concat(suggestedObjectTypes);
            }

            return Enumerable.Empty<string>();
        }

        /// <summary>
        /// For the field name provided, determine if there are any similar field names
        /// that may be the result of a typo.
        /// </summary>
        private IEnumerable<string> getSuggestedFieldNames(
          IGraphType type,
          string fieldName)
        {
            if (type is IComplexGraphType)
            {
                var complexType = type as IComplexGraphType;
                return StringUtils.SuggestionList(fieldName, complexType.Fields.Select(x => x.Name));
            }

            return Enumerable.Empty<string>();
        }
    }
}
