using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using ImageParticleSimulatorWPF.ViewModels;

namespace ImageParticleSimulatorWPF.Views
{
    public partial class SimulationWindow : Window
    {
        private readonly BitmapImage _image;
        private readonly int _ballCount;
        private readonly Stopwatch _frameClock = new();
        private SimulationViewModel? _viewModel;
        private bool _isRendering;

        public SimulationWindow(BitmapImage image, int ballCount)
        {
            InitializeComponent();
            _image = image;
            _ballCount = ballCount;
            ConfigureCanvasSize(image);

            Loaded += OnLoaded;
            Closed += OnClosed;
        }

        private void ConfigureCanvasSize(BitmapSource image)
        {
            Rect workArea = SystemParameters.WorkArea;
            double maxWidth = workArea.Width * 0.82;
            double maxHeight = workArea.Height * 0.78;

            double scale = Math.Min(maxWidth / image.PixelWidth, maxHeight / image.PixelHeight);
            scale = Math.Min(scale, 1.0);
            scale = Math.Max(scale, 0.35);

            double canvasWidth = Math.Max(320, image.PixelWidth * scale);
            double canvasHeight = Math.Max(220, image.PixelHeight * scale);

            BallCanvas.Width = canvasWidth;
            BallCanvas.Height = canvasHeight;
            Width = canvasWidth + 44;
            Height = canvasHeight + 72;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _viewModel = new SimulationViewModel(BallCanvas.ActualWidth, BallCanvas.ActualHeight, _ballCount, _image);
            _viewModel.OnOverlayFadeRequest = OverlayFadeOut;
            _viewModel.OnSimulationCompleted = StopRenderingLoop;

            Overlay.Opacity = 0;
            Overlay.Visibility = Visibility.Visible;

            DoubleAnimation fadeIn = new()
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(1.2),
            };
            Overlay.BeginAnimation(UIElement.OpacityProperty, fadeIn);

            DataContext = _viewModel;
            StartRenderingLoop();
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            StopRenderingLoop();
            Loaded -= OnLoaded;
            Closed -= OnClosed;
        }

        private void StartRenderingLoop()
        {
            if (_isRendering)
            {
                return;
            }

            _isRendering = true;
            _frameClock.Restart();
            CompositionTarget.Rendering += OnRendering;
        }

        private void StopRenderingLoop()
        {
            if (!_isRendering)
            {
                return;
            }

            _isRendering = false;
            CompositionTarget.Rendering -= OnRendering;
            _frameClock.Stop();
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            if (_viewModel is null)
            {
                return;
            }

            double deltaTime = _frameClock.Elapsed.TotalSeconds;
            _frameClock.Restart();

            if (deltaTime <= 0)
            {
                deltaTime = 1.0 / 120.0;
            }

            _viewModel.UpdateFrame(deltaTime);
            BallCanvas.InvalidateVisual();
        }

        private void OverlayFadeOut()
        {
            DoubleAnimation fadeOut = new()
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(1.2),
                FillBehavior = FillBehavior.Stop
            };

            fadeOut.Completed += (_, _) =>
            {
                Overlay.Visibility = Visibility.Collapsed;
                Overlay.Opacity = 1;
            };

            Overlay.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }
    }
}
