using GraphQL.Types;
using GraphQL.Validation.Errors;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Validation.Rules;

public partial class OverlappingFieldsCanBeMerged
{
    // Prove compatibility by response-name groups before constructing diagnostics. The
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
        private Dictionary<GraphQLSelectionSet, CachedField>? _diagnosticFields;
        private PairSet? _diagnosticPairs;

        public ValueTask EnterAsync(ASTNode node, ValidationContext context)
        {
            if (node is not GraphQLSelectionSet selectionSet)
                return default;

            var parentType = context.TypeInfo.GetParentType();
            if (_diagnosticFields == null)
            {
                // Inline fragments were included in their containing selection set.
                var ancestor = context.TypeInfo.GetAncestor(1);
                if (ancestor is GraphQLInlineFragment ||
                    ancestor is GraphQLFragmentDefinition fragment && _checkedFragments?.Contains(fragment.FragmentName.Name) == true ||
                    IsTrivial(selectionSet) ||
                    CheckLocal(context, new SelectionSource(parentType, selectionSet)))
                    return default;

                // Only a proven conflict enables the pairwise diagnostic path. Keep
                // its ordering, nested reasons, and locations for the rest of this
                // document. Diagnostics retain pairwise work, including sparse failures.
                _diagnosticFields = new();
                _diagnosticPairs = new();
            }

            var conflicts = FindConflictsWithinSelectionSet(context, _diagnosticFields,
                _diagnosticPairs!, parentType, selectionSet);
            if (conflicts != null)
            {
                foreach (var conflict in conflicts)
                    context.ReportError(new OverlappingFieldsCanBeMergedError(context, conflict));
            }
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
            return Collect(context, source, ref fields) && CheckChildren(context, ref fields);
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
            if (_fragmentLeafSummaries == null || !_fragmentLeafSummaries.TryGetValue(names, out var summary))
            {
                summary = new FieldMap(CheckKind.Both);
                foreach (var name in names)
                {
                    if (!CollectFragment(context, name, ref summary))
                        return false;
                }
                if (!CheckChildren(context, ref summary))
                    return false;

                // These maps are immutable after insertion. Local fields must not
                // become part of a summary reused under a different parent type.
                (_fragmentLeafSummaries ??= new(HashSet<ROM>.CreateSetComparer())).Add(names, summary);
            }

            var localFields = new FieldMap(CheckKind.Both);
            foreach (var selection in source.SelectionSet.Selections)
            {
                if (selection is GraphQLField field)
                {
                    var type = (source.ParentType as IComplexGraphType)?.GetField(field.Name)?.ResolvedType;
                    var reference = new FieldReference(source.ParentType, field, type);
                    if (!summary.MatchesLeaf(reference) || !simpleLeaves && !localFields.Add(reference))
                        return false;
                }
            }
            return true;
        }

        private bool Collect(ValidationContext context, SelectionSource source, ref FieldMap fields)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            foreach (var selection in source.SelectionSet.Selections)
            {
                if (selection is GraphQLField field)
                {
                    var type = (source.ParentType as IComplexGraphType)?.GetField(field.Name)?.ResolvedType;
                    if (!fields.Add(new FieldReference(source.ParentType, field, type)))
                        return false;
                }
                else if (selection is GraphQLInlineFragment inline)
                {
                    var type = inline.TypeCondition == null
                        ? source.ParentType
                        : inline.TypeCondition.Type.GraphTypeFromType(context.Schema);
                    if (!Collect(context, new SelectionSource(type, inline.SelectionSet), ref fields))
                        return false;
                }
                else if (selection is GraphQLFragmentSpread spread && !CollectFragment(context, spread.FragmentName.Name, ref fields))
                {
                    return false;
                }
            }
            return true;
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

        private bool CheckChildren(ValidationContext context, ref FieldMap fields)
        {
            if (fields.Map != null)
            {
                foreach (var group in fields.Map.Values)
                {
                    if (!CheckChildren(context, group.Collision, fields.Kind))
                        return false;
                }
            }
            else if (fields.First.HasValue && !CheckChildren(context, fields.First.Value.Collision, fields.Kind))
            {
                return false;
            }

            if (fields.Kind == CheckKind.Both && fields.Fragments != null)
                (_checkedFragments ??= new()).UnionWith(fields.Fragments);
            return true;
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

            if ((kind & CheckKind.Shape) != 0 && !CheckMerged(context, group.Children, CheckKind.Shape))
                return false;

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
                if (!CheckMerged(context, children, CheckKind.Names))
                    return false;
            }
            return true;
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
            foreach (var source in sources)
            {
                if (!Collect(context, source, ref fields))
                    return false;
            }
            return CheckChildren(context, ref fields);
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

