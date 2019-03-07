using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Rhino.Mocks;
using Tests.Dummies;
using VSolver.Implementations;
using MockRepository = Moq.MockRepository;

namespace Tests.UnitTests
{

   [TestClass]
    public class FactoryTests
    {

        #region Happy
        [TestMethod]
        public void CreateInstance_NoDependencies()
        {

            var instance = new object();
            var constructorDependencies = new object[0];

            var typeMock = Rhino.Mocks.MockRepository.GenerateMock<Type>();
            typeMock.BackToRecord();
            typeMock.Expect(x => x.GetProperties(Arg<BindingFlags>.Is.Equal(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)))
                .Repeat.Once()
                .Return(new PropertyInfo[0]);
            typeMock.Expect(x => x.GetConstructors(Arg<BindingFlags>.Is.Equal(BindingFlags.Instance | BindingFlags.Public)))
                .Repeat.Once()
                .Return(new ConstructorInfo[1]);
            typeMock.Replay();

            var activator = Rhino.Mocks.MockRepository.GenerateMock<IInstanceActivator>();
            activator.BackToRecord();
            activator.Expect(x => x.CreateInstance(
                    Arg<Type>.Is.Equal(typeMock),
                    Arg<object[]>.Is.Equal(constructorDependencies)))
                .Repeat.Once()
                .Return(instance);
            activator.Replay();

            var propertyDependenciesMock = Rhino.Mocks.MockRepository.GenerateMock<IDictionary<PropertyInfo, object>>();
            propertyDependenciesMock.BackToRecord();
            propertyDependenciesMock.Expect(x => x.ContainsKey(Arg<PropertyInfo>.Is.Anything))
                .Repeat.Never();
            propertyDependenciesMock.Replay();

            var factory = new InstanceFactory(activator);
            var resultInstance = factory.CreateInstance(typeMock, constructorDependencies, propertyDependenciesMock);

            typeMock.VerifyAllExpectations();
            activator.VerifyAllExpectations();
            propertyDependenciesMock.VerifyAllExpectations();
            Assert.AreEqual(instance, resultInstance);

        }

        [TestMethod]
        public void CreateInstance_ConstructorDependenciesOnly()
        {
            var instance = new object();
            var constructorDependencies = new[] { new object() };

            var typeMock = Rhino.Mocks.MockRepository.GenerateMock<Type>();
            typeMock.BackToRecord();
            typeMock.Expect(x => x.GetProperties(Arg<BindingFlags>.Is.Equal(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)))
                .Repeat.Once()
                .Return(new PropertyInfo[0]);
            typeMock.Expect(x => x.GetConstructors(Arg<BindingFlags>.Is.Equal(BindingFlags.Instance | BindingFlags.Public)))
                .Repeat.Once()
                .Return(new ConstructorInfo[1]);
            typeMock.Replay();

            var activator = Rhino.Mocks.MockRepository.GenerateMock<IInstanceActivator>();
            activator.BackToRecord();
            activator.Expect(x => x.CreateInstance(
                    Arg<Type>.Is.Equal(typeMock),
                    Arg<object[]>.Is.Equal(constructorDependencies)))
                .Repeat.Once()
                .Return(instance);
            activator.Replay();

            var propertyDependenciesMock = Rhino.Mocks.MockRepository.GenerateMock<IDictionary<PropertyInfo, object>>();
            propertyDependenciesMock.BackToRecord();
            propertyDependenciesMock.Expect(x => x.ContainsKey(Arg<PropertyInfo>.Is.Anything))
                .Repeat.Never();
            propertyDependenciesMock.Replay();

            var factory = new InstanceFactory(activator);
            var resultInstance = factory.CreateInstance(typeMock, constructorDependencies, propertyDependenciesMock);

            typeMock.VerifyAllExpectations();
            activator.VerifyAllExpectations();
            propertyDependenciesMock.VerifyAllExpectations();
            Assert.AreEqual(instance, resultInstance);
        }

