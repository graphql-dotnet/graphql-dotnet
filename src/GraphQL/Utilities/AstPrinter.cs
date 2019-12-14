using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using GraphQL.Language.AST;

namespace GraphQL.Utilities
{
    public static class AstPrinter
    {
        // new-ing AstPrintVisitor every time we called AstPrinter
        // was killing the performance of introspection queries (20-30% of the call time)
        // because of continually re-running its constructor lambdas
        // so we cache one copy of it here - it's not changed ever anyway
        private static readonly AstPrintVisitor Visitor = new AstPrintVisitor();

        public static string Print(INode node)
        {
            var result = Visitor.Visit(node);
            return result?.ToString() ?? string.Empty;
        }
    }

    public abstract class AstPrintConfig
    {
        private List<AstPrintFieldDefinition> _fields;

        public IEnumerable<AstPrintFieldDefinition> Fields => _fields;

        public abstract Func<INode, bool> Matches { get; }

        public abstract Func<IDictionary<string, object>, object> PrintAst { get; }

        public void Field(AstPrintFieldDefinition field)
        {
            if (_fields == null)
                _fields = new List<AstPrintFieldDefinition>();
            else if (_fields.Exists(x => x.Name == field.Name))
                throw new ExecutionError($"A field with name \"{field.Name}\" already exists!");

            _fields.Add(field);
        }
    }

    public class PrintFormat<T>
    {
        private readonly IDictionary<string, object> _args;

        public PrintFormat(IDictionary<string, object> args)
        {
            _args = args;
        }

        public object Arg(string key)
        {
            _args.TryGetValue(key, out var arg);
            return arg;
        }

        public TVal Arg<TVal>(string key)
        {
            return (TVal)Arg(key);
        }

        public object Arg<TProperty>(Expression<Func<T, TProperty>> argument)
        {
            var name = argument.NameOf();
            return Arg(name);
        }

        public IEnumerable<object> ArgArray<TProperty>(Expression<Func<T, TProperty>> argument)
        {
            var name = argument.NameOf();
            return Arg<IEnumerable<object>>(name);
        }
    }

    public class AstPrintConfig<T> : AstPrintConfig
        where T : INode
    {
        private readonly Func<IDictionary<string, object>, object> _printAst;

        public AstPrintConfig(Func<PrintFormat<T>, object> configure)
        {
            _printAst = args =>
            {
                var f = new PrintFormat<T>(args);
                return configure(f);
            };
        }

        public override Func<INode, bool> Matches => node => node is T;

        public override Func<IDictionary<string, object>, object> PrintAst => _printAst;

        public void Field<TProperty>(Expression<Func<T, TProperty>> resolve)
        {
            var name = resolve.NameOf();
            var def = new AstPrintFieldDefinition
            {
                Name = name,
                Resolver = new ExpressionValueResolver<T, TProperty>(resolve)
            };

            Field(def);
        }
    }

    public class AstPrintFieldDefinition
    {
        public string Name { get; set; }
        public IValueResolver Resolver { get; set; }
    }

    public class ResolveValueContext
    {
        public object Source { get; set; }

        public TType SourceAs<TType>()
        {
            if (Source != null)
            {
                return (TType)Source;
            }

            return default;
        }
    }

    public interface IValueResolver
    {
        object Resolve(ResolveValueContext context);
    }

    public interface IValueResolver<T> : IValueResolver
    {
        new T Resolve(ResolveValueContext context);
    }

    public class ExpressionValueResolver<TObject, TProperty> : IValueResolver<TProperty>
    {
        private readonly Func<TObject, TProperty> _property;

        public ExpressionValueResolver(Expression<Func<TObject, TProperty>> property)
        {
            _property = property.Compile();
        }

        public TProperty Resolve(ResolveValueContext context)
        {
            return _property(context.SourceAs<TObject>());
        }

        object IValueResolver.Resolve(ResolveValueContext context)
        {
            return Resolve(context);
        }
    }

    public class AstPrintVisitor
    {
        private readonly List<AstPrintConfig> _configs = new List<AstPrintConfig>();

