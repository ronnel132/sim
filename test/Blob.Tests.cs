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
      this.blobProps.home = new RadialPosition();
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

    // TODO: Test the blob states for correct behavior
    [Test]
    public void SearchingState() {
      SearchingState ss = new SearchingState();
      this.blobProps.step = 0.25;
      this.blobProps.sensor = new ProximitySensor(0.25);
      Blob blob = new Blob(this.blobProps);
      List<Blob> blobs = new List<Blob>() { blob };
      FoodSite fs = new FoodSite();
      fs.position = new RadialPosition(0.5, Math.PI/2);
      List<FoodSite> food = new List<FoodSite>() { fs };
      Board board = new Board(blobs, food);
      ss.ProcessNext(blob, board);
    }
  }
}