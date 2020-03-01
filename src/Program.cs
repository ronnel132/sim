namespace Simulation {
  public class Program
  {
    static void Main(string[] args)
    {
        // Console.WriteLine("Blob Simulation Program");
        // Console.WriteLine();
        // Console.WriteLine("Enter number of blobs:");
        // string blobCountRaw = Console.ReadLine();
        // int blobCount = int.Parse()
        SimulationProps props = new SimulationProps() {
          numBlobs = 20,
          numFood = 10,
          percentageGreedy = 0.5,
          blobSensorSize = 0.1,
          blobStepSize = 0.1,              
        };
        Board b = new Board(props);
        b.Run(10);
    }
  }
}
