namespace Workout.Cli.Tests;

public class StringInterpolationTests
{
    [Test]
    public void StringInterpolation_WhenStringInterpolationConsistsOfMultipleParameteres_TheyMustBeParsedCorrectly()
    {
        var result = Program.Main(["start", "workout", "--working-directory", "../../../individual-workouts", "--file", "multiple-interpolations.workout", "--debug"]);

        Assert.That(result, Is.EqualTo(0));
    }
}
