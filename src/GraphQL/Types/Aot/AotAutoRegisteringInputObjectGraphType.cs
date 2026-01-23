using System.Reflection;
using GraphQLParser.AST;

namespace GraphQL.Types.Aot;

/// <summary>
/// Provides a base class for registering input object graph types in AOT (Ahead-Of-Time) compiled environments.
/// </summary>
[UnconditionalSuppressMessage("AOT", "IL2091: 'target generic parameter' generic argument does not satisfy 'DynamicallyAccessedMembersAttribute' in 'target method or type'. The generic parameter 'source target parameter' of 'source method or type' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to",
    Justification = "Code using reflection to access unreferenced members has been disabled.")]
public abstract class AotAutoRegisteringInputObjectGraphType<T> : AutoRegisteringInputObjectGraphType<T>
{
    /// <inheritdoc/>
    public AotAutoRegisteringInputObjectGraphType() : base(true)
    {
    }

    /// <inheritdoc/>
    public override GraphQLValue ToAST(object? value) => throw new NotImplementedException("ToAST must be implemented by the derived class.");
    /// <inheritdoc/>
    public override bool IsValidDefault(object value) => throw new NotImplementedException("IsValidDefault must be implemented by the derived class.");
    /// <inheritdoc/>
    protected override IEnumerable<MemberInfo> GetRegisteredMembers() => throw new NotImplementedException("GetRegisteredMembers must be implemented by the derived class if used.");
}
