using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VSolver.Interfaces;

namespace VSolver.Implementations
{
    public class AssemblyLoader : IAssemblyLoader
    {
        public Dictionary<Type, IMetaEntry> LoadAssembly(IDependencyCollector collector, Assembly assembly)
        {
            var collection = GetTypesWithAttribute<Export>(assembly)
                .ToDictionary<Type, Type, IMetaEntry>(type => type.GetCustomAttribute<Export>().BaseType ?? type, type => new MetaEntry()
                {
                    ConcreteInstance = null,
                    ConstructorDependencies = collector.CollectConstructorDependencies(type),
                    PropertiesDependencies = collector.CollectPropertiesDependencies(type),
                    CreateInstanceFunction = null,
                    ImplementationType = type,
                    InterfaceType = type.GetCustomAttribute<Export>().BaseType ?? type,
                    LifeCycle = LifeCycleOption.Transient
                });
            //Search for ImportConstructor
            foreach (var type in GetTypesWithAttribute<ImportConstructor>(assembly))
                collection.Add(type, new MetaEntry()
                {
                    ConcreteInstance = null,
                    ConstructorDependencies = collector.CollectConstructorDependencies(type),
                    PropertiesDependencies = collector.CollectPropertiesDependencies(type),
                    CreateInstanceFunction = null,
                    ImplementationType = type,
                    InterfaceType = type,
                    LifeCycle = LifeCycleOption.Transient
                });
            //Search for Import
            foreach (var type in GetTypesWithPropertyAttribute<Import>(assembly))
                collection.Add(type, new MetaEntry()
                {
                    ConcreteInstance = null,
                    ConstructorDependencies = collector.CollectConstructorDependencies(type),
                    PropertiesDependencies = collector.CollectPropertiesDependencies(type),
                    CreateInstanceFunction = null,
                    ImplementationType = type,
                    InterfaceType = type,
                    LifeCycle = LifeCycleOption.Transient
                });
            return collection;
        }

        private IEnumerable<Type> GetTypesWithAttribute<T>(Assembly assembly) where T : Attribute
        {
            return assembly.GetTypes().Where(type => type.HasAttribute<T>());
        }

        private IEnumerable<Type> GetTypesWithPropertyAttribute<T>(Assembly assembly) where T : Attribute
        {
            return assembly.GetTypes().Where(type => type.GetProperties().Any(x => x.HasAttribute<T>()));
        }
    }
}