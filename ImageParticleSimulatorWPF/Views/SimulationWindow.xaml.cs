using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace ImageParticleSimulatorWPF.Views
{
    public partial class SimulationWindow : Window
    {
        public SimulationWindow(BitmapImage image)
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                var viewModel = new SimulationViewModel(BallCanvas.ActualWidth, BallCanvas.ActualHeight, 1790, image);
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
