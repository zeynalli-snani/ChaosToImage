using ImageParticleSimulatorWPF.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

public class BallData
{
    public Vector InitialVelocity { get; set; }
    public Color Color { get; set; }
    public Point FinalPosition { get; set; }
}

public class SimulationViewModel : INotifyPropertyChanged
{
    private const double BallRadius = 4.0;
    private const int CollisionPasses = 3;

    private readonly List<Ball> _balls;
    private readonly DispatcherTimer _timer;
    private readonly DispatcherTimer _stopTimer;
    private readonly Random _rand = new();
    private readonly Point _center;
    private readonly double _width;
    private readonly double _height;
    private readonly BitmapImage _image;
    private readonly List<BallData> _recordedData = new();

    private bool _stopTimerStarted;
    private int _totalBallsToFire;
    private int _ballsFired;
    private int _spawnTickCounter = 1;
    private bool _isRecordingPhase = true;
    private int _activeBallCount;
    private bool _isOverlayVisible = true;

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
        set
        {
            if (_isOverlayVisible != value)
            {
                _isOverlayVisible = value;
                OnPropertyChanged(nameof(IsOverlayVisible));
            }
        }
    }

    public Action? OnOverlayFadeRequest;
    public Action? OnFrameUpdated;

    public SimulationViewModel(double width, double height, int ballCount, BitmapImage image)
    {
        _width = width;
        _height = height;
        _image = image;
        _center = new Point(width / 2, height / 2);
        _totalBallsToFire = ballCount;
        _balls = new List<Ball>(ballCount);

        for (int i = 0; i < ballCount; i++)
        {
            _balls.Add(new Ball());
        }

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += Tick;
        _timer.Start();

        _stopTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _stopTimer.Tick += StopTimerTick;
    }

    private void Tick(object? sender, EventArgs e)
    {
        if (_isRecordingPhase)
        {
            for (int i = 0; i < _spawnTickCounter && _ballsFired < _totalBallsToFire; i++)
            {
                FireNewBall();
                _ballsFired++;
            }

            _spawnTickCounter++;
            UpdateBalls();

            if (_ballsFired >= _totalBallsToFire && !_stopTimerStarted)
            {
                _stopTimerStarted = true;
                _stopTimer.Start();
            }
        }
        else
        {
            ReplayBalls();
            UpdateBalls();

            if (_ballsFired >= _recordedData.Count && !_stopTimerStarted)
            {
                _stopTimerStarted = true;
                _stopTimer.Start();
            }
        }

        OnFrameUpdated?.Invoke();
    }

    private void StopTimerTick(object? sender, EventArgs e)
    {
        _stopTimer.Stop();

        if (_isRecordingPhase)
        {
            AssignColorsFromImage();
            PrepareReplay();
        }
        else
        {
            _timer.Stop();
        }
    }

    private void FireNewBall()
    {
        double angle = _rand.NextDouble() * 2 * Math.PI;
        double progress = (double)_ballsFired / _totalBallsToFire;
        double speed = 18.0 * (1.0 - progress) + 2.0 * progress;
        Vector velocity = new Vector(Math.Cos(angle), Math.Sin(angle)) * speed;

        Ball ball = _balls[_ballsFired];
        ball.Reset(_center, velocity, BallRadius, Colors.White);
        ActiveBallCount = Math.Max(ActiveBallCount, _ballsFired + 1);
    }

    private void AssignColorsFromImage()
    {
        WriteableBitmap writableBitmap = new(_image);
        int stride = writableBitmap.PixelWidth * (writableBitmap.Format.BitsPerPixel / 8);
        byte[] pixels = new byte[writableBitmap.PixelHeight * stride];
        writableBitmap.CopyPixels(pixels, stride, 0);

        _recordedData.Clear();

        for (int i = 0; i < ActiveBallCount; i++)
        {
            Ball ball = _balls[i];
            double normalizedX = Math.Clamp(ball.Position.X / _width, 0, 1);
            double normalizedY = Math.Clamp(ball.Position.Y / _height, 0, 1);

            int imageX = (int)(normalizedX * (writableBitmap.PixelWidth - 1));
            int imageY = (int)(normalizedY * (writableBitmap.PixelHeight - 1));
            int pixelIndex = imageY * stride + imageX * 4;

            Color color = Colors.White;
            if (pixelIndex + 3 < pixels.Length)
            {
                byte b = pixels[pixelIndex];
                byte g = pixels[pixelIndex + 1];
                byte r = pixels[pixelIndex + 2];
                byte a = pixels[pixelIndex + 3];

                color = Color.FromArgb(a, r, g, b);
            }

            _recordedData.Add(new BallData
            {
                InitialVelocity = ball.FiredVelocity,
                FinalPosition = ball.Position,
                Color = color
            });
        }
    }

    private void PrepareReplay()
    {
        IsOverlayVisible = false;
        OnOverlayFadeRequest?.Invoke();
        _isRecordingPhase = false;
        _ballsFired = 0;
        _stopTimerStarted = false;
        _spawnTickCounter = 1;
        ActiveBallCount = 0;
    }

    private void ReplayBalls()
    {
        for (int i = 0; i < _spawnTickCounter && _ballsFired < _recordedData.Count; i++)
        {
            BallData data = _recordedData[_ballsFired];
            Ball ball = _balls[_ballsFired];

            ball.Reset(_center, data.InitialVelocity, BallRadius, data.Color);
            _ballsFired++;
        }

        ActiveBallCount = Math.Max(ActiveBallCount, _ballsFired);
        _spawnTickCounter++;
    }

    private void UpdateBalls()
    {
        for (int i = 0; i < ActiveBallCount; i++)
        {
            Ball ball = _balls[i];

            ball.Position += ball.Velocity;
            ball.Velocity *= 0.95;

            if (ball.Position.X - ball.Radius <= 0)
            {
                ball.Position = new Point(ball.Radius, ball.Position.Y);
                ball.Velocity = new Vector(-ball.Velocity.X, ball.Velocity.Y);
            }
            else if (ball.Position.X + ball.Radius >= _width)
            {
                ball.Position = new Point(_width - ball.Radius, ball.Position.Y);
                ball.Velocity = new Vector(-ball.Velocity.X, ball.Velocity.Y);
            }

            if (ball.Position.Y - ball.Radius <= 0)
            {
                ball.Position = new Point(ball.Position.X, ball.Radius);
                ball.Velocity = new Vector(ball.Velocity.X, -ball.Velocity.Y);
            }
            else if (ball.Position.Y + ball.Radius >= _height)
            {
                ball.Position = new Point(ball.Position.X, _height - ball.Radius);
                ball.Velocity = new Vector(ball.Velocity.X, -ball.Velocity.Y);
            }
        }

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

                    if (distance < minDistance && distance > 0.0001)
                    {
                        Vector normal = delta / distance;
                        double overlap = minDistance - distance;

                        a.Position -= normal * (overlap / 2);
                        b.Position += normal * (overlap / 2);

                        Vector relativeVelocity = b.Velocity - a.Velocity;
                        double velAlongNormal = Vector.Multiply(relativeVelocity, normal);

                        if (velAlongNormal > 0)
                        {
                            continue;
                        }

                        double impulse = -2.0 * velAlongNormal / 2;
                        Vector impulseVector = impulse * normal;

                        a.Velocity -= impulseVector;
                        b.Velocity += impulseVector;
                    }
                }
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
