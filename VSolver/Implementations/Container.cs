using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using VSolver.Interfaces;
using ApplicationException = System.ApplicationException;
using CreateInstanceFunction = VSolver.Interfaces.CreateInstanceFunction;

namespace VSolver.Implementations
{

    public class Container : IContainer
    {
        private readonly IDictionary<Type, IMetaEntry> _registeredEntries;
        private readonly IContainer _parentContainer;
        private readonly ICompiler _compiler;
        private readonly IDependencyCollector _collector;
        private readonly IAssemblyLoader _assemblyLoader;
        private readonly IDictionary<Type, CreateInstanceFunction> _compiledFunctions;

        private bool _isDisposed;
        private readonly ReaderWriterLockSlim _registeredEntriesLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

    
        public Container():
            this(new Compiler(), new DependencyCollector(), new AssemblyLoader(), new Dictionary<Type, IMetaEntry>(), new Dictionary<Type, CreateInstanceFunction>())
        {
           
        }

        public Container(ICompiler compiler, IDependencyCollector collector, IAssemblyLoader loader, IDictionary<Type, IMetaEntry> registry, IDictionary<Type, CreateInstanceFunction> compiledFunctions)
            :this(compiler, collector, loader, null, registry, compiledFunctions)
        {

        }

        private Container(ICompiler compiler, IDependencyCollector collector, IAssemblyLoader loader,  IContainer parent)
            : this(compiler, collector, loader, parent, new Dictionary<Type, IMetaEntry>(), new Dictionary<Type, CreateInstanceFunction>())
        {
            
        }

        private Container(ICompiler compiler, IDependencyCollector collector, IAssemblyLoader loader, IContainer parent, IDictionary<Type, IMetaEntry> registry,IDictionary<Type, CreateInstanceFunction> compiledFunctions)
        {
            _compiler = compiler;
            _assemblyLoader = loader;
            _collector = collector;
            _registeredEntries = registry ?? new Dictionary<Type, IMetaEntry>();
            _compiledFunctions = compiledFunctions ?? new Dictionary<Type, CreateInstanceFunction>();
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
            return ActivateInstance(metaEntry);
        }

        private object ActivateInstance(IMetaEntry metaEntry)
        {
            return metaEntry.LifeCycle == LifeCycleOption.Singleton
                ? ActivateSingletonInstance(metaEntry)
                : ActivateTransientInstance(metaEntry);
        }

        private object ActivateSingletonInstance(IMetaEntry metaEntry)
        {
            if (metaEntry.ConcreteInstance != null)
            {
                return metaEntry.ConcreteInstance;
            }

            lock (metaEntry)
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
            return GetCompiledFunction(metaEntry)();
        }

        private CreateInstanceFunction GetCompiledFunction(IMetaEntry metaEntry)
        {
            var getCompiledFunctionResult = _compiledFunctions.TryGetValue(metaEntry.InterfaceType, out var compiledFunction);
            if (getCompiledFunctionResult)
            {
                return compiledFunction;
            }

            compiledFunction = CreateFunction(metaEntry);

            if (metaEntry.LifeCycle == LifeCycleOption.Singleton)
            {
                var instance = compiledFunction();
                metaEntry.ConcreteInstance = instance;
                compiledFunction = () => metaEntry.ConcreteInstance;
                _compiledFunctions.Add(metaEntry.InterfaceType, compiledFunction);
            }
            else
            {
                _compiledFunctions.Add(metaEntry.InterfaceType, compiledFunction);
            }
            
            return compiledFunction;
        }

        private CreateInstanceFunction CreateFunction(IMetaEntry metaEntry)
        {
            if (metaEntry.LifeCycle == LifeCycleOption.Singleton)
            {
                var concreteInstance = metaEntry.ConcreteInstance;
                if (concreteInstance != null)
                {
                    return ()=>metaEntry.ConcreteInstance;
                }
            }

            var createInstanceFunction = metaEntry.CreateInstanceFunction;
            if (createInstanceFunction != null)
            {
                return createInstanceFunction;
            }
            
            var constructorInfo = metaEntry.ImplementationType
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single();
            var cDepList = CompileConstructorDependencies(metaEntry);
            var pDepList = CompilePropertiesDependencies(metaEntry);
            return _compiler.Compile(metaEntry.InterfaceType, constructorInfo, cDepList, pDepList);
        }

        private CreateInstanceFunction[] CompileConstructorDependencies(IMetaEntry metaEntry)
        {
            var cDepList = new CreateInstanceFunction[metaEntry.ConstructorDependencies.Length];
            for (var i = 0; i < metaEntry.ConstructorDependencies.Length; i++)
            {
                if (!_registeredEntries.TryGetValue(metaEntry.ConstructorDependencies[i], out var dependencyMetaEntry))
                {
                    throw new ApplicationException($"Error on constructor dependencies on {metaEntry.ImplementationType} : Type {metaEntry.ConstructorDependencies[i].FullName} was not registered!");
                }
                cDepList[i] = GetCompiledFunction(dependencyMetaEntry);
            }
            return cDepList;
        }

        private IDictionary<PropertyInfo, CreateInstanceFunction> CompilePropertiesDependencies(IMetaEntry metaEntry)
        {
            var pDepList = new Dictionary<PropertyInfo, CreateInstanceFunction>();
            foreach (var t in metaEntry.PropertiesDependencies)
            {
                if (!_registeredEntries.TryGetValue(t.PropertyType, out var dependencyMetaEntry))
                {
                    throw new ApplicationException($"Error on properties dependencies on {metaEntry.ImplementationType} : Type {t.PropertyType.FullName} was not registered!");
                }
                pDepList.Add(t, GetCompiledFunction(dependencyMetaEntry));
            }
            return pDepList;
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
            return new Container(_compiler, _collector, _assemblyLoader, this);
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