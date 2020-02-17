using System;
using System.Collections.Generic; 

namespace Simulation {
  internal struct BlobProps {
    public bool isGreedy;
    public RadialPosition home; 
    public IBlobSensor sensor;
    public double step;
  }

  internal interface BlobState {
    void ProcessNext(Blob blob, Board board);
  }

  /*
  Searches for food in a random walk. Goes to the first food that's available, even if another blob is there.
   */
  internal class SearchingState : BlobState {
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
      } else if (blob.position.Distance(this.selectedFood.GetPosition()) <= stepSize) {
        board.VisitFoodSite(this.selectedFood, blob);
        blob.state = new AtFoodSiteState(this.selectedFood);
        blob.position = this.selectedFood.GetPosition(); 
      } else {
        blob.position.StepTo(this.selectedFood.GetPosition(), stepSize);
      }
    }
  }

  internal class AtFoodSiteState : BlobState {
    public FoodSite foodSite;

    public AtFoodSiteState(FoodSite foodSite) {
      this.foodSite = foodSite;
    }
    public void ProcessNext(Blob blob, Board board) {
      // no-op. The mediator will handle state transition
    }
  }

  internal enum Satiety {
    None,
    Half,
    Full,
  }

  internal class HomewardState : BlobState {
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

  internal class HomeState : BlobState {
    private Satiety satiety;

    public HomeState(Satiety satiety) {
      this.satiety = satiety;
    }
    public void ProcessNext(Blob blob, Board board) {
      // TODO: No-op? Where to implement death/reproduce step?
    }
  }

  internal class Blob {
    private Guid id;
    // TODO make all these private
    public BlobProps props;
    public RadialPosition position;
    public BlobState state;

    public Blob(BlobProps props) {
      id = Guid.NewGuid();
      state = new SearchingState();
      this.props = props;
      this.position = this.props.home;
    }

    public Guid GetId() {
      return this.id;
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

    public override Boolean Equals(object obj) {
      if ((obj == null) || ! this.GetType().Equals(obj.GetType())) {
         return false;
      }
      Blob blob = (Blob) obj;
      return this.id == blob.id;
    }
  }
}