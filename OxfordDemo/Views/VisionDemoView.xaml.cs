using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;
using OxfordDemo.Annotations;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace OxfordDemo.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class VisionDemoView : Page, INotifyPropertyChanged
    {
        private VisionServiceClient _serviceClient = new VisionServiceClient(Config.VisionApiKey);

        private StorageFile _selectedImageFile;
        private BitmapImage _currentImage = new BitmapImage();

        private ObservableCollection<BitmapImage> _thumbnails = new ObservableCollection<BitmapImage>();

        public ObservableCollection<BitmapImage> Thumbnails
        {
            get { return _thumbnails; }
        }

        private AnalysisResult _analysisResult;

        public AnalysisResult AnalysisResult
        {
            get { return _analysisResult; }
            set
            {
                if (value != _analysisResult)
                {
                    _analysisResult = value;
                    OnPropertyChanged(nameof(AnalysisResult));
                }
            }
        }

        private OcrResults _ocrResults;

        public OcrResults OcrResult
        {
            get { return _ocrResults; }
            set
            {
                if (value != _ocrResults)
                {
                    _ocrResults = value;
                    OnPropertyChanged(nameof(OcrResult));
                }
            }
        }

        public VisionDemoView()
        {
            this.InitializeComponent();
        }

        private void ProcessThumbnail(Byte[] data)
        {
            
        }

        private async void selectPhotoButton_Click(object sender, RoutedEventArgs e)
        {
            var filePicker = new FileOpenPicker();
            filePicker.FileTypeFilter.Add(".jpg");
            filePicker.FileTypeFilter.Add(".jpeg");
            filePicker.FileTypeFilter.Add(".png");

            var file = await filePicker.PickSingleFileAsync();

            if (file != null)
            {
                _selectedImageFile = file;
                SelectedPhotoPathTextBox.Text = file.Path;
                using (var stream = await file.OpenAsync(FileAccessMode.Read))
                {
                    await _currentImage.SetSourceAsync(stream);

                }
                if (_currentImage.PixelHeight > 4096 || _currentImage.PixelWidth > 4096)
                {
                    var dialog = new MessageDialog("Image size is too large! Must be smaller than 4096 x 4096");
                    await dialog.ShowAsync();
                    AnalyzeButton.IsEnabled = false;
                }
                else
                {
                    SelectedImage.Source = _currentImage;
                    AnalyzeButton.IsEnabled = true;
                }

               
            }
        }

        private async void detectButton_Click(object sender, RoutedEventArgs e)
        {
            var pivotItem = Pivot.SelectedItem as PivotItem;

            if (pivotItem != null)
            {
                if (pivotItem.Name == "AnalysisItem")
                {
                    if (_selectedImageFile != null)
                    {
                        var result = await _serviceClient.AnalyzeImageAsync(await _selectedImageFile.OpenStreamForReadAsync());
                        AnalysisResult = result;
                    }
                }
                if (pivotItem.Name == "ThumbnailsItem")
                {
                    if (_selectedImageFile != null)
                    {
                        var result = await _serviceClient.GetThumbnailAsync(await _selectedImageFile.OpenStreamForReadAsync(), 300, 300);
                        ProcessThumbnail(result);
                    }
                }
                if (pivotItem.Name == "OCRItem")
                {
                    if (_selectedImageFile != null)
                    {
                        var result = await _serviceClient.RecognizeTextAsync(await _selectedImageFile.OpenStreamForReadAsync());
                        OcrResult = result;
                    }
                }
            }
        }

        private async void takePhotoButton_Click(object sender, RoutedEventArgs e)
        {
            var capture = new CameraCaptureUI();
            capture.PhotoSettings.Format = CameraCaptureUIPhotoFormat.Jpeg;
            capture.PhotoSettings.CroppedSizeInPixels = new Size(1280, 720);
            var photo = await capture.CaptureFileAsync(CameraCaptureUIMode.Photo);

            if (photo != null)
            {
                _selectedImageFile = photo;
                SelectedPhotoPathTextBox.Text = photo.Path;
                using (var stream = await photo.OpenAsync(FileAccessMode.Read))
                {
                    await _currentImage.SetSourceAsync(stream);

                }

                SelectedImage.Source = _currentImage;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
