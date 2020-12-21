using System;
using System.Collections.Generic;
using System.Text;

namespace ExponentialEnumerable.Common
{
    public struct InlineEqualityComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T, T, bool> getEquals;
        private readonly Func<T, int> getHashCode;

        public InlineEqualityComparer(Func<T, T, bool> equals, Func<T, int> hashCode = null)
        {
            getEquals = equals;
            getHashCode = hashCode;
        }

        public bool Equals(T x, T y)
        {
            return getEquals(x, y);
        }

        public int GetHashCode(T obj)
        {
            return getHashCode?.Invoke(obj) ?? obj.GetHashCode();
        }
    }
}
