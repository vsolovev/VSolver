using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace VSolver.Interfaces
{
    public interface ICompiler
    {
        CreateInstanceFunction Compile(Type t, ConstructorInfo constructorInfo, CreateInstanceFunction[] constructorDependencies, IDictionary<PropertyInfo, CreateInstanceFunction> properties);
    }
}