using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.Dummies;
using VSolver.Implementations;

namespace Tests.IntegrationTests
{
    [TestClass]
    public class ContainerIntegrationTests
    {
       
        [TestMethod]
        public void Test_RegisterAndResolve_AsTransientType_TypePassedByParameter_BasePassedByParameter_NoFunction_NoDependencies()
        {
            var dummyObject = new DummyNoDependencies();
            var container = new Container();

            Assert.ThrowsException<Exception>(() => container.Resolve<IDummyNoDependencies>());

            container.Register<IDummyNoDependencies, Dummies.DummyNoDependencies>();

            container.RegisterAsSingleton<IDummyNoDependencies, Dummies.DummyNoDependencies>();
            container.RegisterAsSingleton<IDummyNoDependencies>(() => new Dummies.DummyNoDependencies());
            container.RegisterAsSingleton<Dummies.DummyNoDependencies>();

            container.Register<IDummyNoDependencies>(() => new Dummies.DummyNoDependencies());
            container.Register<Dummies.DummyNoDependencies>();

            Assert.AreEqual(dummyObject, container.Resolve<IDummyNoDependencies>());
        }

       

    }
}