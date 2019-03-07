using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Tests.Dummies;
using VSolver.Implementations;
using VSolver.Interfaces;

namespace Tests.UnitTests
{
    [TestClass]
    public class ContainerTests
    {

        #region Register

        [TestMethod]
        [DataRow(LifeCycleOption.Singleton)]
        [DataRow(LifeCycleOption.Transient)]
        public void Test_Register_Generic_Instance_AnyDependencies_Interface(LifeCycleOption option)
        {
            var factory = new Mock<IInstanceFactory>();
            var dependencyCollector = new Mock<IDependencyCollector>();
            var assemblyLoader = new Mock<IAssemblyLoader>();
            var dictionary = new Dictionary<Type, IMetaEntry>();

            dependencyCollector.Setup(x => x.CollectPropertiesDependencies(It.IsAny<Type>())).Returns(new PropertyInfo[1]).Verifiable();
            dependencyCollector.Setup(x => x.CollectConstructorDependencies(It.IsAny<Type>())).Returns(new Type[1]).Verifiable();

            var container = new Container(factory.Object, dependencyCollector.Object, assemblyLoader.Object, dictionary);

            dictionary.Clear();
            var classInstance = new DummyNoDependencies();
            if (option == LifeCycleOption.Singleton)
            {
                container.RegisterAsSingleton<IDummyNoDependencies>(classInstance);
            }
            else
            {
                container.Register<IDummyNoDependencies>(classInstance);
            }
            

            Assert.IsTrue(dictionary[typeof(IDummyNoDependencies)].InterfaceType == typeof(IDummyNoDependencies));
            Assert.IsTrue(dictionary[typeof(IDummyNoDependencies)].ImplementationType == typeof(IDummyNoDependencies));
            Assert.IsTrue(dictionary[typeof(IDummyNoDependencies)].ConcreteInstance == classInstance);
            Assert.IsTrue(dictionary[typeof(IDummyNoDependencies)].LifeCycle == option);
            Assert.IsTrue(dictionary[typeof(IDummyNoDependencies)].CreateInstanceFunction == null);
            Assert.IsTrue(dictionary[typeof(IDummyNoDependencies)].ConstructorDependencies == null);
            Assert.IsTrue(dictionary[typeof(IDummyNoDependencies)].PropertiesDependencies == null);

            dependencyCollector.Verify(x => x.CollectPropertiesDependencies(It.IsAny<Type>()), Times.Never);
            dependencyCollector.Verify(x => x.CollectConstructorDependencies(It.IsAny<Type>()), Times.Never);

        }

        [TestMethod]
        [DataRow(LifeCycleOption.Singleton)]
        [DataRow(LifeCycleOption.Transient)]
        public void Test_Register_Generic_Instance_AnyDependencies_Implementation(LifeCycleOption option)
        {
            var factory = new Mock<IInstanceFactory>();
            var dependencyCollector = new Mock<IDependencyCollector>();
            var assemblyLoader = new Mock<IAssemblyLoader>();
            var dictionary = new Dictionary<Type, IMetaEntry>();

            dependencyCollector.Setup(x => x.CollectPropertiesDependencies(It.IsAny<Type>())).Returns(new PropertyInfo[1]).Verifiable();
            dependencyCollector.Setup(x => x.CollectConstructorDependencies(It.IsAny<Type>())).Returns(new Type[1]).Verifiable();

            var container = new Container(factory.Object, dependencyCollector.Object, assemblyLoader.Object, dictionary);

            dictionary.Clear();
            var classInstance = new DummyNoDependencies();
            if (option == LifeCycleOption.Singleton)
            {
                container.RegisterAsSingleton<DummyNoDependencies>(classInstance);
            }
            else
            {
                container.Register<DummyNoDependencies>(classInstance);
            }


            Assert.IsTrue(dictionary[typeof(DummyNoDependencies)].InterfaceType == typeof(DummyNoDependencies));
            Assert.IsTrue(dictionary[typeof(DummyNoDependencies)].ImplementationType == typeof(DummyNoDependencies));
            Assert.IsTrue(dictionary[typeof(DummyNoDependencies)].ConcreteInstance == classInstance);
            Assert.IsTrue(dictionary[typeof(DummyNoDependencies)].LifeCycle == option);
            Assert.IsTrue(dictionary[typeof(DummyNoDependencies)].CreateInstanceFunction == null);
            Assert.IsTrue(dictionary[typeof(DummyNoDependencies)].ConstructorDependencies == null);
            Assert.IsTrue(dictionary[typeof(DummyNoDependencies)].PropertiesDependencies == null);

            dependencyCollector.Verify(x => x.CollectPropertiesDependencies(It.IsAny<Type>()), Times.Never);
            dependencyCollector.Verify(x => x.CollectConstructorDependencies(It.IsAny<Type>()), Times.Never);

        }


