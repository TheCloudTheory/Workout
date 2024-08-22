namespace Workout.Cli.Tests;

public class ModulesTests
{
    [Test]
    public void Modules_WhenModuleIsReferencedByBicep_ItMustBeSupportedByWorkout()
    {
        var result = Program.Main(["start", "workout", "--working-directory", "../../../individual-workouts", "--file", "modules.workout", "--debug"]);

        Assert.That(result, Is.EqualTo(0));
    }
}
