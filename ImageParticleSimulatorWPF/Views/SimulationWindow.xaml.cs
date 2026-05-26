using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace ImageParticleSimulatorWPF.Views
{
    public partial class SimulationWindow : Window
    {
        public SimulationWindow(BitmapImage image, int ballCount)
        {
            InitializeComponent();
            ConfigureCanvasSize(image);

            Loaded += (s, e) =>
            {
                var viewModel = new SimulationViewModel(BallCanvas.ActualWidth, BallCanvas.ActualHeight, ballCount, image);
                viewModel.OnOverlayFadeRequest = OverlayFadeOut;
                viewModel.OnFrameUpdated = () => BallCanvas.InvalidateVisual();
                Overlay.Opacity = 0;
                Overlay.Visibility = Visibility.Visible;

                var fadeIn = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(2.0),
                };
                Overlay.BeginAnimation(UIElement.OpacityProperty, fadeIn);

                DataContext = viewModel;
            };
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

        private void OverlayFadeOut()
        {
            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(1.5),
                FillBehavior = FillBehavior.Stop
            };

            fadeOut.Completed += (s, e) =>
            {
                Overlay.Visibility = Visibility.Collapsed;
                Overlay.Opacity = 1;
            };

            Overlay.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }


    }
}
