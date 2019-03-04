using System;
using System.Collections.Generic;
using System.Reflection;

namespace VSolver.Interfaces
{
    public interface IInstanceFactory
    {
        object CreateInstance(Type type, object[] constructorDependencies, Dictionary<PropertyInfo, object> propertiesDependencies);
    }
}