namespace Tests.Dummies
{
    public interface IDummyADependencies
    {
        IDummyNoDependencies Dependency { get; set; }
    }
}