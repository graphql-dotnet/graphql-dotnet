using System;
using System.Collections.Generic;

using GraphQL.Types;

namespace GraphQL
{
	public static class GraphTypeRegistry
	{
		static Stack<RegistryEntry> _entries;

		static GraphTypeRegistry()
		{
			_entries.Push(new RegistryEntry(typeof(int), typeof(IntGraphType)));
			_entries.Push(new RegistryEntry(typeof(long), typeof(IntGraphType)));
			_entries.Push(new RegistryEntry(typeof(double), typeof(FloatGraphType)));
			_entries.Push(new RegistryEntry(typeof(float), typeof(FloatGraphType)));
			_entries.Push(new RegistryEntry(typeof(decimal), typeof(DecimalGraphType)));
			_entries.Push(new RegistryEntry(typeof(string), typeof(StringGraphType)));
			_entries.Push(new RegistryEntry(typeof(bool), typeof(BooleanGraphType)));
			_entries.Push(new RegistryEntry(typeof(DateTime), typeof(DateGraphType)));
		}

		public static void Register(Type clrType, Type graphType)
		{
			_entries.Push(new RegistryEntry(clrType, graphType));
		}

		public static Type Get(Type clrType)
		{
			foreach (var entry in _entries)
			{
				if (entry.CLRType == clrType)
				{
					return entry.GraphType;
				}
			}
			return null;
		}
	}

	public class RegistryEntry
	{
		public Type CLRType { get; }
		public Type GraphType { get; }

		public RegistryEntry(Type clrType, Type graphType)
		{
			CLRType = clrType;
			GraphType = graphType;
		}
	}
}
