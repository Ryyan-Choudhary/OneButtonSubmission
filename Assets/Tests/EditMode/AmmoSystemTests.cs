using NUnit.Framework;
using OneButtonSubmission.Core;

public class AmmoSystemTests
{
    [Test]
    public void Starts_At_Given_Count_Clamped_To_Max()
    {
        var ammo = new AmmoSystem(max: 6, start: 6);
        Assert.AreEqual(6, ammo.Current);
        Assert.AreEqual(6, ammo.Max);
        Assert.IsFalse(ammo.IsEmpty);
    }

    [Test]
    public void TryConsume_Decrements_Until_Empty_Then_Fails()
    {
        var ammo = new AmmoSystem(6, 2);
        Assert.IsTrue(ammo.TryConsume());
        Assert.IsTrue(ammo.TryConsume());
        Assert.IsFalse(ammo.TryConsume());
        Assert.AreEqual(0, ammo.Current);
        Assert.IsTrue(ammo.IsEmpty);
    }

    [Test]
    public void Refill_Adds_But_Caps_At_Max()
    {
        var ammo = new AmmoSystem(6, 0);
        ammo.Refill(3);
        Assert.AreEqual(3, ammo.Current);
        ammo.Refill(10);
        Assert.AreEqual(6, ammo.Current);
    }

    [Test]
    public void Refill_Ignores_NonPositive()
    {
        var ammo = new AmmoSystem(6, 2);
        ammo.Refill(0);
        ammo.Refill(-5);
        Assert.AreEqual(2, ammo.Current);
    }
}
