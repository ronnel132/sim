namespace Simulation {
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

}