        [TestMethod]
        public void CreateInstance_PropertiesDependenciesOnly()
        {
            var instance = new object();
            var propertyValue = new object();
            var constructorDependencies = new object[0];

            var propertyInfo = Rhino.Mocks.MockRepository.GenerateMock<PropertyInfo>();
            propertyInfo.BackToRecord();
            propertyInfo.Expect(x => x.SetValue(
                    Arg<object>.Is.Equal(instance),
                    Arg<object>.Is.Equal(propertyValue),
                    Arg<object[]>.Is.Equal(null)))
                .Repeat.Once();
            propertyInfo.Replay();

            var typeMock = Rhino.Mocks.MockRepository.GenerateMock<Type>();
            typeMock.BackToRecord();
            typeMock.Expect(x => x.GetProperties(Arg<BindingFlags>.Is.Equal(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)))
                .Repeat.Once()
                .Return(new[] { propertyInfo });
            typeMock.Expect(x => x.GetConstructors(Arg<BindingFlags>.Is.Equal(BindingFlags.Instance | BindingFlags.Public)))
                .Repeat.Once()
                .Return(new ConstructorInfo[1]);
            typeMock.Replay();

            var activator = Rhino.Mocks.MockRepository.GenerateMock<IInstanceActivator>();
            activator.BackToRecord();
            activator.Expect(x => x.CreateInstance(
                    Arg<Type>.Is.Equal(typeMock),
                    Arg<object[]>.Is.Equal(constructorDependencies)))
                .Repeat.Once()
                .Return(instance);
            activator.Replay();

            var propertyDependenciesMock = Rhino.Mocks.MockRepository.GenerateMock<IDictionary<PropertyInfo, object>>();
            propertyDependenciesMock.BackToRecord();
            propertyDependenciesMock.Expect(x => x[Arg<PropertyInfo>.Is.Equal(propertyInfo)])
                .Repeat.Once()
                .Return(propertyValue);
            propertyDependenciesMock.Expect(x => x.ContainsKey(Arg<PropertyInfo>.Is.Equal(propertyInfo)))
                .Repeat.Once()
                .Return(true);
            propertyDependenciesMock.Replay();

            var factory = new InstanceFactory(activator);
            var resultInstance = factory.CreateInstance(typeMock, constructorDependencies, propertyDependenciesMock);

            typeMock.VerifyAllExpectations();
            activator.VerifyAllExpectations();
            propertyDependenciesMock.VerifyAllExpectations();
            propertyInfo.VerifyAllExpectations();
            Assert.AreEqual(instance, resultInstance);
        }

        [TestMethod]
        public void CreateInstance_AllDependencies()
        {
            var instance = new object();
            var propertyValue = new object();
            var constructorDependencies = new[] { new object() };

            var propertyInfo = Rhino.Mocks.MockRepository.GenerateMock<PropertyInfo>();
            propertyInfo.BackToRecord();
            propertyInfo.Expect(x => x.SetValue(
                    Arg<object>.Is.Equal(instance),
                    Arg<object>.Is.Equal(propertyValue),
                    Arg<object[]>.Is.Equal(null)))
                .Repeat.Once();
            propertyInfo.Replay();

            var typeMock = Rhino.Mocks.MockRepository.GenerateMock<Type>();
            typeMock.BackToRecord();
            typeMock.Expect(x => x.GetProperties(Arg<BindingFlags>.Is.Equal(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)))
                .Repeat.Once()
                .Return(new[] { propertyInfo });
            typeMock.Expect(x => x.GetConstructors(Arg<BindingFlags>.Is.Equal(BindingFlags.Instance | BindingFlags.Public)))
                .Repeat.Once()
                .Return(new ConstructorInfo[1]);
            typeMock.Replay();

            var activator = Rhino.Mocks.MockRepository.GenerateMock<IInstanceActivator>();
            activator.BackToRecord();
            activator.Expect(x => x.CreateInstance(
                    Arg<Type>.Is.Equal(typeMock),
                    Arg<object[]>.Is.Equal(constructorDependencies)))
                .Repeat.Once()
                .Return(instance);
            activator.Replay();

            var propertyDependenciesMock = Rhino.Mocks.MockRepository.GenerateMock<IDictionary<PropertyInfo, object>>();
            propertyDependenciesMock.BackToRecord();
            propertyDependenciesMock.Expect(x => x[Arg<PropertyInfo>.Is.Equal(propertyInfo)])
                .Repeat.Once()
                .Return(propertyValue);
            propertyDependenciesMock.Expect(x => x.ContainsKey(Arg<PropertyInfo>.Is.Equal(propertyInfo)))
                .Repeat.Once()
                .Return(true);
            propertyDependenciesMock.Replay();

            var factory = new InstanceFactory(activator);
            var resultInstance = factory.CreateInstance(typeMock, constructorDependencies, propertyDependenciesMock);

            typeMock.VerifyAllExpectations();
            activator.VerifyAllExpectations();
            propertyDependenciesMock.VerifyAllExpectations();
            propertyInfo.VerifyAllExpectations();
            Assert.AreEqual(instance, resultInstance);
        }
        #endregion
        #region Exceptions

        #endregion
    }
}