using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace GraphQL.Types;

internal static class InputObjectGraphTypeHelper
{
    public static Func<IDictionary<string, object?>, object> BuildParseDictionaryMethod(
        IInputObjectGraphType graphType,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type sourceType)
    {
        if (sourceType == typeof(object))
            return static x => x; //pass through for InputObjectGraphType<object> or untyped InputObjectGraphType
        // get list of fields
        var fields = new List<(string Key, string MemberName, IGraphType ResolvedType)>();
        foreach (var field in graphType.Fields)
        {
            var fieldName = field.GetMetadata<string>(InputObjectGraphType.ORIGINAL_EXPRESSION_PROPERTY_NAME) ?? field.Name;
            var resolvedType = field.ResolvedType
                ?? throw new InvalidOperationException($"Field '{field.Name}' of graph type '{graphType.Name}' does not have the ResolvedType property set.");
            fields.Add((field.Name, fieldName, resolvedType));
        }
        // validate that no two different fields use the same member
        var memberNames = new HashSet<string>(fields.Select(x => x.MemberName));
        if (memberNames.Count != fields.Count)
            throw new InvalidOperationException($"Two fields within graph type '{graphType.Name}' were mapped to the same member.");
        // find best constructor to use, with preference to the constructor with the most parameters
        var bestConstructor = AutoRegisteringHelper.GetConstructor(sourceType);
        // pull out parameters that are applicable for that constructor
        var ctorParameters = bestConstructor.GetParameters();
        var ctorFields = ctorParameters.Join(fields, param => param.Name, field => field.MemberName, (_, x) => x, StringComparer.InvariantCultureIgnoreCase).ToList();
        // find other members
        var objProperties = sourceType.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(x => x.CanWrite).OrderBy(x => x.DeclaringType == sourceType ? 1 : 2).ToList();
        var objFields = sourceType.GetFields(BindingFlags.Instance | BindingFlags.Public).OrderBy(x => x.DeclaringType == sourceType ? 1 : 2).ToList();
        var members = new List<(string Key, MemberInfo Member, Type Type, IGraphType ResolvedType)>(fields.Count - ctorFields.Count);
        if ((fields.Count - ctorFields.Count) > 0)
        {
            foreach (var field in fields)
            {
                // skip members within constructors
                if (ctorFields.Contains(field))
                    continue;
                // check properties
                var objProp = objProperties.SingleOrDefault(x => string.Equals(x.Name, field.MemberName, StringComparison.OrdinalIgnoreCase));
                if (objProp != null)
                {
                    members.Add((field.Key, objProp, objProp.PropertyType, field.ResolvedType));
                    continue;
                }
                var objField = objFields.SingleOrDefault(x => string.Equals(x.Name, field.MemberName, StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidOperationException($"Cannot find member named '{field.MemberName}' on CLR type '{sourceType.GetFriendlyName()}'.");
                members.Add((field.Key, objField, objField.FieldType, field.ResolvedType));
            }
        }
        // create expression; start with the parameter
        // then build the members
        var initExpression = Expression.MemberInit(
            Expression.New(bestConstructor, ctorFields.Select((f, i) => GetExpressionForParameter(f.Key, ctorParameters[i].ParameterType, f.ResolvedType))),
            members.Select(member => Expression.Bind(member.Member, GetExpressionForParameter(member.Key, member.Type, member.ResolvedType))));
        // build the lambda
        var lambda = Expression.Lambda<Func<IDictionary<string, object?>, object>>(Expression.Convert(initExpression, typeof(object)), _dictionaryParam);
        return lambda.Compile();
    }

    private static Expression GetExpressionForParameter(string key, Type type, IGraphType graphType)
    {
        var v = Expression.Variable(typeof(object));
        var v2 = Expression.Variable(type);
        return Expression.Block(
            new[] { v, v2 },
            Expression.IfThenElse(
                Expression.Call(_dictionaryParam, _tryGetDefaultMethod, Expression.Constant(key), v),
                Expression.Assign(v2, CoerceExpression(v, type, graphType)),
                Expression.Assign(v2, Expression.Default(type))),
            v2);
    }

    private static Expression CoerceExpression(Expression expr, Type type, IGraphType graphType)
    {
        // unwrap non-null graph type
        graphType = graphType is NonNullGraphType nonNullGraphType
            ? nonNullGraphType.ResolvedType ?? throw new InvalidOperationException($"ResolvedType is null for graph type '{nonNullGraphType}'.")
            : graphType;

        if (graphType is ListGraphType listGraphType)
        {
            var elementGraphType = listGraphType.ResolvedType ?? throw new InvalidOperationException($"ResolvedType is null for graph type '{listGraphType}'.");
            // determine the type of list to create
            var isArray = false;
            Type? elementType = null;
            if (type.IsArray)
            {
                elementType = type.GetElementType()!;
                isArray = true;
            }
            else if (type == typeof(IEnumerable) || type == typeof(object))
            {
                elementType = typeof(object);
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                elementType = type.GetGenericArguments()[0];
            }
            else if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                elementType = type.GetInterfaces()
                    .FirstOrDefault(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    ?.GetGenericArguments()[0];
            }
            if (elementType == null)
                throw new InvalidOperationException($"Could not determine enumerable type for type '{type.GetFriendlyName()}' while coercing graph type '{graphType}'.");
            // create an expression that represents this:
            // (IEnumerable<object>?)expr?.Select(x => CoerceExpression(x, elementType, listGraphType.ResolvedType))
            var loopVar = Expression.Parameter(typeof(object));
            var loopContent = CoerceExpression(loopVar, elementType, elementGraphType);
            var expr2 = Select(Expression.Convert(expr, typeof(IEnumerable)), loopVar, loopContent, isArray);
            return Expression.Convert(expr2, type);
        }

        // expr will always be of type object; if the field is expecting type object, return it
        if (expr.Type == type)
            return expr;

        // var ret = (expr == null || type.IsAssignableFrom(expr.GetType())
        //     ? expr
        //     : graphType is IInputObjectGraphType
        //         ? InputObjectGraphTypeHelper.ToObjectImplementation(expr, type, graphType)
        //         : ValueConverter.ConvertTo(expr, type);
        // return (type)ret;
        var ret = Ternary(
            Expression.OrElse(Expression.Equal(expr, Expression.Constant(null, expr.Type)), Expression.TypeIs(expr, type)),
            expr,
            graphType is IInputObjectGraphType
                ? Expression.Call(_toObjectMethod, expr, Expression.Constant(type), Expression.Constant(graphType))
                : Expression.Call(_convertToMethod, expr, Expression.Constant(type)));
        return Expression.Convert(ret, type);
    }

    private static readonly MethodInfo _toObjectMethod = typeof(InputObjectGraphTypeHelper).GetMethod(nameof(ToObjectImplementation), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static object ToObjectImplementation(object source, Type type, IInputObjectGraphType mappedType)
    {
        // this function should not execute unless an untyped InputObjectGraphType is in use
        // this could be optimized away by recursively calling BuildParseDictionaryMethod as needed,
        // keeping in mind that circular references are allowed in GraphQL, and that compiled
        // ParseDictionary methods must not be cached with the graph type instance as a key
        // or a memory leak will occur for scoped schemas
        var dic = (IDictionary<string, object?>)source;
        if (ValueConverter.TryConvertTo(dic, type, out object? result, typeof(IDictionary<string, object>)))
            return result!;
        return ObjectExtensions.ToObject(dic, type, mappedType);
    }
    private static readonly MethodInfo _convertToMethod = typeof(InputObjectGraphTypeHelper).GetMethod(nameof(ConvertToImplementation), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static object? ConvertToImplementation(object? value, Type targetType) => ValueConverter.ConvertTo(value, targetType);

    private static Expression Ternary(Expression expr, Expression thenExpr, Expression elseExpr)
    {
        if (thenExpr.Type != elseExpr.Type)
            throw new InvalidOperationException("Ternary expressions must return the same type.");
        var retVariable = Expression.Variable(thenExpr.Type);
        return Expression.Block(
            new[] { retVariable },
            Expression.IfThenElse(
                expr,
                Expression.Assign(retVariable, thenExpr),
                Expression.Assign(retVariable, elseExpr)),
            retVariable);
    }

    private static Expression Select(Expression collection, ParameterExpression loopVar, Expression loopContent, bool asArray)
    {
        var collectionVar = Expression.Variable(collection.Type);
        var enumeratorVar = Expression.Variable(typeof(IEnumerator));
        var getEnumeratorCall = Expression.Call(collectionVar, _getEnumeratorMethod);
        var moveNextCall = Expression.Call(enumeratorVar, _moveNextMethod);
        var getCurrent = Expression.MakeMemberAccess(enumeratorVar, _currentProperty);

        var breakLabel = Expression.Label("label1");
        var returnLabel = Expression.Label("label2");

        var listVariable = Expression.Variable(typeof(List<>).MakeGenericType(loopContent.Type));
        var returnVariable = Expression.Variable(asArray ? loopContent.Type.MakeArrayType() : listVariable.Type);
        var addMethod = listVariable.Type.GetMethod(nameof(List<object>.Add))!;
        var toArrayMethod = listVariable.Type.GetMethod(nameof(List<object>.ToArray))!;

        var loop = Expression.Block(
            returnVariable.Type,
            new[] { collectionVar, enumeratorVar, listVariable, returnVariable },
            Expression.Assign(collectionVar, collection),
            Expression.Assign(returnVariable, Expression.Constant(null, returnVariable.Type)),
            Expression.IfThen(
                Expression.Equal(collectionVar, Expression.Constant(null, collection.Type)),
                Expression.Goto(returnLabel)),
            Expression.Assign(enumeratorVar, getEnumeratorCall),
            Expression.Assign(listVariable, Expression.New(listVariable.Type)),
            Expression.Loop(
                Expression.IfThenElse(
                    Expression.Equal(moveNextCall, Expression.Constant(true)),
                    Expression.Block(new[] { loopVar },
                        Expression.Assign(loopVar, getCurrent),
                        Expression.Call(listVariable, addMethod, loopContent)),
                    Expression.Break(breakLabel)),
                breakLabel),
            Expression.Assign(returnVariable, asArray ? Expression.Call(listVariable, toArrayMethod) : listVariable),
            Expression.Label(returnLabel),
            returnVariable);

        return loop;
    }

    private static readonly MethodInfo _getEnumeratorMethod = typeof(IEnumerable).GetMethod(nameof(IEnumerable.GetEnumerator))!;
    private static readonly MethodInfo _moveNextMethod = typeof(IEnumerator).GetMethod(nameof(IEnumerator.MoveNext))!;
    private static readonly PropertyInfo _currentProperty = typeof(IEnumerator).GetProperty(nameof(IEnumerator.Current))!;
    private static readonly ParameterExpression _dictionaryParam = Expression.Parameter(typeof(IDictionary<string, object?>));
    private static readonly MethodInfo _tryGetDefaultMethod = typeof(IDictionary<string, object?>).GetMethod("TryGetValue")!;
}
