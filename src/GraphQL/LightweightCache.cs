using System.Collections;

namespace GraphQL
{
    /// <summary>
    /// A simple cache based on the provided dictionary.
    /// </summary>
    /// <typeparam name="TKey">The type of the t key.</typeparam>
    /// <typeparam name="TValue">The type of the t value.</typeparam>
    /// <seealso cref="System.Collections.Generic.IEnumerable{TValue}" />
    /// <remarks>https://github.com/JasperFx/baseline/blob/master/src/Baseline/LightweightCache.cs</remarks>
    public class LightweightCache<TKey, TValue> : IEnumerable<TValue>
        where TKey : notnull
    {
        private readonly IDictionary<TKey, TValue> _values;
        private Func<TKey, TValue> _onMissing = delegate (TKey key)
        {
            var message = $"Key '{key}' could not be found";
            throw new KeyNotFoundException(message);
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="LightweightCache{TKey, TValue}"/> class.
        /// </summary>
        public LightweightCache()
            : this(new Dictionary<TKey, TValue>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LightweightCache{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="onMissing">Action to perform if the key is missing. Defaults to <see cref="KeyNotFoundException"/></param>
        public LightweightCache(Func<TKey, TValue> onMissing)
            : this(new Dictionary<TKey, TValue>(), onMissing)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LightweightCache{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="dictionary">The dictionary implementation to use.</param>
        /// <param name="onMissing">Action to perform if the key is missing. Defaults to <see cref="KeyNotFoundException"/></param>
        /// <remarks>This takes a dependency on the provided dictionary. It does not simply copy its values.</remarks>
        public LightweightCache(IDictionary<TKey, TValue> dictionary, Func<TKey, TValue> onMissing)
            : this(dictionary)
        {
            _onMissing = onMissing;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LightweightCache{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="dictionary">The dictionary implementation to use.</param>
        /// <remarks>This takes a dependency on the provided dictionary. It does not simply copy its values.</remarks>
        public LightweightCache(IDictionary<TKey, TValue> dictionary)
        {
            _values = dictionary;
        }

        /// <summary>
        /// Action to perform if the key is missing. Defaults to <see cref="KeyNotFoundException"/>
        /// </summary>
        public Func<TKey, TValue> OnMissing
        {
            set => _onMissing = value;
        }

        /// <summary>
        /// Gets the count.
        /// </summary>
        public int Count => _values.Count;

        /// <summary>
        /// Gets or sets the <typeparamref name="TValue"/> with the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        public TValue this[TKey key]
        {
            get
            {
                if (!_values.TryGetValue(key, out var value))
                {
                    value = _onMissing(key);

                    if (value != null)
                    {
                        _values[key] = value;
                    }
                }

                return value;
            }
            set
            {
                if (_values.ContainsKey(key))
                {
                    _values[key] = value;
                }
                else
                {
                    _values.Add(key, value);
                }
            }
        }

        /// <summary>
        /// Gets the keys.
        /// </summary>
        public IEnumerable<TKey> Keys => _values.Keys;

        /// <summary>
        /// Returns an enumerator that iterates through the values.
        /// </summary>
        /// <returns>An <see cref="System.Collections.IEnumerator"></see> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<TValue>)this).GetEnumerator();

        /// <summary>
        /// Returns an enumerator that iterates through the values.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<TValue> GetEnumerator() => _values.Values.GetEnumerator();

        /// <summary>
        /// Guarantees that the Cache has a value for a given key.
        /// If it does not already exist, it's created using the OnMissing action.
        /// </summary>
        /// <param name="key">The key.</param>
        public void FillDefault(TKey key) => Fill(key, _onMissing(key));

        /// <summary>
        /// Guarantees that the Cache has a value for a given key.
        /// If it does not already exist, it's created using provided default value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The default value.</param>
        public void Fill(TKey key, TValue value)
        {
            if (_values.ContainsKey(key))
            {
                return;
            }

            _values.Add(key, value);
        }

        /// <summary>
        /// Tries the retrieve a given key.
        /// </summary>
        /// <param name="key">The key to retrieve.</param>
        /// <param name="value">The value for the associated key or <c>default(TValue)</c>.</param>
        public bool TryRetrieve(TKey key, out TValue? value) => _values.TryGetValue(key, out value);

        /// <summary>
        /// Performs the specified action for each value.
        /// </summary>
        /// <param name="action">The action to be performed.</param>
        /// <remarks>The order of execution is non-deterministic. If an error occurs, the action will not be performed on the remaining values.</remarks>
        public void Each(Action<TValue> action)
        {
            foreach (var pair in _values)
            {
                action(pair.Value);
            }
        }

        /// <summary>
        /// Performs the specified action for each key/value pair.
        /// </summary>
        /// <param name="action">The action to be performed.</param>
        /// <remarks>The order of execution is non-deterministic. If an error occurs, the action will not be performed on the remaining values.</remarks>
        public void Each(Action<TKey, TValue> action)
        {
            foreach (var pair in _values)
            {
                action(pair.Key, pair.Value);
            }
        }

        /// <summary>
        /// Equivalent to ContainsKey
        /// </summary>
        /// <param name="key">The key.</param>
        public bool Has(TKey key) => _values.ContainsKey(key);

        /// <summary>
        /// Determines if a given value exists in the dictionary.
        /// </summary>
        /// <param name="predicate">The search predicate.</param>
        public bool Exists(Predicate<TValue> predicate)
        {
            bool returnValue = false;

            Each(value => returnValue |= predicate(value));

            return returnValue;
        }

        /// <summary>
        /// Searches for a given value.
        /// </summary>
        /// <param name="predicate">The search predicate.</param>
        /// <returns>The first matching value</returns>
        public TValue? Find(Predicate<TValue> predicate)
        {
            foreach (var pair in _values)
            {
                if (predicate(pair.Value))
                {
                    return pair.Value;
                }
            }

            return default;
        }

        /// <summary>
        /// Returns all values as an array
        /// </summary>
        public TValue[] GetAll()
        {
            var returnValue = new TValue[Count];
            _values.Values.CopyTo(returnValue, 0);

            return returnValue;
        }

        /// <summary>
        /// Removes the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        public void Remove(TKey key)
        {
            if (_values.ContainsKey(key))
            {
                _values.Remove(key);
            }
        }

        /// <summary>
        /// Clears this instance of all key/value pairs.
        /// </summary>
        public void Clear() => _values.Clear();

        /// <summary>
        /// If the dictionary contains the indicated key, performs the action with its value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="action">The action to be performed.</param>
        public void WithValue(TKey key, Action<TValue> action)
        {
            if (_values.ContainsKey(key))
            {
                action(this[key]);
            }
        }

        /// <summary>
        /// Equivalent to Clear()
        /// </summary>
        public void ClearAll() => _values.Clear();
    }
}
