using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using VSolver.Interfaces;
using System.Threading;

namespace VSolver.Implementations
{
    
    public class Container : IContainer
    {
        private readonly IDictionary<Type, IMetaEntry> _registeredEntries;
        private readonly IContainer _parentContainer;
        private readonly IInstanceFactory _factory;
        private readonly IDependencyCollector _collector;
        private readonly IAssemblyLoader _assemblyLoader;

        private bool _isDisposed;
        private readonly ReaderWriterLockSlim _registeredEntriesLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

    
        public Container():
            this(new InstanceFactory(), new DependencyCollector(), new AssemblyLoader(), new Dictionary<Type, IMetaEntry>())
        {
           
        }

        public Container(IInstanceFactory factory, IDependencyCollector collector, IAssemblyLoader loader, IDictionary<Type, IMetaEntry> registry)
            :this(factory, collector, loader, null, registry)
        {

        }

        private Container(IInstanceFactory factory, IDependencyCollector collector, IAssemblyLoader loader,  IContainer parent)
            : this(factory, collector, loader, parent, new Dictionary<Type, IMetaEntry>())
        {
            
        }

        private Container(IInstanceFactory factory, IDependencyCollector collector, IAssemblyLoader loader, IContainer parent, IDictionary<Type, IMetaEntry> registry)
        {
            _factory = factory;
            _assemblyLoader = loader;
            _collector = collector;
            _registeredEntries = registry ?? new Dictionary<Type, IMetaEntry>();
            _parentContainer = parent;
            _isDisposed = false;
        }


        public void Register<T>(T instance)
        {
            Register(typeof(T), typeof(T), null, instance, LifeCycleOption.Transient);
        }

        public void RegisterAsSingleton<T>(T instance)
        {
            Register(typeof(T), typeof(T), null, instance, LifeCycleOption.Singleton);
        }

        public void Register<T>(CreateInstanceFunction activationFunction = null)
        {
            Register(typeof(T), typeof(T), activationFunction, null, LifeCycleOption.Transient);
        }

        public void RegisterAsSingleton<T>(CreateInstanceFunction activationFunction = null)
        {
            Register(typeof(T), typeof(T), activationFunction, null, LifeCycleOption.Singleton);
        }

        public void Register<TInterface, TImplementation>()
        {
            Register(typeof(TInterface), typeof(TImplementation), null, null, LifeCycleOption.Transient);
        }

        public void RegisterAsSingleton<TInterface, TImplementation>()
        {
            Register(typeof(TInterface), typeof(TImplementation), null, null, LifeCycleOption.Singleton);
        }

        private void Register(Type interfaceType, Type implementationType, CreateInstanceFunction activationFunction,
            object instance, LifeCycleOption option)
        {
            _registeredEntriesLock.EnterWriteLock();
            try
            {
                var ready = instance != null || activationFunction != null;
                _registeredEntries.Add(interfaceType, new MetaEntry()
                {
                    ConcreteInstance = instance,
                    CreateInstanceFunction = activationFunction,
                    ConstructorDependencies = ready ? null : _collector.CollectConstructorDependencies(implementationType),
                    PropertiesDependencies = ready ? null : _collector.CollectPropertiesDependencies(implementationType),
                    ImplementationType = implementationType,
                    InterfaceType = interfaceType,
                    LifeCycle = option
                });

            }
            finally
            {
                _registeredEntriesLock.ExitWriteLock();
            }
        }
        
        public object Resolve(Type baseType)
        {
            _registeredEntriesLock.EnterReadLock();
            try
            {
                if (!_registeredEntries.ContainsKey(baseType))
                {
                    if (_parentContainer != null)
                    {
                        return _parentContainer.Resolve(baseType);
                    }

                    throw new ApplicationException($"Type {baseType.FullName} was not registered!");
                }

                var metaEntry = _registeredEntries[baseType];
                return GetPreviouslyResolvedInstance(metaEntry) 
                    ?? ActivateInstance(metaEntry);
            }
            finally
            {
                _registeredEntriesLock.ExitReadLock();
            }
        }

        private object GetPreviouslyResolvedInstance(IMetaEntry metaEntry)
        {
            return metaEntry.ConcreteInstance ?? metaEntry.CreateInstanceFunction?.Invoke();
        }

        private object ActivateSingletonInstance(IMetaEntry metaEntry)
        {
            lock(metaEntry)
            {
                if (metaEntry.ConcreteInstance != null)
                {
                    return metaEntry.ConcreteInstance;
                }

                return metaEntry.ConcreteInstance = ActivateTransientInstance(metaEntry);
            }
        }

        private object ActivateTransientInstance(IMetaEntry metaEntry)
        {
            var constructorDependenciesInstances = metaEntry.ConstructorDependencies.Select(Resolve).ToArray();
            var propertiesDependenciesInstances = metaEntry.PropertiesDependencies.ToDictionary(property => property, property => Resolve(property.PropertyType));
            
            return _factory.CreateInstance(metaEntry.ImplementationType, constructorDependenciesInstances,
                propertiesDependenciesInstances);

        }

        private object ActivateInstance(IMetaEntry metaEntry)
        {
            return metaEntry.LifeCycle == LifeCycleOption.Singleton 
                ? ActivateSingletonInstance(metaEntry) 
                : ActivateTransientInstance(metaEntry);
        }

        public TBase Resolve<TBase>() where TBase:class
        {
            return Resolve(typeof(TBase)) as TBase;
        }
        
        public void AddAssembly(Assembly assembly, AddAssemblyOption option = AddAssemblyOption.DoNotOverrideEntries)
        {
            var assemblyEntries = _assemblyLoader.LoadAssembly(_collector, assembly);
            _registeredEntriesLock.EnterWriteLock();
            try
            {
                foreach (var entry in assemblyEntries)
                {
                    if (_registeredEntries.ContainsKey(entry.Key))
                    {
                        if (option == AddAssemblyOption.OverrideEntries)
                        {
                            _registeredEntries[entry.Key] = entry.Value;
                        }
                    }
                    _registeredEntries.Add(entry.Key, entry.Value);
                }
            }
            finally
            {
                _registeredEntriesLock.ExitWriteLock();
            }
        }

        public IContainer CreateChildContainer()
        {
            return new Container(_factory, _collector, _assemblyLoader, this);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(Container));
            }

            _registeredEntriesLock.EnterWriteLock();
            try
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(Container));
                }

                foreach (var entry in _registeredEntries.Where(x => x.Value.LifeCycle == LifeCycleOption.Singleton).Select(x => x.Value.ConcreteInstance))
                {
                    if (entry is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                _registeredEntries.Clear();
                
                
            }
            finally
            {
                _isDisposed = true;
                _registeredEntriesLock.ExitWriteLock();
                _registeredEntriesLock.Dispose();
            }
        }
    }
}