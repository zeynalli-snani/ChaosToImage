using ImageParticleSimulatorWPF.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageParticleSimulatorWPF.ViewModels
{
    internal sealed class ParticleTarget
    {
        public required Color Color { get; init; }
        public required Point Position { get; init; }
    }

    internal enum SimulationPhase
    {
        Scatter,
        Assemble,
        Settled
    }

    public class SimulationViewModel : INotifyPropertyChanged
    {
        private const double ScatterDuration = 2.4;
        private const double SpawnDuration = 1.15;
        private const double BoundaryBounce = 0.92;
        private const double ScatterDrag = 1.55;
        private const double AssembleSpring = 14.0;
        private const double AssembleDrag = 8.0;
        private const int CollisionPasses = 2;

        private readonly List<Ball> _balls;
        private readonly List<ParticleTarget> _targets;
        private readonly Random _random = new();
        private readonly Point _center;
        private readonly double _width;
        private readonly double _height;
        private readonly double _ballRadius;

        private SimulationPhase _phase = SimulationPhase.Scatter;
        private double _phaseElapsed;
        private double _spawnAccumulator;
        private bool _overlayFadeRequested;
        private int _activeBallCount;
        private bool _isOverlayVisible = true;
        private double _currentFps = 60.0;

        public IReadOnlyList<Ball> Balls => _balls;

        public int ActiveBallCount
        {
            get => _activeBallCount;
            private set
            {
                if (_activeBallCount != value)
                {
                    _activeBallCount = value;
                    OnPropertyChanged(nameof(ActiveBallCount));
                }
            }
        }

        public bool IsOverlayVisible
        {
            get => _isOverlayVisible;
            private set
            {
                if (_isOverlayVisible != value)
                {
                    _isOverlayVisible = value;
                    OnPropertyChanged(nameof(IsOverlayVisible));
                }
            }
        }

        public double CurrentFps
        {
            get => _currentFps;
            private set
            {
                if (Math.Abs(_currentFps - value) > 0.1)
                {
                    _currentFps = value;
                    OnPropertyChanged(nameof(CurrentFps));
                }
            }
        }

        public Action? OnOverlayFadeRequest;
        public Action? OnSimulationCompleted;

        public SimulationViewModel(double width, double height, int ballCount, BitmapImage image)
        {
            _width = Math.Max(width, 1);
            _height = Math.Max(height, 1);
            _center = new Point(_width / 2, _height / 2);
            _ballRadius = CalculateBallRadius(ballCount);
            _targets = BuildTargets(image, ballCount);
            _balls = new List<Ball>(_targets.Count);

            for (int i = 0; i < _targets.Count; i++)
            {
                _balls.Add(new Ball());
            }
        }

        public void UpdateFrame(double deltaTime)
        {
            if (_phase == SimulationPhase.Settled)
            {
                return;
            }

            double dt = Math.Clamp(deltaTime, 1.0 / 240.0, 0.05);
            UpdateFps(dt);

            switch (_phase)
            {
                case SimulationPhase.Scatter:
                    UpdateScatter(dt);
                    break;
                case SimulationPhase.Assemble:
                    UpdateAssemble(dt);
                    break;
            }
        }

        private void UpdateScatter(double dt)
        {
            _phaseElapsed += dt;
            SpawnBalls(dt);
            RunScatterPhysics(dt);

            if (ActiveBallCount == _targets.Count && _phaseElapsed >= ScatterDuration)
            {
                BeginAssemblePhase();
            }
        }

        private void UpdateAssemble(double dt)
        {
            bool allSettled = true;

            for (int i = 0; i < ActiveBallCount; i++)
            {
                Ball ball = _balls[i];
                Point targetPosition = _targets[i].Position;
                Vector toTarget = targetPosition - ball.Position;

                ball.Velocity += toTarget * (AssembleSpring * dt);
                ball.Velocity *= Math.Exp(-AssembleDrag * dt);
                ball.Position += ball.Velocity * dt;

                if (toTarget.LengthSquared < 0.49 && ball.Velocity.LengthSquared < 9.0)
                {
                    ball.Position = targetPosition;
                    ball.Velocity = new Vector();
                }
                else
                {
                    allSettled = false;
                }
            }

            if (allSettled)
            {
                _phase = SimulationPhase.Settled;
                OnSimulationCompleted?.Invoke();
            }
        }

        private void SpawnBalls(double dt)
        {
            if (ActiveBallCount >= _targets.Count)
            {
                return;
            }

            double spawnRate = _targets.Count / SpawnDuration;
            _spawnAccumulator += dt * spawnRate;

            int spawnCount = Math.Max(1, (int)_spawnAccumulator);
            _spawnAccumulator -= Math.Floor(_spawnAccumulator);

            for (int i = 0; i < spawnCount && ActiveBallCount < _targets.Count; i++)
            {
                ActivateBall(ActiveBallCount);
            }
        }

        private void ActivateBall(int index)
        {
            double angle = _random.NextDouble() * Math.PI * 2.0;
            double speed = 170.0 + _random.NextDouble() * 310.0;
            Vector velocity = new(Math.Cos(angle) * speed, Math.Sin(angle) * speed);

            ParticleTarget target = _targets[index];
            Ball ball = _balls[index];
            ball.Reset(_center, velocity, _ballRadius, target.Color);
            ActiveBallCount = index + 1;
        }

        private void RunScatterPhysics(double dt)
        {
            int substeps = Math.Clamp((int)Math.Ceiling(dt / 0.008), 1, 4);
            double substepDt = dt / substeps;

            for (int step = 0; step < substeps; step++)
            {
                for (int i = 0; i < ActiveBallCount; i++)
                {
                    Ball ball = _balls[i];
                    ball.Position += ball.Velocity * substepDt;
                    ball.Velocity *= Math.Exp(-ScatterDrag * substepDt);
                    ResolveBoundaryCollision(ball);
                }

                ResolveBallCollisions();
            }
        }

        private void ResolveBoundaryCollision(Ball ball)
        {
            if (ball.Position.X - ball.Radius <= 0)
            {
                ball.Position = new Point(ball.Radius, ball.Position.Y);
                ball.Velocity = new Vector(-ball.Velocity.X * BoundaryBounce, ball.Velocity.Y);
            }
            else if (ball.Position.X + ball.Radius >= _width)
            {
                ball.Position = new Point(_width - ball.Radius, ball.Position.Y);
                ball.Velocity = new Vector(-ball.Velocity.X * BoundaryBounce, ball.Velocity.Y);
            }

            if (ball.Position.Y - ball.Radius <= 0)
            {
                ball.Position = new Point(ball.Position.X, ball.Radius);
                ball.Velocity = new Vector(ball.Velocity.X, -ball.Velocity.Y * BoundaryBounce);
            }
            else if (ball.Position.Y + ball.Radius >= _height)
            {
                ball.Position = new Point(ball.Position.X, _height - ball.Radius);
                ball.Velocity = new Vector(ball.Velocity.X, -ball.Velocity.Y * BoundaryBounce);
            }
        }

        private void ResolveBallCollisions()
        {
            for (int pass = 0; pass < CollisionPasses; pass++)
            {
                for (int i = 0; i < ActiveBallCount; i++)
                {
                    for (int j = i + 1; j < ActiveBallCount; j++)
                    {
                        Ball a = _balls[i];
                        Ball b = _balls[j];

                        Vector delta = b.Position - a.Position;
                        double distance = delta.Length;
                        double minDistance = a.Radius + b.Radius;

                        if (distance >= minDistance)
                        {
                            continue;
                        }

                        if (distance < 0.0001)
                        {
                            delta = new Vector(_random.NextDouble() - 0.5, _random.NextDouble() - 0.5);
                            distance = Math.Max(delta.Length, 0.0001);
                        }

                        Vector normal = delta / distance;
                        double overlap = minDistance - distance;

                        a.Position -= normal * (overlap * 0.5);
                        b.Position += normal * (overlap * 0.5);

                        Vector relativeVelocity = b.Velocity - a.Velocity;
                        double velocityAlongNormal = Vector.Multiply(relativeVelocity, normal);
                        if (velocityAlongNormal >= 0)
                        {
                            continue;
                        }

                        Vector impulse = normal * velocityAlongNormal;
                        a.Velocity += impulse;
                        b.Velocity -= impulse;
                    }
                }
            }
        }

        private void BeginAssemblePhase()
        {
            _phase = SimulationPhase.Assemble;
            _phaseElapsed = 0;

            if (_overlayFadeRequested)
            {
                return;
            }

            _overlayFadeRequested = true;
            IsOverlayVisible = false;
            OnOverlayFadeRequest?.Invoke();
        }

        private List<ParticleTarget> BuildTargets(BitmapSource image, int ballCount)
        {
            FormatConvertedBitmap formattedBitmap = new(image, PixelFormats.Bgra32, null, 0);
            int width = formattedBitmap.PixelWidth;
            int height = formattedBitmap.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            formattedBitmap.CopyPixels(pixels, stride, 0);

            int columnCount = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(ballCount * (_width / _height))));
            int rowCount = Math.Max(1, (int)Math.Ceiling((double)ballCount / columnCount));
            double cellWidth = _width / columnCount;
            double cellHeight = _height / rowCount;

            List<ParticleTarget> targets = new(ballCount);

            for (int index = 0; index < ballCount; index++)
            {
                int row = index / columnCount;
                int column = index % columnCount;

                if (row >= rowCount)
                {
                    break;
                }

                double normalizedX = (column + 0.5) / columnCount;
                double normalizedY = (row + 0.5) / rowCount;
                int imageX = Math.Clamp((int)Math.Round(normalizedX * (width - 1)), 0, width - 1);
                int imageY = Math.Clamp((int)Math.Round(normalizedY * (height - 1)), 0, height - 1);
                int pixelIndex = imageY * stride + imageX * 4;

                Color color = Colors.White;
                if (pixelIndex + 3 < pixels.Length)
                {
                    color = Color.FromArgb(
                        pixels[pixelIndex + 3],
                        pixels[pixelIndex + 2],
                        pixels[pixelIndex + 1],
                        pixels[pixelIndex]);
                }

                targets.Add(new ParticleTarget
                {
                    Color = color,
                    Position = new Point((column + 0.5) * cellWidth, (row + 0.5) * cellHeight)
                });
            }

            return targets;
        }

        private double CalculateBallRadius(int ballCount)
        {
            double areaPerParticle = (_width * _height) / Math.Max(ballCount, 1);
            double radius = Math.Sqrt(areaPerParticle) * 0.28;
            return Math.Clamp(radius, 1.8, 5.5);
        }

        private void UpdateFps(double dt)
        {
            double instantFps = 1.0 / dt;
            CurrentFps = (_currentFps * 0.88) + (instantFps * 0.12);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
