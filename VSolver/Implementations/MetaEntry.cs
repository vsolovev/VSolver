using System;
using System.Reflection;
using VSolver.Interfaces;

namespace VSolver.Implementations
{
    public class MetaEntry : IMetaEntry
    {
        public delegate Func<object> CompiledExpressionDelegate();

        public Type InterfaceType { get; set; }
        public CreateInstanceFunction CreateInstanceFunction { get; set; }
        public Type[] ConstructorDependencies { get; set; }
        public PropertyInfo[] PropertiesDependencies { get; set; }
        public Type ImplementationType { get; set; }
        public object ConcreteInstance { get; set; }
        public LifeCycleOption LifeCycle { get; set; }
        
        public MetaEntry()
        {
            LifeCycle = LifeCycleOption.Transient;
        }

        protected bool Equals(MetaEntry other)
        {
            return InterfaceType == other?.InterfaceType;
        }

        public bool Equals(IMetaEntry other)
        {
            return InterfaceType == other?.InterfaceType;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MetaEntry) obj);
        }

        public override int GetHashCode()
        {
            return (InterfaceType != null ? InterfaceType.GetHashCode() : 0);
        }
    }
}