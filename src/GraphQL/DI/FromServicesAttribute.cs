using System;
using System.Collections.Generic;
using System.Text;

namespace GraphQL.DI
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public class FromServicesAttribute : Attribute
    {
    }
}
