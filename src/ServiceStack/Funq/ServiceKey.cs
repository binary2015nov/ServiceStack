using System;

namespace Funq
{
    public sealed class ServiceKey : IEquatable<ServiceKey>
    {
        private int hashCode;

        public ServiceKey(Type factoryType, string serviceName)
        {
            FactoryType = factoryType;
            ServiceName = serviceName;

            hashCode = factoryType.GetHashCode();
            if (serviceName != null)
                hashCode ^= serviceName.GetHashCode();

        }

        public Type FactoryType { get; private set; }

        public string ServiceName { get; private set; }

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