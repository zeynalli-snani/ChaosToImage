using ImageParticleSimulatorWPF.Views;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageParticleSimulatorWPF.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private const int DefaultBallCount = 2000;
        private const int MinBallCount = 300;
        private const int MaxBallCount = 5000;

        private BitmapImage? _uploadedImage;
        private int _ballCount = DefaultBallCount;

        public BitmapImage? UploadedImage
        {
            get => _uploadedImage;
            set
            {
                _uploadedImage = value;
                OnPropertyChanged(nameof(UploadedImage));
                (StartSimulationCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public int BallCount
        {
            get => _ballCount;
            set
            {
                int clampedValue = Math.Clamp(value, MinBallCount, MaxBallCount);
                if (_ballCount != clampedValue)
                {
                    _ballCount = clampedValue;
                    OnPropertyChanged(nameof(BallCount));
                }
            }
        }

        private Color[,]? _pixelColors;
        public Color[,]? PixelColors
        {
            get => _pixelColors;
            set
            {
                _pixelColors = value;
                OnPropertyChanged(nameof(PixelColors));
            }
        }


        public ICommand UploadImageCommand { get; }
        public ICommand StartSimulationCommand { get; }

        public MainViewModel()
        {
            UploadImageCommand = new RelayCommand(UploadImage);
            StartSimulationCommand = new RelayCommand(StartSimulation, CanStartSimulation);
        }

        public static BitmapSource ResizeImage(BitmapSource source, int width, int height)
        {
            var scale = new TransformedBitmap(source, new ScaleTransform(
                scaleX: (double)width / source.PixelWidth,
                scaleY: (double)height / source.PixelHeight));

            return scale;
        }

        private void UploadImage()
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg"
            };

            if (dialog.ShowDialog() == true)
            {
                var image = new BitmapImage();
                using (var stream = new FileStream(dialog.FileName, FileMode.Open, FileAccess.Read))
                {
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = stream;
                    image.EndInit();
                    image.Freeze(); // Freeze for cross-thread use
                }

                UploadedImage = image;

                var smallImage = ResizeImage(image, 15, 20);

                PixelColors = ImageHelper.GetPixelColors(image);
            }
        }

        private bool CanStartSimulation()
        {
            return UploadedImage != null;
        }

        private void StartSimulation()
        {
            if (UploadedImage is null)
            {
                return;
            }

            var simWindow = new SimulationWindow(UploadedImage, BallCount);
            simWindow.Show();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
