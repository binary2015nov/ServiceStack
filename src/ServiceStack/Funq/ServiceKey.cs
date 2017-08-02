using System;

namespace Funq
{
    sealed class ServiceKey : IEquatable<ServiceKey>
    {
        public Type FactoryType { get; set; }

        public string ServiceName { get; set; }

        public ServiceKey() { }

        public ServiceKey(Type factoryType, string serviceName)
        {
            FactoryType = factoryType;
            ServiceName = serviceName;          
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
            var hashCode = FactoryType.GetHashCode();
            if (ServiceName != null)
                hashCode ^= ServiceName.GetHashCode();
            return hashCode;
        }

        #endregion
    }
}