        [TestMethod]
        [DataRow(LifeCycleOption.Singleton)]
        [DataRow(LifeCycleOption.Transient)]
        public void Test_Register_Generic_Transient_NoFunction_NoDependencies_Implementation(LifeCycleOption option)
        {
            var factory = new Mock<IInstanceFactory>();
            var dependencyCollector = new Mock<IDependencyCollector>();
            var assemblyLoader = new Mock<IAssemblyLoader>();
            var dictionary = new Dictionary<Type, IMetaEntry>();

            dependencyCollector.Setup(x => x.CollectPropertiesDependencies(It.IsAny<Type>())).Returns(new PropertyInfo[0]).Verifiable();
            dependencyCollector.Setup(x => x.CollectConstructorDependencies(It.IsAny<Type>())).Returns(new Type[0]).Verifiable();

            var container = new Container(factory.Object, dependencyCollector.Object, assemblyLoader.Object, dictionary);

            dictionary.Clear();

            if (option == LifeCycleOption.Singleton)
            {
                container.RegisterAsSingleton<DummyNoDependencies>();
            }
            else
            {
                container.Register<DummyNoDependencies>();
            }
            

            Assert.IsTrue(dictionary[typeof(DummyNoDependencies)].InterfaceType == typeof(DummyNoDependencies));
            Assert.IsTrue(dictionary[typeof(DummyNoDependencies)].ImplementationType == typeof(DummyNoDependencies));
            Assert.IsTrue(dictionary[typeof(DummyNoDependencies)].ConcreteInstance == null);
            Assert.IsTrue(dictionary[typeof(DummyNoDependencies)].LifeCycle == option);
            Assert.IsTrue(dictionary[typeof(DummyNoDependencies)].CreateInstanceFunction == null);
            Assert.IsTrue(dictionary[typeof(DummyNoDependencies)].ConstructorDependencies.Length == 0);
            Assert.IsTrue(dictionary[typeof(DummyNoDependencies)].PropertiesDependencies.Length == 0);
            
            dependencyCollector.Verify(x => x.CollectPropertiesDependencies(It.IsAny<Type>()), Times.Once);
            dependencyCollector.Verify(x => x.CollectConstructorDependencies(It.IsAny<Type>()), Times.Once);

        }

        [TestMethod]
        [DataRow(LifeCycleOption.Singleton)]
        [DataRow(LifeCycleOption.Transient)]
        public void Test_Register_Generic_Transient_NoFunction_WithDependencies_Implementation(LifeCycleOption option)
        {
            var factory = new Mock<IInstanceFactory>();
            var dependencyCollector = new Mock<IDependencyCollector>();
            var assemblyLoader = new Mock<IAssemblyLoader>();
            var dictionary = new Dictionary<Type, IMetaEntry>();

            dependencyCollector.Setup(x => x.CollectPropertiesDependencies(It.IsAny<Type>())).Returns(new PropertyInfo[2]).Verifiable();
            dependencyCollector.Setup(x => x.CollectConstructorDependencies(It.IsAny<Type>())).Returns(new Type[3]).Verifiable();

            var container = new Container(factory.Object, dependencyCollector.Object, assemblyLoader.Object, dictionary);

            dictionary.Clear();

            if (option == LifeCycleOption.Singleton)
            {
                container.RegisterAsSingleton<DummyNoDependencies>();
            }
            else
            {
                container.Register<DummyNoDependencies>();
            }

            

            Assert.IsTrue(dictionary[typeof(DummyNoDependencies)].InterfaceType == typeof(DummyNoDependencies));
            Assert.IsTrue(dictionary[typeof(DummyNoDependencies)].ImplementationType == typeof(DummyNoDependencies));
            Assert.IsTrue(dictionary[typeof(DummyNoDependencies)].ConcreteInstance == null);
            Assert.IsTrue(dictionary[typeof(DummyNoDependencies)].LifeCycle == option);
            Assert.IsTrue(dictionary[typeof(DummyNoDependencies)].CreateInstanceFunction == null);
            Assert.IsTrue(dictionary[typeof(DummyNoDependencies)].ConstructorDependencies.Length == 3);
            Assert.IsTrue(dictionary[typeof(DummyNoDependencies)].PropertiesDependencies.Length == 2);

            dependencyCollector.Verify(x => x.CollectPropertiesDependencies(It.IsAny<Type>()), Times.Once);
            dependencyCollector.Verify(x => x.CollectConstructorDependencies(It.IsAny<Type>()), Times.Once);

        }



