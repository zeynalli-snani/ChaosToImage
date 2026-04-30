using System.Windows;
using System.Windows.Media;

namespace ImageParticleSimulatorWPF.Models
{
    public class Ball
    {
        public Point Position { get; set; }

        public Vector Velocity { get; set; }

        public Vector FiredVelocity { get; set; }

        public double Radius { get; set; } = 5;

        public Color Color { get; set; } = Colors.White;

        public void Reset(Point position, Vector velocity, double radius, Color color)
        {
            Position = position;
            Velocity = velocity;
            FiredVelocity = velocity;
            Radius = radius;
            Color = color;
        }
    }
}
