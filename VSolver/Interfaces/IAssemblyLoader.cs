using System;
using System.Collections.Generic;
using System.Reflection;

namespace VSolver.Interfaces
{
    public enum AddAssemblyOption
    {
        OverrideEntries,
        DoNotOverrideEntries
    }

    public interface IAssemblyLoader
    {
        Dictionary<Type, IMetaEntry> LoadAssembly(IDependencyCollector collector, Assembly assembly);
    }
}