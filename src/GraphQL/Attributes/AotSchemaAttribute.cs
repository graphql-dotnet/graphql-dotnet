namespace GraphQL;

/// <summary>
/// Base class for attributes that configure AOT schema generation.
/// All AOT schema attributes must be applied to a class that derives from <see cref="Types.AotSchema"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public abstract class AotSchemaAttribute : Attribute
{
}
