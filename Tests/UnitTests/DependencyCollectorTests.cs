using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.Dummies;
using VSolver.Implementations;

namespace Tests.UnitTests
{
    [TestClass]
    public class DependencyCollectorTests
    {
        [TestMethod]
        public void Collect_ConstructorDependencies_NoDependencies()
        {
            var dependencyCollector = new DependencyCollector();

            var collection = dependencyCollector.CollectConstructorDependencies(typeof(DummyNoDependencies));

            Assert.AreEqual(0, collection.Length);
        }

        [TestMethod]
        public void Collect_ConstructorDependencies_OneDependency()
        {
            var dependencyCollector = new DependencyCollector();

            var collection = dependencyCollector.CollectConstructorDependencies(typeof(DummyCDependencies));

            Assert.AreEqual(1, collection.Length);
            Assert.IsTrue(typeof(IDummyNoDependencies) == collection[0]);
        }

        [TestMethod]
        public void Collect_PropertiesDependencies_NoDependencies()
        {
            var dependencyCollector = new DependencyCollector();

            var collection = dependencyCollector.CollectPropertiesDependencies(typeof(DummyNoDependencies));

            Assert.AreEqual(0, collection.Length);
        }

        [TestMethod]
        public void Collect_PropertiesDependencies_OneDependency()
        {
            var dependencyCollector = new DependencyCollector();

            var collection = dependencyCollector.CollectPropertiesDependencies(typeof(DummyPDependencies));

            Assert.AreEqual(1, collection.Length);
            Assert.IsTrue(typeof(IDummyNoDependencies) == collection[0].PropertyType);
        }
    }
}