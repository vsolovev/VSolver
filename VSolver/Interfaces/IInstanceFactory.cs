using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace VSolver.Interfaces
{
    public interface IInstanceFactory
    {
        object CreateInstance(Type type, object[] constructorDependencies, IDictionary<PropertyInfo, object> propertiesDependencies);
    }
}