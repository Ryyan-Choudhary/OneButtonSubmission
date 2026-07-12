using NUnit.Framework;
using OneButtonSubmission.Core;

public class GunRotatorTests
{
    [Test]
    public void Advances_By_Speed_Times_Dt()
    {
        float a = GunRotator.Advance(0f, 120f, 0.5f);
        Assert.AreEqual(60f, a, 1e-4f);
    }

    [Test]
    public void Wraps_Past_360()
    {
        float a = GunRotator.Advance(350f, 120f, 0.1f); // 350 + 12 = 362 -> 2
        Assert.AreEqual(2f, a, 1e-4f);
    }

    [Test]
    public void Stays_In_Zero_To_360_Range()
    {
        float a = GunRotator.Advance(359f, 120f, 1f); // 359 + 120 = 479 -> 119
        Assert.GreaterOrEqual(a, 0f);
        Assert.Less(a, 360f);
        Assert.AreEqual(119f, a, 1e-4f);
    }
}
