using Workout.Language.Tokens;

namespace Workout.Language;

internal sealed class TestModel
{
    public TestModel(
        string testName,
        AssertionToken[] rawAssertions
    )
    {
        TestName = testName;
        Assertions = rawAssertions;
    }

    public string TestName { get; }
    public AssertionToken[] Assertions { get; }
}
