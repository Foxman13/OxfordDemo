using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace VisionAPI
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class VisionPage : Page, INotifyPropertyChanged
    {
        private readonly VisionServiceClient _serviceClient = new VisionServiceClient(Config.VisionApiKey);

        private StorageFile _selectedImageFile;
        private BitmapImage _currentImage = new BitmapImage();

        public BitmapImage CurrentImage
        {
            get { return _currentImage; }
            set
            {
                if (value != _currentImage)
                {
                    _currentImage = value;
                    OnPropertyChanged(nameof(CurrentImage));
                }
            }
        }

        public ObservableCollection<BitmapImage> Thumbnails { get; } = new ObservableCollection<BitmapImage>();

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

        public VisionPage()
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
                    _currentImage = new BitmapImage();
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
                    if (Pivot.SelectedItem == AnalysisItem)
                    {
                        AnalysisImage.Source = _currentImage;
                    }
                    if (Pivot.SelectedItem == OcrItem)
                    {
                        OcrImage.Source = _currentImage;
                    }
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
                        if (result != null)
                        {
                            AnalysisResult = result;
                            AnalysisListBox.ItemsSource = result.Categories;
                        }

                    }
                }
                if (pivotItem.Name == "OCRItem")
                {
                    if (_selectedImageFile != null)
                    {
                        var result = await _serviceClient.RecognizeTextAsync(await _selectedImageFile.OpenStreamForReadAsync());
                        OcrResult = result;
                        foreach (var region in OcrResult.Regions)
                        {
                            foreach (var line in region.Lines)
                            {
                                foreach (var word in line.Words)
                                {
                                    OcrResultsTextBox.Text += word.Text;
                                    OcrResultsTextBox.Text += ' ';
                                }
                            }
                        }
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

                AnalysisImage.Source = _currentImage;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ThumbnailsButton_OnClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }

    internal class NotifyPropertyChangedInvocatorAttribute : Attribute
    {
    }
}
