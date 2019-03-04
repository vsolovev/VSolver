using System;
using System.Reflection;

namespace VSolver.Interfaces
{
    public delegate object CreateInstanceFunction();
    public enum LifeCycleOption
    {
        Transient,
        Singleton,
    }
    public interface IMetaEntry:IEquatable<IMetaEntry>
    {
        Type InterfaceType { get; set; }
        CreateInstanceFunction CreateInstanceFunction { get; set; }
        Type[] ConstructorDependencies { get; set; }
        PropertyInfo[] PropertiesDependencies { get; set; }
        Type ImplementationType { get; set; }
        object ConcreteInstance { get; set; }
        LifeCycleOption LifeCycle { get; set; }
    }
}