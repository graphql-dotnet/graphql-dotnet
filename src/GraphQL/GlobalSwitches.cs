using System;
using System.ComponentModel;
using GraphQL.Conversion;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL
{
    /// <summary>
    /// Global options for configuring GraphQL execution.
    /// </summary>
    public static class GlobalSwitches
    {
        /// <summary>
        /// Enables or disables setting default values for 'defaultValue' from <see cref="DefaultValueAttribute"/>.
        /// By default enabled.
        /// </summary>
        public static bool EnableReadDefaultValueFromAttributes { get; set; } = true;

        /// <summary>
        /// Enables or disables setting default values for 'deprecationReason' from <see cref="ObsoleteAttribute"/>.
        /// By default enabled.
        /// </summary>
        public static bool EnableReadDeprecationReasonFromAttributes { get; set; } = true;

        /// <summary>
        /// Enables or disables setting default values for 'description' from <see cref="DescriptionAttribute"/>.
        /// By default enabled.
        /// </summary>
        public static bool EnableReadDescriptionFromAttributes { get; set; } = true;

        /// <summary>
        /// Enables or disables setting default values for 'description' from XML documentation.
        /// By default disabled.
        /// </summary>
        public static bool EnableReadDescriptionFromXmlDocumentation { get; set; } = false;

        /// <summary>
        /// Gets or sets current validation delegate. By default this delegate validates all names according
        /// to the GraphQL <see href="http://spec.graphql.org/June2018/#sec-Names">specification</see>.
        /// <br/><br/>
        /// Setting this delegate allows you to use names not conforming to the specification, for example
        /// 'enum-member'. Only change it when absolutely necessary. This is typically only overridden
        /// when implementing a custom <see cref="INameConverter"/> that fixes names, making them spec-compliant.
        /// <br/><br/>
        /// Keep in mind that regardless of this setting, names are validated upon schema initialization,
        /// after being processed by the <see cref="INameConverter"/>. This is due to the fact that the
        /// parser cannot parse incoming queries with invalid characters in the names, so the resulting
        /// member would become unusable.
        /// </summary>
        public static Action<string, NamedElement> NameValidation = NameValidator.ValidateDefault;

        /// <summary>
        /// This setting can improve performance if your schema uses only scalar types or types marked with
        /// <see cref="GraphQLMetadataAttribute"/> attribute with configured <see cref="GraphQLMetadataAttribute.InputType"/>
        /// or <see cref="GraphQLMetadataAttribute.OutputType"/> properties when constructing fields using expressions:
        /// <br/>
        /// <c>
        /// Field(x => x.Filter);
        /// </c>
        /// <br/>
        /// If you are using a scoped schema with many field expressions, then this setting will help speed up the
        /// initialization of your schema when set to <see langword="false"/>.
        /// <br/>
        /// If you are registering your own mappings via <see cref="ISchema.RegisterTypeMapping(Type, Type)"/>,
        /// then be sure to set to <see langword="true"/>. Otherwise, you will most likely see an error message when
        /// you try to add a field:
        /// <br/>
        /// <c>
        /// The GraphQL type for field 'ParentObject.Filter' could not be derived implicitly from expression 'Field(x => x.Filter)'.
        /// </c>
        /// </summary>
        public static bool UseRuntimeTypeMappings { get; set; } = true;
    }
}
