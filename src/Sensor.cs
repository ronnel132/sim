namespace Simulation {
  // TODO: Maybe abstract the sensor result to allow for different types of sensing results
  internal struct SensorResult {
    public Blob[] blobs;
    public FoodSite[] food; 
  }

  internal interface IBlobSensor {
    SensorResult sense(Blob blob, Board board);
  }

  internal class ProximitySensor : IBlobSensor {
    private double sensingRadius;
    public ProximitySensor(double sensingRadius) {
      this.sensingRadius = sensingRadius; 
    }

    public SensorResult sense(Blob blob, Board board) {
      SensorResult sensorResult;
      sensorResult.blobs = board.FindBlobsNear(blob.GetPosition(), sensingRadius);
      sensorResult.food = board.FindFoodSiteNear(blob.GetPosition(), sensingRadius);
      return sensorResult;
    }
  }

}