using System.Runtime.CompilerServices;
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
///
/// Uses the Simon Adameit algorithm: flatten all fragments, group by output name,
/// check SameResponseShape (types) and SameForCommonParents (names/args) separately,
/// merge sub-selections and recurse, with aggressive caching via CacheEntry.
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

    // ── ThreadStatic caches reused across validation runs on the same thread ──

    [ThreadStatic]
    private static Dictionary<ExpandCacheKey, CacheEntry>? t_expandCache;

    [ThreadStatic]
    private static HashSet<(nint, nint)>? t_reportedPairs;

    [ThreadStatic]
    private static HashSet<ROM>? t_visitedFragments;

    /// <inheritdoc/>
    /// <exception cref="OverlappingFieldsCanBeMergedError"/>
    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context)
    {
        // Reuse ThreadStatic dictionaries; clear at the start of each validation run.
        var expandCache = t_expandCache ??= new();
        var reportedPairs = t_reportedPairs ??= new();

        expandCache.Clear();
        reportedPairs.Clear();

        // Pre-process fragment definitions in post-order so that conflicts within
        // fragments are detected at the innermost level first.  This ensures that
        // when the operation-level visitor later reaches the same leaf conflict through
        // recursive merge-and-check, the dedup set suppresses the doubly-wrapped
        // duplicate and preserves the singly-wrapped error at the correct level.
        foreach (var def in context.Document.Definitions)
        {
            if (def is GraphQLFragmentDefinition fragment)
            {
                var fragmentType = fragment.TypeCondition.Type.GraphTypeFromType(context.Schema);
                PreProcessSelectionSet(context, expandCache, reportedPairs, fragmentType, fragment.SelectionSet);
            }
        }

        // Use leave (post-order) callback so inner SelectionSets within the operation
        // are checked before outer ones.
        return new ValueTask<INodeVisitor?>(new MatchingNodeVisitor<GraphQLSelectionSet>(
            leave: (selectionSet, context) =>
        {
            var parentType = context.TypeInfo.GetParentType();
            CheckSelectionSet(context, expandCache, reportedPairs, parentType, selectionSet);
        }));
    }

    /// <summary>
    /// Recursively walks a SelectionSet tree in post-order and runs the Simon Adameit check
    /// at each level. Used to pre-process fragment definitions before the main visitor runs.
    /// </summary>
    private static void PreProcessSelectionSet(
        ValidationContext context,
        Dictionary<ExpandCacheKey, CacheEntry> expandCache,
        HashSet<(nint, nint)> reportedPairs,
        IGraphType? parentType,
        GraphQLSelectionSet selectionSet)
    {
        // Recurse into children first (post-order)
        for (int i = 0; i < selectionSet.Selections.Count; i++)
        {
            var selection = selectionSet.Selections[i];
            if (selection is GraphQLField field && field.SelectionSet != null)
            {
                FieldType? fieldDef = null;
                if (parentType is IComplexGraphType complexType)
                    fieldDef = complexType.GetField(field.Name);
                var fieldReturnType = fieldDef?.ResolvedType;
                PreProcessSelectionSet(context, expandCache, reportedPairs, fieldReturnType, field.SelectionSet);
            }
            else if (selection is GraphQLInlineFragment inlineFragment)
            {
                var typeCondition = inlineFragment.TypeCondition?.Type;
                var inlineType = typeCondition != null
                    ? typeCondition.GraphTypeFromType(context.Schema)
                    : parentType;
                PreProcessSelectionSet(context, expandCache, reportedPairs, inlineType, inlineFragment.SelectionSet);
            }
        }

        // Then check this level
        CheckSelectionSet(context, expandCache, reportedPairs, parentType, selectionSet);
    }

    private static void CheckSelectionSet(
        ValidationContext context,
        Dictionary<ExpandCacheKey, CacheEntry> expandCache,
        HashSet<(nint, nint)> reportedPairs,
        IGraphType? parentType,
        GraphQLSelectionSet selectionSet)
    {
        var entry = GetOrCreateCacheEntry(context, expandCache, parentType, selectionSet);

        // Optimization: if only 0 or 1 field per response name, no conflicts possible
        if (!entry.HasOverlappingFields)
            return;

        List<Conflict>? conflicts = null;
        entry.SameResponseShapeByName(context, expandCache, reportedPairs, ref conflicts);
        entry.SameForCommonParentsByName(context, expandCache, reportedPairs, ref conflicts);

        if (conflicts != null)
        {
            foreach (var conflict in conflicts)
                context.ReportError(new OverlappingFieldsCanBeMergedError(context, conflict));
        }
    }

    // ── Field expansion: flatten selection sets by inlining all fragments ──

    /// <summary>
    /// Cache key for expanded field maps, keyed by (parentType, selectionSet) using reference equality.
    /// </summary>
    private readonly struct ExpandCacheKey : IEquatable<ExpandCacheKey>
    {
        public readonly IGraphType? ParentType;
        public readonly GraphQLSelectionSet SelectionSet;

        public ExpandCacheKey(IGraphType? parentType, GraphQLSelectionSet selectionSet)
        {
            ParentType = parentType;
            SelectionSet = selectionSet;
        }

        public bool Equals(ExpandCacheKey other)
            => ReferenceEquals(ParentType, other.ParentType) && ReferenceEquals(SelectionSet, other.SelectionSet);

        public override bool Equals(object? obj) => obj is ExpandCacheKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return RuntimeHelpers.GetHashCode(ParentType) * 397 ^ RuntimeHelpers.GetHashCode(SelectionSet);
            }
        }
    }

    private static CacheEntry GetOrCreateCacheEntry(
        ValidationContext context,
        Dictionary<ExpandCacheKey, CacheEntry> cache,
        IGraphType? parentType,
        GraphQLSelectionSet selectionSet)
    {
        var key = new ExpandCacheKey(parentType, selectionSet);
        if (cache.TryGetValue(key, out var entry))
            return entry;

        var fieldMap = new Dictionary<ROM, List<FieldDefPair>>();

        var visitedFragments = t_visitedFragments ??= new();
        visitedFragments.Clear();

        ExpandSelectionSetCore(context, parentType, selectionSet, fieldMap, visitedFragments);

        entry = new CacheEntry(fieldMap);
        cache[key] = entry;
        return entry;
    }

    private static void ExpandSelectionSetCore(
        ValidationContext context,
        IGraphType? parentType,
        GraphQLSelectionSet selectionSet,
        Dictionary<ROM, List<FieldDefPair>> result,
        HashSet<ROM> visitedFragments)
    {
        for (int i = 0; i < selectionSet.Selections.Count; i++)
        {
            var selection = selectionSet.Selections[i];

            if (selection is GraphQLField field)
            {
                var fieldName = field.Name;
                FieldType? fieldDef = null;
                if (parentType is IComplexGraphType complexType)
                    fieldDef = complexType.GetField(fieldName);

                var responseName = field.Alias is null ? fieldName : field.Alias.Name;

                if (!result.TryGetValue(responseName, out var list))
                {
                    list = new List<FieldDefPair>();
                    result[responseName] = list;
                }

                list.Add(new FieldDefPair
                {
                    ParentType = parentType,
                    Field = field,
                    FieldDef = fieldDef
                });
            }
            else if (selection is GraphQLFragmentSpread fragmentSpread)
            {
                var fragmentName = fragmentSpread.FragmentName.Name;

                // Prevent infinite recursion for recursive fragments
                if (!visitedFragments.Add(fragmentName))
                    continue;

                var fragment = context.Document.FindFragmentDefinition(fragmentName);
                if (fragment != null)
                {
                    var fragmentType = fragment.TypeCondition.Type.GraphTypeFromType(context.Schema);
                    ExpandSelectionSetCore(context, fragmentType, fragment.SelectionSet, result, visitedFragments);
                }

                visitedFragments.Remove(fragmentName); // Allow re-entry from different paths
            }
            else if (selection is GraphQLInlineFragment inlineFragment)
            {
                var typeCondition = inlineFragment.TypeCondition?.Type;
                var inlineFragmentType = typeCondition != null
                    ? typeCondition.GraphTypeFromType(context.Schema)
                    : parentType;

                ExpandSelectionSetCore(context, inlineFragmentType, inlineFragment.SelectionSet, result, visitedFragments);
            }
        }
    }

    // ── CacheEntry: core of the Simon Adameit algorithm ──

    /// <summary>
    /// A cached set of fields grouped by response name, with memoized validation operations.
    /// </summary>
    private sealed class CacheEntry
    {
        public readonly Dictionary<ROM, List<FieldDefPair>> FieldMap;

        /// <summary>
        /// True if any response name has more than one field (i.e., there is potential for conflicts).
        /// </summary>
        public readonly bool HasOverlappingFields;

        private bool _didCallSameResponseShapeByName;
        private bool _didCallSameForCommonParentsByName;

        // Cached merged sub-selections per response name.
        // null = not yet computed; entry may be null if no sub-selections exist.
        private Dictionary<ROM, CacheEntry?>? _mergedSubSelectionsCache;

        public CacheEntry(Dictionary<ROM, List<FieldDefPair>> fieldMap)
        {
            FieldMap = fieldMap;

            foreach (var entry in fieldMap)
            {
                if (entry.Value.Count > 1)
                {
                    HasOverlappingFields = true;
                    break;
                }
            }
        }

        /// <summary>
        /// SameResponseShapeByName: for every output name, all fields must have compatible types.
        /// Then recursively check merged sub-selections for the same property.
        /// Sub-conflicts found in merged sub-selections are wrapped under the parent output name.
        /// </summary>
        public void SameResponseShapeByName(
            ValidationContext context,
            Dictionary<ExpandCacheKey, CacheEntry> expandCache,
            HashSet<(nint, nint)> reportedPairs,
            ref List<Conflict>? conflicts)
        {
            if (_didCallSameResponseShapeByName)
                return;
            _didCallSameResponseShapeByName = true;

            foreach (var entry in FieldMap)
            {
                var responseName = entry.Key;
                var fields = entry.Value;

                if (fields.Count <= 1)
                    continue;

                // Check type compatibility between all pairs (with quick-check optimization)
                RequireSameOutputTypeShape(responseName, fields, reportedPairs, ref conflicts);

                // Merge sub-selections and recurse
                var merged = GetOrCreateMergedSubSelections(context, expandCache, responseName, fields);
                if (merged != null && merged.HasOverlappingFields)
                {
                    List<Conflict>? subConflicts = null;
                    merged.SameResponseShapeByName(context, expandCache, reportedPairs, ref subConflicts);

                    // Wrap any sub-conflicts under the current response name
                    if (subConflicts != null)
                    {
                        var wrapped = SubfieldConflicts(subConflicts, responseName, fields[0].Field, fields[fields.Count - 1].Field);
                        if (wrapped != null)
                            (conflicts ??= new()).Add(wrapped);
                    }
                }
            }
        }

        /// <summary>
        /// SameForCommonParentsByName: for every output name, group fields by common parent types,
        /// then within each group all fields must have the same underlying field name and arguments.
        /// Then recursively check merged sub-selections of each group for the same property.
        /// Sub-conflicts found in merged sub-selections are wrapped under the parent output name.
        /// </summary>
        public void SameForCommonParentsByName(
            ValidationContext context,
            Dictionary<ExpandCacheKey, CacheEntry> expandCache,
            HashSet<(nint, nint)> reportedPairs,
            ref List<Conflict>? conflicts)
        {
            if (_didCallSameForCommonParentsByName)
                return;
            _didCallSameForCommonParentsByName = true;

            foreach (var entry in FieldMap)
            {
                var responseName = entry.Key;
                var fields = entry.Value;

                if (fields.Count <= 1)
                    continue;

                // Group by common parent types and check within each group
                foreach (var group in GroupByCommonParents(fields))
                {
                    if (group.Count <= 1)
                        continue;

                    RequireSameNameAndArguments(responseName, group, reportedPairs, ref conflicts);

                    // Merge sub-selections of this parent group and recurse
                    var merged = MergeSubSelectionsForGroup(context, expandCache, group);
                    if (merged != null && merged.HasOverlappingFields)
                    {
                        List<Conflict>? subConflicts = null;
                        merged.SameForCommonParentsByName(context, expandCache, reportedPairs, ref subConflicts);

                        // Wrap any sub-conflicts under the current response name
                        if (subConflicts != null)
                        {
                            var wrapped = SubfieldConflicts(subConflicts, responseName, group[0].Field, group[group.Count - 1].Field);
                            if (wrapped != null)
                                (conflicts ??= new()).Add(wrapped);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets or creates a merged CacheEntry from the sub-selections of all fields with a given response name.
        /// </summary>
        private CacheEntry? GetOrCreateMergedSubSelections(
            ValidationContext context,
            Dictionary<ExpandCacheKey, CacheEntry> expandCache,
            ROM responseName,
            List<FieldDefPair> fields)
        {
            _mergedSubSelectionsCache ??= new();

            if (_mergedSubSelectionsCache.TryGetValue(responseName, out var cached))
                return cached;

            var merged = MergeSubSelectionsForGroup(context, expandCache, fields);
            _mergedSubSelectionsCache[responseName] = merged;
            return merged;
        }

        /// <summary>
        /// Merge sub-selections of the given fields into a single CacheEntry.
        /// </summary>
        private static CacheEntry? MergeSubSelectionsForGroup(
            ValidationContext context,
            Dictionary<ExpandCacheKey, CacheEntry> expandCache,
            List<FieldDefPair> fields)
        {
            Dictionary<ROM, List<FieldDefPair>>? mergedMap = null;

            for (int i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                var subSelectionSet = (field.Field as GraphQLField)?.SelectionSet;
                if (subSelectionSet == null)
                    continue;

                var subType = field.FieldDef?.ResolvedType?.GetNamedType();
                var subEntry = GetOrCreateCacheEntry(context, expandCache, subType, subSelectionSet);

                if (subEntry.FieldMap.Count == 0)
                    continue;

                if (mergedMap == null)
                {
                    mergedMap = new Dictionary<ROM, List<FieldDefPair>>(subEntry.FieldMap.Count);
                }

                foreach (var subField in subEntry.FieldMap)
                {
                    if (!mergedMap.TryGetValue(subField.Key, out var list))
                    {
                        list = new List<FieldDefPair>(subField.Value.Count);
                        mergedMap[subField.Key] = list;
                    }
                    list.AddRange(subField.Value);
                }
            }

            return mergedMap != null ? new CacheEntry(mergedMap) : null;
        }
    }

    // ── Conflict checking methods ──

    /// <summary>
    /// Check that all fields in a group have compatible output types.
    /// Optimization: compare all against the first; only do full pairwise if a mismatch is found.
    /// </summary>
    private static void RequireSameOutputTypeShape(
        ROM responseName,
        List<FieldDefPair> fields,
        HashSet<(nint, nint)> reportedPairs,
        ref List<Conflict>? conflicts)
    {
        var type0 = fields[0].FieldDef?.ResolvedType;

        // Quick check: compare all against the first
        bool allMatch = true;
        if (type0 != null)
        {
            for (int i = 1; i < fields.Count; i++)
            {
                var typeI = fields[i].FieldDef?.ResolvedType;
                if (typeI != null && DoTypesConflict(type0, typeI))
                {
                    allMatch = false;
                    break;
                }
            }
        }
        else
        {
            // If first has no type, check if any pair has conflicting types
            for (int i = 0; i < fields.Count && allMatch; i++)
            {
                var typeI = fields[i].FieldDef?.ResolvedType;
                if (typeI == null)
                    continue;
                for (int j = i + 1; j < fields.Count; j++)
                {
                    var typeJ = fields[j].FieldDef?.ResolvedType;
                    if (typeJ != null && DoTypesConflict(typeI, typeJ))
                    {
                        allMatch = false;
                        break;
                    }
                }
            }
        }

        if (allMatch)
            return;

        // Full pairwise comparison for error reporting
        for (int i = 0; i < fields.Count; i++)
        {
            var typeI = fields[i].FieldDef?.ResolvedType;
            if (typeI == null)
                continue;

            for (int j = i + 1; j < fields.Count; j++)
            {
                // Skip identical field references (duplicates from fragment expansion)
                if (ReferenceEquals(fields[i].Field, fields[j].Field))
                    continue;

                var typeJ = fields[j].FieldDef?.ResolvedType;
                if (typeJ != null && DoTypesConflict(typeI, typeJ))
                {
                    if (TryAddReportedPair(reportedPairs, fields[i].Field, fields[j].Field))
                    {
                        (conflicts ??= new()).Add(new Conflict
                        {
                            Reason = new ConflictReason
                            {
                                Name = (string)responseName,
                                Message = new Message
                                {
                                    Msg = $"they return conflicting types {typeI} and {typeJ}"
                                }
                            },
                            FieldsLeft = new List<ISelectionNode> { fields[i].Field },
                            FieldsRight = new List<ISelectionNode> { fields[j].Field }
                        });
                    }
                }
            }
        }
    }

    /// <summary>
    /// Check that all fields in a (common-parent) group have the same underlying field name and arguments.
    /// Optimization: compare all against the first; only do full pairwise if a mismatch is found.
    /// </summary>
    private static void RequireSameNameAndArguments(
        ROM responseName,
        List<FieldDefPair> fields,
        HashSet<(nint, nint)> reportedPairs,
        ref List<Conflict>? conflicts)
    {
        var node0 = fields[0].Field;
        var name0 = node0.GetName();
        var args0 = node0.GetArguments();

        // Quick check: compare all against the first
        bool allMatch = true;
        for (int i = 1; i < fields.Count; i++)
        {
            // Skip identical field references
            if (ReferenceEquals(fields[i].Field, node0))
                continue;

            var nameI = fields[i].Field.GetName();
            if (name0 != nameI || !SameArguments(args0, fields[i].Field.GetArguments()))
            {
                allMatch = false;
                break;
            }
        }

        if (allMatch)
            return;

        // Full pairwise comparison for error reporting
        for (int i = 0; i < fields.Count; i++)
        {
            for (int j = i + 1; j < fields.Count; j++)
            {
                if (ReferenceEquals(fields[i].Field, fields[j].Field))
                    continue;

                var nameI = fields[i].Field.GetName();
                var nameJ = fields[j].Field.GetName();

                if (nameI != nameJ)
                {
                    if (TryAddReportedPair(reportedPairs, fields[i].Field, fields[j].Field))
                    {
                        (conflicts ??= new()).Add(new Conflict
                        {
                            Reason = new ConflictReason
                            {
                                Name = (string)responseName,
                                Message = new Message
                                {
                                    Msg = $"{nameI} and {nameJ} are different fields"
                                }
                            },
                            FieldsLeft = new List<ISelectionNode> { fields[i].Field },
                            FieldsRight = new List<ISelectionNode> { fields[j].Field }
                        });
                    }
                }
                else if (!SameArguments(fields[i].Field.GetArguments(), fields[j].Field.GetArguments()))
                {
                    if (TryAddReportedPair(reportedPairs, fields[i].Field, fields[j].Field))
                    {
                        (conflicts ??= new()).Add(new Conflict
                        {
                            Reason = new ConflictReason
                            {
                                Name = (string)responseName,
                                Message = new Message
                                {
                                    Msg = "they have differing arguments"
                                }
                            },
                            FieldsLeft = new List<ISelectionNode> { fields[i].Field },
                            FieldsRight = new List<ISelectionNode> { fields[j].Field }
                        });
                    }
                }
            }
        }
    }

    /// <summary>
    /// Groups fields by common parent types as per the Simon Adameit algorithm.
    /// Fields with the same concrete parent type go into the same group.
    /// Fields with abstract (interface/union) parents are included in every concrete group.
    /// If there are no concrete parents, all fields form a single group.
    /// </summary>
    private static List<List<FieldDefPair>> GroupByCommonParents(List<FieldDefPair> fields)
    {
        List<FieldDefPair>? abstractGroup = null;
        Dictionary<IGraphType, List<FieldDefPair>>? concreteGroups = null;

        for (int i = 0; i < fields.Count; i++)
        {
            var field = fields[i];
            if (field.ParentType is IObjectGraphType)
            {
                concreteGroups ??= new(ReferenceEqualityComparer.Instance);
                if (!concreteGroups.TryGetValue(field.ParentType, out var group))
                {
                    group = new List<FieldDefPair>();
                    concreteGroups[field.ParentType] = group;
                }
                group.Add(field);
            }
            else
            {
                (abstractGroup ??= new()).Add(field);
            }
        }

        if (concreteGroups == null || concreteGroups.Count == 0)
        {
            // No concrete parents at all — all fields in one group
            return new List<List<FieldDefPair>> { fields };
        }

        var result = new List<List<FieldDefPair>>(concreteGroups.Count);
        foreach (var group in concreteGroups.Values)
        {
            if (abstractGroup != null)
            {
                // Merge abstract fields into each concrete group
                group.AddRange(abstractGroup);
            }
            result.Add(group);
        }

        return result;
    }

    // ── Helpers ──

    // Given a series of Conflicts which occurred between two sub-fields,
    // generate a single Conflict.
    private static Conflict? SubfieldConflicts(
        List<Conflict>? conflicts,
        ROM responseName,
        ISelectionNode node1,
        ISelectionNode node2)
    {
        if (conflicts?.Count > 0)
        {
            return new Conflict
            {
                Reason = new ConflictReason
                {
                    Name = (string)responseName,
                    Message = new Message
                    {
                        Msgs = conflicts.Select(c => c.Reason).ToList()
                    }
                },
                FieldsLeft = conflicts.Aggregate(new List<ISelectionNode> { node1 }, (allfields, conflict) =>
                {
                    allfields.AddRange(conflict.FieldsLeft);
                    return allfields;
                }),
                FieldsRight = conflicts.Aggregate(new List<ISelectionNode> { node2 }, (allfields, conflict) =>
                {
                    allfields.AddRange(conflict.FieldsRight);
                    return allfields;
                })
            };
        }

        return null;
    }

    /// <summary>
    /// Adds a pair of field nodes to the reported set (order-independent).
    /// Returns true if the pair was newly added (not previously reported).
    /// </summary>
    private static bool TryAddReportedPair(HashSet<(nint, nint)> reportedPairs, ISelectionNode a, ISelectionNode b)
    {
        var ha = RuntimeHelpers.GetHashCode(a);
        var hb = RuntimeHelpers.GetHashCode(b);
        // Use consistent ordering so (a,b) and (b,a) are treated as the same pair
        var pair = ha <= hb ? ((nint)ha, (nint)hb) : ((nint)hb, (nint)ha);
        return reportedPairs.Add(pair);
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
        if (arguments1 == null && arguments2 == null)
            return true;

        if (arguments1 != null && arguments2 == null)
            return false;

        if (arguments1 == null && arguments2 != null)
            return false;

        if (arguments1!.Count != arguments2!.Count)
        {
            return false;
        }

        return arguments1.All(arg1 =>
        {
            var arg2 = arguments2.FirstOrDefault(x => x.Name == arg1.Name);

            if (arg2 == null)
            {
                return false;
            }
            return SameValue(arg1, arg2);
        });
    }

    private static bool SameValue(GraphQLArgument arg1, GraphQLArgument arg2)
    {
        // normalize values prior to comparison by using ASTNode.Print
        return arg1.Value is null && arg2.Value is null ||
            arg1.Value is not null && arg2.Value is not null && arg1.Value.Print() == arg2.Value.Print();
    }

    private sealed class FieldDefPair
    {
        public IGraphType? ParentType { get; set; } = null!;
        public ISelectionNode Field { get; set; } = null!;
        public FieldType? FieldDef { get; set; }
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

    /// <summary>
    /// Provides a reference equality comparer for <see cref="IGraphType"/> instances.
    /// </summary>
    private sealed class ReferenceEqualityComparer : IEqualityComparer<IGraphType>
    {
        public static readonly ReferenceEqualityComparer Instance = new();
        public bool Equals(IGraphType? x, IGraphType? y) => ReferenceEquals(x, y);
        public int GetHashCode(IGraphType obj) => RuntimeHelpers.GetHashCode(obj);
    }
}

internal static class ISelectionNodeExtensions
{
    public static ROM GetName(this ISelectionNode selection)
         => selection switch
         {
             GraphQLField field => field.Name,
             GraphQLFragmentSpread fragmentSpread => fragmentSpread.FragmentName.Name,
             _ => default
         };

    public static GraphQLArguments? GetArguments(this ISelectionNode selection)
    {
        return selection is GraphQLField field
            ? field.Arguments
            : null;
    }

    public static GraphQLSelectionSet? GetSelectionSet(this ISelectionNode selection)
        => selection switch
        {
            GraphQLField field => field.SelectionSet,
            GraphQLInlineFragment inlineFragment => inlineFragment.SelectionSet,
            _ => null
        };
}
