using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQL.Utilities;
using GraphQL.Validation.Errors;

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
        public static readonly FieldsOnCorrectType Instance = new FieldsOnCorrectType();

        public Task<INodeVisitor> ValidateAsync(ValidationContext context)
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
                            var suggestedTypeNames = GetSuggestedTypeNames(context.Schema, type, fieldName).ToList();

                            // If there are no suggested types, then perhaps this was a typo?
                            var suggestedFieldNames = suggestedTypeNames.Count > 0
                                ? Array.Empty<string>()
                                : GetSuggestedFieldNames(type, fieldName);

                            // Report an error, including helpful suggestions.
                            context.ReportError(new FieldsOnCorrectTypeError(context, node, type, suggestedTypeNames, suggestedFieldNames));
                        }
                    }
                });
            }).ToTask();
        }

        /// <summary>
        /// Go through all of the implementations of type, as well as the interfaces
        /// that they implement. If any of those types include the provided field,
        /// suggest them, sorted by how often the type is referenced,  starting
        /// with Interfaces.
        /// </summary>
        private IEnumerable<string> GetSuggestedTypeNames(
          ISchema schema,
          IGraphType type,
          string fieldName)
        {
            if (type is IAbstractGraphType absType)
            {
                var suggestedObjectTypes = new List<string>();
                var interfaceUsageCount = new LightweightCache<string, int>(key => 0);

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
        private IEnumerable<string> GetSuggestedFieldNames(
          IGraphType type,
          string fieldName)
        {
            if (type is IComplexGraphType complexType)
            {
                return StringUtils.SuggestionList(fieldName, complexType.Fields.Select(x => x.Name));
            }

            return Enumerable.Empty<string>();
        }
    }
}
