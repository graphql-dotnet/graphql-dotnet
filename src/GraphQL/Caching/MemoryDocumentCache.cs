using System;
using System.Collections.Concurrent;
using System.Linq;
using GraphQL.Language.AST;

namespace GraphQL.Caching
{
    /// <summary>
    /// A basic implementation of a document cache, limited by a configured amount of memory.
    /// </summary>
    public class MemoryDocumentCache : IDocumentCache
    {
        private readonly int _maxSize;
        private readonly int _maxSizeToCache;
        private readonly int _compactTo;
        private int _currentSize = 0;
        private readonly ConcurrentDictionary<string, DocumentInfo> _dictionary = new ConcurrentDictionary<string, DocumentInfo>();

        /// <summary>
        /// Initializes a new instance with the specified parameters.
        /// </summary>
        /// <param name="maxTotalQueryLength">The total length of all queries cached in this instance. Assume maximum memory used is about 10x this value. Will not cache queries larger than 1/3 of this value. During cache compression, reduces cache size by 1/3 of this value.</param>
        public MemoryDocumentCache(int maxTotalQueryLength)
        {
            if (maxTotalQueryLength <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxTotalQueryLength));
            _maxSize = maxTotalQueryLength;
            _maxSizeToCache = _maxSize / 3;
            _compactTo = _maxSize / 3 * 2;
        }

        private bool CacheOversized => System.Threading.Interlocked.CompareExchange(ref _currentSize, 0, 0) > _maxSize;
        private bool CacheOverCompressTo => System.Threading.Interlocked.CompareExchange(ref _currentSize, 0, 0) > _compactTo;

        /// <inheritdoc/>
        public Document this[string query]
        {
            get
            {
                if (_dictionary.TryGetValue(query, out var value))
                {
                    value.Accessed = DateTime.Now;
                    return value.Document;
                }
                return null;
            }
            set
            {
                if (query == null)
                    throw new ArgumentNullException(nameof(query));
                if (query.Length > _maxSizeToCache)
                    return;
                if (CacheOversized)
                {
                    var sortedValues = _dictionary.ToArray().OrderBy(x => x.Value.Accessed);
                    foreach (var entry in sortedValues)
                    {
                        if (CacheOverCompressTo)
                        {
                            if (_dictionary.TryRemove(entry.Key, out var removed))
                                System.Threading.Interlocked.Add(ref _currentSize, -removed.Size);
                        }
                        else
                            break;
                    }
                }
                if (_dictionary.TryAdd(query, new DocumentInfo(value, query.Length)))
                {
                    System.Threading.Interlocked.Add(ref _currentSize, query.Length);
                }
            }
        }

        private class DocumentInfo
        {
            public DocumentInfo(Document document, int size)
            {
                Document = document;
                Accessed = DateTime.Now;
                Size = size;
            }
            public Document Document { get; }
            public DateTime Accessed { get; set; }
            public int Size { get; }
        }
    }

}
