namespace Workout.Cli.Tests;

public class ParametersTests
{
    [Test]
    public void Parameters_WhenParametersAreProvidedInWorkout_TheyMustBePassedToTheTemplate()
    {
        var result = Program.Main(["start", "workout", "--working-directory", "../../../individual-workouts", "--file", "params.workout", "--debug"]);

        Assert.That(result, Is.EqualTo(0));
    }
}
