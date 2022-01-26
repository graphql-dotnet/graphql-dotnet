using System;
using System.Collections.Generic;
using System.Reflection;
using GraphQL.Types;

namespace GraphQL
{
    /// <summary>
    /// Contains type and nullability information for a method return type or argument type.
    /// </summary>
    public sealed class TypeInformation
    {
        private Type? _graphType;

        /// <summary>
        /// The member being inspected.
        /// </summary>
        public MemberInfo MemberInfo { get; }

        /// <summary>
        /// Indicates that this is an input type (an argument); false for output types.
        /// </summary>
        public bool IsInputType { get; }

        /// <summary>
        /// The underlying CLR type represented. This might be the underlying type of a <see cref="Nullable{T}"/>
        /// or the underlying type of a <see cref="IEnumerable{T}"/>.
        /// </summary>
        public Type Type { get; set; }

        /// <summary>
        /// Indicates if the underlying type is nullable.
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// Indicates that this represents a list of elements.
        /// </summary>
        public bool IsList { get; set; }

        /// <summary>
        /// Indicates if the list is nullable.
        /// </summary>
        public bool ListIsNullable { get; set; }

        /// <summary>
        /// The graph type of the underlying CLR type or <see langword="null"/> to detect the graph type automatically.
        /// </summary>
        public Type? GraphType
        {
            get => _graphType;
            set
            {
                if (value == null)
                {
                    _graphType = null;
                    return;
                }
                if (IsInputType)
                {
                    if (!value.IsInputType())
                    {
                        throw new ArgumentOutOfRangeException(nameof(value), "Value can only be an input graph type.");
                    }
                }
                else if (!value.IsOutputType())
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Value can only be an output graph type.");
                }
                if (!value.IsNamedType())
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Value must be a named type.");
                }
                _graphType = value;
            }
        }

        /// <summary>
        /// Initializes an instance with the specified properties.
        /// </summary>
        /// <param name="memberInfo">The member being inspected.</param>
        /// <param name="isInputType">Indicates that this is an input type (an argument); false for output types.</param>
        /// <param name="type">The underlying type.</param>
        /// <param name="isNullable">Indicates that the underlying type is nullable.</param>
        /// <param name="isList">Indicates that this represents a list of elements.</param>
        /// <param name="listIsNullable">Indicates that the list is nullable.</param>
        /// <param name="graphType">The graph type of the underlying CLR type; null if not specified.</param>
        public TypeInformation(MemberInfo memberInfo, bool isInputType, Type type, bool isNullable, bool isList, bool listIsNullable, Type? graphType)
        {
            MemberInfo = memberInfo;
            IsInputType = isInputType;
            Type = type;
            IsNullable = isNullable;
            IsList = isList;
            ListIsNullable = listIsNullable;
            GraphType = graphType;
        }


        /// <summary>
        /// Analyzes a property and returns a <see cref="TypeInformation"/>
        /// struct containing type information necessary to select a graph type.
        /// </summary>
        public static TypeInformation GetTypeInformation(PropertyInfo propertyInfo, bool isInputProperty)
        {
            var isList = false;
            var isNullableList = false;
            foreach (var type in typeTree)
            {
                if (type.Type.IsArray)
                {
                    //unwrap type and mark as list
                    isList = true;
                    isNullableList = type.Nullable != Nullability.NonNullable;
                    continue;
                }
                if (type.Type.IsGenericType)
                {
                    var g = type.Type.GetGenericTypeDefinition();
                    if (_listTypes.Contains(g))
                    {
                        //unwrap type and mark as list
                        isList = true;
                        isNullableList = type.Nullable != Nullability.NonNullable;
                        continue;
                    }
                }
                if (type.Type == typeof(IEnumerable) || type.Type == typeof(ICollection))
                {
                    //assume list of nullable object
                    isList = true;
                    isNullableList = type.Nullable != Nullability.NonNullable;
                    break;
                }
                //found match
                var nullable = type.Nullable != Nullability.NonNullable;
                return new TypeInformation(propertyInfo, isInputProperty, type.Type, nullable, isList, isNullableList, null);
            }
            //unknown type
            return new TypeInformation(propertyInfo, isInputProperty, typeof(object), true, isList, isNullableList, null);
        }


        /// <summary>
        /// Returns a graph type constructed based on the properties set within this instance.
        /// If <see cref="GraphType"/> is <see langword="null"/>, the graph type is generated via
        /// <see cref="TypeExtensions.GetGraphTypeFromType(Type, bool, TypeMappingMode)"/>.
        /// The graph type is then wrapped with <see cref="NonNullGraphType{T}"/> and/or
        /// <see cref="ListGraphType{T}"/> as appropriate.
        /// </summary>
        public Type GetConstructedGraphType()
        {
            var t = GraphType;
            if (t != null)
            {
                if (!IsNullable)
                    t = typeof(NonNullGraphType<>).MakeGenericType(t);
            }
            else
            {
                t = Type.GetGraphTypeFromType(IsNullable, IsInputType ? TypeMappingMode.InputType : TypeMappingMode.OutputType);
            }
            if (IsList)
            {
                t = typeof(ListGraphType<>).MakeGenericType(t);
                if (!ListIsNullable)
                    t = typeof(NonNullGraphType<>).MakeGenericType(t);
            }
            return t;
        }

        /// <summary>
        /// Returns a new instance with <see cref="RequiredAttribute"/>, <see cref="OptionalAttribute"/>, <see cref="RequiredListAttribute"/>,
        /// <see cref="OptionalListAttribute"/>, <see cref="IdAttribute"/> and <see cref="DIGraphAttribute"/>
        /// applied as necessary.
        /// </summary>
        internal TypeInformation ApplyAttributes(ICustomAttributeProvider member)
        {
            var typeInformation = this; //copy struct
            //var member = examineParent ? (ICustomAttributeProvider)typeInformation.ParameterInfo.Member : typeInformation.ParameterInfo;
            if (typeInformation.IsNullable)
            {
                if (member.IsDefined(typeof(System.ComponentModel.DataAnnotations.RequiredAttribute), false))
                    typeInformation.IsNullable = false;
            }
            return typeInformation;
        }

    }
}
