namespace GraphQL;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
internal class AllowedOnAttribute<T> : Attribute { }

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
internal class AllowedOnAttribute<T1, T2> : Attribute { }
