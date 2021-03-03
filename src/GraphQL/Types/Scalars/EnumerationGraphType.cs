using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using GraphQL.Language.AST;
using GraphQL.Utilities;

namespace GraphQL.Types
{
    /// <summary>
    /// Also called Enums, enumeration types are a special kind of scalar that is restricted to a
    /// particular set of allowed values. This allows you to:
    /// 1. Validate that any arguments of this type are one of the allowed values.
    /// 2. Communicate through the type system that a field will always be one of a finite set of values.
    /// </summary>
    public class EnumerationGraphType : ScalarGraphType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnumerationGraphType"/> class.
        /// </summary>
        public EnumerationGraphType()
        {
            Values = new EnumValues();
        }

        /// <summary>
        /// Adds a value to the allowed set of enumeration values.
        /// </summary>
        /// <param name="name">The name of the enumeration member, as exposed through the GraphQL endpoint (e.g. "RED").</param>
        /// <param name="description">A description of the enumeration member.</param>
        /// <param name="value">The value of the enumeration member, as referenced by the code (e.g. <see cref="ConsoleColor.Red"/>).</param>
        /// <param name="deprecationReason">The reason this enumeration member has been deprecated; <see langword="null"/> if this member has not been deprecated.</param>
        public void AddValue(string name, string description, object value, string deprecationReason = null)
        {
            AddValue(new EnumValueDefinition
            {
                Name = name,
                Description = description,
                Value = value,
                DeprecationReason = deprecationReason
            });
        }

        /// <summary>
        /// Adds a value to the allowed set of enumeration values.
        /// </summary>
        public void AddValue(EnumValueDefinition value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            NameValidator.ValidateName(value.Name, NamedElement.EnumValue);
            Values.Add(value);
        }

        /// <summary>
        /// Returns the allowed set of enumeration values.
        /// </summary>
        public EnumValues Values { get; }

        /// <inheritdoc/>
        public override object Serialize(object value)
        {
            var valueString = value.ToString(); //TODO: find only by value?
            var foundByName = Values.FindByName(valueString);
            if (foundByName != null)
            {
                return foundByName.Name;
            }

            var foundByValue = Values.FindByValue(value);
            return foundByValue?.Name;
        }

        /// <inheritdoc/>
        public override object ParseLiteral(IValue value) => value is EnumValue enumValue ? ParseValue(enumValue.Name) : null;

        /// <inheritdoc/>
        public override object ParseValue(object value)
        {
            if (value == null)
            {
                return null;
            }

            var found = Values.FindByName(value.ToString());
            return found?.Value;
        }

        /// <inheritdoc/>
        public override IValue ToAST(object value)
        {
            var serialized = (string)Serialize(value);
            return serialized != null ? new EnumValue(serialized) : null;
        }
    }

    /// <summary>
    /// Allows you to automatically register the necessary enumeration members for the specified enum.
    /// Supports <see cref="DescriptionAttribute"/> and <see cref="ObsoleteAttribute"/>.
    /// Also it can get descriptions for enum fields from the XML comments.
    /// </summary>
    /// <typeparam name="TEnum"> The enum to take values from. </typeparam>
    public class EnumerationGraphType<TEnum> : EnumerationGraphType where TEnum : Enum
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnumerationGraphType"/> class.
        /// </summary>
        public EnumerationGraphType()
        {
            var type = typeof(TEnum);
            var names = Enum.GetNames(type);
            var enumMembers = names.Select(n => (name: n, member: type
                    .GetMember(n, BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .First()));
            var enumGraphData = enumMembers.Select(e => (
                name: ChangeEnumCase(e.name),
                value: Enum.Parse(type, e.name),
                description: e.member.Description(),
                deprecation: e.member.ObsoleteMessage()
            ));

            Name = type.Name.ToPascalCase();
            Description ??= typeof(TEnum).Description();
            DeprecationReason ??= typeof(TEnum).ObsoleteMessage();

            foreach (var (name, value, description, deprecation) in enumGraphData)
            {
                AddValue(name, description, value, deprecation);
            }
        }

        /// <summary>
        /// Changes the case of the specified enum name. By default changes it to constant case (uppercase, using underscores to separate words).
        /// </summary>
        protected virtual string ChangeEnumCase(string val) => val.ToConstantCase();
    }

    /// <summary>
    /// A class that represents a set of enumeration definitions.
    /// </summary>
    public class EnumValues : IEnumerable<EnumValueDefinition>
    {
        internal List<EnumValueDefinition> List { get; } = new List<EnumValueDefinition>();

        /// <summary>
        /// Returns an enumeration definition for the specified name.
        /// </summary>
        public EnumValueDefinition this[string name] => FindByName(name);

        /// <summary>
        /// Gets the count of enumeration definitions.
        /// </summary>
        public int Count => List.Count;

        /// <summary>
        /// Adds an enumeration definition to the set.
        /// </summary>
        /// <param name="value"></param>
        public void Add(EnumValueDefinition value) => List.Add(value ?? throw new ArgumentNullException(nameof(value)));

        /// <summary>
        /// Returns an enumeration definition for the specified name.
        /// </summary>
        public EnumValueDefinition FindByName(string name, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            // DO NOT USE LINQ ON HOT PATH
            foreach (var def in List)
            {
                if (def.Name.Equals(name, comparison))
                    return def;
            }

            return null;
        }

        /// <summary>
        /// Returns an enumeration definition for the specified name.
        /// </summary>
        internal EnumValueDefinition FindByName(ReadOnlySpan<char> name)
        {
            // DO NOT USE LINQ ON HOT PATH
            foreach (var def in List)
            {
                if (name.SequenceEqual(def.Name.AsSpan()))
                    return def;
            }

            return null;
        }

        /// <summary>
        /// Returns an enumeration definition for the specified value.
        /// </summary>
        public EnumValueDefinition FindByValue(object value)
        {
            if (value is Enum)
            {
                value = Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType())); //TODO: allocation, move work with enums into new generic class
            }

            // DO NOT USE LINQ ON HOT PATH
            foreach (var def in List)
            {
                if (def.UnderlyingValue.Equals(value))
                    return def;
            }

            return null;
        }

        /// <inheritdoc cref="IEnumerable.GetEnumerator"/>
        public IEnumerator<EnumValueDefinition> GetEnumerator() => List.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// A class that represents an enumeration definition.
    /// </summary>
    [DebuggerDisplay("{Name}: {Value}")]
    public class EnumValueDefinition : MetadataProvider, IProvideDescription, IProvideDeprecationReason
    {
        /// <summary>
        /// The name of the enumeration member, as exposed through the GraphQL endpoint (e.g. "RED").
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// A description of the enumeration member.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The reason this enumeration member has been deprecated; <see langword="null"/> if this member has not been deprecated.
        /// </summary>
        public string DeprecationReason { get; set; }

        private object _value;
        /// <summary>
        /// The value of the enumeration member, as referenced by the code (e.g. <see cref="ConsoleColor.Red"/>).
        /// </summary>
        public object Value
        {
            get => _value;
            set
            {
                _value = value;
                if (value is Enum)
                    value = Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType()));
                UnderlyingValue = value;
            }
        }

        /// <summary>
        /// When mapped to a member of an <see cref="Enum"/>, contains the underlying enumeration value; otherwise contains <see cref="Value" />.
        /// </summary>
        internal object UnderlyingValue { get; set; }
    }
}
