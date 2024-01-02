namespace GraphQL;

[AttributeUsage(AttributeTargets.Method)]
internal class AllowedOnAttribute<T> : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
internal class AllowedOnAttribute<T1, T2> : Attribute { }
