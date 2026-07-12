using NUnit.Framework;
using OneButtonSubmission.Core;

public class UprightControllerTests
{
    [Test]
    public void Zero_Lean_Zero_Velocity_Gives_Zero_Torque()
    {
        float t = UprightController.ComputeTorque(0f, 0f, 10f, 1f, 100f);
        Assert.AreEqual(0f, t, 1e-4f);
    }

    [Test]
    public void Positive_Lean_Produces_Negative_Corrective_Torque()
    {
        float t = UprightController.ComputeTorque(1f, 0f, 10f, 1f, 100f);
        Assert.AreEqual(-10f, t, 1e-4f);
    }

    [Test]
    public void Torque_Is_Clamped_To_Max()
    {
        // huge lean would demand -6000, clamp to -400
        float t = UprightController.ComputeTorque(30f, 0f, 200f, 30f, 400f);
        Assert.AreEqual(-400f, t, 1e-4f);
    }

    [Test]
    public void Damping_Opposes_Angular_Velocity()
    {
        // no lean, spinning positively -> negative (damping) torque
        float t = UprightController.ComputeTorque(0f, 5f, 10f, 2f, 100f);
        Assert.AreEqual(-10f, t, 1e-4f);
    }
}
