using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using AForge.Video;
using AForge.Video.DirectShow;


namespace WebcamTimelapseNET5
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TimelapseSettings settings = new TimelapseSettings();

        public MainWindow()
        {
            InitializeComponent();

            videoSourceCombo.ItemsSource = CapturerAforge.getVideoSources();

            settings.Bind(this);
            settings.BindConfig("config");
            settings.attachPresetManager(presetManPanel);

        }

        private void goBtn_Click(object sender, RoutedEventArgs e)
        {
            new CapturerAforge((CapturerAforge.CaptureResult result) => {
                Dispatcher.Invoke(() => {
                    lastImage.Source = result.image;
                    lastDiffTxt.Text = result.lastDiff.ToString();
                    lastFpFTxt.Text = result.calculatedFramesPerFrame.ToString();
                });
            }).dostuff((AForge.Video.DirectShow.FilterInfo)videoSourceCombo.SelectedItem, settings);
        }
    }
}