        [TestMethod]
        [DataRow(LifeCycleOption.Singleton)]
        [DataRow(LifeCycleOption.Transient)]
        public void Test_Register_Generic_Transient_WithFunction_AnyDependencies_Interface(LifeCycleOption option)
        {
            var factory = new Mock<IInstanceFactory>();
            var dependencyCollector = new Mock<IDependencyCollector>();
            var assemblyLoader = new Mock<IAssemblyLoader>();
            var dictionary = new Dictionary<Type, IMetaEntry>();

            dependencyCollector.Setup(x => x.CollectPropertiesDependencies(It.IsAny<Type>())).Returns(new PropertyInfo[2]).Verifiable();
            dependencyCollector.Setup(x => x.CollectConstructorDependencies(It.IsAny<Type>())).Returns(new Type[3]).Verifiable();

            var container = new Container(factory.Object, dependencyCollector.Object, assemblyLoader.Object, dictionary);

            dictionary.Clear();

            if (option == LifeCycleOption.Singleton)
            {
                container.RegisterAsSingleton<IDummyNoDependencies>(() => new DummyNoDependencies());
            }
            else
            {
                container.Register<IDummyNoDependencies>(() => new DummyNoDependencies());
            }

            

            Assert.IsTrue(dictionary[typeof(IDummyNoDependencies)].InterfaceType == typeof(IDummyNoDependencies));
            Assert.IsTrue(dictionary[typeof(IDummyNoDependencies)].ImplementationType == typeof(IDummyNoDependencies));
            Assert.IsTrue(dictionary[typeof(IDummyNoDependencies)].ConcreteInstance == null);
            Assert.IsTrue(dictionary[typeof(IDummyNoDependencies)].LifeCycle == option);
            Assert.IsTrue(dictionary[typeof(IDummyNoDependencies)].CreateInstanceFunction != null);
            Assert.IsTrue(dictionary[typeof(IDummyNoDependencies)].ConstructorDependencies == null);
            Assert.IsTrue(dictionary[typeof(IDummyNoDependencies)].PropertiesDependencies == null);

            dependencyCollector.Verify(x => x.CollectPropertiesDependencies(It.IsAny<Type>()), Times.Never);
            dependencyCollector.Verify(x => x.CollectConstructorDependencies(It.IsAny<Type>()), Times.Never);

        }


        [TestMethod]
        [DataRow(LifeCycleOption.Singleton)]
        [DataRow(LifeCycleOption.Transient)]
        public void Test_Register_Generic_Transient_WithFunction_AnyDependencies_Implementation(LifeCycleOption option)
        {
            var factory = new Mock<IInstanceFactory>();
            var dependencyCollector = new Mock<IDependencyCollector>();
            var assemblyLoader = new Mock<IAssemblyLoader>();
            var dictionary = new Dictionary<Type, IMetaEntry>();

            dependencyCollector.Setup(x => x.CollectPropertiesDependencies(It.IsAny<Type>())).Returns(new PropertyInfo[2]).Verifiable();
            dependencyCollector.Setup(x => x.CollectConstructorDependencies(It.IsAny<Type>())).Returns(new Type[3]).Verifiable();

            var container = new Container(factory.Object, dependencyCollector.Object, assemblyLoader.Object, dictionary);

            dictionary.Clear();

            if (option == LifeCycleOption.Singleton)
            {
                container.RegisterAsSingleton<DummyNoDependencies>(() => new DummyNoDependencies());
            }
            else
            {
                container.Register<DummyNoDependencies>(() => new DummyNoDependencies());
            }
            

            Assert.IsTrue(dictionary[typeof(DummyNoDependencies)].InterfaceType == typeof(DummyNoDependencies));
            Assert.IsTrue(dictionary[typeof(DummyNoDependencies)].ImplementationType == typeof(DummyNoDependencies));
            Assert.IsTrue(dictionary[typeof(DummyNoDependencies)].ConcreteInstance == null);
            Assert.IsTrue(dictionary[typeof(DummyNoDependencies)].LifeCycle == option);
            Assert.IsTrue(dictionary[typeof(DummyNoDependencies)].CreateInstanceFunction != null);
            Assert.IsTrue(dictionary[typeof(DummyNoDependencies)].ConstructorDependencies == null);
            Assert.IsTrue(dictionary[typeof(DummyNoDependencies)].PropertiesDependencies == null);

            dependencyCollector.Verify(x => x.CollectPropertiesDependencies(It.IsAny<Type>()), Times.Never);
            dependencyCollector.Verify(x => x.CollectConstructorDependencies(It.IsAny<Type>()), Times.Never);

        }


