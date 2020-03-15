using System;
using System.Collections.Generic; 
using System.Collections.Concurrent;
using System.Threading; 

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
    private ConcurrentDictionary<FoodSite, FoodSiteMediator> mediatorMap;

    public FoodSiteMediatorStore() {
      this.mediatorMap = new ConcurrentDictionary<FoodSite, FoodSiteMediator>();
    }

    private class FoodSiteMediator {
      private int countTime = 0;
      private FoodSite foodSite;
      private List<Blob> blobs;
      private Mutex mutex = new Mutex();
      
      public FoodSiteMediator(FoodSite fs) {
        this.foodSite = fs;
        this.blobs = new List<Blob>();
      }

      public Boolean TryVisit(Blob b) {
        this.mutex.WaitOne();
        if (this.blobs.Contains(b)) {
          throw new InvalidOperationException(String.Format("Attempting to add blob id {0} to mediator", b.GetId()));
        }

        if (this.blobs.Count + 1 > Constants.MAX_PER_FOODSITE) {
          this.mutex.ReleaseMutex();
          return false;
        }

        this.blobs.Add(b);
        this.mutex.ReleaseMutex();
        return true;
      }

      public Boolean FoodSiteAvailable() {
        Boolean available = this.blobs.Count <= 1 && !this.foodSite.IsEaten();
        return available;
      }

      public void ProcessNext() {
        this.countTime++;

        if (this.countTime >= Constants.FOOD_LOCKUP_PERIOD) {
          if (this.blobs.Count == 1) {
            this.blobs[0].SetSatiety(Satiety.Full);
            this.blobs[0].SendHome();
          } else if (this.blobs.Count == 2) {
            Boolean blob1Greedy = this.blobs[0].GetBlobProps().isGreedy;
            Boolean blob2Greedy = this.blobs[1].GetBlobProps().isGreedy;
            // TODO: Abstract this algorithm into a strategy to keep implementation away from Mediator
            if (!blob1Greedy && !blob2Greedy) {
              // They share
              this.blobs[0].SetSatiety(Satiety.Half);
              this.blobs[0].SendHome();
              this.blobs[1].SetSatiety(Satiety.Half);
              this.blobs[1].SendHome();
            } else if (blob1Greedy && !blob2Greedy) {
              this.blobs[0].SetSatiety(Satiety.Full);
              this.blobs[0].SendHome();
              this.blobs[1].SetSatiety(Satiety.None);
              this.blobs[1].SendHome();
            } else if (!blob1Greedy && blob2Greedy) {
              this.blobs[0].SetSatiety(Satiety.None);
              this.blobs[0].SendHome();
              this.blobs[1].SetSatiety(Satiety.Full);
              this.blobs[1].SendHome();
            } else {
              // blob1Greedy && blob2Greedy
              this.blobs[0].SetSatiety(Satiety.None);
              this.blobs[0].SendHome();
              this.blobs[1].SetSatiety(Satiety.None);
              this.blobs[1].SendHome();
            }
          } else {
            throw new InvalidOperationException(String.Format("Blob count exceeds {0}. Blob count: {1}",
              Constants.MAX_PER_FOODSITE, this.blobs.Count));
          }
          this.foodSite.SetState(FoodState.Eaten);
        }
      }
    }

    public Boolean TryVisitFoodSite(FoodSite foodSite, Blob b) {
      if (!this.mediatorMap.ContainsKey(foodSite)) {
        this.mediatorMap.TryAdd(foodSite, new FoodSiteMediator(foodSite));
      }
      return this.mediatorMap[foodSite].TryVisit(b);
    }

    public Boolean FoodSiteAvailable(FoodSite foodSite) {
      if (!this.mediatorMap.ContainsKey(foodSite)) {
        return true;
      }
      return this.mediatorMap[foodSite].FoodSiteAvailable();
    }

    public void ProcessNext() {
      foreach (FoodSite foodSite in this.mediatorMap.Keys) {
        if (foodSite.IsEaten()) {
          this.mediatorMap.TryRemove(foodSite, out _);
        } else {
          this.mediatorMap[foodSite].ProcessNext();
        }
      }
    }
  }
}