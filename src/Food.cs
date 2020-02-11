using System;
using System.Collections.Generic; 

namespace Simulation {
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
}