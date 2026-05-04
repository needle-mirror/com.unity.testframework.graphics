using System;
using System.Threading;

namespace UnityEditor.TestTools.Graphics
{
    sealed class AtomicInt : IEquatable<int>, IEquatable<AtomicInt>
    {
        readonly Guid m_Id;
        int m_Value;

        public AtomicInt(int value = 0)
        {
            m_Id = Guid.NewGuid();
            m_Value = value;
        }

        public bool Equals(AtomicInt other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return m_Value == other.m_Value;
        }

        public bool Equals(int other)
        {
            return m_Value == other;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is AtomicInt other && Equals(other);
        }

        public override int GetHashCode()
        {
            return m_Id.GetHashCode();
        }

        internal int Value
        {
            get => Interlocked.CompareExchange(ref m_Value, 0, 0);
            set => Interlocked.Exchange(ref m_Value, value);
        }

        internal int Increment() => Interlocked.Increment(ref m_Value);

        internal int Decrement() => Interlocked.Decrement(ref m_Value);

        internal bool TrySet(int expected, int desired) =>
            Interlocked.CompareExchange(ref m_Value, desired, expected) == expected;

        public static bool operator ==(AtomicInt a, int b)
        {
            return a?.Equals(b) ?? false;
        }

        public static bool operator ==(int a, AtomicInt b)
        {
            return b?.Equals(a) ?? false;
        }

        public static bool operator !=(int a, AtomicInt b)
        {
            return !(a == b);
        }

        public static bool operator !=(AtomicInt a, int b)
        {
            return !(a == b);
        }
    }
}
