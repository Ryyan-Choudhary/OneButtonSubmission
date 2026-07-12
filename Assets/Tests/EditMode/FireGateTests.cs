using NUnit.Framework;
using OneButtonSubmission.Core;

public class FireGateTests
{
    [Test]
    public void Can_Fire_Before_First_Shot()
    {
        var gate = new FireGate(0.6f);
        Assert.IsTrue(gate.CanFire(0f));
        Assert.AreEqual(1f, gate.ReloadProgress(0f));
    }

    [Test]
    public void Cannot_Fire_During_Cooldown()
    {
        var gate = new FireGate(0.6f);
        gate.RegisterFire(0f);
        Assert.IsFalse(gate.CanFire(0.3f));
        Assert.AreEqual(0.5f, gate.ReloadProgress(0.3f), 1e-4f);
    }

    [Test]
    public void Can_Fire_After_Cooldown_Elapses()
    {
        var gate = new FireGate(0.6f);
        gate.RegisterFire(0f);
        Assert.IsTrue(gate.CanFire(0.6f));
        Assert.IsTrue(gate.CanFire(1.0f));
        Assert.AreEqual(1f, gate.ReloadProgress(0.6f));
    }
}
