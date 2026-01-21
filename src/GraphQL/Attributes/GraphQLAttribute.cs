using System.Reflection;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL;

/// <summary>
/// Allows additional configuration to be applied to a type, field or query argument definition.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = true)]
public abstract class GraphQLAttribute : Attribute
{
    /// <summary>
    /// Updates the properties of the specified <see cref="TypeConfig"/> as necessary.
    /// </summary>
    public virtual void Modify(TypeConfig type)
    {
    }

    /// <summary>
    /// Updates the properties of the specified <see cref="FieldConfig"/> as necessary.
    /// </summary>
    public virtual void Modify(FieldConfig field)
    {
    }

    /// <summary>
    /// Updates the properties of the specified <see cref="IGraphType"/> as necessary.
    /// </summary>
    public virtual void Modify(IGraphType graphType)
    {
    }

    /// <summary>
    /// Updates the properties of the specified <see cref="IGraphType"/> as necessary.
    /// </summary>
    public virtual void Modify(IGraphType graphType, Type sourceType)
    {
    }

    /// <summary>
    /// Updates the properties of the specified <see cref="EnumValueDefinition"/> as necessary.
    /// </summary>
    public virtual void Modify(EnumValueDefinition enumValueDefinition)
    {
    }

    /// <summary>
    /// Updates the properties of the specified <see cref="FieldType"/> as necessary.
    /// </summary>
    /// <param name="fieldType">The <see cref="FieldType"/> to update.</param>
    /// <param name="isInputType">Indicates if the graph type containing this field is an input type.</param>
    public virtual void Modify(FieldType fieldType, bool isInputType)
    {
    }

    /// <summary>
    /// Updates the properties of the specified <see cref="FieldType"/> as necessary
    /// when adding a field to a graph type. Typically you should use <see cref="Modify(FieldType, bool)"/>
    /// instead of this method unless you need the additional properties exposed in this method.
    /// </summary>
    /// <param name="graphType">The <see cref="IGraphType"/> the field will be added to.</param>
    /// <param name="fieldType">The <see cref="FieldType"/> to update.</param>
    /// <param name="memberInfo">The <see cref="MemberInfo"/> that the field was generated from.</param>
    /// <param name="isInputType">Indicates if the graph type containing this field is an input type.</param>
    /// <param name="ignore">Indicates that the field should not be added to the graph type.</param>
    public virtual void Modify(FieldType fieldType, bool isInputType, IGraphType graphType, MemberInfo memberInfo, ref bool ignore)
    {
    }

    /// <summary>
    /// Updates the properties of the specified <see cref="TypeInformation"/> as necessary.
    /// </summary>
    public virtual void Modify(TypeInformation typeInformation)
    {
    }

    /// <summary>
    /// Updates the properties of the specified <see cref="ArgumentInformation"/> as necessary.
    /// </summary>
    public virtual void Modify(ArgumentInformation argumentInformation)
    {
    }

    /// <summary>
    /// Updates the properties of the specified <see cref="QueryArgument"/> as necessary.
    /// </summary>
    public virtual void Modify(QueryArgument queryArgument)
    {
    }

    /// <summary>
    /// Updates the properties of the specified <see cref="QueryArgument"/> as necessary.
    /// </summary>
    public virtual void Modify(QueryArgument queryArgument, ParameterInfo parameterInfo)
    {
    }

    /// <summary>
    /// Determines if a specified member should be included during automatic generation
    /// of a graph type from a CLR type.
    /// <br/><br/>
    /// When called for enumeration values, <paramref name="isInputType"/> is <see langword="null"/>.
    /// </summary>
    public virtual bool ShouldInclude(MemberInfo memberInfo, bool? isInputType) => true;

    /// <summary>
    /// Determines the order in which GraphQL attributes are applied to the graph type, field type, or parameter definition.
    /// Attributes with the lowest <see cref="Priority"/> value are applied first.
    /// The default priority is 1.
    /// </summary>
    public virtual float Priority => 1.0f;
}
