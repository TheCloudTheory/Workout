using System;

namespace Workout.Cli.Tests;

public class VariablesTests
{
    [Test]
    public void Variables_WhenVariablesAreProvidedInWorkout_TheyMustBeCompiledAndAvailable()
    {
        var result = Program.Main(["start", "workout", "--working-directory", "../../../individual-workouts", "--file", "variables.workout"]);

        Assert.That(result, Is.EqualTo(0));
    }
}
