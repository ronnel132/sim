using System;
using System.Collections.Generic; 

namespace Simulation {
  public class RadialPosition {
    public double theta;
    public double radius;

    public double Distance(RadialPosition rp) {
      double x_1 = rp.radius * Math.Cos(rp.theta); 
      double y_1 = rp.radius * Math.Sin(rp.theta);

      double x_2 = this.radius * Math.Cos(this.theta);
      double y_2 = this.radius * Math.Sin(this.theta);

      return Math.Sqrt(Math.Pow(x_2 - x_1, 2) + Math.Pow(y_2 - y_1, 2));
    } 

    private void SetNewCoordinates(double x_new, double y_new) {
      double radius = Math.Sqrt(Math.Pow(x_new, 2) + Math.Pow(y_new, 2));
      double theta = Math.Atan(y_new / x_new);
      if (this.radius > 1) {
        // Hop to the other side
        this.radius = 1 - (radius % 1);
        this.theta = -theta;
      } else {
        this.radius = radius;
        this.theta = theta;
      }
    }

    public void StepTo(RadialPosition rp, double stepSize) {
      double x_1 = this.radius * Math.Cos(this.theta);
      double y_1 = this.radius * Math.Sin(this.theta);

      double x_2 = rp.radius * Math.Cos(rp.theta); 
      double y_2 = rp.radius * Math.Sin(rp.theta);

      double theta = Math.Atan((x_2 - x_1) / (y_2 - y_1));
      double x_new = x_1 + Math.Cos(theta) * stepSize;
      double y_new = y_1 + Math.Sin(theta) * stepSize;

      this.SetNewCoordinates(x_new, y_new);
    }

    public void RandomStep(double stepSize) {
      Random random = new Random();
      double theta = random.NextDouble() * 2 * Math.PI;

      double delta_x = stepSize * Math.Cos(theta);
      double delta_y = stepSize * Math.Sin(theta);

      double x = this.radius * Math.Cos(this.theta);
      double y = this.radius * Math.Sin(this.theta);

      double x_new = x + delta_x;
      double y_new = y + delta_y;

      this.SetNewCoordinates(x_new, y_new);
    }
  }

  // TODO: Maybe abstract the sensor result to allow for different types of sensing results
  public struct SensorResult {
    public Blob[] blobs;
    public FoodSite[] food; 
  }

  public interface IBlobSensor {
    SensorResult sense(Blob blob, Board board);
  }

  public class ProximitySensor : IBlobSensor {
    private double sensingRadius;
    public ProximitySensor(double sensingRadius) {
      this.sensingRadius = sensingRadius; 
    }

    public SensorResult sense(Blob blob, Board board) {
      SensorResult sensorResult;
      sensorResult.blobs = board.FindBlobsNear(blob.position, sensingRadius);
      sensorResult.food = board.FindFoodSiteNear(blob.position, sensingRadius);
      return sensorResult;
    }
  }

  public struct BlobProps {
    public bool isGreedy;
    public RadialPosition home; 
    public IBlobSensor sensor;
    public double step;
  }

  public interface BlobState {
    void ProcessNext(Blob blob, Board board);
  }

  /*
  Searches for food in a random walk. Goes to the first food that's available, even if another blob is there.
   */
  public class SearchingState : BlobState {
    private FoodSite selectedFood = null;

    private void ValidateExistingSelection(Blob blob, Board board) {
      if (this.selectedFood == null) {
        return;
      }
      if (!board.FoodSiteAvailable(this.selectedFood)) {
        this.selectedFood = null;
      }
    }

    private void TrySelectFood(Board board, FoodSite[] sensedFoodSites) {
      if (this.selectedFood != null) {
        return;
      }
      // Make a random selection among available foods
      List<FoodSite> available = new List<FoodSite>();
      foreach (FoodSite f in sensedFoodSites) {
        if (board.FoodSiteAvailable(f)) {
          available.Add(f); 
        }
      }
      if (available.Count > 0) {
        Random rand = new Random();
        this.selectedFood = available[rand.Next(0, available.Count)];
      }
    }

    public void ProcessNext(Blob blob, Board board) {
      SensorResult senses = board.AcceptSensor(blob, blob.props.sensor);
      Blob[] sensedBlobs = senses.blobs;
      FoodSite[] sensedFoodSites = senses.food;

      if (this.selectedFood == null) {
        return;
      }
      if (!board.FoodSiteAvailable(this.selectedFood)) {
        this.selectedFood = null;
      }

      TrySelectFood(board, sensedFoodSites);

      double stepSize = blob.props.step;
      if (this.selectedFood == null) {
        // Random walk
        // TODO: Explore more intelligent search strategies
        blob.position.RandomStep(stepSize);
      } else if (blob.position.Distance(this.selectedFood.position) <= stepSize) {
        board.VisitFoodSite(this.selectedFood, blob);
        blob.state = new AtFoodSiteState(this.selectedFood);
        blob.position = this.selectedFood.position; 
      } else {
        blob.position.StepTo(this.selectedFood.position, stepSize);
      }
    }
  }

  public class AtFoodSiteState : BlobState {
    public FoodSite foodSite;

    public AtFoodSiteState(FoodSite foodSite) {
      this.foodSite = foodSite;
    }
    public void ProcessNext(Blob blob, Board board) {
      // no-op. The mediator will handle state transition
    }
  }

