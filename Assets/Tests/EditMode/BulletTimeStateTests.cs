using NUnit.Framework;
using OneButtonSubmission.Core;

public class BulletTimeStateTests
{
    [Test]
    public void Starts_Ready_And_Full()
    {
        var bt = new BulletTimeState(1f, 2f);
        Assert.IsTrue(bt.CanActivate);
        Assert.IsFalse(bt.IsActive);
        Assert.AreEqual(1f, bt.MeterFill, 1e-4f);
    }

    [Test]
    public void Activate_Then_Drains()
    {
        var bt = new BulletTimeState(1f, 2f);
        bt.Activate();
        Assert.IsTrue(bt.IsActive);
        Assert.IsFalse(bt.CanActivate);
        Assert.IsFalse(bt.Tick(0.5f));
        Assert.AreEqual(0.5f, bt.MeterFill, 1e-4f);
    }

    [Test]
    public void Depletes_And_Enters_Cooldown()
    {
        var bt = new BulletTimeState(1f, 2f);
        bt.Activate();
        Assert.IsFalse(bt.Tick(0.5f));
        Assert.IsTrue(bt.Tick(0.6f)); // crosses 1.0 -> depleted this tick
        Assert.AreEqual(BulletTimeState.Phase.Cooldown, bt.Current);
        Assert.IsFalse(bt.CanActivate);
        Assert.AreEqual(0f, bt.MeterFill, 1e-4f);
    }

    [Test]
    public void Cooldown_Refills_Then_Ready()
    {
        var bt = new BulletTimeState(1f, 2f);
        bt.Activate();
        bt.Tick(1f); // deplete
        Assert.IsFalse(bt.Tick(1f));
        Assert.AreEqual(0.5f, bt.MeterFill, 1e-4f);
        bt.Tick(1f); // reaches cooldown
        Assert.AreEqual(BulletTimeState.Phase.Ready, bt.Current);
        Assert.IsTrue(bt.CanActivate);
        Assert.AreEqual(1f, bt.MeterFill, 1e-4f);
    }

    [Test]
    public void Deactivate_Early_Enters_Cooldown()
    {
        var bt = new BulletTimeState(2f, 2f);
        bt.Activate();
        bt.Tick(0.5f);
        bt.Deactivate();
        Assert.AreEqual(BulletTimeState.Phase.Cooldown, bt.Current);
        Assert.IsFalse(bt.CanActivate);
    }

    [Test]
    public void Cannot_Activate_While_Cooling()
    {
        var bt = new BulletTimeState(1f, 2f);
        bt.Activate();
        bt.Deactivate();
        bt.Activate(); // ignored during cooldown
        Assert.AreEqual(BulletTimeState.Phase.Cooldown, bt.Current);
    }
}
