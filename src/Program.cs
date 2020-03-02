namespace Simulation {
  public class Program
  {
    static void Main(string[] args)
    {
        SimulationProps props = new SimulationProps() {
          numBlobs = 100,
          numFood = 20,
          percentageGreedy = 0.1,
          blobSensorSize = 0.05,
          blobStepSize = 0.05,              
        };
        Board b = new Board(props);
        b.Run(100);
    }
  }
}
