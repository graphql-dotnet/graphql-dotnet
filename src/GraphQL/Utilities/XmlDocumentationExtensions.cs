using System.Collections.Concurrent;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.XPath;

namespace GraphQL.Utilities
{
    /// <summary>
    /// Provides extension methods for reading XML comments from reflected members.
    /// </summary>
    internal static class XmlDocumentationExtensions
    {
        private static readonly ConcurrentDictionary<string, XDocument?> _cachedXml = new(StringComparer.OrdinalIgnoreCase);

        private static string GetParameterName(this ParameterInfo parameter) => GetTypeName(parameter.ParameterType);

        private static string? NullIfEmpty(this string? text) => text == string.Empty ? null : text;

        private static string GetTypeName(Type type)
        {
            if (type.IsGenericType)
            {
                string baseName = type.GetGenericTypeDefinition().ToString();
                baseName = baseName.Substring(0, baseName.IndexOf('`'));
                return $"{baseName}{{{string.Join(",", type.GetGenericArguments().Select(GetTypeName))}}}";
            }

            return type.FullName!;
        }

        /// <summary>
        /// Returns the expected name for a member element in the XML documentation file.
        /// </summary>
        /// <param name="member">The reflected member.</param>
        /// <returns>The name of the member element.</returns>
        private static string GetMemberElementName(MemberInfo member)
        {
            char prefixCode;
            string memberName = member is Type t
                ? t.FullName! // member is a Type
                : member.DeclaringType!.FullName + "." + member.Name;  // member belongs to a Type
            memberName = memberName.Replace('+', '.');

            switch (member.MemberType)
            {
                case MemberTypes.Constructor:
                    // XML documentation uses slightly different constructor names
                    memberName = memberName.Replace(".ctor", "#ctor");
                    goto case MemberTypes.Method;

                case MemberTypes.Method:
                    prefixCode = 'M';
                    // parameters are listed according to their type, not their name
                    string paramTypesList = string.Join(",", ((MethodBase)member).GetParameters().Select(x => x.GetParameterName()).ToArray());
                    if (!string.IsNullOrEmpty(paramTypesList))
                        memberName += "(" + paramTypesList + ")";
                    break;

                case MemberTypes.Event:
                    prefixCode = 'E';
                    break;

                case MemberTypes.Field:
                    prefixCode = 'F';
                    break;

                case MemberTypes.NestedType:
                    // XML documentation uses slightly different nested type names
                    goto case MemberTypes.TypeInfo;

                case MemberTypes.TypeInfo:
                    prefixCode = 'T';
                    break;

                case MemberTypes.Property:
                    prefixCode = 'P';
                    break;

                default:
                    throw new ArgumentException("Unknown member type", nameof(member));
            }

            // elements are of the form "M:Namespace.Class.Method"
            return $"{prefixCode}:{memberName}";
        }

        private static XDocument? GetDocument(Assembly asm, string pathToXmlFile)
        {
            string assemblyName = asm.GetName().FullName;
            XDocument? doc = null;

            return _cachedXml.GetOrAdd(assemblyName, key =>
            {

                try
                {
                    if (File.Exists(pathToXmlFile))
                        doc = XDocument.Load(pathToXmlFile);

                    if (doc == null)
                    {
                        string relativePath = Path.Combine(Path.GetDirectoryName(asm.Location)!, pathToXmlFile);
                        if (File.Exists(relativePath))
                            doc = XDocument.Load(relativePath);
                    }
                }
#pragma warning disable RCS1075 // Avoid empty catch clause that catches System.Exception.
                catch (Exception)
                {
                    // No logging is needed
                }
#pragma warning restore RCS1075 // Avoid empty catch clause that catches System.Exception.

                return doc;
            });
        }

        /// <summary>
        /// Returns the XML documentation (summary tag) for the specified member.
        /// </summary>
        /// <param name="member">The reflected member.</param>
        /// <returns>The contents of the summary tag for the member.</returns>
        public static string? GetXmlDocumentation(this MemberInfo member) => GetXmlDocumentation(member, member.Module.Assembly.GetName().Name + ".xml");

        /// <summary>
        /// Returns the XML documentation (summary tag) for the specified member.
        /// </summary>
        /// <param name="member">The reflected member.</param>
        /// <param name="pathToXmlFile">Path to the XML documentation file.</param>
        /// <returns>The contents of the summary tag for the member.</returns>
        public static string? GetXmlDocumentation(this MemberInfo member, string pathToXmlFile) => GetXmlDocumentation(member, GetDocument(member.Module.Assembly, pathToXmlFile));

        /// <summary>
        /// Returns the XML documentation (summary tag) for the specified member.
        /// </summary>
        /// <param name="member">The reflected member.</param>
        /// <param name="xml">XML documentation.</param>
        /// <returns>The contents of the summary tag for the member.</returns>
        public static string? GetXmlDocumentation(this MemberInfo member, XDocument? xml) => xml?.XPathEvaluate(
            $"string(/doc/members/member[@name='{GetMemberElementName(member)}']/summary)").ToString()!.Trim().NullIfEmpty();

        /// <summary>
        /// Returns the XML documentation (returns/param tag) for the specified parameter.
        /// </summary>
        /// <param name="parameter">The reflected parameter (or return value).</param>
        /// <returns>The contents of the returns/param tag for the parameter.</returns>
        public static string? GetXmlDocumentation(this ParameterInfo parameter) => GetXmlDocumentation(parameter, parameter.Member.Module.Assembly.GetName().Name + ".xml");

        /// <summary>
        /// Returns the XML documentation (returns/param tag) for the specified parameter.
        /// </summary>
        /// <param name="parameter">The reflected parameter (or return value).</param>
        /// <param name="pathToXmlFile">Path to the XML documentation file.</param>
        /// <returns>The contents of the returns/param tag for the parameter.</returns>
        public static string? GetXmlDocumentation(this ParameterInfo parameter, string pathToXmlFile) => GetXmlDocumentation(parameter, GetDocument(parameter.Member.Module.Assembly, pathToXmlFile));

        /// <summary>
        /// Returns the XML documentation (returns/param tag) for the specified parameter.
        /// </summary>
        /// <param name="parameter">The reflected parameter (or return value).</param>
        /// <param name="xml">XML documentation.</param>
        /// <returns>The contents of the returns/param tag for the parameter.</returns>
        public static string? GetXmlDocumentation(this ParameterInfo parameter, XDocument? xml) =>
            parameter.IsRetval || string.IsNullOrEmpty(parameter.Name)
                ? xml?.XPathEvaluate(
                    $"string(/doc/members/member[@name='{GetMemberElementName(parameter.Member)}']/returns)").ToString()!.Trim().NullIfEmpty()
                : xml?.XPathEvaluate(
                    $"string(/doc/members/member[@name='{GetMemberElementName(parameter.Member)}']/param[@name='{parameter.Name}'])").ToString()!.Trim().NullIfEmpty();
    }
}
