using System;
using System.Threading; 

namespace Simulation {
  internal class RadialPosition {
    public double theta;
    public double radius;
    private static Random rng = new Random();
    private static Mutex mutex = new Mutex();

    public RadialPosition() {
      this.theta = 0;
      this.radius = 0;
    }

    public RadialPosition(RadialPosition rp) {
      this.theta = rp.theta;
      this.radius = rp.radius;
    }

    public RadialPosition(double radius, double theta) {
      this.radius = radius;
      this.theta = theta % (2 * Math.PI);
    }

    public double Distance(RadialPosition rp) {
      double x_1 = rp.radius * Math.Cos(rp.theta); 
      double y_1 = rp.radius * Math.Sin(rp.theta);

      double x_2 = this.radius * Math.Cos(this.theta);
      double y_2 = this.radius * Math.Sin(this.theta);

      return Math.Sqrt(Math.Pow(x_2 - x_1, 2) + Math.Pow(y_2 - y_1, 2));
    } 

    private void SetNewCoordinates(double x_new, double y_new) {
      double radius = Math.Sqrt(Math.Pow(x_new, 2) + Math.Pow(y_new, 2));
      double theta = Math.Atan2(y_new, x_new);
      if (radius > 1) {
        // Hop to the other side
        this.radius = 1 - (radius % 1);
        this.theta = -theta;
      } else {
        this.radius = radius;
        this.theta = theta;
      }
    }

    public void StepTo(RadialPosition rp, double stepSize) {
      double x_1 = this.radius * Math.Cos(this.theta);
      double y_1 = this.radius * Math.Sin(this.theta);

      double x_2 = rp.radius * Math.Cos(rp.theta); 
      double y_2 = rp.radius * Math.Sin(rp.theta);

      // Calculate the normalized subtraction vector
      double delta_x = x_2 - x_1;
      double delta_y = y_2 - y_1;
      double magnitude = Math.Sqrt(Math.Pow(delta_x, 2) + Math.Pow(delta_y, 2));
      double scale = stepSize / magnitude;

      this.SetNewCoordinates(x_1 + delta_x * scale, y_1 + delta_y * scale);
    }

    public void RandomStep(double stepSize) {
      mutex.WaitOne();
      double theta = rng.NextDouble() * 2 * Math.PI;
      mutex.ReleaseMutex();

      double delta_x = stepSize * Math.Cos(theta);
      double delta_y = stepSize * Math.Sin(theta);

      double x = this.radius * Math.Cos(this.theta);
      double y = this.radius * Math.Sin(this.theta);

      double x_new = x + delta_x;
      double y_new = y + delta_y;

      this.SetNewCoordinates(x_new, y_new);
    }
  }

}