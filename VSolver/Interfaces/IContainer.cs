using System;
using System.Collections.Generic;
using System.Reflection;

namespace VSolver.Interfaces
{

    public interface IContainer
    {
        bool IsChild { get; }

        void Register<TImpl>(Type baseType = null, LifeCycleOption option = LifeCycleOption.Transient);
        void Register(Type implementationType, Type baseType=null, LifeCycleOption option = LifeCycleOption.Transient);
        void Register<TImpl, TBase>(LifeCycleOption option = LifeCycleOption.Transient);
        void Register<TImpl>(CreateInstanceFunction createFunction, Type baseType = null, LifeCycleOption option = LifeCycleOption.Transient);
        void Register(Type implementationType, CreateInstanceFunction createFunction, Type baseType = null, LifeCycleOption option = LifeCycleOption.Transient);
        void Register(object instance, Type baseType, LifeCycleOption option = LifeCycleOption.Transient);



        object Resolve(Type baseType);
        TBase Resolve<TBase>() where TBase:class;
        void AddAssembly(Assembly assembly, AddAssemblyOption option);

        IContainer CreateChildContainer();
        
    }
}