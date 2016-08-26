using System;
using System.Collections;
using System.Collections.Generic;

namespace GraphQL
{
    public class LightweightCache<TKey, TValue> : IEnumerable<TValue>
    {
        private readonly IDictionary<TKey, TValue> _values;

        private Func<TValue, TKey> _getKey = delegate { throw new NotImplementedException(); };

        private Func<TKey, TValue> _onMissing = delegate (TKey key) {
            var message = $"Key '{key}' could not be found";
            throw new KeyNotFoundException(message);
        };

        public LightweightCache()
            : this(new Dictionary<TKey, TValue>())
        {
        }

        public LightweightCache(Func<TKey, TValue> onMissing)
            : this(new Dictionary<TKey, TValue>(), onMissing)
        {
        }

        public LightweightCache(IDictionary<TKey, TValue> dictionary, Func<TKey, TValue> onMissing)
            : this(dictionary)
        {
            _onMissing = onMissing;
        }

        public LightweightCache(IDictionary<TKey, TValue> dictionary)
        {
            _values = dictionary;
        }


        public Func<TKey, TValue> OnMissing
        {
            set { _onMissing = value; }
        }

        public Func<TValue, TKey> GetKey
        {
            get { return _getKey; }
            set { _getKey = value; }
        }

        public int Count
        {
            get { return _values.Count; }
        }

        public TValue First
        {
            get
            {
                foreach (var pair in _values)
                {
                    return pair.Value;
                }

                return default(TValue);
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                TValue value;

                if (!_values.TryGetValue(key, out value))
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

        public IEnumerable<TKey> Keys => _values.Keys;

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<TValue>)this).GetEnumerator();
        }

        public IEnumerator<TValue> GetEnumerator()
        {
            return _values.Values.GetEnumerator();
        }

        /// <summary>
        ///     Guarantees that the Cache has the default value for a given key.
        ///     If it does not already exist, it's created.
        /// </summary>
        /// <param name="key"></param>
        public void FillDefault(TKey key)
        {
            Fill(key, _onMissing(key));
        }

        public void Fill(TKey key, TValue value)
        {
            if (_values.ContainsKey(key))
            {
                return;
            }

            _values.Add(key, value);
        }

        public bool TryRetrieve(TKey key, out TValue value)
        {
            value = default(TValue);

            if (_values.ContainsKey(key))
            {
                value = _values[key];
                return true;
            }

            return false;
        }

        public void Each(Action<TValue> action)
        {
            foreach (var pair in _values)
            {
                action(pair.Value);
            }
        }

        public void Each(Action<TKey, TValue> action)
        {
            foreach (var pair in _values)
            {
                action(pair.Key, pair.Value);
            }
        }

        public bool Has(TKey key)
        {
            return _values.ContainsKey(key);
        }

        public bool Exists(Predicate<TValue> predicate)
        {
            var returnValue = false;

            Each(delegate (TValue value) { returnValue |= predicate(value); });

            return returnValue;
        }

        public TValue Find(Predicate<TValue> predicate)
        {
            foreach (var pair in _values)
            {
                if (predicate(pair.Value))
                {
                    return pair.Value;
                }
            }

            return default(TValue);
        }

        public TValue[] GetAll()
        {
            var returnValue = new TValue[Count];
            _values.Values.CopyTo(returnValue, 0);

            return returnValue;
        }

        public void Remove(TKey key)
        {
            if (_values.ContainsKey(key))
            {
                _values.Remove(key);
            }
        }

        public void Clear()
        {
            _values.Clear();
        }

        public void WithValue(TKey key, Action<TValue> action)
        {
            if (_values.ContainsKey(key))
            {
                action(this[key]);
            }
        }

        public void ClearAll()
        {
            _values.Clear();
        }
    }
}
