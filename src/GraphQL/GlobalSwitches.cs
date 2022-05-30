using System.ComponentModel;
using GraphQL.Conversion;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL
{
    // decorate any test classes which contain tests that modify these global switches with
    //   [Collection("StaticTests")]
    // these tests will run sequentially after all other tests are complete
    // be sure to restore the global switch to its initial state with a finally block

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

        /// <summary>
        /// Specifies whether to map all types that implement <see cref="IEnumerable{T}"/> to list
        /// graph types within <see cref="TypeExtensions.GetGraphTypeFromType(Type, bool, TypeMappingMode)"/>,
        /// or only common collection types such as <see cref="IEnumerable{T}"/>, <see cref="IList{T}"/>,
        /// <see cref="List{T}"/>, <see cref="IReadOnlyCollection{T}"/> and similar. <see cref="HashSet{T}"/>
        /// and <see cref="ISet{T}"/> are also supported, but not dictionary types.
        /// <br/><br/>
        /// When set to <see langword="true"/>, dictionaries are also detected as lists of
        /// <see cref="KeyValuePair{TKey, TValue}"/> which may be unintended.
        /// <br/><br/>
        /// When set to <see langword="false"/>, custom collection types such as <see cref="TypeFields"/>
        /// would not be detected and would need to be cast to the correct <see cref="IEnumerable{T}"/> type
        /// when defining the field so that <see cref="GraphQLClrOutputTypeReference{T}"/> is generated
        /// for the field type with the proper <see cref="IEnumerable{T}"/> type as its generic type.
        /// </summary>
        public static bool MapAllEnumerableTypes { get; set; } = true;
    }
}