  public enum Satiety {
    None,
    Half,
    Full,
  }

  public class HomewardState : BlobState {
    private Satiety satiety;

    public HomewardState(Satiety satiety) {
      this.satiety = satiety;
    }

    public void ProcessNext(Blob blob, Board board) {
      double stepSize = blob.props.step;
      if (blob.position.Distance(blob.props.home) > stepSize) {
        blob.position.StepTo(blob.props.home, blob.props.step);
      } else {
        blob.position = blob.props.home;
        blob.state = new HomeState(this.satiety);
      }
    }
  }

  public class HomeState : BlobState {
    private Satiety satiety;

    public HomeState(Satiety satiety) {
      this.satiety = satiety;
    }
    public void ProcessNext(Blob blob, Board board) {
      // TODO: No-op? Where to implement death/reproduce step?
    }
  }

  public class Blob {
    public Guid id;
    public BlobProps props;
    public RadialPosition position;
    public BlobState state;

    public Blob(BlobProps props) {
      id = Guid.NewGuid();
      state = new SearchingState();
      this.props = props;
    }

    public void ProcessNext(Board board) {
      this.state.ProcessNext(this, board);
    }

    public void SendHome(Satiety satiety = Satiety.None) {
      this.state = new HomewardState(satiety);
    }

    public override int GetHashCode() {
      return this.id.GetHashCode();
    }

    public override Boolean Equals(object? obj) {
      if ((obj == null) || ! this.GetType().Equals(obj.GetType())) {
         return false;
      }
      Blob blob = (Blob) obj;
      return this.id == blob.id;
    }
  }

  public enum FoodState {
    Available,
    Eaten,
  }

  public class FoodSite {
    public Guid id;
    public RadialPosition position;
    public FoodState state;
    public FoodSite() {
      this.id = Guid.NewGuid();
      this.state = FoodState.Available;
    }

    public Boolean IsEaten() {
      return this.state == FoodState.Eaten;
    }

    public override int GetHashCode() {
      return this.id.GetHashCode();
    }

    public override Boolean Equals(object? obj) {
      if ((obj == null) || ! this.GetType().Equals(obj.GetType())) {
         return false;
      }
      FoodSite food = (FoodSite) obj;
      return this.id == food.id;
    }
  }

  public class FoodSiteMediator {
    private int countTime = 0;
    private FoodSite foodSite;
    private List<Blob> blobs;
    
    public FoodSiteMediator(FoodSite fs) {
      this.foodSite = fs;
    }

    public void Visit(Blob b) {
      if (this.blobs.Contains(b)) {
        throw new InvalidOperationException(String.Format("Attempting to add blob id {0} to mediator", b.id));
      }

      this.blobs.Add(b);
    }

    public Boolean FoodSiteAvailable() {
      return this.blobs.Count <= 1 && !this.foodSite.IsEaten();
    }

    public void ProcessNext() {
      this.countTime++;

      if (this.countTime >= Constants.FOOD_LOCKUP_PERIOD) {
        if (this.blobs.Count == 1) {
          this.blobs[0].SendHome(Satiety.Full);
        } else if (this.blobs.Count == 2) {
          Boolean blob1Greedy = this.blobs[0].props.isGreedy;
          Boolean blob2Greedy = this.blobs[2].props.isGreedy;
          // TODO: Abstract this algorithm into a strategy to keep implementation away from Mediator
          if (!blob1Greedy && !blob2Greedy) {
            // They share
            this.blobs[0].SendHome(Satiety.Half);
            this.blobs[1].SendHome(Satiety.Half);
          } else if (blob1Greedy && !blob2Greedy) {
            this.blobs[0].SendHome(Satiety.Full);
            this.blobs[1].SendHome(Satiety.None);
          } else if (!blob1Greedy && blob2Greedy) {
            this.blobs[0].SendHome(Satiety.None);
            this.blobs[1].SendHome(Satiety.Full);
          } else {
            // blob1Greedy && blob2Greedy
            this.blobs[0].SendHome(Satiety.None);
            this.blobs[1].SendHome(Satiety.None);
          }
        } else {
          throw new InvalidOperationException(String.Format("Blob count exceeds {0}. Blob count: {1}",
            Constants.MAX_PER_FOODSITE, this.blobs.Count));
        }
      }
      this.foodSite.state = FoodState.Eaten;
    }
  }

  public class FoodSiteMediatorStore {
    private Dictionary<Guid, FoodSiteMediator> mediatorMap;

    public void VisitFoodSite(FoodSite foodSite, Blob b) {
      if (!this.mediatorMap.ContainsKey(foodSite.id)) {
        this.mediatorMap.Add(foodSite.id, new FoodSiteMediator(foodSite));
      }
      this.mediatorMap[foodSite.id].Visit(b);
    }

    public Boolean FoodSiteAvailable(FoodSite foodSite) {
      if (!this.mediatorMap.ContainsKey(foodSite.id)) {
        return false;
      }
      return this.mediatorMap[foodSite.id].FoodSiteAvailable();
    }

    public void ProcessNext() {
      foreach (Guid foodSiteId in this.mediatorMap.Keys) {
        this.mediatorMap[foodSiteId].ProcessNext();
      }
    }
  }

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