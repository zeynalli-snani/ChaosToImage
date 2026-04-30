using ImageParticleSimulatorWPF.Models;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace ImageParticleSimulatorWPF.Views
{
    public class ParticleCanvas : FrameworkElement
    {
        private readonly Dictionary<Color, SolidColorBrush> _brushCache = new();

        public IReadOnlyList<Ball>? Balls
        {
            get => (IReadOnlyList<Ball>?)GetValue(BallsProperty);
            set => SetValue(BallsProperty, value);
        }

        public static readonly DependencyProperty BallsProperty =
            DependencyProperty.Register(
                nameof(Balls),
                typeof(IReadOnlyList<Ball>),
                typeof(ParticleCanvas),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public int ActiveBallCount
        {
            get => (int)GetValue(ActiveBallCountProperty);
            set => SetValue(ActiveBallCountProperty, value);
        }

        public static readonly DependencyProperty ActiveBallCountProperty =
            DependencyProperty.Register(
                nameof(ActiveBallCount),
                typeof(int),
                typeof(ParticleCanvas),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            drawingContext.DrawRectangle(Brushes.Black, null, new Rect(0, 0, ActualWidth, ActualHeight));

            if (Balls is null || ActiveBallCount <= 0)
            {
                return;
            }

            int count = ActiveBallCount < Balls.Count ? ActiveBallCount : Balls.Count;

            for (int i = 0; i < count; i++)
            {
                Ball ball = Balls[i];
                Brush brush = GetBrush(ball.Color);

                drawingContext.DrawEllipse(brush, null, ball.Position, ball.Radius, ball.Radius);
            }
        }

        private Brush GetBrush(Color color)
        {
            if (_brushCache.TryGetValue(color, out SolidColorBrush? brush))
            {
                return brush;
            }

            SolidColorBrush newBrush = new(color);
            newBrush.Freeze();
            _brushCache[color] = newBrush;
            return newBrush;
        }
    }
}
