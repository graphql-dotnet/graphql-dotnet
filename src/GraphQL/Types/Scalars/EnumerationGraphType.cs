using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using GraphQL.Utilities;
using GraphQLParser;
using GraphQLParser.AST;

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
            Values = CreateValues();
        }

        /// <summary>
        /// Adds a value to the allowed set of enumeration values.
        /// </summary>
        /// <param name="name">The name of the enumeration member, as exposed through the GraphQL endpoint (e.g. "RED").</param>
        /// <param name="description">A description of the enumeration member.</param>
        /// <param name="value">The value of the enumeration member, as referenced by the code (e.g. <see cref="ConsoleColor.Red"/>).</param>
        /// <param name="deprecationReason">The reason this enumeration member has been deprecated; <see langword="null"/> if this member has not been deprecated.</param>
        public void AddValue(string name, string? description, object? value, string? deprecationReason = null)
        {
            AddValue(new EnumValueDefinition(name, value)
            {
                Description = description,
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
        public EnumValuesBase Values { get; }

        protected virtual EnumValuesBase CreateValues() => new EnumValues();

        /// <inheritdoc/>
        public override object? ParseLiteral(GraphQLValue value) => value switch
        {
            GraphQLEnumValue enumValue => Values.FindByName(enumValue.Name)?.Value ?? ThrowLiteralConversionError(value),
            GraphQLNullValue _ => null,
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override bool CanParseLiteral(GraphQLValue value) => value switch
        {
            GraphQLEnumValue enumValue => Values.FindByName(enumValue.Name) != null,
            GraphQLNullValue _ => true,
            _ => false
        };

        /// <inheritdoc/>
        public override object? ParseValue(object? value) => value switch
        {
            string s => Values.FindByName(s)?.Value ?? ThrowValueConversionError(value),
            null => null,
            _ => ThrowValueConversionError(value)
        };

        /// <inheritdoc/>
        public override bool CanParseValue(object? value) => value switch
        {
            string s => Values.FindByName(s) != null,
            null => true,
            _ => false
        };

        /// <inheritdoc/>
        public override object? Serialize(object? value)
        {
            var foundByValue = Values.FindByValue(value);

            return foundByValue == null
                ? value == null ? null : ThrowSerializationError(value)
                : foundByValue.Name;
        }

        /// <inheritdoc/>
        public override GraphQLValue ToAST(object? value)
        {
            var foundByValue = Values.FindByValue(value);

            return foundByValue == null
                ? value == null ? GraphQLValuesCache.Null : ThrowASTConversionError(value)
                : new GraphQLEnumValue { Name = new GraphQLName(foundByValue.Name) };
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
        private static readonly EnumCaseAttribute? _caseAttr = typeof(TEnum).GetCustomAttribute<EnumCaseAttribute>();

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
                deprecation: e.member.ObsoleteMessage(),
                member: e.member
            ));

            Name = type.Name.ToPascalCase();
            Description ??= typeof(TEnum).Description();
            DeprecationReason ??= typeof(TEnum).ObsoleteMessage();

            foreach (var (name, value, description, deprecation, member) in enumGraphData)
            {
                var enumValue = new EnumValueDefinition
                {
                    Name = name,
                    Value = value,
                    Description = description,
                    DeprecationReason = deprecation,
                };
                bool ignore = false;
                foreach (var attr in member.GetCustomAttributes<GraphQLAttribute>())
                {
                    if (!attr.ShouldInclude(member, null))
                    {
                        ignore = true;
                        break;
                    }
                    attr.Modify(enumValue);
                }
                if (ignore)
                    continue;
                AddValue(enumValue);
            }

            foreach (var attr in type.GetCustomAttributes<GraphQLAttribute>())
            {
                attr.Modify(this);
            }
        }

        protected override EnumValuesBase CreateValues() => new EnumValues<TEnum>();

        /// <summary>
        /// Changes the case of the specified enum name.
        /// By default changes it to constant case (uppercase, using underscores to separate words).
        /// </summary>
        protected virtual string ChangeEnumCase(string val)
            => _caseAttr == null ? val.ToConstantCase() : _caseAttr.ChangeEnumCase(val);
    }

    public abstract class EnumValuesBase : IEnumerable<EnumValueDefinition>
    {
        /// <summary>
        /// Returns an enumeration definition for the specified name and <see langword="null"/> if not found.
        /// </summary>
        public EnumValueDefinition? this[string name] => FindByName(name);

        /// <summary>
        /// Gets the count of enumeration definitions.
        /// </summary>
        public abstract int Count { get; }

        /// <summary>
        /// Adds an enumeration definition to the set.
        /// </summary>
        /// <param name="value"></param>
        public abstract void Add(EnumValueDefinition value);

        /// <summary>
        /// Returns an enumeration definition for the specified name.
        /// </summary>
        public abstract EnumValueDefinition? FindByName(ROM name);

        /// <summary>
        /// Returns an enumeration definition for the specified value.
        /// </summary>
        public abstract EnumValueDefinition? FindByValue(object? value);

        public abstract IEnumerator<EnumValueDefinition> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// A class that represents a set of enumeration definitions.
    /// </summary>
    public class EnumValues : EnumValuesBase
    {
        private List<EnumValueDefinition> List { get; } = new List<EnumValueDefinition>();

        /// <inheritdoc/>
        public override int Count => List.Count;

        /// <inheritdoc/>
        public override void Add(EnumValueDefinition value) => List.Add(value ?? throw new ArgumentNullException(nameof(value)));

        /// <inheritdoc/>
        public override EnumValueDefinition? FindByName(ROM name)
        {
            // DO NOT USE LINQ ON HOT PATH
            foreach (var def in List)
            {
                if (def.Name == name)
                    return def;
            }

            return null;
        }

        /// <inheritdoc/>
        public override EnumValueDefinition? FindByValue(object? value)
        {
            if (value is Enum)
            {
                value = Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType())); //ISSUE:allocation
            }

            // DO NOT USE LINQ ON HOT PATH
            foreach (var def in List)
            {
                if (Equals(def.UnderlyingValue, value))
                    return def;
            }

            return null;
        }

        public override IEnumerator<EnumValueDefinition> GetEnumerator() => List.GetEnumerator();
    }

    public class EnumValues<TEnum> : EnumValuesBase where TEnum : Enum
    {
        private Dictionary<ROM, EnumValueDefinition> DictionaryByName { get; } = new();
        private Dictionary<TEnum, EnumValueDefinition> DictionaryByValue { get; } = new();

        /// <inheritdoc/>
        public override int Count => DictionaryByName.Count;

        /// <inheritdoc/>
        public override void Add(EnumValueDefinition value)
        {
            if (value.Value is not TEnum e)
                throw new ArgumentException($"Only values of {typeof(TEnum).Name} supported", nameof(value));

            DictionaryByName[value.Name] = value;
            DictionaryByValue[e] = value;
        }

        /// <inheritdoc/>
        public override EnumValueDefinition? FindByName(ROM name)
        {
            return DictionaryByName.TryGetValue(name, out var def)
                ? def
                : null;
        }

        /// <inheritdoc/>
        public override EnumValueDefinition? FindByValue(object? value)
        {
            // fast path
            if (value is TEnum e)
                return DictionaryByValue.TryGetValue(e, out var def) ? def : null;

            // slow path - for example from Serialize(int)
            foreach (var item in DictionaryByName)
            {
                if (Equals(item.Value.UnderlyingValue, value))
                    return item.Value;
            }

            return null;
        }

        public override IEnumerator<EnumValueDefinition> GetEnumerator() => DictionaryByName.Values.GetEnumerator();
    }

    /// <summary>
    /// A class that represents an enumeration definition.
    /// </summary>
    [DebuggerDisplay("{Name}: {Value}")]
    public class EnumValueDefinition : MetadataProvider, IProvideDescription, IProvideDeprecationReason
    {
        public EnumValueDefinition(string name, object? value)
        {
            Name = name;
            Value = value;
            UnderlyingValue = value is Enum
                ? Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType()))
                : value;
        }

        /// <summary>
        /// The name of the enumeration member, as exposed through the GraphQL endpoint (e.g. "RED").
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// A description of the enumeration member.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// The reason this enumeration member has been deprecated; <see langword="null"/> if this member has not been deprecated.
        /// </summary>
        public string? DeprecationReason
        {
            get => this.GetDeprecationReason();
            set => this.SetDeprecationReason(value);
        }

        /// <summary>
        /// The value of the enumeration member, as referenced by the code (e.g. <see cref="ConsoleColor.Red"/>).
        /// </summary>
        public object? Value { get; }

        /// <summary>
        /// When mapped to a member of an <see cref="Enum"/>, contains the underlying enumeration value; otherwise contains <see cref="Value" />.
        /// </summary>
        internal object? UnderlyingValue { get; }
    }

    /// <summary>
    /// Allows to change the case of the enum names for enum marked with that attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Enum, AllowMultiple = false)]
    public abstract class EnumCaseAttribute : Attribute
    {
        /// <summary>
        /// Changes the case of the specified enum name.
        /// </summary>
        public abstract string ChangeEnumCase(string val);
    }

    /// <summary>
    /// Returns a constant case version of enum names.
    /// For example, converts 'StringError' into 'STRING_ERROR'.
    /// </summary>
    public class ConstantCaseAttribute : EnumCaseAttribute
    {
        /// <inheritdoc />
        public override string ChangeEnumCase(string val) => val.ToConstantCase();
    }

    /// <summary>
    /// Returns a camel case version of enum names.
    /// </summary>
    public class CamelCaseAttribute : EnumCaseAttribute
    {
        /// <inheritdoc />
        public override string ChangeEnumCase(string val) => val.ToCamelCase();
    }

    /// <summary>
    /// Returns a pascal case version of enum names.
    /// </summary>
    public class PascalCaseAttribute : EnumCaseAttribute
    {
        /// <inheritdoc />
        public override string ChangeEnumCase(string val) => val.ToPascalCase();
    }
}