        public bool Add(FieldReference field)
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
                    bool valid = first.Add(field, Kind);
                    First = first;
                    return valid;
                }
                Map = new() { { firstName, first }, { name, new FieldEntry(field) } };
                return true;
            }
            if (Map.TryGetValue(name, out var group))
            {
                bool valid = group.Add(field, Kind);
                Map[name] = group;
                return valid;
            }
            Map.Add(name, new FieldEntry(field));
            return true;
        }

        public readonly bool MatchesLeaf(FieldReference field)
        {
            var name = field.Field.Alias?.Name ?? field.Field.Name;
            if (Map != null)
                return !Map.TryGetValue(name, out var entry) || entry.MatchesLeaf(field);
            return First is not FieldEntry first || name != (first.First.Field.Alias?.Name ?? first.First.Field.Name) || first.MatchesLeaf(field);
        }
    }

    // A unique field occupies only a representative and a nullable collision
    // state. Large structs in every dictionary slot would penalize unique aliases.
    private struct FieldEntry(FieldReference first)
    {
        public FieldReference First = first;
        public FieldGroup? Collision;

        public bool Add(FieldReference field, CheckKind kind)
        {
            if (Collision != null)
                return Collision.Add(field, kind);

            // Repeated leaves on the same parent require no collection or object.
            if (ReferenceEquals(First.ParentType, field.ParentType) && First.Field.SelectionSet == null && field.Field.SelectionSet == null)
            {
                if ((kind & CheckKind.Shape) != 0 && First.Type != null && field.Type != null && DoTypesConflict(First.Type, field.Type))
                    return false;
                if ((kind & CheckKind.Names) != 0 && !FieldGroup.SameField(First.Field, field.Field))
                    return false;
                if (First.Type == null && field.Type != null)
                    First = field;
                return true;
            }

            Collision = new FieldGroup(First);
            return Collision.Add(field, kind);
        }

        public readonly bool MatchesLeaf(FieldReference field)
        {
            if (Collision != null)
                return Collision.MatchesLeaf(field);
            if (First.Type != null && field.Type != null && DoTypesConflict(First.Type, field.Type))
                return false;
            return First.ParentType is IObjectGraphType && field.ParentType is IObjectGraphType && !ReferenceEquals(First.ParentType, field.ParentType) ||
                FieldGroup.SameField(First.Field, field.Field);
        }
    }

    private sealed class FieldGroup
    {
        public readonly FieldReference First;
        private IGraphType? _knownType;
        private FieldReference? _abstract;
        private Dictionary<IGraphType, FieldReference>? _byParent;
        private FieldReference? _firstChild;
        public List<FieldReference>? Children;

        public FieldGroup(FieldReference first)
        {
            First = first;
            _knownType = first.Type;
            _abstract = first.ParentType is IObjectGraphType ? null : first;
            _byParent = null;
            _firstChild = first.Field.SelectionSet == null ? null : first;
            Children = null;
        }

        public bool Add(FieldReference field, CheckKind kind)
        {
            if ((kind & CheckKind.Shape) != 0 && field.Type != null)
            {
                if (_knownType != null && DoTypesConflict(_knownType, field.Type))
                    return false;
                // Unknown fields do not prevent comparing the remaining known types.
                _knownType ??= field.Type;
            }
            if ((kind & CheckKind.Names) != 0 && !SameForCommonParents(field))
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

        public bool MatchesLeaf(FieldReference field)
        {
            if (_knownType != null && field.Type != null && DoTypesConflict(_knownType, field.Type))
                return false;
            if (_abstract is FieldReference abstractField)
                return SameField(abstractField.Field, field.Field);

            if (field.ParentType is IObjectGraphType)
            {
                if (ReferenceEquals(First.ParentType, field.ParentType))
                    return SameField(First.Field, field.Field);
                return _byParent == null || !_byParent.TryGetValue(field.ParentType, out var representative) ||
                    SameField(representative.Field, field.Field);
            }

            // An abstract (or unknown) local parent overlaps every concrete
            // representative. This read must not promote or mutate the summary.
            if (!SameField(First.Field, field.Field))
                return false;
            if (_byParent != null)
            {
                foreach (var representative in _byParent.Values)
                {
                    if (!SameField(representative.Field, field.Field))
                        return false;
                }
            }
            return true;
        }

        private bool SameForCommonParents(FieldReference field)
        {
            if (_abstract is FieldReference abstractField)
                return SameField(abstractField.Field, field.Field);

            if (field.ParentType is not IObjectGraphType)
            {
                if (!SameField(First.Field, field.Field))
                    return false;
                if (_byParent != null)
                {
                    foreach (var representative in _byParent.Values)
                    {
                        if (!SameField(representative.Field, field.Field))
                            return false;
                    }
                }
                _abstract = field;
                _byParent = null;
                return true;
            }
            if (ReferenceEquals(First.ParentType, field.ParentType))
                return SameField(First.Field, field.Field);

            _byParent ??= new() { { First.ParentType!, First } };
            if (_byParent.TryGetValue(field.ParentType, out var previous))
                return SameField(previous.Field, field.Field);
            _byParent.Add(field.ParentType, field);
            return true;
        }

        public static bool SameField(GraphQLField first, GraphQLField second)
            => ReferenceEquals(first, second) || first.Name == second.Name && SameArguments(first.Arguments, second.Arguments);
    }
}
