using GraphQL.Types;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Validation.Errors;

/// <inheritdoc cref="Rules.VariablesInAllowedPosition"/>
[Serializable]
public class VariablesInAllowedPositionError : ValidationError
{
    internal const string NUMBER = "5.8.5";

    /// <summary>
    /// Initializes a new instance with the specified properties.
    /// </summary>
    public VariablesInAllowedPositionError(ValidationContext context, GraphQLVariableDefinition varDef, IGraphType varType, VariableUsage usage)
        : base(context.Document.Source, NUMBER, BadVarPosMessage(usage, varType.ToString()!))
    {
        var varDefLoc = Location.FromLinearPosition(context.Document.Source, varDef.Location.Start);
        var usageLoc = Location.FromLinearPosition(context.Document.Source, usage.Node.Location.Start);

        AddLocation(varDefLoc);
        AddLocation(usageLoc);
    }

    internal static string BadVarPosMessage(VariableUsage usage, string varType)
    {
        string usageType = usage.Type.ToString()!;
        if (usage.IsRequired && !usageType.EndsWith("!"))
            usageType += "!";
        return BadVarPosMessage(usage.Node.Name.StringValue, varType, usageType);
    }

    internal static string BadVarPosMessage(string varName, string varType, string expectedType)
        => $"Variable '${varName}' of type '{varType}' used in position expecting type '{expectedType}'.";
}
