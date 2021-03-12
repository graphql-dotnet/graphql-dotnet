using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using GraphQL.Language.AST;

namespace GraphQL.Utilities
{
    public static class AstPrinter
    {
        // new-ing AstPrintVisitor every time we called AstPrinter
        // was killing the performance of introspection queries (20-30% of the call time)
        // because of continually re-running its constructor lambdas
        // so we cache one copy of it here - it's not changed ever anyway
        private static readonly AstPrintVisitor _visitor = new AstPrintVisitor();

        /// <summary>
        /// Returns a string representation of the specified node.
        /// </summary>
        public static string Print(INode node)
        {
            var result = _visitor.Visit(node);
            return result?.ToString() ?? string.Empty;
        }
    }

    internal class AstPrintConfig
    {
        internal List<AstPrintFieldDefinition> FieldsList { get; } = new List<AstPrintFieldDefinition>();
        public IEnumerable<AstPrintFieldDefinition> Fields => FieldsList;
        public Func<INode, bool> Matches { get; set; }
        public Func<IDictionary<string, object>, object> PrintAst { get; set; }

        public void Field(AstPrintFieldDefinition field)
        {
            if (FieldsList.Exists(x => x.Name == field.Name))
            {
                throw new ArgumentException($"A field with name '{field.Name}' already exists!", nameof(field));
            }

            FieldsList.Add(field);
        }
    }

    internal class PrintFormat<T>
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

    internal class AstPrintConfig<T> : AstPrintConfig
        where T : INode
    {
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

        public void Print(Func<PrintFormat<T>, object> configure)
        {
            PrintAst = args =>
            {
                var f = new PrintFormat<T>(args);
                return configure(f);
            };
        }
    }

    internal class AstPrintFieldDefinition
    {
        public string Name { get; set; }
        public IValueResolver Resolver { get; set; }
    }

    internal readonly struct ResolveValueContext
    {
        public ResolveValueContext(object source)
        {
            Source = source;
        }

        public object Source { get; }

        public TType SourceAs<TType>()
        {
            if (Source != null)
            {
                return (TType)Source;
            }

            return default;
        }
    }

    internal interface IValueResolver
    {
        object Resolve(in ResolveValueContext context);
    }

    internal interface IValueResolver<T> : IValueResolver
    {
        new T Resolve(in ResolveValueContext context);
    }

    internal class ExpressionValueResolver<TObject, TProperty> : IValueResolver<TProperty>
    {
        private readonly Func<TObject, TProperty> _property;

        public ExpressionValueResolver(Expression<Func<TObject, TProperty>> property)
        {
            _property = property.Compile();
        }

        public TProperty Resolve(in ResolveValueContext context)
        {
            return _property(context.SourceAs<TObject>());
        }

        object IValueResolver.Resolve(in ResolveValueContext context)
        {
            return Resolve(context);
        }
    }

    internal class AstPrintVisitor
    {
        private readonly List<AstPrintConfig> _configs = new List<AstPrintConfig>();

