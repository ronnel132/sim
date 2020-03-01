using System;
using System.Collections.Generic; 

namespace Simulation {
  internal enum FoodState {
    Available,
    Eaten,
  }

  internal class FoodSite {
    private Guid id;
    private RadialPosition position;
    private FoodState state;
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

    public override Boolean Equals(object obj) {
      if ((obj == null) || ! this.GetType().Equals(obj.GetType())) {
         return false;
      }
      FoodSite food = (FoodSite) obj;
      return this.id == food.id;
    }

    public RadialPosition GetPosition() {
      return this.position;
    }

    public void SetPosition(RadialPosition position) {
      this.position = position;
    }

    public FoodState GetState() {
      return this.state;
    }

    public void SetState(FoodState state) {
      this.state = state;
    }
  }

  internal class FoodSiteMediatorStore {
    private Dictionary<FoodSite, FoodSiteMediator> mediatorMap;

    public FoodSiteMediatorStore() {
      this.mediatorMap = new Dictionary<FoodSite, FoodSiteMediator>();
    }

    private class FoodSiteMediator {
      private int countTime = 0;
      private FoodSite foodSite;
      private List<Blob> blobs;
      
      public FoodSiteMediator(FoodSite fs) {
        this.foodSite = fs;
        this.blobs = new List<Blob>();
      }

      public void Visit(Blob b) {
        if (this.blobs.Contains(b)) {
          throw new InvalidOperationException(String.Format("Attempting to add blob id {0} to mediator", b.GetId()));
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
            Boolean blob1Greedy = this.blobs[0].GetBlobProps().isGreedy;
            Boolean blob2Greedy = this.blobs[2].GetBlobProps().isGreedy;
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
        this.foodSite.SetState(FoodState.Eaten);
      }
    }

    public void VisitFoodSite(FoodSite foodSite, Blob b) {
      if (!this.mediatorMap.ContainsKey(foodSite)) {
        this.mediatorMap.Add(foodSite, new FoodSiteMediator(foodSite));
      }
      this.mediatorMap[foodSite].Visit(b);
    }

    public Boolean FoodSiteAvailable(FoodSite foodSite) {
      if (!this.mediatorMap.ContainsKey(foodSite)) {
        return false;
      }
      return this.mediatorMap[foodSite].FoodSiteAvailable();
    }

    public void ProcessNext() {
      foreach (FoodSite foodSite in this.mediatorMap.Keys) {
        this.mediatorMap[foodSite].ProcessNext();
      }
    }
  }
}