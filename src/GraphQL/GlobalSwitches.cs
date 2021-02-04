using System;
using System.ComponentModel;
using GraphQL.Conversion;
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
        /// </summary>
        public static Action<string, NameType> Validation = NameValidator.ValidateDefault;

        /// <summary>
        /// Gets or sets current validation delegate during schema initialization. By default this delegate
        /// validates all names according to the GraphQL <see href="http://spec.graphql.org/June2018/#sec-Names">specification</see>.
        /// <br/><br/>
        /// Setting this delegate allows you to use names not conforming to the specification, for example
        /// 'enum-member'. Only change it when absolutely necessary. Keep in mind that the parser cannot
        /// parse incoming queries with invalid characters in the names, so it is likely that the resulting
        /// member will be unusable.
        /// </summary>
        public static Action<string, NameType> ValidationOnSchemaInitialize = NameValidator.ValidateDefault;
    }
}
