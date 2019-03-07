namespace Tests.Dummies
{
    public class DummyADependencies : IDummyADependencies
    {
        public IDummyNoDependencies Dependency { get; set; }

        public DummyADependencies(IDummyPDependencies dependency)
        {

        }
    }
}