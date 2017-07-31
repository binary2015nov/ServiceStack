using System;

namespace Funq
{
    sealed class ServiceKey : IEquatable<ServiceKey>
    {
        private int hashCode;

        public Type FactoryType { get; private set; }

        public string ServiceName { get; private set; }

        public ServiceKey(Type factoryType, string serviceName)
        {
            FactoryType = factoryType;
            ServiceName = serviceName;
            this.hashCode = factoryType.GetHashCode();
            if (serviceName != null)
                hashCode ^= serviceName.GetHashCode();
        }

        #region Equality

        public bool Equals(ServiceKey other)
        {
            if (other == null)
                return false;

            return this.FactoryType == other.FactoryType && this.ServiceName == other.ServiceName;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as ServiceKey);
        }

        public override int GetHashCode()
        {
            return hashCode;
        }

        #endregion
    }
}