        public AstPrintVisitor()
        {
            Config<Document>(
                f =>
                {
                    var ops = Join(f.ArgArray(x => x.Operations), "\n\n");
                    var frags = Join(f.ArgArray(x => x.Fragments), "\n\n");

                    var result = Join(new[] { ops, frags }, "\n\n") + "\n";
                    return result;
                },
                c =>
                {
                    c.Field(x => x.Operations);
                    c.Field(x => x.Fragments);
                }
            );

            Config<Operation>(
                f =>
                {
                    var op = f.Arg(x => x.OperationType).ToString().ToLower(CultureInfo.InvariantCulture);
                    var name = f.Arg(x => x.Name)?.ToString();
                    var variables = Wrap("(", Join(f.ArgArray(x => x.Variables), ", "), ")");
                    var directives = Join(f.ArgArray(x => x.Directives), " ");
                    var selectionSet = f.Arg(x => x.SelectionSet);

                    return string.IsNullOrWhiteSpace(name)
                           && string.IsNullOrWhiteSpace(directives)
                           && string.IsNullOrWhiteSpace(variables)
                        ? selectionSet
                        : Join(new[] { op, Join(new[] { name, variables }, ""), directives, selectionSet }, " ");
                },
                c =>
                {
                    c.Field(x => x.OperationType);
                    c.Field(x => x.Name);
                    c.Field(x => x.Variables);
                    c.Field(x => x.Directives);
                    c.Field(x => x.SelectionSet);
                }
            );

            Config<InlineFragment>(
                f =>
                {
                    var directives = Join(f.ArgArray(x => x.Directives), " ");
                    var selectionSet = f.Arg(x => x.SelectionSet);
                    var typename = f.Arg(x => x.Type);
                    var body = string.IsNullOrWhiteSpace(directives)
                        ? selectionSet
                        : Join(new[] { directives, selectionSet }, " ");

                    return $"... on {typename} {body}";
                },
                c =>
                {
                    c.Field(x => x.Directives);
                    c.Field(x => x.SelectionSet);
                    c.Field(x => x.Type);
                }
            );

            Config<VariableDefinition>(
                f => $"${f.Arg(x => x.Name)}: {f.Arg(x => x.Type)}",
                c =>
                {
                    c.Field(x => x.Name);
                    c.Field(x => x.Type);
                    c.Field(x => x.DefaultValue);
                }
            );

            Config<SelectionSet>(
                f => Block(f.ArgArray(x => x.Selections)),
                c => c.Field(x => x.Selections)
            );

            Config<Arguments>(
                f => f.Arg(x => x.Children),
                c => c.Field(x => x.Children)
            );

            Config<Argument>(
                f => $"{f.Arg(x => x.Name)}: {f.Arg(x => x.Value)}",
                c =>
                {
                    c.Field(x => x.Name);
                    c.Field(x => x.Value);
                }
            );

            Config<Field>(
                f =>
                {
                    var alias = f.Arg(x => x.Alias);
                    var name = f.Arg(x => x.Name);
                    var args = Join(f.ArgArray(x => x.Arguments), ", ");
                    var directives = Join(f.ArgArray(x => x.Directives), " ");
                    var selectionSet = f.Arg(x => x.SelectionSet);

                    var result = Join(new[]
                    {
                            Wrap("", alias, ": ") + name + Wrap("(", args, ")"),
                            directives,
                            selectionSet
                        }, " ");
                    return result;
                },
                c =>
                {
                    c.Field(x => x.Alias);
                    c.Field(x => x.Name);
                    c.Field(x => x.Arguments);
                    c.Field(x => x.Directives);
                    c.Field(x => x.SelectionSet);
                }
            );

            // Value

            Config<VariableReference>(
                f => $"${f.Arg(x => x.Name)}",
                c => c.Field(x => x.Name)
            );

            Config<NullValue>(f => "null");

            Config<IntValue>(
                f => f.Arg(x => x.Value),
                c => c.Field(x => x.Value)
            );

            Config<UIntValue>(
                f => f.Arg(x => x.Value),
                c => c.Field(x => x.Value)
            );

            Config<ULongValue>(
                f => f.Arg(x => x.Value),
                c => c.Field(x => x.Value)
            );

            Config<ByteValue>(
                f => f.Arg(x => x.Value),
                c => c.Field(x => x.Value)
            );

            Config<SByteValue>(
                f => f.Arg(x => x.Value),
                c => c.Field(x => x.Value)
            );

            Config<ShortValue>(
                f => f.Arg(x => x.Value),
                c => c.Field(x => x.Value)
            );

            Config<UShortValue>(
                f => f.Arg(x => x.Value),
                c => c.Field(x => x.Value)
            );

            Config<DecimalValue>(
                f => f.Arg(x => x.Value),
                c => c.Field(x => x.Value)
            );

            Config<LongValue>(
                f => f.Arg(x => x.Value),
                c => c.Field(x => x.Value)
            );

            Config<BigIntValue>(
                f => f.Arg(x => x.Value),
                c => c.Field(x => x.Value)
            );

            Config<FloatValue>(
                f =>
                {
                    var val = (double)f.Arg(x => x.Value);
                    return val.ToString("0.0##############", CultureInfo.InvariantCulture);
                },
                c => c.Field(x => x.Value)
            );

            Config<StringValue>(
                f =>
                {
                    var val = f.Arg(x => x.Value);
                    if (!string.IsNullOrWhiteSpace(val?.ToString()) && !val.ToString().StartsWith("\"", StringComparison.InvariantCulture))
                    {
                        val = $"\"{val}\"";
                    }
                    return val;
                },
                c => c.Field(x => x.Value)
            );

            Config<BooleanValue>(
                f => f.Arg(x => x.Value)?.ToString().ToLower(CultureInfo.InvariantCulture),
                c => c.Field(x => x.Value)
            );

            Config<EnumValue>(
                f => f.Arg(x => x.Name),
                c => c.Field(x => x.Name)
            );

            Config<ListValue>(
                f => $"[{Join(f.ArgArray(x => x.Values), ", ")}]",
                c => c.Field(x => x.Values)
            );

            Config<ObjectValue>(
                f => $"{{{Join(f.ArgArray(x => x.ObjectFields), ", ")}}}",
                c => c.Field(x => x.ObjectFields)
            );

            Config<ObjectField>(
                f => $"{f.Arg(x => x.Name)}: {f.Arg(x => x.Value)}",
                c =>
                {
                    c.Field(x => x.Name);
                    c.Field(x => x.Value);
                }
            );

            Config<UriValue>(
                f => f.Arg(x => x.Value)?.ToString().ToLower(CultureInfo.InvariantCulture),
                c => c.Field(x => x.Value)
            );

            // Directive
            Config<Directive>(
                f =>
                {
                    var name = f.Arg(x => x.Name);
                    var args = Join(f.ArgArray(x => x.Arguments), ", ");
                    return $"@{name}" + Wrap("(", args, ")");
                },
                c =>
                {
                    c.Field(x => x.Name);
                    c.Field(x => x.Arguments);
                }
            );

            // Type
            Config<NamedType>(
                f => f.Arg(x => x.Name),
                c => c.Field(x => x.Name)
            );

            Config<ListType>(
                f => $"[{f.Arg(x => x.Type)}]",
                c => c.Field(x => x.Type)
            );

            Config<NonNullType>(
                f => $"{f.Arg(x => x.Type)}!",
                c => c.Field(x => x.Type)
            );

            // Type System Definitions
        }

