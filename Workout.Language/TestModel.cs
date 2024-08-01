namespace Workout.Language;

internal sealed class TestModel
{
    public TestModel(
        string testName,
        string[] rawAssertions
    )
    {
        TestName = testName;
        RawAssertions = rawAssertions;
    }

    public string TestName { get; }
    public string[] RawAssertions { get; }
}
