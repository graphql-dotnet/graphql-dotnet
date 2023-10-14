using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Federation.Instrumentation
{
    /// <summary>
    /// Represents hierarchical path from parent field to child field as a series of segments
    /// </summary>
    public class ResultPath
    {
        /// <summary>
        /// Root path. All paths start from here.
        /// </summary>
        public static ResultPath ROOT_PATH { get; } = new ResultPath();
        private readonly ResultPath _parent;
        private readonly object _segment;
        private int _hash;

        private ResultPath()
        {
            _parent = null;
            _segment = null;
        }

        private ResultPath(ResultPath parent, string segment)
        {
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            _segment = segment ?? throw new ArgumentNullException(nameof(segment));
        }

        private ResultPath(ResultPath parent, int segment)
        {
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            _segment = segment;
        }

        public ResultPath Segment(string segment) => new ResultPath(this, segment);

        public ResultPath Segment(int segment) => new ResultPath(this, segment);

        /// <summary>
        /// Converts the path to a list of segments
        /// </summary>
        /// <returns></returns>
        public IEnumerable<object> ToList()
        {
            if (_parent == null)
            {
                return Enumerable.Empty<object>();
            }

            var list = new LinkedList<object>();

            var currentPath = this;

            while (currentPath._segment != null)
            {
                _ = list.AddFirst(currentPath._segment);
                currentPath = currentPath._parent;
            }

            return list;
        }

        /// <summary>
        /// Creates an execution path from a list of objects
        /// </summary>
        /// <param name="objects">path objects</param>
        /// <returns>A new execution path</returns>
        public static ResultPath FromList(List<object> objects) 
        {
            if (objects == null)
                throw new ArgumentNullException(nameof(objects));

            var path = ResultPath.ROOT_PATH;
            foreach (object obj in objects)
            {
                if (obj is string @string)
                    path = path.Segment(@string);
                else
                    path = path.Segment((int)obj);
            }
            return path;
        }

        /// <summary>
        /// determines if the path is the root path.
        /// </summary>
        /// <returns>true if the path is root path</returns>
        public bool IsRootPath() => this == ROOT_PATH;

        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (obj == null || GetType().FullName != obj.GetType().FullName)
                return false;
            var self = this;
            var that = (ResultPath)obj;
            while (self._segment != null && that._segment != null)
            {
                if (!Object.Equals(self._segment, that._segment))
                    return false;
                self = self._parent;
                that = that._parent;
            }
            return self.IsRootPath() && that.IsRootPath();
        }

        public override int GetHashCode()
        {
            int tempHash = _hash;
            if (tempHash != 0)
                return tempHash;

            tempHash = 1;
            var self = this;

            while (self != null)
            {
                object value = self._segment;
                tempHash = 31 * tempHash + (value == null ? 0 : value.GetHashCode());
                self = self._parent;
            }
            _hash = tempHash;

            return tempHash;
        }
    }
}

