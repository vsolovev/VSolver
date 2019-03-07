using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using VSolver.Interfaces;

namespace VSolver.Implementations
{
    public interface IInstanceActivator
    {
        object CreateInstance(Type type, object[] args);
    }

    public class InstanceActivator : IInstanceActivator
    {
        public object CreateInstance(Type type, object[] args)
        {
            return Activator.CreateInstance(type, args, new object[0]);
        }
    }

    public class InstanceFactory : IInstanceFactory
    {
        private readonly IInstanceActivator _activator;

        public InstanceFactory(): this(new InstanceActivator())
        {
            
        }

        public InstanceFactory(IInstanceActivator activator)
        {
            _activator = activator;
        }

        public object CreateInstance(Type type, object[] constructorDependencies, IDictionary<PropertyInfo, object> propertiesDependencies) 
        {
            var instance = CreateBaseInstance(type, constructorDependencies);
            foreach (var property in propertiesDependencies)
            {
                property.Key.SetValue(instance, property.Value);
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
            return _activator.CreateInstance(type, dependencies);
        }

    }
}