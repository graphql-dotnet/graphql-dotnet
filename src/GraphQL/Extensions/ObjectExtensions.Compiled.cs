using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using GraphQL.Conversion;
using GraphQL.Types;

namespace GraphQL;

public static partial class ObjectExtensions
{
    /// <summary>
    /// Compiles a function to convert a dictionary to an object based on a specified <see cref="IInputObjectGraphType"/> instance.
    /// The compiled function assumes the passed dictionary object is not <see langword="null"/>.
    /// </summary>
    [RequiresDynamicCode("This method uses expression trees to compile code at runtime.")]
    public static Func<IDictionary<string, object?>, object> CompileToObject(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
        Type sourceType,
        IInputObjectGraphType graphType,
        IValueConverter valueConverter)
    {
        var conv = valueConverter.GetConversion(typeof(IDictionary<string, object?>), sourceType);
        if (conv != null)
            return conv;

        var info = GetReflectionInformation(sourceType, graphType);
        try
        {
            return CompileToObject(info, valueConverter);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to compile input object conversion for CLR type '{sourceType.GetFriendlyName()}' and graph type '{graphType}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Compiles a function to convert a dictionary to an object based on a specified <see cref="ReflectionInfo"/> instance.
    /// </summary>
    [RequiresDynamicCode("This method uses expression trees to compile code at runtime.")]
    private static Func<IDictionary<string, object?>, object> CompileToObject(ReflectionInfo info, IValueConverter valueConverter)
    {
        var bestConstructor = info.Constructor;
        var ctorFields = info.CtorFields;
        var members = info.MemberFields;

        // T obj;
        var objParam = Expression.Variable(info.Type, "obj");

        // obj = new T(...) { ... };
        var expressions = new List<Expression>(members.Count(x => !x.IsRequired && !x.IsInitOnly) + 2)
        {
            Expression.Assign(
                objParam,
                Expression.MemberInit(
                    bestConstructor == null
                        ? Expression.New(info.Type) // implicit public parameterless constructor of structs
                        : Expression.New(bestConstructor, ctorFields.Select(GetExpressionForCtorParameter)),
                    members.Where(x => x.IsRequired || x.IsInitOnly).Select(GetBindingForMember)))
        };

        // set properties on obj when they exist in the dictionary
        foreach (var member in members.Where(x => !x.IsRequired && !x.IsInitOnly))
        {
            expressions.Add(ConditionallySetMember(objParam, member));
        }

        // return obj;
        expressions.Add(objParam);

        var block = Expression.Block(new[] { objParam }, expressions);

        // build the lambda
        var lambda = Expression.Lambda<Func<IDictionary<string, object?>, object>>(
            Expression.Convert(block, typeof(object)),
            _dictionaryParam);

        // compile the lambda and return it
        return lambda.Compile();

        Expression GetExpressionForCtorParameter(ReflectionInfo.CtorParameterInfo member)
        {
            if (member.Key == null)
                return Expression.Constant(member.ParameterInfo.DefaultValue, member.ParameterInfo.ParameterType);

            return GetCoerceOrDefault(
                member.Key,
                member.ParameterInfo.ParameterType,
                member.GraphType!,
                member.ParameterInfo.Name!);
        }

        MemberAssignment GetBindingForMember(ReflectionInfo.MemberFieldInfo member)
        {
            var type = member.Member is PropertyInfo propertyInfo ? propertyInfo.PropertyType : ((FieldInfo)member.Member).FieldType;

            return Expression.Bind(
                member.Member,
                GetCoerceOrDefault(member.Key, type, member.GraphType, member.Member.Name));
        }

        Expression GetCoerceOrDefault(string key, Type type, IGraphType graphType, string fieldName)
        {
            /*
             * ValueTuple<object?, bool> value;
             * T ret;
             * value = GetOrDefaultImplementation(dic, key);
             * if (value.Item2)
             * {
             *     ret = CoerceExpression<T>(value.Item1, member.MemberType, member.GraphType);
             * }
             * else
             * {
             *     ret = default(T);
             * }
             * return ret;
             */
            var param = Expression.Variable(typeof(ValueTuple<object?, bool>), "value");
            var ret = Expression.Variable(type, "ret");
            return Expression.Block(
                new[] { param, ret },
                Expression.Assign(
                    param,
                    Expression.Call(_getOrDefaultMethod, _dictionaryParam, Expression.Constant(key, typeof(string)))),
                Expression.IfThenElse(
                    Expression.Equal(
                        Expression.MakeMemberAccess(param, typeof(ValueTuple<object?, bool>).GetField("Item2")!),
                        Expression.Constant(true)),
                    Expression.Assign(
                        ret,
                        CoerceExpression(
                            Expression.MakeMemberAccess(param, typeof(ValueTuple<object?, bool>).GetField("Item1")!),
                            type,
                            graphType,
                            false,
                            fieldName)),
                    Expression.Assign(ret, Expression.Default(type))),
                ret);
        }

        Expression ConditionallySetMember(ParameterExpression objParam, ReflectionInfo.MemberFieldInfo member)
        {
            var type = member.Member is PropertyInfo propertyInfo ? propertyInfo.PropertyType : ((FieldInfo)member.Member).FieldType;

            /*
             * ValueTuple<object?, bool> value;
             * value = GetOrDefaultImplementation2(dic, key);
             * if (value.Item2)
             * {
             *     obj.Prop = CoerceExpression<T>(value.Item1, member.MemberType, member.GraphType);
             * }
             */
            var param = Expression.Variable(typeof(ValueTuple<object?, bool>), "value");
            return Expression.Block(
                [param],
                Expression.Assign(
                    param,
                    Expression.Call(_getOrDefaultMethod, _dictionaryParam, Expression.Constant(member.Key, typeof(string)))),
                Expression.IfThen(
                    Expression.Equal(
                        Expression.MakeMemberAccess(param, typeof(ValueTuple<object?, bool>).GetField("Item2")!),
                        Expression.Constant(true)),
                    Expression.Assign(
                        Expression.MakeMemberAccess(objParam, member.Member),
                        CoerceExpression(
                            Expression.MakeMemberAccess(param, typeof(ValueTuple<object?, bool>).GetField("Item1")!),
                            type,
                            member.GraphType,
                            false,
                            member.Member.Name))));
        }

        Expression CoerceExpression(Expression expr, Type type, IGraphType graphType, bool asObject, string fieldName)
        {
            // if requested type is object, return the expression as is
            if (type == typeof(object))
                return expr.Type == typeof(object) ? expr : Expression.Convert(expr, typeof(object));

            // if the expression is already of the requested type, return it as is,
            // or if the expression is null, return default(T)
            var returnType = asObject ? typeof(object) : type;
            var param = Expression.Parameter(expr.Type, "param");
            return Expression.Block(
                [param],
                Expression.Assign(param, expr),
                Expression.Condition(
                    Expression.TypeIs(param, type),
                    param.Type == returnType ? param : Expression.Convert(param, returnType),
                    Expression.Condition(
                        Expression.Equal(param, Expression.Constant(null, param.Type)),
                        Expression.Default(returnType),
                        CoerceExpressionInternal(param, type, graphType, asObject, fieldName))));
        }

        Expression CoerceExpressionInternal(Expression expr, Type type, IGraphType graphType, bool asObject, string fieldName)
        {
            // unwrap non-null graph type
            graphType = graphType is NonNullGraphType nonNullGraphType
                ? nonNullGraphType.ResolvedType ?? throw new InvalidOperationException($"ResolvedType is null for graph type '{nonNullGraphType}'.")
                : graphType;

            if (graphType is ListGraphType listGraphType)
            {
                var elementGraphType = listGraphType.ResolvedType ?? throw new InvalidOperationException($"ResolvedType is null for graph type '{listGraphType}'.");
                // create an expression that represents this:
                // var expr2 = ((IEnumerable)expr).ToObjectArray();
                // for (int i = 0; i < expr2.Length; i++)
                // {
                //     expr2[i] = CoerceExpression(elementType, expr2[i]);
                // }
                // ret = listConverter.GetConversion(type, elementType)(expr);

                IListConverter listConverter;
                try
                {
                    listConverter = valueConverter.GetListConverter(type);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to retrieve a list converter for type '{type.GetFriendlyName()}' for the list graph type '{graphType}' on field '{fieldName}': {ex.Message}", ex);
                }
                var underlyingType = Nullable.GetUnderlyingType(listConverter.ElementType) ?? listConverter.ElementType;
                Expression<Func<object?[], object>> converterExpression = (arg) => listConverter.Convert(arg);
                var arrayVariable = Expression.Variable(typeof(object?[]), "expr2");
                Expression ret = Expression.Block(
                    [arrayVariable],
                    Expression.Assign(arrayVariable, Expression.Call(_convertToObjectArrayMethod, expr)),
                    UpdateArray(arrayVariable, (loopVar) => CoerceExpression(loopVar, underlyingType, elementGraphType, true, fieldName)),
                    Expression.Invoke(converterExpression, arrayVariable));

                if (!asObject)
                    ret = Expression.Convert(ret, type);

                return ret;
            }

            var ret2 = Expression.Call(_getPropertyValueMethod, Expression.Constant(type), expr, Expression.Constant(graphType), Expression.Constant(valueConverter));
            return !asObject ? Expression.Convert(ret2, type) : ret2;
        }
    }

    private static readonly MethodInfo _convertToObjectArrayMethod = typeof(ObjectExtensions).GetMethod(nameof(ConvertToObjectArray), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static object?[] ConvertToObjectArray(object list)
    {
        if (list is IEnumerable enumerable)
            return enumerable.ToObjectArray();

        throw new InvalidOperationException($"Cannot coerce collection of type '{list?.GetType().GetFriendlyName()}' to IEnumerable.");
    }

    private static readonly MethodInfo _getPropertyValueMethod = typeof(ObjectExtensions).GetMethod(nameof(GetPropertyValue), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static object? GetPropertyValue(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields)]
        Type returnType, object? value, IGraphType mappedType, IValueConverter valueConverter)
    {
        // CoerceExpression already contains short-circuit logic for null and type compatibility

        // in the rare circumstance that the value is not a compatible type,
        //   use the reflection-based converter for object types, and
        //   the value converter for value types

        // note that during literal/variable parsing, input object graph types
        //   are typically already converted to the correct type, as ParseDictionary
        //   is called during parsing, so this code path would not normally be hit.

        // matches only when mappedType is an input object graph type AND the value is a
        //   dictionary (not yet parsed from a dictionary into an object)
        if (value is IDictionary<string, object?> dictionary && mappedType is IInputObjectGraphType inputObjectGraphType)
        {
            // note that ToObject checks the ValueConverter before parsing the dictionary
            return valueConverter.ToObject(dictionary, returnType, inputObjectGraphType);
        }

        return valueConverter.ConvertTo(value, returnType);
    }


    private static Expression UpdateArray(ParameterExpression objectArray, Func<ParameterExpression, Expression> loopContent)
    {
        /*
         * if (objectArray != null)
         * {
         *     for (int i = 0; i < objectArray.Length; i++)
         *     {
         *         var item = objectArray[i];
         *         objectArray[i] = loopContent(item);
         *     }
         * }
         */
        var indexVar = Expression.Variable(typeof(int), "i");
        var itemVar = Expression.Variable(typeof(object), "item");
        var breakLabel = Expression.Label("breakLabel");
        return Expression.IfThen(
            Expression.NotEqual(objectArray, Expression.Constant(null, objectArray.Type)),
            Expression.Block(
                [indexVar, itemVar],
                Expression.Assign(indexVar, Expression.Constant(0, indexVar.Type)),
                Expression.Loop(
                    Expression.IfThenElse(
                        Expression.LessThan(indexVar, Expression.ArrayLength(objectArray)),
                        Expression.Block(
                            Expression.Assign(itemVar, Expression.ArrayAccess(objectArray, indexVar)),
                            Expression.Assign(
                                Expression.ArrayAccess(objectArray, Expression.PostIncrementAssign(indexVar)),
                                loopContent(itemVar))),
                        Expression.Break(breakLabel)),
                    breakLabel)));
    }

    private static readonly ParameterExpression _dictionaryParam = Expression.Parameter(typeof(IDictionary<string, object?>), "dic");

    private static readonly MethodInfo _getOrDefaultMethod = typeof(ObjectExtensions).GetMethod(nameof(GetOrDefaultImplementation), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static ValueTuple<object?, bool> GetOrDefaultImplementation(IDictionary<string, object?> obj, string key)
        => obj.TryGetValue(key, out var value) ? ((object?, bool))(value, true) : (default, false);
}
