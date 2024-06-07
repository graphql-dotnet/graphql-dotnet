using System.ComponentModel;
using GraphQL.Types;

namespace GraphQL.Federation.Types;

/// <summary>
/// The purpose of the link.
/// </summary>
[ConstantCase]
public enum LinkPurpose
{
    /// <summary>
    /// 'Security' features provide metadata necessary to securely resolve fields.
    /// </summary>
    [Description("`SECURITY` features provide metadata necessary to securely resolve fields.")]
    Security = 0,

    /// <summary>
    /// 'Execution' features provide metadata necessary for operation execution.
    /// </summary>
    [Description("`EXECUTION` features provide metadata necessary for operation execution.")]
    Execution = 1,
}
