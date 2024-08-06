namespace Workout.Cli.Tests;

public class MultipleImportsTests
{
    [Test]
    public void MultipleImports_WhenMultipleFilesAreImportedIntoWorkoutFiles_AllResourcesMustBeAvailable()
    {
        var result = Program.Main(["start", "workout", "--working-directory", "../../../individual-workouts", "--file", "multiple-imports.workout"]);

        Assert.That(result, Is.EqualTo(0));
    }
}
