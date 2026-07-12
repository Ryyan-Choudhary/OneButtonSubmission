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

    [Test]
    public void UpBoost_Adds_Flat_Vertical_Kick()
    {
        // barrel right -> recoil left, boost lifts it
        Vector2 impulse = RecoilCalculator.Impulse(0f, 10f, 4f);
        Assert.AreEqual(-10f, impulse.x, 1e-4f);
        Assert.AreEqual(4f, impulse.y, 1e-4f);
    }

    [Test]
    public void CancelOpposing_Kills_Velocity_Fighting_The_Shot()
    {
        // drifting left, shot pushes right -> leftward speed fully cancelled
        Vector2 v = RecoilCalculator.CancelOpposing(
            new Vector2(-11f, 3f), new Vector2(12f, 0f), 1f);
        Assert.AreEqual(0f, v.x, 1e-4f);
        Assert.AreEqual(3f, v.y, 1e-4f); // perpendicular part untouched
    }

    [Test]
    public void CancelOpposing_Keeps_Aligned_Velocity_For_Stacking()
    {
        // already moving with the shot -> nothing cancelled, momentum stacks
        Vector2 v = RecoilCalculator.CancelOpposing(
            new Vector2(8f, 2f), new Vector2(12f, 0f), 1f);
        Assert.AreEqual(8f, v.x, 1e-4f);
        Assert.AreEqual(2f, v.y, 1e-4f);
    }

    [Test]
    public void CancelOpposing_Partial_Factor_Cancels_Partially()
    {
        Vector2 v = RecoilCalculator.CancelOpposing(
            new Vector2(-10f, 0f), new Vector2(5f, 0f), 0.5f);
        Assert.AreEqual(-5f, v.x, 1e-4f);
    }
}
