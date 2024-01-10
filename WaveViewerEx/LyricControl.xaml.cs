using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
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
using System.Windows.Shapes;

namespace WaveViewerEx
{
    /// <summary>
    /// LyricControl.xaml 的交互逻辑
    /// </summary>
    public partial class LyricControl : UserControl
    {
        public LyricControl()
        {
            InitializeComponent();
        }

        private class LyricLine
        {
            public TimeSpan Time { get; set; }
            public string? Lyric { get; set; }
        }

        public void Load_Data(StreamReader data, double width)
        {
            LyricLine[] lyrics = LyricDecoder(data);

            CanvasContainer.Width = width;

            for (int i = 0; i < lyrics.Length - 1; i++)
            {
                TextBlock t = new TextBlock();
                t.Text = lyrics[i].Lyric;
                t.TextAlignment = TextAlignment.Left;
                Canvas.SetLeft(t, Time2Pixels(lyrics[i].Time));
                t.Width = Time2Pixels(lyrics[i + 1].Time - lyrics[i].Time) - WaveControlEx.lineWidth;
                t.TextWrapping = TextWrapping.Wrap;
                Canvas.SetTop(t, 0);
                t.Background = Brushes.Yellow;
                CanvasContainer.Children.Add(t);
            }
        }

        public void ChangeScroll(double offset)
        {
            ScrollContainer.ScrollToHorizontalOffset(offset);
        }

        private double Time2Pixels(TimeSpan time)
        {
            return time.TotalSeconds * WaveControlEx.lineWidth * WaveControlEx.linesPerSecond;
        }

        private static LyricLine[] LyricDecoder(StreamReader reader)
        {
            LinkedList<LyricLine> list = new LinkedList<LyricLine>();
            while (true)
            {
                string? line = reader.ReadLine();
                if (line == null) break;

                int splitIndex = line.IndexOf(']');

                string timeString = line.Substring(1, splitIndex - 1);
                if (!(timeString[0] >= '0' && timeString[0] <= '9')) //确保是歌词而不是歌曲信息
                {
                    continue;
                }

                LyricLine lyricLine = new LyricLine();

                lyricLine.Time = TimeDecoder(timeString);
                lyricLine.Lyric = line.Substring(splitIndex + 1);

                list.AddLast(lyricLine);
            }
            reader.Close();
            return list.ToArray();
        }

        private static TimeSpan TimeDecoder(string data)
        {
            string[] timeBlock1 = data.Split(':');
            int mm = Convert.ToInt32(timeBlock1[0]);
            int ss, xx;
            if (timeBlock1.Length == 2)
            {
                string[] timeBlock2 = timeBlock1[1].Split('.');
                ss = Convert.ToInt32(timeBlock2[0]);
                xx = Convert.ToInt32(timeBlock2[1]);
            } else
            {
                ss = Convert.ToInt32(timeBlock1[1]);
                xx = Convert.ToInt32(timeBlock1[2]);
            }

            return new TimeSpan(0, 0, mm, ss, xx * 10);
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CanvasContainer.Margin = new Thickness(e.NewSize.Width / 2, 0, e.NewSize.Width, 0);
        }
    }
}
