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
    private static Random rng = new Random(); 
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

    internal List<Blob> FindBlobsNear(RadialPosition pos, double radius) {
      ensureSimulationActive();
      List<Blob> blobs = new List<Blob>();
      foreach(Blob b in this.blobs) {
        RadialPosition b_pos = b.GetPosition();
        double dist = b_pos.Distance(pos);
        if (dist <= radius) {
          blobs.Add(b);
        }
      }
      return blobs;
    }

    internal List<FoodSite> FindFoodSiteNear(RadialPosition pos, double radius) {
      ensureSimulationActive();
      List<FoodSite> food = new List<FoodSite>();
      foreach(FoodSite f in this.food) {
        RadialPosition f_pos = f.GetPosition();
        double dist = f_pos.Distance(pos);
        if (dist <= radius) {
          food.Add(f);
        }
      }
      return food;
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
      if (this.food.Count == 0) {
        foreach (Blob b in this.blobs) {
          if (!b.IsHome() && !b.IsHomeward()) {
            b.SendHome();
          }
        }
      }
    }

    private void PlaceBlobsUniformly() {
      Utils.Shuffle<Blob>(this.blobs);
      int blobCount = this.blobs.Count;
      double spacing = (2 * Math.PI) / blobCount;
      for (int i = 0; i < blobCount; i++) {
        RadialPosition home = new RadialPosition(1, spacing * (i + 1));
        this.blobs[i].SetPosition(home);
        this.blobs[i].SetHome(home);
      }
    }

    private void PlaceFoodRandomly(int numFood) {
      for (int i = 0; i < numFood; i++) {
        FoodSite food = new FoodSite();
        food.SetPosition(new RadialPosition(rng.NextDouble(), 2 * Math.PI * rng.NextDouble()));
        this.food.Add(food);
      }
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

      for (int i = 0; i < epochs; i++) {
        int greedyCount = this.blobs.FindAll((b) => b.GetBlobProps().isGreedy).Count;
        int notGreedyCount = this.blobs.FindAll((b) => !b.GetBlobProps().isGreedy).Count;
        Console.WriteLine(String.Format("Epoch: {0}, GreedyCount: {1}, NotGreedyCount: {2}", i, greedyCount, notGreedyCount));

        this.simulationActive = true;
        // Rehome blobs uniformly across the circle
        this.PlaceBlobsUniformly();
        // Create food and distribute across board
        this.PlaceFoodRandomly(this.simulationProps.numFood);
        // ProcessNext() until all food is eaten and all blobs return home
        while (this.blobs.Exists((blob) => !blob.IsHome())) {
          this.ProcesIterationStep();
        }
        this.simulationActive = false;
        // Add and remove blobs based on outcomes
        List<Blob> newBlobs = new List<Blob>();
        foreach (Blob b in this.blobs) {
          if (b.GetSatiety() == Satiety.None) {
            // Dead
          } else if (b.GetSatiety() == Satiety.Half) {
            newBlobs.Add(new Blob(b));
            // Lives
          } else if (b.GetSatiety() == Satiety.Full) {
            // Reproduce
            newBlobs.Add(new Blob(b));
            newBlobs.Add(new Blob(b));
          }
        }
        this.blobs = newBlobs;
      }
    }
  }
}