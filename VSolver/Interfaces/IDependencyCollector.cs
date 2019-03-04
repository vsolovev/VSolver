using System;
using System.Reflection;

namespace VSolver.Interfaces
{
    public interface IDependencyCollector
    {
        Type[] CollectConstructorDependencies(Type implementationType);
        PropertyInfo[] CollectPropertiesDependencies(Type implementationType);
    }
}