using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using GraphQL.Dynamic.Types.Introspection;
using GraphQL.Introspection;
using GraphQL.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GraphQL.Dynamic.Types.LiteralGraphType
{
    public class RemoteLiteralGraphType : ObjectGraphType
    {
        public delegate Introspection.Schema RemoteSchemaFetcher(string url);

        private static object FetchRemoteServerSchemaMutex = new object();
        private static ConcurrentDictionary<string, Introspection.Schema> RemoteServerSchemas { get; set; } = new ConcurrentDictionary<string, Introspection.Schema>();

        private static ConcurrentDictionary<string, HashSet<Type>> RemoteServerTypes { get; set; } = new ConcurrentDictionary<string, HashSet<Type>>();

        private bool _hasAddedFields;

        private readonly string _remoteLocation;
        private readonly string _name;

        public RemoteLiteralGraphType(string remoteLocation, string name)
        {
            _remoteLocation = remoteLocation;
            _name = name;
        }

        public override string CollectTypes(TypeCollectionContext context)
        {
            if (!string.IsNullOrWhiteSpace(_remoteLocation) && !string.IsNullOrWhiteSpace(_name))
            {
                // get remote server information
                // if we haven't fetched the remote types for this remote:
                if (!RemoteServerSchemas.TryGetValue(_remoteLocation, out var schema))
                {
                    throw new Exception($"Schema not already loaded for remote {_remoteLocation}");
                }

                // get type
                if (!TryGetFieldTypeFromRemoteSchema(schema, _name, out var type))
                {
                    // if no type found: fail
                    // TODO: fail better
                    throw new Exception($"Failed to find type '{_name}' in remote '{_remoteLocation}' schema");
                }

                Name = LiteralGraphTypeHelpers.GenerateRemoteTypeName(_remoteLocation, _name);

                if (!_hasAddedFields)
                {
                    var fields = GetFieldsForFieldType(_remoteLocation, type)?.Where(f => f != null).ToArray();
                    foreach (var field in fields ?? new FieldType[] { })
                    {
                        AddField(field);
                    }

                    _hasAddedFields = true;
                }
            }

            return base.CollectTypes(context);
        }

        public static async Task<IEnumerable<Type>> LoadRemotes(IEnumerable<RemoteDescriptor> remotes, Func<TypeElement, bool> typeFilter = null, RemoteSchemaFetcher remoteSchemaFetcher = null)
        {
            if (typeFilter == null)
            {
                typeFilter = t => !t.Name.StartsWith("__") && t.Kind != TypeElementTypeKind.Scalar;
            }

            if (remoteSchemaFetcher == null)
            {
                remoteSchemaFetcher = FetchRemoteSchemaViaHttp;
            }

            var parentConstructor = typeof(RemoteLiteralGraphType).GetConstructor(new[] { typeof(string), typeof(string) });
            var metadataAttributeConstructor = typeof(RemoteLiteralGraphTypeMetadataAttribute).GetConstructor(new[] { typeof(string), typeof(string), typeof(string) });

            // Convert each remote into a new assembly asynchronously
            var tasks = remotes
                .Select(remote =>
                {
                    return Task.Run(() =>
                    {
                        var url = remote.Url;
                        var schema = FetchRemoteServerSchema(url, remoteSchemaFetcher);

                        var assemblyName = new AssemblyName($"GraphQL.Dynamic.RemoteLiteralGraphTypes{Guid.NewGuid().ToString("N")}");
                        var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);

                        var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name, $"{assemblyName.Name}.dll");

                        var types = schema.Types
                            .Where(typeFilter)
                            .Select(schemaType =>
                            {
                                var typeName = schemaType.Name;

                                // [RemoteLiteralGraphTypeMetadata(<remote.Moniker>, <remote.Url>, <typeName>)]
                                // public class GeneratedRemoteLiteralGraphType : RemoteLiteralGraphType
                                var typeBuilder = moduleBuilder.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class, typeof(RemoteLiteralGraphType));
                                typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(metadataAttributeConstructor, new[] { remote.Moniker, url, typeName }));

                                // public GeneratedRemoteLiteralGraphType(): base(<remote>, <typeName>) { }
                                var ctorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
                                var ctorGenerator = ctorBuilder.GetILGenerator();
                                ctorGenerator.Emit(OpCodes.Ldarg_0);
                                ctorGenerator.Emit(OpCodes.Ldstr, url);
                                ctorGenerator.Emit(OpCodes.Ldstr, typeName);
                                ctorGenerator.Emit(OpCodes.Call, parentConstructor);
                                ctorGenerator.Emit(OpCodes.Ret);

                                return typeBuilder.CreateType();
                            })
                            .ToList();

                        // Update cache
                        var typeSet = new HashSet<Type>(types);
                        RemoteServerTypes.AddOrUpdate(url, typeSet, (key, old) => typeSet);

                        return types;
                    });
                })
                .ToList();

            // Wait for schema & type resolution
            var jaggedTypes = await Task.WhenAll(tasks);

            // Flatten
            return jaggedTypes.SelectMany(t => t).ToList();
        }

        private static IEnumerable<FieldType> GetFieldsForFieldType(string remote, Introspection.TypeElement parentField)
        {
            var inputFields = parentField.InputFields
                ?.Select(f => new Field
                {
                    Name = f.Name,
                    Description = f.Description,
                    Type = f.Type,
                    Args = new FieldArg[] { }
                })
                .ToArray();

            var fields = (parentField.Fields ?? new Field[] { }).Concat(inputFields ?? new Field[] { });
            if (fields.Count() == 0)
            {
                return null;
            }

            return fields
                .Select(field =>
                {
                    return new RemoteLiteralGraphTypeMemberInfo
                    {
                        DeclaringTypeName = parentField.Name,
                        Name = field.Name,
                        Type = IntrospectionTypeToLiteralGraphTypeMemberInfoType(field.Type),
                        TypeName = IntrospectionTypeToLiteralGraphTypeMemberInfoTypeName(field.Type),
                        IsList = field.Type.Kind == TypeElementTypeKind.List,
                        GetValueFn = ctx =>
                        {
                            var token = ((JToken)ctx.Source)[field.Name];

                            // ...If the token is an array, hand back an IEnumerable<JToken>
                            if (token is JArray array)
                            {
                                return array.Select(t => t).ToList();
                            }

                            // ...If the token is a simple value, hand that back
                            if (token is JValue value)
                            {
                                return value.Value;
                            }

                            // ...Otherwise, give the token back as a JObject
                            return token.Value<object>();
                        }
                    };
                })
                // TODO: handle unresolvable types (I'm looking at you UNION)
                .Where(member => member.Type != LiteralGraphTypeMemberInfoType.Unknown)
                .Select(member => LiteralGraphTypeHelpers.GetFieldTypeForMember(member, ComplexFieldTypeResolver))
                .ToList();

            FieldType ComplexFieldTypeResolver(LiteralGraphTypeMemberInfo member)
            {
                if (!(member is RemoteLiteralGraphTypeMemberInfo literalMember))
                {
                    return null;
                }

                if (!RemoteServerTypes.TryGetValue(remote, out var remoteTypes))
                {
                    return null;
                }

                // Check if the member is a scalar type. If so, get the appropriate built-in GraphType; 
                // otherwise, try to find the type in the list of remote types
                var typeName = literalMember.TypeName;
                var schemaType = LiteralGraphTypeHelpers.GetPrimitiveGraphType(typeName)
                    ?? remoteTypes.FirstOrDefault(t => t.Name == typeName);

                var realType = literalMember.IsList
                    ? typeof(ListGraphType<>).MakeGenericType(schemaType)
                    : schemaType;

                return new FieldType
                {
                    Name = literalMember.Name,
                    Type = realType,
                    Resolver = LiteralGraphTypeHelpers.CreateFieldResolverFor(literalMember)
                };
            }
        }

        private static LiteralGraphTypeMemberInfoType IntrospectionTypeToLiteralGraphTypeMemberInfoType(TypeElementType type)
        {
            switch (type.Kind)
            {
                case TypeElementTypeKind.List:
                case TypeElementTypeKind.Object:
                    return LiteralGraphTypeMemberInfoType.Complex;
                case TypeElementTypeKind.Scalar:
                    return ScalarGraphTypeNameToMemberInfoType(type.Name);
                case TypeElementTypeKind.NonNull:
                    return IntrospectionTypeToLiteralGraphTypeMemberInfoType(type.OfType);
                default:
                    return LiteralGraphTypeMemberInfoType.Unknown;
            }
        }

        private static string IntrospectionTypeToLiteralGraphTypeMemberInfoTypeName(TypeElementType type)
        {
            switch (type.Kind)
            {
                case TypeElementTypeKind.Object:
                case TypeElementTypeKind.Scalar:
                    return type.Name;
                case TypeElementTypeKind.List:
                case TypeElementTypeKind.NonNull:
                    return IntrospectionTypeToLiteralGraphTypeMemberInfoTypeName(type.OfType);
                default:
                    return null;
            }
        }

        private static LiteralGraphTypeMemberInfoType ScalarGraphTypeNameToMemberInfoType(string name)
        {
            if (name == "Int")
            {
                return LiteralGraphTypeMemberInfoType.Int;
            }

            if (name == "String")
            {
                return LiteralGraphTypeMemberInfoType.String;
            }

            if (name == "Boolean")
            {
                return LiteralGraphTypeMemberInfoType.Boolean;
            }

            if (name == "Float")
            {
                return LiteralGraphTypeMemberInfoType.Float;
            }

            if (name == "ID")
            {
                return LiteralGraphTypeMemberInfoType.Guid;
            }

            if (name == "Date")
            {
                return LiteralGraphTypeMemberInfoType.DateTime;
            }

            if (name == "Decimal")
            {
                return LiteralGraphTypeMemberInfoType.Long;
            }

            return LiteralGraphTypeMemberInfoType.Unknown;
        }

        private bool TryGetFieldTypeFromRemoteSchema(Introspection.Schema schema, string name, out Introspection.TypeElement field)
        {
            field = schema.Types.FirstOrDefault(t => t.Name == name);

            return field != null;
        }

        private static Introspection.Schema FetchRemoteServerSchema(string remote, RemoteSchemaFetcher remoteSchemaFetcher)
        {
            // lock types mutex
            lock (FetchRemoteServerSchemaMutex)
            {
                // did someone else fetch it while we were waiting?
                if (!RemoteServerSchemas.TryGetValue(remote, out var schema))
                {
                    //  fetch remote type information
                    if (!TryGetRemoteSchema(remote, remoteSchemaFetcher, out schema))
                    {
                        // if it fails: fail
                        // TODO: fail better
                        throw new Exception("Failed to get remote schema");
                    }

                    // if it succeeds: save type information for all types
                    RemoteServerSchemas.AddOrUpdate(remote, schema, (key, old) => schema);
                }

                return schema;
            }
        }

        private static bool TryGetRemoteSchema(string remoteLocation, RemoteSchemaFetcher remoteSchemaFetcher, out Introspection.Schema schema)
        {
            schema = null;

            try
            {
                schema = remoteSchemaFetcher(remoteLocation);
            }
            catch (Exception)
            {
                // TODO: log stuff
            }

            return schema != null;
        }

        private static Introspection.Schema FetchRemoteSchemaViaHttp(string url)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders
                    .Accept
                    .Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var query = new
                {
                    Query = SchemaIntrospection.IntrospectionQuery
                };

                var response = client.PostAsync(new Uri(url), new StringContent(JsonConvert.SerializeObject(query), Encoding.UTF8, "application/json"))
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();

                var json = response.Content
                    .ReadAsStringAsync()
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();

                return Root.FromJson(json)?.Data?.Schema;
            }
        }

        internal class RemoteLiteralGraphTypeMemberInfo : LiteralGraphTypeMemberInfo
        {
            public string TypeName { get; set; }
        }
    }
}