        public void Config<T>(Func<PrintFormat<T>, object> printAst, Action<AstPrintConfig<T>> configure = null)
            where T : INode
        {
            if (_configs.Any(c => c is AstPrintConfig<T>))
                throw new ExecutionError($"A config for \"{typeof(T).Name}\" already exists!");

            var config = new AstPrintConfig<T>(printAst);
            configure?.Invoke(config);
            _configs.Add(config);
        }

        private string Join(IEnumerable<object> nodes, string separator)
        {
            return nodes != null
                ? string.Join(
                    separator,
                    nodes.Where(n => n != null)
                        .Where(n => !string.IsNullOrWhiteSpace(n.ToString()))
                        .Select(n => n.ToString()))
                : "";
        }

        private string Block(IEnumerable<object> nodes)
        {
            var list = nodes.ToList();
            return list.Any()
                ? Indent($"{{\n{Join(list, "\n")}") + "\n}"
                : "";
        }

        private string Wrap(string start, object middle, string end)
        {
            var m = middle?.ToString() ?? "";
            return !string.IsNullOrWhiteSpace(m)
                ? $"{start}{m}{end}"
                : "";
        }

        private string Indent(string str)
        {
            return Regex.Replace(str, "\n", "\n  ");
        }

        public object Visit(INode node)
        {
            return ApplyConfig(node);
        }

        public object ApplyConfig(INode node)
        {
            var config = _configs.SingleOrDefault(c => c.Matches(node));

            if (config != null)
            {
                var vals = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

                config.Fields?.Apply(f =>
                {
                    var ctx = new ResolveValueContext
                    {
                        Source = node
                    };

                    var result = f.Resolver.Resolve(ctx);
                    if (result is INode nodeResult)
                    {
                        result = ApplyConfig(nodeResult);
                    }
                    else if (!(result is string) && result is IEnumerable enumerable)
                    {
                        result = GetListResult(enumerable);
                    }

                    vals[f.Name] = result;
                });

                return config.PrintAst(vals);
            }

            return null;
        }

        private object GetListResult(IEnumerable enumerable)
        {
            var list = new List<object>();
            foreach (var item in enumerable)
            {
                if (item is INode node)
                {
                    var listResult = ApplyConfig(node);
                    if (listResult != null)
                    {
                        list.Add(listResult);
                    }
                }
            }
            return list;
        }
    }
}
