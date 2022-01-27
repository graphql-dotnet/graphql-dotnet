using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using GraphQL.Types;

namespace GraphQL.Types
{
    /// <summary>
    /// Contains type and nullability information for a method return type or argument type.
    /// </summary>
    public class TypeInformation
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

        private static readonly Type[] _listTypes = new Type[] {
            typeof(IEnumerable<>),
            typeof(IList<>),
            typeof(List<>),
            typeof(ICollection<>),
            typeof(IReadOnlyCollection<>),
            typeof(IReadOnlyList<>),
            typeof(HashSet<>),
            typeof(ISet<>),
        };

        /// <summary>
        /// Initializes an instance containing type information necessary to select a graph type.
        /// The instance is populated based on inspecting the type and NRT annotations on the specified property.
        /// </summary>
        public TypeInformation(PropertyInfo propertyInfo, bool isInputProperty)
        {
            MemberInfo = propertyInfo;
            IsInputType = isInputProperty;

            var typeTree = Interpret(new NullabilityInfoContext().Create(propertyInfo), isInputProperty);

            foreach (var type in typeTree)
            {
                //detect list types, but not lists of lists
                if (!IsList)
                {
                    if (type.Type.IsArray)
                    {
                        //unwrap type and mark as list
                        IsList = true;
                        ListIsNullable = type.Nullable != NullabilityState.NotNull;
                        continue;
                    }
                    if (type.Type.IsGenericType)
                    {
                        var g = type.Type.GetGenericTypeDefinition();
                        if (Array.IndexOf(_listTypes, g) >= 0)
                        {
                            //unwrap type and mark as list
                            IsList = true;
                            ListIsNullable = type.Nullable != NullabilityState.NotNull;
                            continue;
                        }
                    }
                    if (type.Type == typeof(IEnumerable) || type.Type == typeof(ICollection))
                    {
                        //assume list of nullable object
                        IsList = true;
                        ListIsNullable = type.Nullable != NullabilityState.NotNull;
                        break;
                    }
                }
                //found match
                IsNullable = type.Nullable != NullabilityState.NotNull;
                Type = type.Type;
                return;
            }
            //unknown type
            IsNullable = true;
            Type = typeof(object);
        }

        /// <summary>
        /// Flattens a complex <see cref="NullabilityInfo"/> structure into a list types and nullability flags.
        /// <see cref="Nullable{T}"/> structs return their underlying type rather than <see cref="Nullable{T}"/>.
        /// </summary>
        private static IEnumerable<(Type Type, NullabilityState Nullable)> Interpret(NullabilityInfo info, bool isInputProperty)
        {
            var list = new List<(Type, NullabilityState)>(info.GenericTypeArguments.Length + 1);
            RecursiveLoop(info);
            return list;

            void RecursiveLoop(NullabilityInfo info)
            {
                if (info.Type.IsGenericType)
                {
                    var nullableType = Nullable.GetUnderlyingType(info.Type);
                    if (nullableType != null)
                    {
                        list.Add((nullableType, NullabilityState.Nullable));
                    }
                    else
                    {
                        list.Add((info.Type, isInputProperty ? info.ReadState : info.WriteState));
                    }
                    foreach (var t in info.GenericTypeArguments)
                    {
                        RecursiveLoop(t);
                    }
                }
                else if (info.ElementType != null)
                {
                    list.Add((info.Type, isInputProperty ? info.ReadState : info.WriteState));
                    RecursiveLoop(info.ElementType);
                }
                else
                {
                    list.Add((info.Type, isInputProperty ? info.ReadState : info.WriteState));
                }
            }
        }

        /// <summary>
        /// Returns a graph type constructed based on the properties set within this instance.
        /// If <see cref="GraphType"/> is <see langword="null"/>, the graph type is generated via
        /// <see cref="TypeExtensions.GetGraphTypeFromType(Type, bool, TypeMappingMode)"/>.
        /// The graph type is then wrapped with <see cref="NonNullGraphType{T}"/> and/or
        /// <see cref="ListGraphType{T}"/> as appropriate.
        /// </summary>
        public virtual Type ConstructGraphType()
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
        /// Applies <see cref="GraphQLAttribute"/> attributes for the specified member to this instance.
        /// </summary>
        public virtual void ApplyAttributes()
        {
            if (MemberInfo.IsDefined(typeof(System.ComponentModel.DataAnnotations.RequiredAttribute), false))
            {
                if (IsList)
                {
                    ListIsNullable = false;
                }
                else
                {
                    IsNullable = false;
                }
            }

            var attributes = MemberInfo.GetCustomAttributes(typeof(GraphQLAttribute), false);
            foreach (var attr in attributes)
            {
                ((GraphQLAttribute)attr).Modify(this);
            }
        }
    }
}
