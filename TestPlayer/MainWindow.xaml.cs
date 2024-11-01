using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Diagnostics;


namespace TestPlayer
{
    public partial class MainWindow : Window
    {

        DispatcherTimer timer = new DispatcherTimer();
        bool posSliderDragging = false;
        String trackPath = ""; // full path to file of currently playing track		


        public MainWindow()
        {
            InitializeComponent();
            this.WindowState = WindowState.Maximized; // Start the application in fullscreen
            timer.Interval = new TimeSpan(0, 0, 0, 0, 100); // 100ms
            timer.Tick += new EventHandler(Timer_Tick); // Timer for the position slider
        }

        // This function is called when the timer ticks
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!posSliderDragging)
            {
                PosSlider.Value = Me.Position.TotalMilliseconds;
            }
        }

        // This function is called when an item in the playlistBox is selected
        private void PlayPlaylist()
        {
            int selectedItemIndex = -1;
            if (playlistBox.Items.Count > 0)
            {
                selectedItemIndex = playlistBox.SelectedIndex;
                if (selectedItemIndex > -1)
                {
                    trackPath = playlistBox.Items[selectedItemIndex].ToString();
                    trackLabel.Content = trackPath;
                    PlayTrack();
                }
            }

        }

        // This function is called when an item in the playlistBox is selected
        private void PlayTrack()
        {
            bool ok = true;
            FileInfo fi = null;
            Uri src;
            try
            {
                fi = new FileInfo(trackPath);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
                ok = false;
            }
            if (ok)
            {
                // check that the file actually exists
                if (!fi.Exists)
                {
                    System.Windows.MessageBox.Show("Cannot find " + trackPath);
                }
                else
                {
                    // if the MediaElement can find its Source the MediFailed event-handler should take over..
                    src = new Uri(trackPath);
                    Me.Source = src;
                    // assign the defaults (from slider positions) when a track starts playing
                    Me.SpeedRatio = SpeedSlider.Value;
                    Me.Volume = VolSlider.Value;
                    Me.Balance = BalanceSlider.Value;
                    Me.Play();
                    timer.Start();
                }
            }
        }

        // This function is called when the Start button is clicked
        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            if (playlistBox.Items.Count > 0)
            {
                PlayPlaylist();
            }
            else
            {
                PlayTrack();
            }
        }

        // This function is called when the Stop button is clicked
        private void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            Me.Stop();
            timer.Stop();
        }

        // This function is called when the Play button is clicked
        private void PauseBtn_Click(object sender, RoutedEventArgs e)
        {
            Me.Pause();
        }

        // This function is called when the media has been opened
        private void Media_MediaOpened(object sender, RoutedEventArgs e)
        {
            PosSlider.Maximum = Me.NaturalDuration.TimeSpan.TotalMilliseconds;
            SpeedSlider.Value = 1;
        }

        // Stop payback when position is changed so that you don't get any
        private void PosSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            posSliderDragging = true;
            Me.Stop();
        }

        // and start again when you've finished
        private void PosSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            posSliderDragging = false;
            Me.Play();
        }

        // Position slider
        private void PosSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int SliderValue = (int)PosSlider.Value;
            if (posSliderDragging)
            {
                Me.Position = TimeSpan.FromMilliseconds(SliderValue);
            }
        }

        // Volume slider
        private void VolSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Me.Volume = VolSlider.Value;
        }

        // Speed slider
        private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Me.SpeedRatio = SpeedSlider.Value;
        }


        // Balance slider
        private void BalanceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Me.Balance = BalanceSlider.Value;
        }


        // This function is called when the media has finished playing
        private void Media_MediaEnded(object sender, RoutedEventArgs e)
        {
            int nextTrackIndex = -1;
            int numberOfTracks = -1;
            Me.Stop();
            numberOfTracks = playlistBox.Items.Count;
            if (numberOfTracks > 0)
            {
                nextTrackIndex = playlistBox.SelectedIndex + 1;
                if (nextTrackIndex >= numberOfTracks)
                {
                    nextTrackIndex = 0;
                }
                playlistBox.SelectedIndex = nextTrackIndex;
                PlayPlaylist();
            }
        }


        // File processing		
        private void ShowInfo()
        {
            string dirname;
            string filename;
            string header;
            string data;
            Shell32.Shell shell = new Shell32.Shell();
            dirname = Path.GetDirectoryName(trackPath);
            filename = Path.GetFileName(trackPath);
            Shell32.Folder folder = shell.NameSpace(dirname);
            Shell32.FolderItem folderitem = folder.ParseName(filename);
            infoBox.Text = dirname + "\n" + filename;
            for (int i = 0; i <= 315; i++)
            {
                header = folder.GetDetailsOf(null, i);
                data = folder.GetDetailsOf(folderitem, i);
                if (String.IsNullOrEmpty(header))
                {
                    header = "[Unknown header]";
                }
                if (String.IsNullOrEmpty(data))
                {
                    data = "[No data]";
                }
                infoBox.AppendText($"\n{i} {header} {data}");
            }
        }

        // This function opens a file and plays it
        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            Nullable<bool> result;
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "";
            dlg.DefaultExt = ".mp4";
            dlg.Filter = ".mp4|*.mp4|4.mp3|*.mp3|.mpg|*.mpg|.wmv|*.wmv|All files (*.*)|*.*";
            dlg.CheckFileExists = true;
            result = dlg.ShowDialog();
            if (result == true)
            {
                playlistBox.Items.Clear();
                playlistBox.Visibility = Visibility.Hidden;
                infoBox.Visibility = Visibility.Visible;
                // Open document
                trackPath = dlg.FileName;
                trackLabel.Content = trackPath;
                ShowInfo();
                PlayTrack();
            }

        }

        //  This function opens a folder and adds all .mp4 and .mp3 files to the playlistBox
        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            String folderpath = "";
            string[] mp4Files;
            string[] mp3Files;		
            FolderBrowserDialog fd = new FolderBrowserDialog();
            DialogResult result = fd.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                folderpath = fd.SelectedPath;
            }
            if (!string.IsNullOrEmpty(folderpath))
            {
                playlistBox.Items.Clear();
                playlistBox.Visibility = Visibility.Visible;
                infoBox.Visibility = Visibility.Hidden;

                // Get both .mp4 and .mp3 files
                mp4Files = Directory.GetFiles(folderpath, "*.mp4");
                mp3Files = Directory.GetFiles(folderpath, "*.mp3");

                // Add all .mp4 and .mp3 files to the playlistBox
                foreach (string fn in mp4Files)
                {
                    playlistBox.Items.Add(fn);
                }
                foreach (string fn in mp3Files)
                {
                    playlistBox.Items.Add(fn);
                }

                // Set the first item as selected (if available)
                if (playlistBox.Items.Count > 0)
                {
                    playlistBox.SelectedIndex = 0;
                }
            }
        }


        private void CloseApp_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Media_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            // This fires if the media.Source can't be found or can't be played
            System.Windows.MessageBox.Show("Unable to play " + trackPath + " [" + e.ErrorException.Message + "]");
        }

        private bool IsValidTrack(string aTrack)
        {
            return (aTrack.EndsWith(".mp4"));
        }

        // Function which lets the user drag file/s directly into the application
        private void Files_Drop(object sender, System.Windows.DragEventArgs e)
        {
            string[]? trackpaths = e.Data.GetData(System.Windows.DataFormats.FileDrop) as string[];
            if (trackpaths == null)
            {
                return;
            }
            foreach (string s in trackpaths)
            {
                if (IsValidTrack(s))
                {
                    playlistBox.Items.Add(s);
                }
            }
            if (playlistBox.Items.Count > 0)
            {
                playlistBox.Visibility = Visibility.Visible;
                infoBox.Visibility = Visibility.Hidden;
                playlistBox.SelectedIndex = 0;
            }
        }

        // Function for opening the Info Panel
        private void InfoPanel_Click(object sender, RoutedEventArgs e)
        {
            InfoWindow info = new InfoWindow();
            info.Show();
        }

    }
}
