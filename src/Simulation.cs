using System;
using System.Collections.Generic; 

namespace Simulation {
  public class Board {
    private HashSet<Blob> blobs;
    private HashSet<FoodSite> food;
    private FoodSiteMediatorStore mediatorStore;
    public Board() {
      this.mediatorStore = new FoodSiteMediatorStore();
    }

    public SensorResult AcceptSensor(Blob blob, IBlobSensor sensor) {
      return sensor.sense(blob, this);
    }

    public void VisitFoodSite(FoodSite foodSite, Blob b) {
      this.mediatorStore.VisitFoodSite(foodSite, b);
    }

    public Boolean FoodSiteAvailable(FoodSite foodSite) {
      return this.mediatorStore.FoodSiteAvailable(foodSite);
    }

    public Blob[] FindBlobsNear(RadialPosition pos, double radius) {
      List<Blob> blobs = new List<Blob>();
      foreach(Blob b in this.blobs) {
        RadialPosition b_pos = b.position;
        double dist = b_pos.Distance(pos);
        if (dist <= radius) {
          blobs.Add(b);
        }
      }
      return blobs.ToArray();
    }

    public FoodSite[] FindFoodSiteNear(RadialPosition pos, double radius) {
      List<FoodSite> food = new List<FoodSite>();
      foreach(FoodSite f in this.food) {
        RadialPosition f_pos = f.position;
        double dist = f_pos.Distance(pos);
        if (dist <= radius) {
          food.Add(f);
        }
      }
      return food.ToArray();
    }

    public void ProcessNext() {
      foreach (Blob b in this.blobs) {
        b.ProcessNext(this);
      }
      this.mediatorStore.ProcessNext();
      this.food.RemoveWhere(fs => fs.IsEaten());
    }
  }

  public struct SimulationProps {
    int foodCount;
    int blobCount;
  }

  public class Simulation {
    private SimulationProps props;
    public Simulation(SimulationProps props) {
      this.props = props;
      // TODO: initialize the board based on props
    }

    public void run() {
      // TODO: implement the run logic
    }
  }
}