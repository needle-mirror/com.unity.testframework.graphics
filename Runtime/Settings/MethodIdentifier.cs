using System;
using NUnit.Framework.Interfaces;

namespace UnityEngine.TestTools.Graphics
{
    [Serializable]
    struct MethodIdentifier : IEquatable<MethodIdentifier>
    {
        [SerializeField]
        internal string typeName;

        [SerializeField]
        internal string methodName;

        [SerializeField]
        internal string[] parameterTypeNames;

        internal MethodIdentifier(string typeName, string methodName, string[] parameterTypeNames)
        {
            this.typeName = typeName;
            this.methodName = methodName;
            this.parameterTypeNames = parameterTypeNames;
        }

        internal static MethodIdentifier FromIMethodInfo(IMethodInfo methodInfo, ITest suite)
        {
            var parameters = methodInfo.GetParameters();
            var paramTypeNames = new string[parameters.Length];
            for (var i = 0; i < parameters.Length; ++i)
                paramTypeNames[i] = parameters[i].ParameterType.FullName;
            return new MethodIdentifier(suite.FullName, methodInfo.Name, paramTypeNames);
        }

        public override string ToString()
        {
            var paramList = parameterTypeNames != null ? string.Join(",", parameterTypeNames) : "";
            return $"{typeName}.{methodName}({paramList})";
        }

        public bool Equals(MethodIdentifier other)
        {
            if (typeName != other.typeName || methodName != other.methodName)
                return false;
            if ((parameterTypeNames == null) != (other.parameterTypeNames == null))
                return false;
            if (parameterTypeNames == null)
                return true;
            if (parameterTypeNames.Length != other.parameterTypeNames?.Length)
                return false;
            for (var i = 0; i < parameterTypeNames.Length; ++i)
            {
                if (parameterTypeNames[i] != other.parameterTypeNames[i])
                    return false;
            }
            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is MethodIdentifier other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hash = HashCode.Combine(typeName, methodName);
            if (parameterTypeNames != null)
            {
                foreach (var p in parameterTypeNames)
                    hash = HashCode.Combine(hash, p);
            }
            return hash;
        }
    }
}
