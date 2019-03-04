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
        public bool IsChild { get; }
        private readonly Dictionary<Type, IMetaEntry> _registeredEntries;
        private readonly IContainer _parentContainer;
        private readonly IInstanceFactory _factory;
        private readonly IDependencyCollector _collector;
        private readonly IAssemblyLoader _assemblyLoader;

        private bool _isDisposed;
        private readonly object _locker = new object();
        private readonly ReaderWriterLockSlim _containerLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

    
        public Container():this(new InstanceFactory(), new DependencyCollector(), new AssemblyLoader())
        {
           
        }

        public Container(IInstanceFactory factory, IDependencyCollector collector, IAssemblyLoader loader)
        {
            _registeredEntries = new Dictionary<Type, IMetaEntry>();
            _factory = factory;
            _collector = collector;
            _assemblyLoader = loader;
            _parentContainer = null;
            _isDisposed = false;
        }

        private Container(IInstanceFactory factory, IDependencyCollector collector, IAssemblyLoader loader, IContainer parent)
        {
            _factory = factory;
            _assemblyLoader = loader;
            _collector = collector;
            _registeredEntries = new Dictionary<Type, IMetaEntry>();
            _parentContainer = parent;
            _isDisposed = false;
            IsChild = true;
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
            _containerLock.EnterWriteLock();
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
                _containerLock.ExitWriteLock();
            }

        }

        public void Register(object instance, Type baseType, LifeCycleOption option = LifeCycleOption.Transient)
        {
            _containerLock.EnterWriteLock();
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
                _containerLock.ExitWriteLock();
            }

        }

        public object Resolve(Type baseType)
        {
            
            _containerLock.EnterReadLock();
            IMetaEntry metaEntry;
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

                metaEntry = _registeredEntries[baseType];
                
                var instance = GetPreviouslyResolvedInstance(metaEntry);
                if (instance != null)
                {
                    return instance;
                }
            }
            finally
            {
                _containerLock.ExitReadLock();
            }

            _containerLock.EnterWriteLock();
            try
            {
                var instance = GetPreviouslyResolvedInstance(metaEntry);
                return instance ?? ActivateInstance(metaEntry);
            }
            finally
            {
                _containerLock.ExitWriteLock();
            }
        }

        private object GetPreviouslyResolvedInstance(IMetaEntry metaEntry)
        {
            return metaEntry.ConcreteInstance ?? metaEntry.CreateInstanceFunction?.Invoke();
        }

        private object ActivateInstance(IMetaEntry metaEntry)
        {
            var constructorDependenciesInstances = metaEntry.ConstructorDependencies.Select(Resolve).ToArray();
            var propertiesDependenciesInstances = metaEntry.PropertiesDependencies.ToDictionary(propertyInfo => propertyInfo, 
                propertyInfo => Resolve(propertyInfo.PropertyType));

            var instance = _factory.CreateInstance(metaEntry.ImplementationType, constructorDependenciesInstances,
                propertiesDependenciesInstances);

            if (metaEntry.LifeCycle == LifeCycleOption.Singleton)
            {
                metaEntry.ConcreteInstance = instance;
            }

            return instance;
        }

        public TBase Resolve<TBase>() where TBase:class
        {
            return Resolve(typeof(TBase)) as TBase;
        }

        
        
        public void AddAssembly(Assembly assembly, AddAssemblyOption option = AddAssemblyOption.DoNotOverrideEntries)
        {
            var assemblyEntries = _assemblyLoader.LoadAssembly(_collector, assembly);
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

        public IContainer CreateChildContainer()
        {
            Monitor.Enter(_locker);
            var child = new Container(_factory, _collector, _assemblyLoader, this);
            Monitor.Exit(_locker);
            return child;
        }

        public void Dispose()
        {
            lock (_locker)
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
                _isDisposed = true;

            }
        }
    }
}