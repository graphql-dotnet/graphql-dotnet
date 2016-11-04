using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using GraphQL.Language.AST;

namespace GraphQL.Utilities
{
    public static class AstPrinter
    {
        public static string Print(INode node)
        {
            var printer = new AstPrintVisitor();
            return printer.Visit(node)?.ToString() ?? "";
        }
    }

    public class AstPrintConfig
    {
        private readonly List<AstPrintFieldDefinition> _fields = new List<AstPrintFieldDefinition>();

        public IEnumerable<AstPrintFieldDefinition> Fields => _fields;
        public Func<INode, bool> Matches { get; set; }
        public Func<IDictionary<string, object>, object> PrintAst { get; set; }

        public void Field(AstPrintFieldDefinition field)
        {
            if (_fields.Exists(x => x.Name == field.Name))
            {
                throw new ExecutionError($"A field with name \"{field.Name}\" aleady exists!");
            }

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
            object arg;
            _args.TryGetValue(key, out arg);
            return arg;
        }

        public TVal Arg<TVal>(string key)
        {
            return (TVal) Arg(key);
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
            PrintAst = (args) =>
            {
                var f = new PrintFormat<T>(args);
                return configure(f);
            };
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
                return (TType) Source;
            }

            return default(TType);
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
        private readonly Expression<Func<TObject, TProperty>> _property;

        public ExpressionValueResolver(Expression<Func<TObject, TProperty>> property)
        {
            _property = property;
        }

        public TProperty Resolve(ResolveValueContext context)
        {
            return _property.Compile()(context.SourceAs<TObject>());
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
            Config<Document>(c =>
            {
                c.Field(x => x.Operations);
                c.Field(x => x.Fragments);
                c.Print(p =>
                {
                    var ops = join(p.ArgArray(x => x.Operations), "\n\n");
                    var frags = join(p.ArgArray(x => x.Fragments), "\n\n");

                    var result = join(new[] {ops, frags}, "\n\n") + "\n";
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
                    var op = p.Arg(x => x.OperationType).ToString().ToLower();
                    var name = p.Arg(x => x.Name)?.ToString();
                    var variables = wrap("(", join(p.ArgArray(x => x.Variables), ", "), ")");
                    var directives = join(p.ArgArray(x => x.Directives), " ");
                    var selectionSet = p.Arg(x => x.SelectionSet);

                    return string.IsNullOrWhiteSpace(name)
                           && string.IsNullOrWhiteSpace(directives)
                           && string.IsNullOrWhiteSpace(variables)
                        ? selectionSet
                        : join(new[] {op, join(new[] {name, variables}, ""), directives, selectionSet}, " ");
                });
            });

            Config<VariableDefinition>(c =>
            {
                c.Field(x => x.Name);
                c.Field(x => x.Type);
                c.Field(x => x.DefaultValue);
                c.Print(p =>
                {
                    return $"${p.Arg(x => x.Name)}: {p.Arg(x => x.Type)}";
                });
            });

            Config<SelectionSet>(c =>
            {
                c.Field(x => x.Selections);
                c.Print(p =>
                {
                    return block(p.ArgArray(x => x.Selections));
                });
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
                    var args = join(n.ArgArray(x => x.Arguments), ", ");
                    var directives = join(n.ArgArray(x => x.Directives), " ");
                    var selectionSet = n.Arg(x => x.SelectionSet);

                    var result = join(new []
                    {
                        wrap("", alias, ": ") + name + wrap("(", args, ")"),
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
            Config<LongValue>(c =>
            {
                c.Field(x => x.Value);
                c.Print(f => f.Arg(x => x.Value));
            });
            Config<FloatValue>(c =>
            {
                c.Field(x => x.Value);
                c.Print(f => $"{f.Arg(x => x.Value), 0:0.0##}");
            });
            Config<StringValue>(c =>
            {
                c.Field(x => x.Value);
                c.Print(f =>
                {
                    var val = f.Arg(x => x.Value);
                    if (!string.IsNullOrWhiteSpace(val?.ToString()) && !val.ToString().StartsWith("\""))
                    {
                        val = $"\"{val}\"";
                    }
                    return val;
                });
            });
            Config<BooleanValue>(c =>
            {
                c.Field(x => x.Value);
                c.Print(f => f.Arg(x => x.Value)?.ToString().ToLower());
            });
            Config<EnumValue>(c =>
            {
                c.Field(x => x.Name);
                c.Print(p => p.Arg(x => x.Name));
            });
            Config<ListValue>(c =>
            {
                c.Field(x => x.Values);
                c.Print(p => $"[{join(p.ArgArray(x => x.Values), ", ")}]");
            });
            Config<ObjectValue>(c =>
            {
                c.Field(x => x.ObjectFields);
                c.Print(p => $"{{{join(p.ArgArray(x=>x.ObjectFields), ", ")}}}");
            });
            Config<ObjectField>(c =>
            {
                c.Field(x => x.Name);
                c.Field(x => x.Value);
                c.Print(p => $"{p.Arg(x => x.Name)}: {p.Arg(x => x.Value)}");
            });

            // Directive

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
            var config = new AstPrintConfig<T>();
            config.Matches = n => n is T;
            configure(config);
            _configs.Add(config);
        }

        private string join(IEnumerable<object> nodes, string separator)
        {
            return nodes != null
                ? string.Join(
                    separator,
                    nodes.Where(n => n != null)
                        .Where(n => !string.IsNullOrWhiteSpace(n.ToString()))
                        .Select(n => n.ToString()))
                : "";
        }

        private string block(IEnumerable<object> nodes)
        {
            var list = nodes.ToList();
            return list.Any()
                ? indent($"{{\n{join(list, "\n")}") + "\n}"
                : "";
        }

        private string wrap(string start, object middle, string end)
        {
            var m = middle?.ToString() ?? "";
            return !string.IsNullOrWhiteSpace(m)
                ? $"{start}{m}{end}"
                : "";
        }

        private string indent(string str)
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

                config.Fields.Apply(f =>
                {
                    var ctx = new ResolveValueContext
                    {
                        Source = node
                    };

                    var result = f.Resolver.Resolve(ctx);

                    if (result is INode)
                    {
                        result = ApplyConfig(result as INode);
                    }
                    else if (result is IEnumerable && !(result is string))
                    {
                        result = GetListResult(result as IEnumerable);
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
                if (item is INode)
                {
                    var listResult = ApplyConfig(item as INode);
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
