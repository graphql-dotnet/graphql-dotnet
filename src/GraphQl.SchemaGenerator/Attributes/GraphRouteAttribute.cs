using System;

namespace GraphQl.SchemaGenerator.Attributes
{
    /// <summary>
    ///     Attribute provided on an api endpoint can be converted into graph ql.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class GraphRouteAttribute : Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Type ResponseType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool IsMutation { get; set; }


        /// <summary>
        /// 
        /// </summary>
        public GraphRouteAttribute()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        public GraphRouteAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="responseType"></param>
        public GraphRouteAttribute(string name, Type responseType)
        {
            Name = name;
            ResponseType = responseType;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="responseType"></param>
        /// <param name="isMutation"></param>
        public GraphRouteAttribute(string name, Type responseType, bool isMutation)
        {
            Name = name;
            ResponseType = responseType;
            IsMutation = isMutation;
        }
    }
}
