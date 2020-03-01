using System;
using System.Collections.Generic; 

namespace Simulation {
  internal struct BlobProps {
    public bool isGreedy;
    public RadialPosition home; 
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
    private FoodSite selectedFood = null;

    public SearchingState(Blob b) : base(b) { }

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

    public override void ProcessNext(Board board) {
      SensorResult senses = board.AcceptSensor(this.blob, blob.GetBlobProps().sensor);
      Blob[] sensedBlobs = senses.blobs;
      FoodSite[] sensedFoodSites = senses.food;

      if (this.selectedFood == null) {
        return;
      }
      if (!board.FoodSiteAvailable(this.selectedFood)) {
        this.selectedFood = null;
      }

      TrySelectFood(board, sensedFoodSites);

      double stepSize = blob.GetBlobProps().step;
      if (this.selectedFood == null) {
        // Random walk
        // TODO: Explore more intelligent search strategies
        blob.GetPosition().RandomStep(stepSize);
      } else if (blob.GetPosition().Distance(this.selectedFood.GetPosition()) <= stepSize) {
        board.VisitFoodSite(this.selectedFood, blob);
        blob.SetBlobState(new AtFoodSiteState(this.blob, this.selectedFood));
        blob.SetPosition(this.selectedFood.GetPosition());
      } else {
        blob.GetPosition().StepTo(this.selectedFood.GetPosition(), stepSize);
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
    public HomewardState(Blob b, Satiety satiety) : base(b) {
      this.blob.SetSatiety(satiety);
    }

    public override void ProcessNext(Board board) {
      double stepSize = blob.GetBlobProps().step;
      if (blob.GetPosition().Distance(blob.GetBlobProps().home) > stepSize) {
        blob.GetPosition().StepTo(blob.GetBlobProps().home, blob.GetBlobProps().step);
      } else {
        blob.SetPosition(blob.GetBlobProps().home);
        blob.SetBlobState(new HomeState(this.blob));
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

    public Blob(BlobProps props) {
      id = Guid.NewGuid();
      state = new SearchingState(this);
      this.props = props;
      this.position = this.props.home;
      this.satiety = Satiety.None;
    }

    public Blob(Blob existingBlob) {
      id = Guid.NewGuid();
      state = new SearchingState(this);
      this.props = existingBlob.GetBlobProps();
      this.position = this.props.home;
      this.satiety = Satiety.None;
    }

    public RadialPosition GetPosition() {
      return this.position;
    }

    public void SetPosition(RadialPosition rp) {
      this.position = rp;
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
      this.satiety = satiety;
    }

    public Guid GetId() {
      return this.id;
    }

    public void ProcessNext(Board board) {
      this.state.ProcessNext(board);
    }

    public void SendHome(Satiety satiety = Satiety.None) {
      this.SetBlobState(new HomewardState(this, satiety));
    }

    public override int GetHashCode() {
      return this.id.GetHashCode();
    }

    public Boolean IsHome() {
      return this.state.GetType() == typeof(HomeState);
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