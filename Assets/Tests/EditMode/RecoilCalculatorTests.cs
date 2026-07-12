using NUnit.Framework;
using UnityEngine;
using OneButtonSubmission.Core;

public class RecoilCalculatorTests
{
    [Test]
    public void Barrel_Down_Launches_Up()
    {
        // barrel at -90 (pointing down) -> impulse points up (+Y)
        Vector2 impulse = RecoilCalculator.Impulse(-90f, 10f);
        Assert.AreEqual(0f, impulse.x, 1e-4f);
        Assert.AreEqual(10f, impulse.y, 1e-4f);
    }

    [Test]
    public void Barrel_Right_Pushes_Left()
    {
        // barrel at 0 (pointing +X) -> impulse points -X
        Vector2 impulse = RecoilCalculator.Impulse(0f, 10f);
        Assert.AreEqual(-10f, impulse.x, 1e-4f);
        Assert.AreEqual(0f, impulse.y, 1e-4f);
    }

    [Test]
    public void Magnitude_Equals_Force()
    {
        Vector2 impulse = RecoilCalculator.Impulse(37f, 8f);
        Assert.AreEqual(8f, impulse.magnitude, 1e-4f);
    }
}
