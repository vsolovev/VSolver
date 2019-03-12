using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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

            if (implementationType.GetConstructors(BindingFlags.Instance | BindingFlags.Public).Length != 1)
            {
                throw new ApplicationException($"Type {implementationType.FullName} doen't have single public constructor. ImportConstructor attribute cant be applied.");
            }

            _registeredEntriesLock.EnterWriteLock();
            try
            {
                var activatorNeeded = instance == null && activationFunction == null;
                _registeredEntries.Add(interfaceType, new MetaEntry()
                {
                    ConcreteInstance = instance,
                    CreateInstanceFunction = activationFunction,
                    ConstructorDependencies = activatorNeeded ? _collector.CollectConstructorDependencies(implementationType) : null,
                    PropertiesDependencies = activatorNeeded ? _collector.CollectPropertiesDependencies(implementationType) : null,
                    ImplementationType = implementationType,
                    InterfaceType = interfaceType,
                    LifeCycle = option,
                    CachedExpression = null
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
                return InternalResolve(baseType);
            }
            finally
            {
                _registeredEntriesLock.ExitReadLock();
            }
        }

        private object InternalResolve(Type baseType)
        {
            if (!_registeredEntries.TryGetValue(baseType, out var metaEntry))
            {
                if (_parentContainer != null)
                {
                    return _parentContainer.Resolve(baseType);
                }

                throw new ApplicationException($"Type {baseType.FullName} was not registered!");
            }

            return GetPreviouslyResolvedInstance(metaEntry)
                   ?? ActivateInstance(metaEntry);
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


        // [metaEntry.ImplementationType] = class MyImplementation(param1, param2, param3)
        private Expression CreateExpression(IMetaEntry metaEntry)
        {
            var expression = Expression.New(metaEntry.ImplementationType);
            var cDepList = new List<Expression>();

            foreach (var item in metaEntry.ConstructorDependencies)
            {
                if (!_registeredEntries.TryGetValue(item, out var dependencyMetaEntry))
                {
                    throw new ApplicationException($"Type {item.FullName} was not registered!");
                }
                cDepList.Add(dependencyMetaEntry.CachedExpression ?? CreateExpression(dependencyMetaEntry));
            }

            var constructorCallExp = Expression.New(metaEntry.ImplementationType.GetConstructors().Single(), cDepList.ToArray());

            var pDepList = new List<MemberBinding>();
            foreach (var item in metaEntry.PropertiesDependencies)
            {
                if (!_registeredEntries.TryGetValue(item.PropertyType, out var dependencyMetaEntry))
                {
                    throw new ApplicationException($"Type {item.PropertyType.FullName} was not registered!");
                }
                
                pDepList.Add(Expression.Bind(item, dependencyMetaEntry.CachedExpression ?? CreateExpression(dependencyMetaEntry)));
            }

            var propExpression = Expression.MemberInit(constructorCallExp, pDepList.ToArray());

            return propExpression;
        }


        private object ActivateTransientInstance(IMetaEntry metaEntry)
        {

            //var expression = metaEntry.CachedExpression ?? CreateExpression(metaEntry);
            


            var constructorDependenciesInstances = new object[metaEntry.ConstructorDependencies.Length];
            for (var i = 0; i < constructorDependenciesInstances.Length; i++)
            {
                constructorDependenciesInstances[i] = InternalResolve(metaEntry.ConstructorDependencies[i]);
            }

            var propertiesDependenciesInstances = new KeyValuePair<PropertyInfo, object>[metaEntry.PropertiesDependencies.Length];
            for (var i = 0; i < propertiesDependenciesInstances.Length; i++)
            {
                var dependency = metaEntry.PropertiesDependencies[i];
                var propertyValueInstance = InternalResolve(dependency.PropertyType);
                propertiesDependenciesInstances[i] = new KeyValuePair<PropertyInfo, object>(dependency, propertyValueInstance);
            }

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