using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VSolver.Interfaces;

namespace VSolver.Implementations
{
    public class DependencyCollector : IDependencyCollector
    {
        public Type[] CollectConstructorDependencies(Type implementationType)
        {
            var constructors = implementationType.GetConstructors();
            if (constructors.Length != 1)
            {
                throw new ApplicationException($"Type {implementationType.FullName} has more than 1 constructor. ImportConstructor attribute cant be applied.");
            }
            return constructors.Single().GetParameters().Select(parameter => parameter.ParameterType).ToArray();
        }

        public PropertyInfo[] CollectPropertiesDependencies(Type implementationType)
        {
            return implementationType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => !(x.PropertyType.IsValueType || x.PropertyType.IsPrimitive)).ToArray();
        }
    }
}