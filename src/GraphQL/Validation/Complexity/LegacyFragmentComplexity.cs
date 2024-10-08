namespace GraphQL.Validation.Complexity;

/// <summary>
/// Class to track complexity of fragment defined in GraphQL document.
/// </summary>
[Obsolete("Please use the new complexity analyzer. This class will be removed in v9.")]
internal sealed class LegacyFragmentComplexity
{
    /// <summary>
    /// Depth of fragment.
    /// <br/><br/>
    /// Depth 0: fragment frag1 on Type { f }
    /// <br/>
    /// Depth 1: fragment frag1 on Type { f { ff } }
    /// <br/>
    /// Depth 2: fragment frag1 on Type { f { ff { fff } } }
    /// </summary>
    public int Depth { get; set; }

    /// <summary>
    /// Complexity of fragment.
    /// </summary>
    public double Complexity { get; set; }
}
