using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VSolver.Interfaces;
using System.Threading;

namespace VSolver.Implementations
{
    public class Container : IContainer
    {
        private readonly Dictionary<Type, IMetaEntry> _registeredEntries;
        private readonly IContainer _parentContainer;
        private readonly IInstanceFactory _factory;
        private readonly IDependencyCollector _collector;
        private readonly IAssemblyLoader _assemblyLoader;

        private bool _isDisposed;
        private readonly ReaderWriterLockSlim _registeredEntriesLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

    
        public Container():
            this(new InstanceFactory(), new DependencyCollector(), new AssemblyLoader())
        {
           
        }

        public Container(IInstanceFactory factory, IDependencyCollector collector, IAssemblyLoader loader)
            :this(factory, collector, loader, null)
        {

        }

        private Container(IInstanceFactory factory, IDependencyCollector collector, IAssemblyLoader loader, IContainer parent)
        {
            _factory = factory;
            _assemblyLoader = loader;
            _collector = collector;
            _registeredEntries = new Dictionary<Type, IMetaEntry>();
            _parentContainer = parent;
            _isDisposed = false;
        }

        public void Register<TImpl>(Type baseType = null, LifeCycleOption option = LifeCycleOption.Transient)
        {
            Register(typeof(TImpl), null, baseType, option);
        }

        public void Register(Type implementationType, Type baseType = null, LifeCycleOption option = LifeCycleOption.Transient)
        {
            Register(implementationType, null, baseType, option);
        }

        public void Register<TImpl, TBase>(LifeCycleOption option = LifeCycleOption.Transient)
        {
            Register(typeof(TImpl), null, typeof(TBase), option);
        }

        public void Register<TImpl>(CreateInstanceFunction createFunction, Type baseType = null, LifeCycleOption option = LifeCycleOption.Transient)
        {
            Register(typeof(TImpl), createFunction, baseType, option);
        }

        public void Register(Type implementationType, CreateInstanceFunction createFunction, Type baseType = null, LifeCycleOption option = LifeCycleOption.Transient)
        {
            _registeredEntriesLock.EnterWriteLock();
            try
            {
                _registeredEntries.Add(baseType ?? implementationType, new MetaEntry()
                {
                    ConcreteInstance = null,
                    CreateInstanceFunction = createFunction,
                    ConstructorDependencies = _collector.CollectConstructorDependencies(implementationType),
                    PropertiesDependencies = _collector.CollectPropertiesDependencies(implementationType),
                    ImplementationType = implementationType,
                    InterfaceType = baseType ?? implementationType,
                    LifeCycle = option
                });
            }
            finally
            {
                _registeredEntriesLock.ExitWriteLock();
            }

        }

        public void Register(object instance, Type baseType, LifeCycleOption option = LifeCycleOption.Transient)
        {
            _registeredEntriesLock.EnterWriteLock();
            try
            {
                _registeredEntries.Add(baseType, new MetaEntry()
                {
                    ConcreteInstance = instance,
                    ConstructorDependencies = null,
                    CreateInstanceFunction = null,
                    ImplementationType = null,
                    InterfaceType = baseType,
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
            var propertiesDependenciesInstances = metaEntry.PropertiesDependencies.ToDictionary(propertyInfo => propertyInfo,
                propertyInfo => Resolve(propertyInfo.PropertyType));

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
                _registeredEntriesLock.Dispose();
                _isDisposed = true;
            }
            finally
            {
                _registeredEntriesLock.ExitWriteLock();
            }
        }
    }
}