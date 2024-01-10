using Microsoft.Win32;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using NAudio.Wave.SampleProviders;
using System.IO;

namespace MusicPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        IWavePlayer wavePlayer;
        AudioFileReader audioFileReader;

        Timer progressUpdateTimer;


        TimeSpan _oldTime;
        DateTime _lastUpdateTime;

        bool statusPlaying = false;
        


        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "支持的文件|*.mp3;*.wav;*.aiff|所有文件|*.*";
            if (ofd.ShowDialog() == true)
            {
                Load_Program(ofd.FileName);
            }
        }

        private void Load_Program(string filePath)
        {
            this.Title = filePath;

            audioFileReader = new AudioFileReader(filePath);
            IWaveProvider sampleProvider = audioFileReader.ToWaveProvider();
            byte[] waveData = new byte[audioFileReader.Length];
            sampleProvider.Read(waveData, 0, waveData.Length);
            float[] decodedWaveData = new float[audioFileReader.Length / sizeof(float)];
            Buffer.BlockCopy(waveData, 0, decodedWaveData, 0, waveData.Length);

            for (int i = 0; i < decodedWaveData.Length; i++)
            {
                decodedWaveData[i] = Math.Abs(decodedWaveData[i]);
            }

            float amplifiedRate = 0.5f / decodedWaveData.Average();
            for (int i = 0;i < decodedWaveData.Length; i++)
            {
                decodedWaveData[i] *= amplifiedRate;
            }


            //byte[] waveData = new byte[audioFileReader.Length];
            //audioFileReader.Read(waveData, 0, waveData.Length);
            //audioFileReader.Dispose();



            audioFileReader = new AudioFileReader(filePath);
            wavePlayer = new WaveOutEvent();
            wavePlayer.Init(audioFileReader);



            /*
            uint[] decodedWaveData = new uint[audioFileReader.Length / audioFileReader.WaveFormat.BitsPerSample * 8];
            uint maxValue = 0;
            switch (audioFileReader.WaveFormat.BitsPerSample)
            {
                case 8:
                    for (int i = 0; i < decodedWaveData.Length; i++)
                    {
                        decodedWaveData[i] = waveData[i];
                    }
                    maxValue = byte.MaxValue;
                    break;
                case 16:
                    for (int i = 0; i < decodedWaveData.Length; i++)
                    {
                        decodedWaveData[i] = BitConverter.ToUInt16(waveData, i * 2);
                    }
                    maxValue = ushort.MaxValue;
                    break;
                case 24:
                    for (int i = 0; i < decodedWaveData.Length; i++)
                    {
                        MessageBox.Show("UNSUPPORTED FORMAT");
                        throw new NotImplementedException();
                    }
                    break;
                case 32:
                    for (int i = 0; i < decodedWaveData.Length; i++)
                    {
                        decodedWaveData[i] = BitConverter.ToUInt32(waveData, i * 4);
                    }
                    maxValue = uint.MaxValue;
                    break;
            }
            */

            
            


            wavePlayer.PlaybackStopped += WavePlayer_PlaybackStopped;

            double width = waveControlEx.Load_Data(decodedWaveData, audioFileReader.WaveFormat.Channels, audioFileReader.WaveFormat.SampleRate, 1);
            waveControlEx.ProgressChange += WaveControlEx_ProgressChange;
            waveControlEx.ScrollChange += WaveControlEx_ScrollChange;

            lyricControl.Load_Data(new StreamReader(filePath.Remove(filePath.LastIndexOf('.')) + ".lrc"), width);


            _lastUpdateTime = DateTime.Now;
            _oldTime = audioFileReader.CurrentTime;

            progressUpdateTimer = new Timer();
            progressUpdateTimer.Interval = 10;
            progressUpdateTimer.Elapsed += ProgressUpdateTimer_Tick;
            progressUpdateTimer.Start();
        }

        private void WaveControlEx_ScrollChange(double offset)
        {
            lyricControl.ChangeScroll(offset);
        }

        private void WavePlayer_PlaybackStopped(object? sender, StoppedEventArgs e)
        {
            StopPlaying();
        }

        private void WaveControlEx_ProgressChange(TimeSpan currentTime)
        {
            audioFileReader.CurrentTime = currentTime;
        }

        private void ProgressUpdateTimer_Tick(object? sender, EventArgs e)
        {
            DateTime startTime = DateTime.Now;
            if (_oldTime == audioFileReader.CurrentTime && statusPlaying)
            {
                this.Dispatcher.Invoke(new Action(() => waveControlEx.UpdatePlayingProgress(audioFileReader.CurrentTime + (DateTime.Now - _lastUpdateTime))));
            } else
            {
                this.Dispatcher.Invoke(new Action(() => waveControlEx.UpdatePlayingProgress(audioFileReader.CurrentTime)));
                _oldTime = audioFileReader.CurrentTime;
            }
            _lastUpdateTime = DateTime.Now;
            Trace.WriteLine(DateTime.Now - startTime);
        }

        private void playButton_Click(object sender, RoutedEventArgs e)
        {
            if (statusPlaying)
            {
                StopPlaying();
            } else
            {
                StartPlaying();
            }
        }
        private void StopPlaying()
        {
            if (wavePlayer != null)
            {
                statusPlaying = false;
                playButton.Content = "播放";
                wavePlayer.Pause();
                waveControlEx.StopAutoScrolling();
            }
        }

        private void StartPlaying()
        {
            if (wavePlayer != null)
            {
                statusPlaying = true;
                playButton.Content = "暂停";
                waveControlEx.StartAutoScrolling();
                wavePlayer.Play();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            wavePlayer?.Dispose();
            audioFileReader?.Dispose();
            progressUpdateTimer?.Stop();
            progressUpdateTimer?.Close();
            progressUpdateTimer?.Dispose();
            System.Environment.Exit(0);
        }
    }
}
