namespace Workout.Cli.Tests;

public class E2ETests
{
    [Test]
    public void E2E_WhenRunningWorkoutCommandForSuccessfulTests_WorkoutMustSucceed()
    {
        var result = Program.Main(["start", "workout", "--working-directory", "../../../test-workouts"]);

        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void E2E_WhenRunningWorkoutCommandForFailingTest_WorkoutMustFailt()
    {
        var result = Program.Main(["start", "workout", "--working-directory", "../../../failing-workouts"]);

        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void E2E_WhenRunningWorkoutCommandForSingleTest_WorkoutMustRunForSingleTestOnly()
    {
        var result = Program.Main(["start", "workout", "--working-directory", "../../../test-workouts", "--test-case", "smokeTest2"]);

        Assert.That(result, Is.EqualTo(0));
    }
}