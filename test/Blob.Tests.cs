using NUnit.Framework;
using System;
using System.Collections.Generic; 
using Simulation;

namespace Simulation.Tests 
{
  public class BlobTests 
  {
    private BlobProps blobProps; 
    [SetUp]
    public void Initialize() {
      this.blobProps = new BlobProps();
      this.blobProps.isGreedy = true;
      this.blobProps.sensor = new ProximitySensor(0.03);
      this.blobProps.step = 0.01;
    }
    [Test]
    public void Equality1()
    {
      Blob b1 = new Blob(this.blobProps);
      Assert.AreEqual(b1, b1);
    }

    [Test]
    public void Equality2() {
      Blob b1 = new Blob(this.blobProps);
      Blob b2 = new Blob(this.blobProps);
      Assert.AreNotEqual(b1, b2);
    }
  }
}