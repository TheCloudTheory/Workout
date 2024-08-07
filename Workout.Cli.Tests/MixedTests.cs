namespace Workout.Cli.Tests;

public class MixedTests
{
    [Test]
    public void Mixed_WhenParametersAreProvidedInWorkoutAndVariablesAreDefined_BothMustBeParsedCorrectly()
    {
        var result = Program.Main(["start", "workout", "--working-directory", "../../../individual-workouts", "--file", "params-and-variables.workout", "--debug"]);

        Assert.That(result, Is.EqualTo(0));
    }
}