        public AstPrintVisitor()
        {
            Config<Document>(c =>
            {
                c.Field(x => x.Operations);
                c.Field(x => x.Fragments);
                c.Print(p =>
                {
                    var ops = Join(p.ArgArray(x => x.Operations), "\n\n");
                    var frags = Join(p.ArgArray(x => x.Fragments), "\n\n");

                    var result = Join(new[] { ops, frags }, "\n\n") + "\n";
                    return result;
                });
            });

            Config<Operation>(c =>
            {
                c.Field(x => x.OperationType);
                c.Field(x => x.Name);
                c.Field(x => x.Variables);
                c.Field(x => x.Directives);
                c.Field(x => x.SelectionSet);
                c.Print(p =>
                {
                    var op = p.Arg(x => x.OperationType).ToString().ToLower(CultureInfo.InvariantCulture);
                    var name = p.Arg(x => x.Name)?.ToString();
                    var variables = Wrap("(", Join(p.ArgArray(x => x.Variables), ", "), ")");
                    var directives = Join(p.ArgArray(x => x.Directives), " ");
                    var selectionSet = p.Arg(x => x.SelectionSet);

                    return string.IsNullOrWhiteSpace(name)
                           && string.IsNullOrWhiteSpace(directives)
                           && string.IsNullOrWhiteSpace(variables)
                        ? selectionSet
                        : Join(new[] { op, Join(new[] { name, variables }, ""), directives, selectionSet }, " ");
                });
            });

            Config<InlineFragment>(c =>
            {
                c.Field(x => x.Directives);
                c.Field(x => x.SelectionSet);
                c.Field(x => x.Type);
                c.Print(p =>
                {
                    var directives = Join(p.ArgArray(x => x.Directives), " ");
                    var selectionSet = p.Arg(x => x.SelectionSet);
                    var typename = p.Arg(x => x.Type);
                    var body = string.IsNullOrWhiteSpace(directives)
                        ? selectionSet
                        : Join(new[] { directives, selectionSet }, " ");

                    return $"... on {typename} {body}";
                });
            });

            Config<VariableDefinition>(c =>
            {
                c.Field(x => x.Name);
                c.Field(x => x.Type);
                c.Field(x => x.DefaultValue);
                c.Print(p => $"${p.Arg(x => x.Name)}: {p.Arg(x => x.Type)}");
            });

            Config<SelectionSet>(c =>
            {
                c.Field(x => x.SelectionsList);
                c.Print(p => Block(p.ArgArray(x => x.SelectionsList)));
            });

            Config<Arguments>(c =>
            {
                c.Field(x => x.Children);
                c.Print(p =>
                {
                    var result = p.Arg(x => x.Children);
                    return result;
                });
            });

            Config<Argument>(c =>
            {
                c.Field(x => x.Name);
                c.Field(x => x.Value);
                c.Print(p => $"{p.Arg(x => x.Name)}: {p.Arg(x => x.Value)}");
            });

            Config<Field>(c =>
            {
                c.Field(x => x.Alias);
                c.Field(x => x.Name);
                c.Field(x => x.Arguments);
                c.Field(x => x.Directives);
                c.Field(x => x.SelectionSet);
                c.Print(n =>
                {
                    var alias = n.Arg(x => x.Alias);
                    var name = n.Arg(x => x.Name);
                    var args = Join(n.ArgArray(x => x.Arguments), ", ");
                    var directives = Join(n.ArgArray(x => x.Directives), " ");
                    var selectionSet = n.Arg(x => x.SelectionSet);

                    var result = Join(new[]
                    {
                        Wrap("", alias, ": ") + name + Wrap("(", args, ")"),
                        directives,
                        selectionSet
                    }, " ");
                    return result;
                });
            });

            // Value

            Config<VariableReference>(c =>
            {
                c.Field(x => x.Name);
                c.Print(p => $"${p.Arg(x => x.Name)}");
            });

            Config<IntValue>(c =>
            {
                c.Field(x => x.Value);
                c.Print(f => f.Arg(x => x.Value));
            });

            Config<NullValue>(c => c.Print(f => "null"));

            Config<LongValue>(c =>
            {
                c.Field(x => x.Value);
                c.Print(f => f.Arg(x => x.Value));
            });

            Config<BigIntValue>(c =>
            {
                c.Field(x => x.Value);
                c.Print(f => f.Arg(x => x.Value));
            });

            Config<FloatValue>(c =>
            {
                c.Field(x => x.Value);
                c.Print(f =>
                {
                    var val = (double)f.Arg(x => x.Value);
                    return val.ToString("0.0##############", CultureInfo.InvariantCulture);
                });
            });

            Config<DecimalValue>(c =>
            {
                c.Field(x => x.Value);
                c.Print(f =>
                {
                    var val = (decimal)f.Arg(x => x.Value);
                    return val.ToString("0.0##############", CultureInfo.InvariantCulture);
                });
            });

            Config<StringValue>(c =>
            {
                c.Field(x => x.Value);
                c.Print(f =>
                {
                    var val = f.Arg(x => x.Value);
                    if (!string.IsNullOrWhiteSpace(val?.ToString()) && !val.ToString().StartsWith("\"", StringComparison.InvariantCulture))
                    {
                        val = $"\"{val}\"";
                    }
                    return val;
                });
            });

            Config<BooleanValue>(c =>
            {
                c.Field(x => x.Value);
                c.Print(f => f.Arg(x => x.Value)?.ToString().ToLower(CultureInfo.InvariantCulture));
            });

            Config<EnumValue>(c =>
            {
                c.Field(x => x.Name);
                c.Print(p => p.Arg(x => x.Name));
            });

            Config<ListValue>(c =>
            {
                c.Field(x => x.Values);
                c.Print(p => $"[{Join(p.ArgArray(x => x.Values), ", ")}]");
            });

            Config<ObjectValue>(c =>
            {
                c.Field(x => x.ObjectFields);
                c.Print(p => $"{{{Join(p.ArgArray(x => x.ObjectFields), ", ")}}}");
            });

            Config<ObjectField>(c =>
            {
                c.Field(x => x.Name);
                c.Field(x => x.Value);
                c.Print(p => $"{p.Arg(x => x.Name)}: {p.Arg(x => x.Value)}");
            });

            // Directive
            Config<Directive>(c =>
            {
                c.Field(x => x.Name);
                c.Field(x => x.Arguments);
                c.Print(n =>
                {
                    var name = n.Arg(x => x.Name);
                    var args = Join(n.ArgArray(x => x.Arguments), ", ");
                    return $"@{name}" + Wrap("(", args, ")");
                });
            });

            // Type
            Config<NamedType>(c =>
            {
                c.Field(x => x.Name);
                c.Print(p => p.Arg(x => x.Name));
            });

            Config<ListType>(c =>
            {
                c.Field(x => x.Type);
                c.Print(p => $"[{p.Arg(x => x.Type)}]");
            });

            Config<NonNullType>(c =>
            {
                c.Field(x => x.Type);
                c.Print(p => $"{p.Arg(x => x.Type)}!");
            });

            // Type System Definitions
        }

        public void Config<T>(Action<AstPrintConfig<T>> configure)
            where T : INode
        {
            var config = new AstPrintConfig<T>
            {
                Matches = n => n is T
            };
            configure(config);
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
            return list.Count > 0
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
            return str.Replace("\n", "\n  ");
        }

        public object Visit(INode node)
        {
            return ApplyConfig(node);
        }

        public object ApplyConfig(INode node)
        {
            var config = FindFor(node);

            if (config != null)
            {
                var vals = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

                foreach (var f in config.FieldsList)
                {
                    var ctx = new ResolveValueContext(node);

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
                }

                return config.PrintAst(vals);
            }

            return null;
        }

        private AstPrintConfig FindFor(INode node)
        {
            // DO NOT USE LINQ ON HOT PATH
            foreach (var c in _configs)
            {
                if (c.Matches(node))
                    return c;
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
