namespace Tests.Dummies
{
    public class DummyPDependencies : IDummyPDependencies
    {
        public IDummyNoDependencies Dependency { get; set; }
    }
}