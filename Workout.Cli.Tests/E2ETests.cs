namespace Workout.Cli.Tests;

public class E2ETests
{
    [Test]
    public void E2E_WhenRunningWorkoutCommand_ShouldRunTestsForTheCurrentDirectory()
    {
        var result = Program.Main(["start", "workout", "--working-directory", "../../../test-workouts"]);

        Assert.That(result, Is.EqualTo(0));
    }
}