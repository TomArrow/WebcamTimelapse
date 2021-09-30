using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
            /*
            for(int i = 0; i < 10; i++)
            {

                bool blah2 = false;
                bool blah = true;
                float blahfloat = Unsafe.As<bool, float>(ref blah);
                sbyte blahbyte = Unsafe.As<bool, sbyte>(ref blah);
                double reverseBoolFloat = 1.0 / (double)blahfloat;
                float blah2float = Unsafe.As<bool, float>(ref blah2);
                MessageBox.Show(blahbyte + " , "+(blahfloat).ToString() + " , " + (reverseBoolFloat).ToString()  + " , " + (blahfloat*reverseBoolFloat).ToString() + " , " + blah2float.ToString());

            }
            */

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
