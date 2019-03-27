using System;
using System.Collections.Generic;
using System.Reflection;

namespace VSolver.Interfaces
{

    public interface IContainer: IDisposable
    {

        void Register<T>(T instance);
        void RegisterAsSingleton<T>(T instance);
        void Register<T>(CreateInstanceFunction activationFunction = null);
        void RegisterAsSingleton<T>(CreateInstanceFunction activationFunction = null);
        void Register<TInterface, TImplementation>();
        void RegisterAsSingleton<TInterface, TImplementation>();
        object Resolve(Type baseType);
        TBase Resolve<TBase>() where TBase:class;
        void AddAssembly(Assembly assembly, AddAssemblyOption option);
        IContainer CreateChildContainer();
    }
}