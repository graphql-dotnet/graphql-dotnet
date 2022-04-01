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
        /// <br/>
        /// By default enabled.
        /// </summary>
        public static bool EnableReadDefaultValueFromAttributes { get; set; } = true;

        /// <summary>
        /// Enables or disables setting default values for 'deprecationReason' from <see cref="ObsoleteAttribute"/>.
        /// <br/>
        /// By default enabled.
        /// </summary>
        public static bool EnableReadDeprecationReasonFromAttributes { get; set; } = true;

        /// <summary>
        /// Enables or disables setting default values for 'description' from <see cref="DescriptionAttribute"/>.
        /// <br/>
        /// By default enabled.
        /// </summary>
        public static bool EnableReadDescriptionFromAttributes { get; set; } = true;

        /// <summary>
        /// Enables or disables setting default values for 'description' from XML documentation.
        /// <br/>
        /// By default disabled.
        /// </summary>
        public static bool EnableReadDescriptionFromXmlDocumentation { get; set; } = false;

        /// <summary>
        /// Gets or sets current validation delegate. By default this delegate validates all names according
        /// to the GraphQL <see href="https://spec.graphql.org/October2021/#sec-Names">specification</see>.
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
        /// Specifies whether to use the names of parent (declaring) types in case of nested graph types
        /// when calculating default graph type name.
        /// <br/>
        /// By default disabled.
        /// </summary>
        public static bool UseDeclaringTypeNames { get; set; } = false;
    }
}
