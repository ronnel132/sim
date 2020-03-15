using System;
using System.Collections.Generic; 

namespace Simulation {
  internal struct BlobProps {
    public bool isGreedy;
    public IBlobSensor sensor;
    public double step;
  }

  internal abstract class BlobState {
    protected Blob blob;
    public BlobState(Blob b) {
      this.blob = b;
    }
    public abstract void ProcessNext(Board board);
  }

  /*
  Searches for food in a random walk. Goes to the first food that's available, even if another blob is there.
   */
  internal class SearchingState : BlobState {
    private static Random rng = new Random();

    public SearchingState(Blob b) : base(b) { }

    private List<FoodSite> GetAvailableFood(Board board, List<FoodSite> sensedFoodSites) {
      // Make a random selection among available foods
      List<FoodSite> available = new List<FoodSite>();
      foreach (FoodSite f in sensedFoodSites) {
        if (board.FoodSiteAvailable(f)) {
          available.Add(f); 
        }
      }
      return available;
    }

    private List<FoodSite> GetReachableFood(List<FoodSite> available, double stepSize) {
      List<FoodSite> reachable = new List<FoodSite>();
      foreach (FoodSite fs in available) {
        if (blob.GetPosition().Distance(fs.GetPosition()) < stepSize) {
          reachable.Add(fs);
        }
      }
      return reachable;
    }

    public override void ProcessNext(Board board) {
      SensorResult senses = board.AcceptSensor(this.blob, blob.GetBlobProps().sensor);
      List<Blob> sensedBlobs = senses.blobs;
      List<FoodSite> sensedFoodSites = senses.food;

      double stepSize = blob.GetBlobProps().step;

      List<FoodSite> available = GetAvailableFood(board, sensedFoodSites);
      List<FoodSite> reachable = GetReachableFood(available, stepSize);

      if (available.Count == 0) {
        // Random walk
        // TODO: Explore more intelligent search strategies
        blob.GetPosition().RandomStep(stepSize);
      } else if (reachable.Count > 0) {
        // Try until success
        foreach (FoodSite fs in reachable) {
          if (board.TryVisitFoodSite(fs, this.blob)) {
            blob.SetBlobState(new AtFoodSiteState(this.blob, fs));
            blob.SetPosition(fs.GetPosition());
            break;
          }
        }
      } else {
        // Just go to the first one
        // TODO: Maybe make this choice random
        blob.GetPosition().StepTo(available[0].GetPosition(), stepSize);
      }
    }
  }

  internal class AtFoodSiteState : BlobState {
    public FoodSite foodSite;

    public AtFoodSiteState(Blob b, FoodSite foodSite) : base(b) {
      this.foodSite = foodSite;
    }
    public override void ProcessNext(Board board) {
      // no-op. The mediator will handle state transition
    }
  }

  internal enum Satiety {
    None,
    Half,
    Full,
  }

  internal class HomewardState : BlobState {
    public HomewardState(Blob b) : base(b) { }

    public override void ProcessNext(Board board) {
      double stepSize = this.blob.GetBlobProps().step;
      if (this.blob.GetPosition().Distance(this.blob.GetHome()) > stepSize) {
        this.blob.GetPosition().StepTo(this.blob.GetHome(), this.blob.GetBlobProps().step);
      } else {
        this.blob.SetPosition(this.blob.GetHome());
        this.blob.SetBlobState(new HomeState(this.blob));
      }
    }
  }

  internal class HomeState : BlobState {
    public HomeState(Blob b) : base(b) { }

    public override void ProcessNext(Board board) { }
  }

  internal class Blob {
    private Guid id;
    // TODO make all these private
    private BlobProps props;
    private RadialPosition position;
    private BlobState state;
    private Satiety satiety;
    private RadialPosition home;

    public Blob(BlobProps props) {
      id = Guid.NewGuid();
      state = new SearchingState(this);
      this.props = props;
      this.position = new RadialPosition();
      this.home = new RadialPosition();
      this.satiety = Satiety.None;
    }

    public Blob(Blob existingBlob) {
      id = Guid.NewGuid();
      state = new SearchingState(this);
      this.props = existingBlob.GetBlobProps();
      this.position = new RadialPosition(existingBlob.GetPosition());
      this.home = new RadialPosition(existingBlob.GetHome());
      this.satiety = Satiety.None;
    }

    public RadialPosition GetPosition() {
      return this.position;
    }

    public void SetPosition(RadialPosition rp) {
      this.position = new RadialPosition(rp);
    }

    public BlobProps GetBlobProps() {
      return this.props;
    }

    public void SetBlobState(BlobState state) {
      this.state = state;
    }

    public Satiety GetSatiety() {
      return this.satiety;
    }

    public void SetSatiety(Satiety satiety) {
      // TODO: Fix this
      Dictionary<Satiety, int> ordering = new Dictionary<Satiety, int>();
      ordering.Add(Satiety.None, 0);
      ordering.Add(Satiety.Half, 1);
      ordering.Add(Satiety.Full, 2);
      if (ordering[satiety] < ordering[this.satiety]) {
        return;
      }
      this.satiety = satiety;
    }

    public void SetHome(RadialPosition rp) {
      this.home = new RadialPosition(rp);
    }

    public RadialPosition GetHome() {
      return this.home;
    }

    public Guid GetId() {
      return this.id;
    }

    public void ProcessNext(Board board) {
      if (this.state == null) {
        throw new InvalidOperationException("State is not initialized");
      }
      this.state.ProcessNext(board);
    }

    public void SendHome() {
      if (this.IsHome()) {
        throw new InvalidOperationException("Sending blob home that is already home");
      }
      this.SetBlobState(new HomewardState(this));
    }

    public override int GetHashCode() {
      return this.id.GetHashCode();
    }

    public Boolean IsHome() {
      return this.state.GetType() == typeof(HomeState);
    }

    public Boolean IsHomeward() {
      return this.state.GetType() == typeof(HomewardState);
    }

    public override Boolean Equals(object obj) {
      if ((obj == null) || ! this.GetType().Equals(obj.GetType())) {
         return false;
      }
      Blob blob = (Blob) obj;
      return this.id == blob.id;
    }
  }
}