﻿using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using OxfordDemo.Annotations;
using OxfordDemo.Model;
using System.Windows.Input;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace OxfordDemo.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FacesDemoView : Page, INotifyPropertyChanged
    {
        private const string ApiKey = "08e96376b43443d1be7aa67684d1c887"; // TODO: insert your Face API key here

        private readonly FaceServiceClient _client = new FaceServiceClient(ApiKey);

        private StorageFile _selectedImageFile;
        private readonly BitmapImage _currentImage = new BitmapImage();
        private bool _isSaveNew = false;
        private DispatcherTimer _trainingTimer = new DispatcherTimer();

        public ObservableCollection<PersonGroupItem> PersonGroups { get; } = new ObservableCollection<PersonGroupItem>();

        private PersonGroupItem _selectedPersonGroupItem;
        public PersonGroupItem SelectedPersonGroupItem
        {
            get { return _selectedPersonGroupItem; }
            set
            {
                _selectedPersonGroupItem = value;
                if (_selectedPersonGroupItem != null)
                {
                    RemoveGroupButton.IsEnabled = true;
                    SaveGroupButton.IsEnabled = true;
                    TrainGroupButton.IsEnabled = true;
                }
                OnPropertyChanged(nameof(SelectedPersonGroupItem));
            }
        }

        private Person _selectedPerson;
        public Person SelectedPerson
        {
            get { return _selectedPerson; }
            set {
                _selectedPerson = value;
                if (_selectedPerson != null)
                {
                    RemovePersonButton.IsEnabled = true;
                    SavePersonButton.IsEnabled = true;
                }
                OnPropertyChanged(nameof(SelectedPerson));
            }
        }

        public FacesDemoView()
        {
            this.InitializeComponent();
            DataContext = this;

            _trainingTimer.Interval = TimeSpan.FromSeconds(15);
            _trainingTimer.Tick += _trainingTimer_Tick;
        }

        private async void _trainingTimer_Tick(object sender, object e)
        {
            if (SelectedPersonGroupItem != null)
            {
                var status = await _client.GetPersonGroupTrainingStatusAsync(SelectedPersonGroupItem.Group.PersonGroupId);
                if (status.Status == "suceeded")
                {
                    
                }
            }
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await LoadPersonGroupsAsync();
        }

        #region Face Detection
        private async Task<Face[]> DetectFacesInFileAsync(StorageFile file)
        {
            return await DetectFacesInStreamAsync(await file.OpenStreamForReadAsync());
        }

        private async Task<Face[]> DetectFacesInStreamAsync(Stream stream)
        {
            return await _client.DetectAsync(stream, false, true, true, false);
        }

        private async Task<Face[]> DetectFacesInUrlAsync(string url)
        {
            return await _client.DetectAsync(url, false, true, true, false);
        }

        private void ResetFaceDetectionOverlay()
        {
            PhotoOverlayCanvas.Children.Clear();
        }

        private void RenderFaceDetectionResults(Face[] faces)
        {
            ResetFaceDetectionOverlay();
            foreach (var face in faces)
            {
                var heightRatio = SelectedImage.ActualHeight / _currentImage.PixelHeight;
                var widthRatio = SelectedImage.ActualWidth / _currentImage.PixelWidth;
                var localRectHeight = face.FaceRectangle.Height * heightRatio;
                var localRectWidth = face.FaceRectangle.Width * widthRatio;

                Rectangle rect = new Rectangle()
                {
                    Stroke = new SolidColorBrush(Colors.Red),
                    Height = localRectHeight,
                    Width = localRectWidth,
                    IsTapEnabled = true,
                    IsRightTapEnabled = true,
                    IsHitTestVisible = true
                };
                PopupMenu menu = new PopupMenu();


                var horizontalOffset = (PhotoOverlayCanvas.ActualWidth - SelectedImage.ActualWidth) / 2;
                var verticalOffset = (PhotoOverlayCanvas.ActualHeight - SelectedImage.ActualHeight) / 2;

                var localRectLeft = face.FaceRectangle.Left * widthRatio + horizontalOffset;
                var localRectTop = face.FaceRectangle.Top * heightRatio + verticalOffset;

                StackPanel outlinePanel = new StackPanel()
                {
                    IsRightTapEnabled = true,
                    IsHitTestVisible = true,
                    Background = new SolidColorBrush(Colors.Transparent)
                };
                outlinePanel.RightTapped += async (o, e) =>
                {
                    var command = await menu.ShowForSelectionAsync(GetElementRect(o as FrameworkElement));
                };
                outlinePanel.Loaded += (s, e) =>
                {
                    Canvas.SetLeft(outlinePanel, localRectLeft - ((outlinePanel.ActualWidth - localRectWidth) / 2));
                    Canvas.SetTop(outlinePanel, localRectTop - (outlinePanel.ActualHeight - localRectHeight));
                };
                Grid grid = new Grid() { Background = new SolidColorBrush(Colors.White) };
                TextBlock description = new TextBlock()
                {
                    Text = string.Format("{0} year old {1}", face.Attributes.Age, face.Attributes.Gender),
                    Foreground = new SolidColorBrush(Colors.Black),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                grid.Children.Add(description);
                outlinePanel.Children.Add(grid);
                outlinePanel.Children.Add(rect);
                PhotoOverlayCanvas.Children.Add(outlinePanel);

                menu.Commands.Add(new UICommand("Assign", async (o) =>
                {
                    var selectedFace = o.Id as Face;
                    await AssignFaceToPersonAsync(selectedFace);
                }, face));
                menu.Commands.Add(new UICommand("Identify", async (o) =>
                {
                    var selectedFace = o.Id as Face;
                    var person = await IdentifyPersonAsync(selectedFace);
                    if (person != null)
                    {
                        description.Text = person.Name;
                        Canvas.SetLeft(outlinePanel, localRectLeft - ((outlinePanel.ActualWidth - localRectWidth) / 2));
                    }
                }, face));
            }
        }

        public Rect GetElementRect(FrameworkElement element)
        {
            GeneralTransform buttonTransform = element.TransformToVisual(null);
            Point point = buttonTransform.TransformPoint(new Point());
            return new Rect(point, new Size(element.ActualWidth, element.ActualHeight));
        }

        #endregion

        #region Persons
        private async Task<Person[]> GetPersonsAsync(string groupId)
        {
            return await _client.GetPersonsAsync(groupId);
        }

        private async Task LoadPersonGroupsAsync()
        {
            PersonGroups.Clear();

            var groups = await _client.GetPersonGroupsAsync();

            foreach(var group in groups)
            {
                var groupItem = new PersonGroupItem() {Group = group};

                try
                {
                    var trainingResult = await _client.GetPersonGroupTrainingStatusAsync(group.PersonGroupId);
                    groupItem.LastTrained = trainingResult.EndTime;
                }
                catch (ClientException ex)
                {
                    // hopefully, this means the groupId had no entries for training
                }
                finally
                {
                    PersonGroups.Add(groupItem);
                    await LoadPersons(groupItem);
                }

            }
        }

        private async Task LoadPersons(PersonGroupItem groupItem)
        {
            var persons = await GetPersonsAsync(groupItem.Group.PersonGroupId);
            foreach (var person in persons)
            {
                groupItem.Persons.Add(person);
            }
        }


        private async Task<CreatePersonResult> CreatePersonAsync(Person person)
        {
            return await _client.CreatePersonAsync(SelectedPersonGroupItem.Group.PersonGroupId, person.FaceIds, person.Name, person.UserData);
        }

        private async Task UpdatePersonAsync(Person person)
        {
            await
                _client.UpdatePersonAsync(SelectedPersonGroupItem.Group.PersonGroupId, person.PersonId, person.FaceIds,
                    person.Name, person.UserData);
        }

        private async Task DeletePersonAsync(string groupId, Guid personId)
        {
            await _client.DeletePersonAsync(groupId, personId);
        }

        private async Task CreatePersonGroupAsync(PersonGroup group)
        {
            await _client.CreatePersonGroupAsync(group.PersonGroupId, group.Name, group.UserData);
        }

        private async Task DeletePersonGroupAsync(string groupId)
        {
            await _client.DeletePersonGroupAsync(groupId);
        }

        private async Task UpdatePersonGroupAsync(PersonGroup group)
        {
            await _client.UpdatePersonGroupAsync(group.PersonGroupId, group.Name, group.UserData);
        }

        private async Task AddPersonFaceAsync(string groupId, Guid personId, Guid faceId)
        {
            await _client.AddPersonFaceAsync(groupId, personId, faceId);
        }

        private async Task<Person> IdentifyPersonAsync(Face face)
        {
            Person result = null;
            foreach(var groupItem in PersonGroups)
            {
                if (groupItem.LastTrained.Year > 2000)
                {
                    var results = await _client.IdentifyAsync(groupItem.Group.PersonGroupId, new Guid[] { face.FaceId });
                    if (results != null && results.Length > 0)
                    {
                        foreach (var res in results)
                        {
                            foreach (var candidate in res.Candidates)
                            {
                                if (candidate.Confidence > 0.8)
                                {
                                    foreach (var pg in PersonGroups)
                                    {
                                        if (pg.Persons.Any(p => p.PersonId == candidate.PersonId))
                                        {
                                            result = pg.Persons.First(p => p.PersonId == candidate.PersonId);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return result;
        }

        private async Task AssignFaceToPersonAsync(Face face)
        {
            ContentDialog dialog = new ContentDialog();
            dialog.IsPrimaryButtonEnabled = true;
            dialog.IsSecondaryButtonEnabled = true;
            dialog.PrimaryButtonText = "Okay";
            dialog.SecondaryButtonText = "Cancel";
            ListBox listBox = new ListBox();
            var personsCombined = new List<Person>();
            foreach (var groupItem in PersonGroups)
            {
                personsCombined.AddRange(groupItem.Persons);
            }
            listBox.ItemsSource = personsCombined;
            listBox.DisplayMemberPath = "Name";
            dialog.Content = listBox;
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var person = listBox.SelectedItem as Person;
                if (person != null)
                {
                    var groupItem = PersonGroups.First(g => g.Persons.Contains(person));
                    if(groupItem != null)
                        await AddPersonFaceAsync(groupItem.Group.PersonGroupId, person.PersonId, face.FaceId);
                }
                    
            }
        }

        private async void detectButton_Click(object sender, RoutedEventArgs e)
        {
            Face[] faces = null;

            if (SelectPhotoRadioButton.IsChecked.Value || TakePhotoRadioButton.IsChecked.Value)
            {
                if (_selectedImageFile != null)
                {
                    faces = await DetectFacesInFileAsync(_selectedImageFile);
                }
            }
            else
            {
                // handle a URL
                Uri uri;
                var result = Uri.TryCreate(UrlTextBox.Text, UriKind.Absolute, out uri) && uri.Scheme == "http" || uri.Scheme == "https";

                if (result)
                {
                    _currentImage.UriSource = uri;
                    SelectedImage.Source = _currentImage;
                    faces = await DetectFacesInUrlAsync(uri.OriginalString);
                }

            }

            if (faces != null && faces.Length > 0)
            {
                RenderFaceDetectionResults(faces);
            }
        }

        private async void selectPhotoButton_Click(object sender, RoutedEventArgs e)
        {
            ResetFaceDetectionOverlay();
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

                SelectedImage.Source = _currentImage;
            }
        }

        private async void takePhotoButton_Click(object sender, RoutedEventArgs e)
        {
            ResetFaceDetectionOverlay();
            CameraCaptureUI capture = new CameraCaptureUI();
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

        private void assignButton_Click(object sender, RoutedEventArgs e)
        {

        }
        #endregion

        private async void Pivot_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //var pivotItem = e.AddedItems[0] as PivotItem;
            //if (pivotItem != null && pivotItem.Equals(PersonPivotItem))
            //{
            //    await LoadPersonGroups();
            //}
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void NewGroupButton_OnClick(object sender, RoutedEventArgs e)
        {
            var groupItem = new PersonGroupItem() {Group = new PersonGroup()};
            SelectedPersonGroupItem = groupItem;
            _isSaveNew = true;
        }

        private void NewPersonButton_OnClick(object sender, RoutedEventArgs e)
        {
            var person = new Person();
            SelectedPerson = person;
            _isSaveNew = true;
        }

        private async void SaveGroupButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (SelectedPersonGroupItem != null)
            {
                if (_isSaveNew)
                {
                    await CreatePersonGroupAsync(SelectedPersonGroupItem.Group);
                    PersonGroups.Add((SelectedPersonGroupItem));
                }
                else
                {
                    await UpdatePersonGroupAsync(SelectedPersonGroupItem.Group);
                    await LoadPersonGroupsAsync();
                }
                _isSaveNew = false;
            }
        }

        private async void SavePersonButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (SelectedPerson != null)
            {
                if (_isSaveNew)
                {
                    await CreatePersonAsync(SelectedPerson);
                    await LoadPersons(SelectedPersonGroupItem);
                }
                else
                {
                    await
                        UpdatePersonAsync(SelectedPerson);
                }
                _isSaveNew = false;
            }
        }

        private async void RemoveGroupButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (SelectedPersonGroupItem != null)
            {
                var groupId = SelectedPersonGroupItem.Group.PersonGroupId;
                await DeletePersonGroupAsync(groupId);
                //PersonGroups.Remove(groupItem);
                await LoadPersonGroupsAsync();
            }
 
        }

        private async void RemovePersonButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (SelectedPersonGroupItem != null && SelectedPerson != null)
            {
                await DeletePersonAsync(SelectedPersonGroupItem.Group.PersonGroupId, SelectedPerson.PersonId);
                //SelectedPersonGroupItem.Persons.Remove(SelectedPerson);
                await LoadPersons(SelectedPersonGroupItem);
            }
                
        }

        private async void TrainGroupButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (SelectedPersonGroupItem != null)
            {
                await _client.TrainPersonGroupAsync((SelectedPersonGroupItem.Group.PersonGroupId));
                _trainingTimer.Start();
            }
        }
    }
}