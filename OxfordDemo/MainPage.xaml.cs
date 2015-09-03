using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using OxfordDemo.Views;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace OxfordDemo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            NavigationListBox.SelectedIndex = 0;
        }

        private void navigationListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = e.AddedItems[0] as ListBoxItem;

            if(selectedItem != null && selectedItem.Name == "FaceItem")
            {
                NavigationFrame.Navigate(typeof(FacesDemoView));
            }

            if (selectedItem != null && selectedItem.Name == "VisionItem")
            {
                NavigationFrame.Navigate(typeof(VisionDemoView));
            }

            if (selectedItem != null && selectedItem.Name == "SpeechItem")
            {
                NavigationFrame.Navigate(typeof(SpeechDemoView));
            }
        }
    }
}
