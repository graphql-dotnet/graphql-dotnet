using GraphQL.Types;
using GraphQL.Validation.Errors;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Validation.Rules;

/// <summary>
/// Overlapping fields are mergable:
///
/// If multiple field selections with the same response names are encountered during execution,
/// the field and arguments to execute and the resulting value should be unambiguous. Therefore
/// any two field selections which might both be encountered for the same object are only valid
/// if they are equivalent.
/// </summary>
public class OverlappingFieldsCanBeMerged : ValidationRuleBase
{
    /// <summary>
    /// Returns a static instance of this validation rule.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    public static readonly OverlappingFieldsCanBeMerged Instance = new();
#pragma warning restore CS0618 // Type or member is obsolete

    /// <inheritdoc cref="OverlappingFieldsCanBeMerged"/>
    [Obsolete("Please use the Instance property to retrieve a static instance. This constructor will be removed in v9.")]
    public OverlappingFieldsCanBeMerged()
    {
    }

    /// <inheritdoc/>
    /// <exception cref="OverlappingFieldsCanBeMergedError"/>
    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context)
        => new(new MergeVisitor());

    // Compare response-name groups and report conflicts as they are encountered. The
    // common path combines the shape and common-parent checks in one pass. Different
    // concrete parents require a separate shape check: their descendants remain
    // mutually exclusive for names/arguments, even when their return types coincide.
    private sealed class MergeVisitor : INodeVisitor
    {
        private Dictionary<ROM, GraphQLFragmentDefinition>? _fragments;
        private HashSet<ROM>? _checkedFragments;
        private HashSet<HashSet<ROM>>? _checkedFragmentGroups;
        private Dictionary<HashSet<ROM>, FieldMap>? _fragmentLeafSummaries;
        private HashSet<SelectionGroup>? _checkedGroups;

        public ValueTask EnterAsync(ASTNode node, ValidationContext context)
        {
            if (node is not GraphQLSelectionSet selectionSet)
                return default;

            // Inline fragments were included in their containing selection set.
            var ancestor = context.TypeInfo.GetAncestor(1);
            if (ancestor is GraphQLInlineFragment ||
                ancestor is GraphQLFragmentDefinition fragment && _checkedFragments?.Contains(fragment.FragmentName.Name) == true ||
                IsTrivial(selectionSet))
                return default;

            // TypeInfo keeps list/non-null wrappers on selection-set parents.
            // Field lookup and mutual exclusion require the underlying named type.
            CheckLocal(context, new SelectionSource(context.TypeInfo.GetParentType()?.GetNamedType(), selectionSet));
            return default;
        }

        public ValueTask LeaveAsync(ASTNode node, ValidationContext context) => default;

        private static bool IsTrivial(GraphQLSelectionSet selectionSet)
        {
            // A single field cannot conflict locally. Its children are visited later.
            if (selectionSet.Selections.Count == 1 && selectionSet.Selections[0] is GraphQLField)
                return true;

            // Unaliased, argument-free leaves on one parent can only repeat the
            // exact same field. This scan needs no dictionaries or field records.
            foreach (var selection in selectionSet.Selections)
            {
                if (selection is not GraphQLField { Alias: null, Arguments: null, SelectionSet: null })
                    return false;
            }
            return true;
        }

        private bool Check(ValidationContext context, SelectionSource source)
        {
            var fields = new FieldMap(CheckKind.Both);
            bool valid = Collect(context, source, ref fields);
            return CheckChildren(context, ref fields, valid);
        }

        private bool CheckLocal(ValidationContext context, SelectionSource source)
        {
            // Independent aliases often select the same named fragments. Their
            // local AST selection sets differ, but the fields being checked do not.
            // Classify before allocating: inline fragments and local composites
            // need the full merge, while local leaves can use a reusable summary.
            var selections = source.SelectionSet.Selections;
            GraphQLFragmentSpread? first = null;
            bool multipleNames = false;
            bool hasLeaves = false;
            bool simpleLeaves = true;
            foreach (var selection in selections)
            {
                if (selection is GraphQLFragmentSpread spread)
                {
                    first ??= spread;
                    multipleNames |= spread.FragmentName.Name != first.FragmentName.Name;
                }
                else if (selection is GraphQLField { SelectionSet: null } field)
                {
                    hasLeaves = true;
                    simpleLeaves &= field.Alias == null && field.Arguments == null;
                }
                else
                {
                    return Check(context, source);
                }
            }

            if (first == null)
                return Check(context, source);
            if (!hasLeaves && !multipleNames)
                return _checkedFragments?.Contains(first.FragmentName.Name) == true || Check(context, source);

            var names = new HashSet<ROM>();
            foreach (var selection in selections)
            {
                if (selection is GraphQLFragmentSpread spread)
                    names.Add(spread.FragmentName.Name);
            }

            if (hasLeaves)
                return CheckMixedLeaves(context, source, names, simpleLeaves);

            if (_checkedFragmentGroups?.Contains(names) == true)
                return true;
            if (!Check(context, source))
                return false;

            // A group is reusable only after both shape and name checks succeed.
            // An individually valid fragment can still conflict with another one.
            (_checkedFragmentGroups ??= new(HashSet<ROM>.CreateSetComparer())).Add(names);
            return true;
        }

        private bool CheckMixedLeaves(ValidationContext context, SelectionSource source, HashSet<ROM> names, bool simpleLeaves)
        {
            bool valid = true;
            if (_fragmentLeafSummaries == null || !_fragmentLeafSummaries.TryGetValue(names, out var summary))
            {
                summary = new FieldMap(CheckKind.Both);
                foreach (var name in names)
                    valid &= CollectFragment(context, name, ref summary);
                valid = CheckChildren(context, ref summary, valid);

                // Only complete, compatible summaries can be reused. Failed fields
                // are omitted from the merge, but independent selections still run.
                if (valid)
                    (_fragmentLeafSummaries ??= new(HashSet<ROM>.CreateSetComparer())).Add(names, summary);
            }

            var localFields = new FieldMap(CheckKind.Both);
            foreach (var selection in source.SelectionSet.Selections)
            {
                if (selection is GraphQLField field)
                {
                    var type = TypeInfo.GetFieldDef(context.Schema, source.ParentType, field)?.ResolvedType;
                    var reference = new FieldReference(source.ParentType, field, type);
                    valid &= summary.MatchesLeaf(context, reference);
                    if (!simpleLeaves)
                        valid &= localFields.Add(context, reference);
                }
            }
            return valid;
        }

        private bool Collect(ValidationContext context, SelectionSource source, ref FieldMap fields)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            // A conflicting field is not merged, but other selections can still
            // be checked against their representatives without a pairwise scan.
            bool valid = true;
            foreach (var selection in source.SelectionSet.Selections)
            {
                if (selection is GraphQLField field)
                {
                    var type = TypeInfo.GetFieldDef(context.Schema, source.ParentType, field)?.ResolvedType;
                    valid &= fields.Add(context, new FieldReference(source.ParentType, field, type));
                }
                else if (selection is GraphQLInlineFragment inline)
                {
                    var type = inline.TypeCondition == null
                        ? source.ParentType
                        : inline.TypeCondition.Type.GraphTypeFromType(context.Schema);
                    valid &= Collect(context, new SelectionSource(type, inline.SelectionSet), ref fields);
                }
                else if (selection is GraphQLFragmentSpread spread)
                {
                    valid &= CollectFragment(context, spread.FragmentName.Name, ref fields);
                }
            }
            return valid;
        }

        private bool CollectFragment(ValidationContext context, ROM name, ref FieldMap fields)
        {
            // Keep this entry for the entire group, including other paths through
            // a fragment DAG. Removing it on return is exponential.
            if (!(fields.Fragments ??= new()).Add(name))
                return true;
            if (_fragments == null)
            {
                _fragments = new();
                foreach (var definition in context.Document.Definitions)
                {
                    if (definition is GraphQLFragmentDefinition fragment && !_fragments.ContainsKey(fragment.FragmentName.Name))
                        _fragments.Add(fragment.FragmentName.Name, fragment);
                }
            }
            return !_fragments.TryGetValue(name, out var found) ||
                Collect(context, new SelectionSource(found.TypeCondition.Type.GraphTypeFromType(context.Schema), found.SelectionSet), ref fields);
        }

        private bool CheckChildren(ValidationContext context, ref FieldMap fields, bool valid)
        {
            if (fields.Map != null)
            {
                foreach (var group in fields.Map.Values)
                {
                    valid &= CheckChildren(context, group.Collision, fields.Kind);
                }
            }
            else if (fields.First.HasValue)
            {
                valid &= CheckChildren(context, fields.First.Value.Collision, fields.Kind);
            }

            if (valid && fields.Kind == CheckKind.Both && fields.Fragments != null)
                (_checkedFragments ??= new()).UnionWith(fields.Fragments);
            return valid;
        }

        private bool CheckChildren(ValidationContext context, FieldGroup? group, CheckKind kind)
        {
            if (group?.Children == null)
                return true;

            // There is no need to partition child selections unless at least two
            // distinct concrete parents occur. Abstract parents overlap all groups.
            IGraphType? concreteType = null;
            bool partition = false;
            foreach (var child in group.Children)
            {
                if (child.ParentType is IObjectGraphType)
                {
                    if (concreteType != null && !ReferenceEquals(concreteType, child.ParentType))
                    {
                        partition = true;
                        break;
                    }
                    concreteType = child.ParentType;
                }
            }
            if (!partition || kind == CheckKind.Shape)
                return CheckMerged(context, group.Children, kind);

            bool valid = (kind & CheckKind.Shape) == 0 || CheckMerged(context, group.Children, CheckKind.Shape);

            Dictionary<IGraphType, List<FieldReference>>? concrete = null;
            List<FieldReference>? abstractFields = null;
            foreach (var child in group.Children)
            {
                if (child.ParentType is IObjectGraphType)
                {
                    concrete ??= new();
                    if (!concrete.TryGetValue(child.ParentType, out var children))
                        concrete.Add(child.ParentType, children = new());
                    children.Add(child);
                }
                else
                {
                    (abstractFields ??= new()).Add(child);
                }
            }
            foreach (var children in concrete!.Values)
            {
                if (abstractFields != null)
                    children.AddRange(abstractFields);
                valid &= CheckMerged(context, children, CheckKind.Names);
            }
            return valid;
        }

        private bool CheckMerged(ValidationContext context, List<FieldReference> children, CheckKind kind)
        {
            if (children.Count <= 1)
                return true;

            var sources = new HashSet<SelectionSource>();
            foreach (var child in children)
                sources.Add(new SelectionSource(child.Type?.GetNamedType(), child.Field.SelectionSet!));
            // Repeating one AST selection does not introduce a new comparison.
            if (sources.Count == 1)
                return true;

            // Cache the actual source set, not object hash codes as identities. Add
            // before recursion to terminate cycles crossing composite fields.
            var key = new SelectionGroup(sources, kind);
            if (!(_checkedGroups ??= new()).Add(key))
                return true;

            var fields = new FieldMap(kind);
            bool valid = true;
            foreach (var source in sources)
                valid &= Collect(context, source, ref fields);
            valid = CheckChildren(context, ref fields, valid);
            if (!valid)
                _checkedGroups.Remove(key);
            return valid;
        }
    }

    [Flags]
    private enum CheckKind { Shape = 1, Names = 2, Both = Shape | Names }

    private readonly record struct SelectionSource(IGraphType? ParentType, GraphQLSelectionSet SelectionSet);

    private readonly record struct FieldReference(IGraphType? ParentType, GraphQLField Field, IGraphType? Type);

    private sealed class SelectionGroup(HashSet<SelectionSource> sources, CheckKind kind) : IEquatable<SelectionGroup>
    {
        private readonly HashSet<SelectionSource> _sources = sources;
        private readonly CheckKind _kind = kind;

        public bool Equals(SelectionGroup? other) => other != null && _kind == other._kind && _sources.SetEquals(other._sources);

        public override bool Equals(object? obj) => obj is SelectionGroup other && Equals(other);

        public override int GetHashCode()
        {
            int hash = (int)_kind;
            foreach (var source in _sources)
                hash = unchecked(hash + source.GetHashCode());
            return hash;
        }
    }

    // One response name is kept inline. The dictionary appears only on the second
    // name; its values are structs, and repeated leaves never need a list.
    private struct FieldMap(CheckKind kind)
    {
        public readonly CheckKind Kind = kind;
        public FieldEntry? First;
        public Dictionary<ROM, FieldEntry>? Map;
        public HashSet<ROM>? Fragments;

        public bool Add(ValidationContext context, FieldReference field)
        {
            var name = field.Field.Alias?.Name ?? field.Field.Name;
            if (!First.HasValue)
            {
                First = new FieldEntry(field);
                return true;
            }
            var first = First.Value;
            var firstName = first.First.Field.Alias?.Name ?? first.First.Field.Name;
            if (Map == null)
            {
                if (name == firstName)
                {
                    bool valid = first.Add(context, field, Kind);
                    First = first;
                    return valid;
                }
                Map = new() { { firstName, first }, { name, new FieldEntry(field) } };
                return true;
            }
            if (Map.TryGetValue(name, out var group))
            {
                bool valid = group.Add(context, field, Kind);
                Map[name] = group;
                return valid;
            }
            Map.Add(name, new FieldEntry(field));
            return true;
        }

        public readonly bool MatchesLeaf(ValidationContext context, FieldReference field)
        {
            var name = field.Field.Alias?.Name ?? field.Field.Name;
            if (Map != null)
                return !Map.TryGetValue(name, out var entry) || entry.MatchesLeaf(context, field);
            return First is not FieldEntry first || name != (first.First.Field.Alias?.Name ?? first.First.Field.Name) || first.MatchesLeaf(context, field);
        }
    }

    // A unique field occupies only a representative and a nullable collision
    // state. Large structs in every dictionary slot would penalize unique aliases.
    private struct FieldEntry(FieldReference first)
    {
        public FieldReference First = first;
        public FieldGroup? Collision;

        public bool Add(ValidationContext context, FieldReference field, CheckKind kind)
        {
            if (Collision != null)
                return Collision.Add(context, field, kind);

            // Repeated leaves on the same parent require no collection or object.
            if (ReferenceEquals(First.ParentType, field.ParentType) && First.Field.SelectionSet == null && field.Field.SelectionSet == null)
            {
                if ((kind & CheckKind.Shape) != 0 && !CheckType(context, First.Field, First.Type, field))
                    return false;
                if ((kind & CheckKind.Names) != 0 && !FieldGroup.CheckSameField(context, First.Field, field.Field))
                    return false;
                if (First.Type == null && field.Type != null)
                    First = field;
                return true;
            }

            Collision = new FieldGroup(First);
            return Collision.Add(context, field, kind);
        }

        public readonly bool MatchesLeaf(ValidationContext context, FieldReference field)
        {
            if (Collision != null)
                return Collision.MatchesLeaf(context, field);
            if (!CheckType(context, First.Field, First.Type, field))
                return false;
            return First.ParentType is IObjectGraphType && field.ParentType is IObjectGraphType && !ReferenceEquals(First.ParentType, field.ParentType) ||
                FieldGroup.CheckSameField(context, First.Field, field.Field);
        }
    }

    private sealed class FieldGroup
    {
        public readonly FieldReference First;
        private IGraphType? _knownType;
        private GraphQLField _knownTypeField;
        private FieldReference? _abstract;
        private Dictionary<IGraphType, FieldReference>? _byParent;
        private FieldReference? _firstChild;
        public List<FieldReference>? Children;

        public FieldGroup(FieldReference first)
        {
            First = first;
            _knownType = first.Type;
            _knownTypeField = first.Field;
            _abstract = first.ParentType is IObjectGraphType ? null : first;
            _byParent = null;
            _firstChild = first.Field.SelectionSet == null ? null : first;
            Children = null;
        }

        public bool Add(ValidationContext context, FieldReference field, CheckKind kind)
        {
            if ((kind & CheckKind.Shape) != 0 && field.Type != null)
            {
                if (!CheckType(context, _knownTypeField, _knownType, field))
                    return false;
                // Keep the actual known field for diagnostics when the first field
                // in the group has no resolved type.
                if (_knownType == null)
                {
                    _knownType = field.Type;
                    _knownTypeField = field.Field;
                }
            }
            if ((kind & CheckKind.Names) != 0 && !SameForCommonParents(context, field))
                return false;

            if (field.Field.SelectionSet != null)
            {
                if (_firstChild is not FieldReference firstChild)
                    _firstChild = field;
                else if (field.Field.SelectionSet != firstChild.Field.SelectionSet || !ReferenceEquals(field.Type, firstChild.Type))
                    (Children ??= new() { firstChild }).Add(field);
            }
            return true;
        }

        public bool MatchesLeaf(ValidationContext context, FieldReference field)
        {
            if (!CheckType(context, _knownTypeField, _knownType, field))
                return false;
            if (_abstract is FieldReference abstractField)
                return CheckSameField(context, abstractField.Field, field.Field);

            if (field.ParentType is IObjectGraphType)
            {
                if (ReferenceEquals(First.ParentType, field.ParentType))
                    return CheckSameField(context, First.Field, field.Field);
                return _byParent == null || !_byParent.TryGetValue(field.ParentType, out var representative) ||
                    CheckSameField(context, representative.Field, field.Field);
            }

            // An abstract (or unknown) local parent overlaps every concrete
            // representative. This read must not promote or mutate the summary.
            if (!CheckSameField(context, First.Field, field.Field))
                return false;
            if (_byParent != null)
            {
                foreach (var representative in _byParent.Values)
                {
                    if (!CheckSameField(context, representative.Field, field.Field))
                        return false;
                }
            }
            return true;
        }

        private bool SameForCommonParents(ValidationContext context, FieldReference field)
        {
            if (_abstract is FieldReference abstractField)
                return CheckSameField(context, abstractField.Field, field.Field);

            if (field.ParentType is not IObjectGraphType)
            {
                if (!CheckSameField(context, First.Field, field.Field))
                    return false;
                if (_byParent != null)
                {
                    foreach (var representative in _byParent.Values)
                    {
                        if (!CheckSameField(context, representative.Field, field.Field))
                            return false;
                    }
                }
                _abstract = field;
                _byParent = null;
                return true;
            }
            if (ReferenceEquals(First.ParentType, field.ParentType))
                return CheckSameField(context, First.Field, field.Field);

            _byParent ??= new() { { First.ParentType!, First } };
            if (_byParent.TryGetValue(field.ParentType, out var previous))
                return CheckSameField(context, previous.Field, field.Field);
            _byParent.Add(field.ParentType, field);
            return true;
        }

        public static bool CheckSameField(ValidationContext context, GraphQLField first, GraphQLField second)
        {
            if (ReferenceEquals(first, second))
                return true;
            if (first.Name != second.Name)
            {
                context.ReportError(new OverlappingFieldsCanBeMergedError(context, first, second,
                    $"{first.Name} and {second.Name} are different fields"));
                return false;
            }
            if (!SameArguments(first.Arguments, second.Arguments))
            {
                context.ReportError(new OverlappingFieldsCanBeMergedError(context, first, second, "they have differing arguments"));
                return false;
            }
            return true;
        }
    }

    private static bool CheckType(ValidationContext context, GraphQLField first, IGraphType? firstType, FieldReference second)
    {
        if (firstType != null && second.Type != null && DoTypesConflict(firstType, second.Type))
        {
            context.ReportError(new OverlappingFieldsCanBeMergedError(context, first, second.Field,
                $"they return conflicting types {firstType} and {second.Type}"));
            return false;
        }
        return true;
    }

    private static bool DoTypesConflict(IGraphType type1, IGraphType type2)
    {
        if (type1 is ListGraphType type1List)
        {
            return type2 is ListGraphType type2List
                ? DoTypesConflict(type1List.ResolvedType!, type2List.ResolvedType!)
                : true;
        }

        if (type2 is ListGraphType)
        {
            return true;
        }

        if (type1 is NonNullGraphType type1NonNull)
        {
            return type2 is NonNullGraphType type2NonNull
                ? DoTypesConflict(type1NonNull.ResolvedType!, type2NonNull.ResolvedType!)
                : true;
        }

        if (type2 is NonNullGraphType)
        {
            return true;
        }

        if (type1.IsLeafType() || type2.IsLeafType())
        {
            return !type1.Equals(type2);
        }

        return false;
    }

    private static bool SameArguments(GraphQLArguments? arguments1, GraphQLArguments? arguments2)
    {
        if (ReferenceEquals(arguments1, arguments2))
            return true;
        if (arguments1 == null || arguments2 == null || arguments1.Count != arguments2.Count)
            return false;

        // Aligned names need no lookup buffer, even for large argument sets.
        int start = 0;
        while (start < arguments1.Count && arguments1[start].Name == arguments2[start].Name)
        {
            if (!SameValue(arguments1[start].Value, arguments2[start].Value))
                return false;
            ++start;
        }
        if (start == arguments1.Count)
            return true;

        // Keep small reordered suffixes allocation-free without allowing an
        // unbounded pairwise scan. UniqueArgumentNames handles duplicate names.
        if (arguments1.Count - start <= 8)
        {
            for (int i = start; i < arguments1.Count; ++i)
            {
                var argument = arguments1[i];
                bool found = false;
                for (int j = start; j < arguments2.Count; ++j)
                {
                    if (argument.Name != arguments2[j].Name)
                        continue;
                    if (!SameValue(argument.Value, arguments2[j].Value))
                        return false;
                    found = true;
                    break;
                }
                if (!found)
                    return false;
            }
            return true;
        }

        var values = new Dictionary<ROM, GraphQLValue?>(arguments2.Count - start);
        for (int i = start; i < arguments2.Count; ++i)
            values[arguments2[i].Name] = arguments2[i].Value;
        for (int i = start; i < arguments1.Count; ++i)
        {
            var argument = arguments1[i];
            if (!values.TryGetValue(argument.Name, out var other) || !SameValue(argument.Value, other))
                return false;
        }
        return true;
    }

    private static bool SameValue(GraphQLValue? value1, GraphQLValue? value2)
    {
        if (ReferenceEquals(value1, value2))
            return true;
        if (value1 == null || value2 == null || value1.Kind != value2.Kind)
            return false;

        // Kind ignores parser subclasses carrying locations/comments. String
        // values are already decoded, while numeric token kinds remain distinct.
        return value1 switch
        {
            GraphQLVariable variable => value2 is GraphQLVariable other && variable.Name == other.Name,
            GraphQLEnumValue enumValue => value2 is GraphQLEnumValue other && enumValue.Name == other.Name,
            GraphQLListValue list => value2 is GraphQLListValue other && SameListValues(list, other),
            GraphQLObjectValue obj => value2 is GraphQLObjectValue other && SameObjectValues(obj, other),
            IHasValueNode scalar => value2 is IHasValueNode other && scalar.Value == other.Value,
            _ => false,
        };
    }

    private static bool SameListValues(GraphQLListValue value1, GraphQLListValue value2)
    {
        var values1 = value1.Values;
        var values2 = value2.Values;
        if (ReferenceEquals(values1, values2))
            return true;
        if ((values1?.Count ?? 0) != (values2?.Count ?? 0))
            return false;
        if (values1 == null || values2 == null)
            return true;

        // List order is significant, including lists nested in input objects.
        for (int i = 0; i < values1.Count; ++i)
        {
            if (!SameValue(values1[i], values2[i]))
                return false;
        }
        return true;
    }

    private static bool SameObjectValues(GraphQLObjectValue value1, GraphQLObjectValue value2)
    {
        var fields1 = value1.Fields;
        var fields2 = value2.Fields;
        if (ReferenceEquals(fields1, fields2))
            return true;
        if ((fields1?.Count ?? 0) != (fields2?.Count ?? 0))
            return false;
        if (fields1 == null || fields2 == null)
            return true;

        int start = 0;
        while (start < fields1.Count && fields1[start].Name == fields2[start].Name)
        {
            if (!SameValue(fields1[start].Value, fields2[start].Value))
                return false;
            ++start;
        }
        if (start == fields1.Count)
            return true;

        // Input object fields are unordered. As with arguments, only a large
        // reordered suffix needs a dictionary; the common aligned case does not.
        if (fields1.Count - start <= 8)
        {
            for (int i = start; i < fields1.Count; ++i)
            {
                var field = fields1[i];
                bool found = false;
                for (int j = start; j < fields2.Count; ++j)
                {
                    if (field.Name != fields2[j].Name)
                        continue;
                    if (!SameValue(field.Value, fields2[j].Value))
                        return false;
                    found = true;
                    break;
                }
                if (!found)
                    return false;
            }
            return true;
        }

        // Assignment keeps malformed duplicate keys non-throwing; their own
        // validation rule owns that error.
        var values = new Dictionary<ROM, GraphQLValue?>(fields2.Count - start);
        for (int i = start; i < fields2.Count; ++i)
            values[fields2[i].Name] = fields2[i].Value;
        for (int i = start; i < fields1.Count; ++i)
        {
            var field = fields1[i];
            if (!values.TryGetValue(field.Name, out var other) || !SameValue(field.Value, other))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Describes a conflict between two fields in a document.
    /// </summary>
    public class Conflict
    {
        /// <summary>
        /// Returns the reason for the conflict.
        /// </summary>
        public ConflictReason Reason { get; set; } = null!;

        /// <summary>
        /// Returns a list of fields that are in conflict.
        /// </summary>
        public List<ISelectionNode> FieldsLeft { get; set; } = null!;

        /// <summary>
        /// Returns a list of fields that are in conflict.
        /// </summary>
        public List<ISelectionNode> FieldsRight { get; set; } = null!;
    }

    /// <summary>
    /// Describes the reason for a conflict.
    /// </summary>
    public class ConflictReason
    {
        /// <summary>
        /// The name of the field in conflict.
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// Returns a message descriptor describing the conflict.
        /// </summary>
        public Message Message { get; set; } = null!;
    }

    /// <summary>
    /// A message descriptor describing a conflict.
    /// </summary>
    public class Message
    {
        /// <summary>
        /// Returns the conflict message.
        /// </summary>
        public string? Msg { get; set; }

        /// <summary>
        /// Returns a list of conflict reasons that triggered this conflict.
        /// </summary>
        public List<ConflictReason>? Msgs { get; set; }
    }
}
