using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VSolver.Interfaces;

namespace VSolver.Implementations
{
    public class InstanceFactory : IInstanceFactory
    {
        public object CreateInstance(Type type, object[] constructorDependencies, Dictionary<PropertyInfo, object> propertiesDependencies) 
        {
            var instance = CreateBaseInstance(type, constructorDependencies);
            foreach (var property in type.GetProperties())
            {
                if (!propertiesDependencies.ContainsKey(property))
                {
                    throw new ApplicationException($"No registered type exists for property type {property.PropertyType}");
                }
                property.SetValue(instance, propertiesDependencies[property]);
            }
            return instance;

        }

        private object CreateBaseInstance(Type type, object[] dependencies)
        {
            var constructors = type.GetConstructors();
            if (constructors.Length != 1)
            {
                throw new ApplicationException($"Type {type.FullName} has more than 1 constructor. ImportConstructor attribute cant be applied.");
            }
            return Activator.CreateInstance(type, dependencies, new object[0]);
        }

    }
}