using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GraphQL.Reflection
{
    /// <summary>
    /// Provides extension methods to <see cref="ParameterInfo"/> and <see cref="PropertyInfo"/> instances to read nullable type reference annotations.
    /// </summary>
    public static class ReflectionExtensions
    {
        /// <summary>
        /// Examines the parameter type and walks the type definitions, for each returning the nullability information for the type.
        /// <para>
        /// For instance, for Test&lt;string, int&gt;, first returns nullability for Test&lt;string, int&gt;, then string, then int.
        /// </para>
        /// <para>
        /// See <seealso href="https://github.com/dotnet/roslyn/blob/main/docs/features/nullable-metadata.md"/>.
        /// </para>
        /// </summary>
        public static IEnumerable<(Type Type, Nullability Nullable)> GetNullabilityInformation(this ParameterInfo parameter)
        {
            if (parameter == null)
                throw new ArgumentNullException(nameof(parameter));

            var (nullableContext, nullabilityBytes) = GetMethodParameterNullability(parameter);
            var list = new List<(Type, Nullability)>();
            var index = 0;
            Consider(parameter.ParameterType, false, list, nullabilityBytes, nullableContext, ref index);
            if (nullabilityBytes != null && nullabilityBytes.Count != index)
                throw new NullabilityInterpretationException(parameter);
            return list;
        }

        /// <summary>
        /// Examines the property type and walks the type definitions, for each returning the nullability information for the type.
        /// <para>
        /// For instance, for Test&lt;string, int&gt;, first returns nullability for Test&lt;string, int&gt;, then string, then int.
        /// </para>
        /// <para>
        /// See <seealso href="https://github.com/dotnet/roslyn/blob/main/docs/features/nullable-metadata.md"/>.
        /// </para>
        /// </summary>
        public static IEnumerable<(Type Type, Nullability Nullable)> GetNullabilityInformation(this PropertyInfo property)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            var (nullableContext, nullabilityBytes) = GetMemberNullability(property);
            var list = new List<(Type, Nullability)>();
            var index = 0;
            Consider(property.PropertyType, false, list, nullabilityBytes, nullableContext, ref index);
            if (nullabilityBytes != null && nullabilityBytes.Count != index)
                throw new NullabilityInterpretationException(property);
            return list;
        }

        /// <summary>
        /// Returns the default NRT annotation for the parameter, and a list of the NRT annotations for the type.
        /// </summary>
        private static (Nullability, IList<CustomAttributeTypedArgument>?) GetMethodParameterNullability(ParameterInfo parameter)
        {
            var nullableContext = GetMethodDefaultNullability(parameter.Member);

            return ExamineMemberAttributes(nullableContext, parameter.CustomAttributes)
                ?? throw new NullabilityInterpretationException(parameter);
        }

        /// <summary>
        /// Returns the default NRT annotation for the member, and a list of the NRT annotations for the type.
        /// </summary>
        private static (Nullability, IList<CustomAttributeTypedArgument>?) GetMemberNullability(MemberInfo member)
        {
            var nullableContext = GetMethodDefaultNullability(member);

            return ExamineMemberAttributes(nullableContext, member.CustomAttributes)
                ?? throw new NullabilityInterpretationException(member);
        }

        /// <summary>
        /// Returns the default NRT annotation for the member.
        /// </summary>
        private static Nullability GetMethodDefaultNullability(MemberInfo method)
        {
            var nullable = Nullability.Unknown;

            // check the parent type first to see if there's a nullable context attribute set for it
            CheckDeclaringType(method.DeclaringType);

            // now check the method to see if there's a nullable context attribute set for it
            var attribute = method.CustomAttributes.FirstOrDefault(x =>
                x.AttributeType.FullName == "System.Runtime.CompilerServices.NullableContextAttribute" &&
                x.ConstructorArguments.Count == 1 &&
                x.ConstructorArguments[0].ArgumentType == typeof(byte));
            if (attribute != null)
            {
                nullable = (Nullability)(byte)attribute.ConstructorArguments[0].Value;
            }

            return nullable;

            void CheckDeclaringType(Type parentType)
            {
                if (parentType.DeclaringType != null)
                    CheckDeclaringType(parentType.DeclaringType);
                var attribute = parentType.CustomAttributes.FirstOrDefault(x =>
                    x.AttributeType.FullName == "System.Runtime.CompilerServices.NullableContextAttribute" &&
                    x.ConstructorArguments.Count == 1 &&
                    x.ConstructorArguments[0].ArgumentType == typeof(byte));
                if (attribute != null)
                {
                    nullable = (Nullability)(byte)attribute.ConstructorArguments[0].Value;
                }
            }
        }

        /// <summary>
        /// Returns the default NRT annotation for the member/parameter along with the list of NRT annotations for the
        /// member/parameter if applicable. Returns <see langword="null"/> if the NRT annotations cannot be parsed.
        /// </summary>
        private static (Nullability, IList<CustomAttributeTypedArgument>?)? ExamineMemberAttributes(Nullability nullableContext, IEnumerable<CustomAttributeData> customAttributes)
        {
            foreach (var attribute in customAttributes)
            {
                if (attribute.AttributeType.FullName == "System.Runtime.CompilerServices.NullableAttribute" && attribute.ConstructorArguments.Count == 1)
                {
                    var argType = attribute.ConstructorArguments[0].ArgumentType;
                    if (argType == typeof(byte))
                    {
                        return ((Nullability)(byte)attribute.ConstructorArguments[0].Value, null);
                    }
                    else if (argType == typeof(byte[]))
                    {
                        return (nullableContext, (IList<CustomAttributeTypedArgument>)attribute.ConstructorArguments[0].Value!);
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            return (nullableContext, null);
        }

        /// <summary>
        /// Walks the type definition and adds each type and its NRT annotation to the specified list
        /// </summary>
        private static void Consider(Type type, bool nullableValueType, List<(Type, Nullability)> list, IList<CustomAttributeTypedArgument>? nullabilityBytes, Nullability nullableContext, ref int index)
        {
            if (type.IsValueType)
            {
                if (type.IsGenericType)
                {
                    if (type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        //do not increment index for Nullable<T>
                        //do not add Nullable<T> to the list but rather just add underlying type
                        Consider(type.GenericTypeArguments[0], true, list, nullabilityBytes, nullableContext, ref index);
                    }
                    else
                    {
                        //generic structs that are not Nullable<T> will contain a 0 in the array
                        index++;
                        list.Add((type, nullableValueType ? Nullability.Nullable : Nullability.NonNullable));
                        for (int i = 0; i < type.GenericTypeArguments.Length; i++)
                        {
                            Consider(type.GenericTypeArguments[i], false, list, nullabilityBytes, nullableContext, ref index);
                        }
                    }
                }
                else
                {
                    //do not increment index for concrete value types
                    list.Add((type, nullableValueType ? Nullability.Nullable : Nullability.NonNullable));
                }
            }
            else
            {
                //pull the nullability flag from the array and increment index
                var nullable = nullabilityBytes != null ? (Nullability)(byte)nullabilityBytes[index++].Value : nullableContext;
                list.Add((type, nullable));

                if (type.IsArray)
                {
                    Consider(type.GetElementType(), false, list, nullabilityBytes, nullableContext, ref index);
                }
                else if (type.IsGenericType)
                {
                    for (int i = 0; i < type.GenericTypeArguments.Length; i++)
                    {
                        Consider(type.GenericTypeArguments[i], false, list, nullabilityBytes, nullableContext, ref index);
                    }
                }
            }
        }
    }
}
