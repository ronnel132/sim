using System;

namespace Simulation {
  public class RadialPosition {
    public double theta;
    public double radius;

    public double Distance(RadialPosition rp) {
      double x_1 = rp.radius * Math.Cos(rp.theta); 
      double y_1 = rp.radius * Math.Sin(rp.theta);

      double x_2 = this.radius * Math.Cos(this.theta);
      double y_2 = this.radius * Math.Sin(this.theta);

      return Math.Sqrt(Math.Pow(x_2 - x_1, 2) + Math.Pow(y_2 - y_1, 2));
    } 

    private void SetNewCoordinates(double x_new, double y_new) {
      double radius = Math.Sqrt(Math.Pow(x_new, 2) + Math.Pow(y_new, 2));
      double theta = Math.Atan(y_new / x_new);
      if (this.radius > 1) {
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

      double theta = Math.Atan((x_2 - x_1) / (y_2 - y_1));
      double x_new = x_1 + Math.Cos(theta) * stepSize;
      double y_new = y_1 + Math.Sin(theta) * stepSize;

      this.SetNewCoordinates(x_new, y_new);
    }

    public void RandomStep(double stepSize) {
      Random random = new Random();
      double theta = random.NextDouble() * 2 * Math.PI;

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