        [TestMethod]
        [DataRow(LifeCycleOption.Singleton)]
        [DataRow(LifeCycleOption.Transient)]
        public void Test_Register_Generic_Transient_NoDependencies(LifeCycleOption option)
        {
            var factory = new Mock<IInstanceFactory>();
            var dependencyCollector = new Mock<IDependencyCollector>();
            var assemblyLoader = new Mock<IAssemblyLoader>();
            var dictionary = new Dictionary<Type, IMetaEntry>();

            dependencyCollector.Setup(x => x.CollectPropertiesDependencies(It.IsAny<Type>())).Returns(new PropertyInfo[0]).Verifiable();
            dependencyCollector.Setup(x => x.CollectConstructorDependencies(It.IsAny<Type>())).Returns(new Type[0]).Verifiable();

            var container = new Container(factory.Object, dependencyCollector.Object, assemblyLoader.Object, dictionary);

            dictionary.Clear();

            if (option == LifeCycleOption.Singleton)
            {
                container.RegisterAsSingleton<IDummyNoDependencies, DummyNoDependencies>();
            }
            else
            {
                container.Register<IDummyNoDependencies, DummyNoDependencies>();
            }

            

            Assert.IsTrue(dictionary[typeof(IDummyNoDependencies)].InterfaceType == typeof(IDummyNoDependencies));
            Assert.IsTrue(dictionary[typeof(IDummyNoDependencies)].ImplementationType == typeof(DummyNoDependencies));
            Assert.IsTrue(dictionary[typeof(IDummyNoDependencies)].ConcreteInstance == null);
            Assert.IsTrue(dictionary[typeof(IDummyNoDependencies)].LifeCycle == option);
            Assert.IsTrue(dictionary[typeof(IDummyNoDependencies)].CreateInstanceFunction == null);
            Assert.IsTrue(dictionary[typeof(IDummyNoDependencies)].ConstructorDependencies.Length == 0);
            Assert.IsTrue(dictionary[typeof(IDummyNoDependencies)].PropertiesDependencies.Length == 0);

            dependencyCollector.Verify(x => x.CollectPropertiesDependencies(It.IsAny<Type>()), Times.Once);
            dependencyCollector.Verify(x => x.CollectConstructorDependencies(It.IsAny<Type>()), Times.Once);

        }


        [TestMethod]
        [DataRow(LifeCycleOption.Singleton)]
        [DataRow(LifeCycleOption.Transient)]
        public void Test_Register_Generic_Transient_WithDependencies(LifeCycleOption option)
        {
            var factory = new Mock<IInstanceFactory>();
            var dependencyCollector = new Mock<IDependencyCollector>();
            var assemblyLoader = new Mock<IAssemblyLoader>();
            var dictionary = new Dictionary<Type, IMetaEntry>();

            dependencyCollector.Setup(x => x.CollectPropertiesDependencies(It.IsAny<Type>())).Returns(new PropertyInfo[3]).Verifiable();
            dependencyCollector.Setup(x => x.CollectConstructorDependencies(It.IsAny<Type>())).Returns(new Type[2]).Verifiable();

            var container = new Container(factory.Object, dependencyCollector.Object, assemblyLoader.Object, dictionary);

            dictionary.Clear();

            if (option == LifeCycleOption.Singleton)
            {
                container.RegisterAsSingleton<IDummyNoDependencies, DummyNoDependencies>();
            }
            else
            {
                container.Register<IDummyNoDependencies, DummyNoDependencies>();
            }

            

            Assert.IsTrue(dictionary[typeof(IDummyNoDependencies)].InterfaceType == typeof(IDummyNoDependencies));
            Assert.IsTrue(dictionary[typeof(IDummyNoDependencies)].ImplementationType == typeof(DummyNoDependencies));
            Assert.IsTrue(dictionary[typeof(IDummyNoDependencies)].ConcreteInstance == null);
            Assert.IsTrue(dictionary[typeof(IDummyNoDependencies)].LifeCycle == option);
            Assert.IsTrue(dictionary[typeof(IDummyNoDependencies)].CreateInstanceFunction == null);
            Assert.IsTrue(dictionary[typeof(IDummyNoDependencies)].ConstructorDependencies.Length == 2);
            Assert.IsTrue(dictionary[typeof(IDummyNoDependencies)].PropertiesDependencies.Length == 3);

            dependencyCollector.Verify(x => x.CollectPropertiesDependencies(It.IsAny<Type>()), Times.Once);
            dependencyCollector.Verify(x => x.CollectConstructorDependencies(It.IsAny<Type>()), Times.Once);

        }

        #endregion

        #region Resolve
        



        #endregion

    }
}