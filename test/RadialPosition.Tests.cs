using NUnit.Framework;
using System;
using Simulation;

namespace Simulation.Tests 
{
  public class RadialPositionTests
  {
    [TestCase(0, Math.PI, 2)]
    [TestCase(Math.PI/2, 3*Math.PI/2, 2)]
    [TestCase(Math.PI/4, 5*Math.PI/4, 2)]
    public void Distance(double theta1, double theta2, double dist)
    {
      RadialPosition rp1 = new RadialPosition(1, theta1);
      double _dist = rp1.Distance(new RadialPosition(1, theta2));
      Assert.AreEqual(Math.Round(dist, 2), Math.Round(_dist, 2));
    }

    [TestCase(0.5, 0, .3)]
    [TestCase(0, 0, .25)]
    [TestCase(0, Math.PI/2, .25)]
    public void RandomStep(double radius, double theta, double stepSize) {
      RadialPosition rp1 = new RadialPosition(radius, theta);
      rp1.RandomStep(stepSize);
      double dist = rp1.Distance(new RadialPosition(radius, theta));
      Assert.AreEqual(Math.Round(stepSize, 2), Math.Round(dist, 2));
    }

    [TestCase(1, Math.PI/2, 0.5)]
    [TestCase(1, Math.PI, 1)]
    [TestCase(1, Math.PI, 0.5)]
    [TestCase(0.5, 4*Math.PI/5, 0.33)]
    public void StepTo(double radius, double theta, double stepSize) {
      RadialPosition rp1 = new RadialPosition(0, 0);
      RadialPosition rp2 = new RadialPosition(radius, theta);
      rp1.StepTo(rp2, stepSize);

      Assert.AreEqual(stepSize, rp1.radius, "Radius is wrong");
      Assert.AreEqual(theta, rp1.theta, "Angle is wrong");
    }
  }
}