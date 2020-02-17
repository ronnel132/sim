using System;
using System.Collections.Generic; 

namespace Simulation {
  public struct SimulationProps {
    public int numBlobs;
    public int numFood;
    public double percentageGreedy;
    public double blobSensorSize;
    public double blobStepSize;
  }

  public class Board {
    private List<Blob> blobs;
    private List<FoodSite> food;
    private FoodSiteMediatorStore mediatorStore;
    private Boolean simulationActive;
    private SimulationProps simulationProps;
    public Board(SimulationProps simProps) {
      this.simulationActive = false;
      this.simulationProps = simProps;
    }

    private void ensureSimulationActive() {
      if (!this.simulationActive) {
        throw new InvalidOperationException("Attempting to call iteration function outside of iteration");
      }
    }

    internal SensorResult AcceptSensor(Blob blob, IBlobSensor sensor) {
      ensureSimulationActive();
      return sensor.sense(blob, this);
    }

    internal void VisitFoodSite(FoodSite foodSite, Blob b) {
      ensureSimulationActive();
      this.mediatorStore.VisitFoodSite(foodSite, b);
    }

    internal Boolean FoodSiteAvailable(FoodSite foodSite) {
      ensureSimulationActive();
      return this.mediatorStore.FoodSiteAvailable(foodSite);
    }

    internal Blob[] FindBlobsNear(RadialPosition pos, double radius) {
      ensureSimulationActive();
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

    internal FoodSite[] FindFoodSiteNear(RadialPosition pos, double radius) {
      ensureSimulationActive();
      List<FoodSite> food = new List<FoodSite>();
      foreach(FoodSite f in this.food) {
        RadialPosition f_pos = f.GetPosition();
        double dist = f_pos.Distance(pos);
        if (dist <= radius) {
          food.Add(f);
        }
      }
      return food.ToArray();
    }

    private void ProcesIterationStep() {
      ensureSimulationActive();
      // TODO: would be interesting to explore running blob processing on multiple cpu threads
      // TODO: is ordering a concern? i.e. ordering of blob processing and mediator processing
      foreach (Blob b in this.blobs) {
        b.ProcessNext(this);
      }
      this.mediatorStore.ProcessNext();
      this.food.RemoveAll(fs => fs.IsEaten());
    }

    private void PlaceBlobsUniformly() {

    }

    public void Run(int epochs) {
      this.mediatorStore = new FoodSiteMediatorStore();
      this.blobs = new List<Blob>();
      this.food = new List<FoodSite>();
      int numGreedy = Convert.ToInt32(this.simulationProps.numBlobs * this.simulationProps.percentageGreedy);
      int numNonGreedy = this.simulationProps.numBlobs - numGreedy;

      for (int i = 0; i < numGreedy; i++) {
        BlobProps blobProps = new BlobProps {
          isGreedy = true,
          sensor = new ProximitySensor(this.simulationProps.blobSensorSize),
          step = this.simulationProps.blobStepSize,
        };

        this.blobs.Add(new Blob(blobProps));
      }

      for (int i = 0; i < numNonGreedy; i++) {
        BlobProps blobProps = new BlobProps {
          isGreedy = false,
          sensor = new ProximitySensor(this.simulationProps.blobSensorSize),
          step = this.simulationProps.blobStepSize,
        };

        this.blobs.Add(new Blob(blobProps));
      }

      this.PlaceBlobsUniformly();
      for (int i = 0; i < epochs; i++) {
        this.simulationActive = true;
        // Rehome blobs uniformly across the circle
        // Create food and distribute across board
        // Create and randomly distribute food across the circle
        // ProcessNext() until all blobs are eaten and they return home
        this.simulationActive = false;
        // Add and remove blobs based on outcomes
      }
    }
  }
}