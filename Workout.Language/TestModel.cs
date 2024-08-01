namespace Workout.Language;

internal sealed class TestModel
{
    public TestModel(
        string testName
    )
    {
        TestName = testName;
    }

    public string TestName { get; }
}
