using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
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
    /// WaveControlEx.xaml 的交互逻辑
    /// </summary>
    public partial class WaveControlEx : UserControl
    {
        int channel;
        public static readonly int linesPerSecond = 8;
        public static readonly int lineWidth = 4;
        int samplingRate;
        double scrollWidth;
        double factor;

        double pointerOffset = 0;
        bool autoScrolling = false;

        float[][]? waveData;
        Canvas[]? channelCanvas;
        Line[][]? lines;
        public WaveControlEx()
        {
            InitializeComponent();
        }

        public double Load_Data(float[] data, int channel, int samplingRate, double factor)
        {
            if (data == null)
            {
                return 0;
            }
            Clear();

            this.channel = channel;
            this.samplingRate = samplingRate;
            this.factor = factor;

            waveData = Enumerable.Range(0, channel).Select(i => Enumerable.Range(0, data.Length / channel).Select(j => data[i + (j * channel)]).ToArray()).ToArray();

            channelCanvas = new Canvas[channel];

            lines = new Line[channel][];



            for (int i = 0;i < channel;i++)
            {
                RowDefinition r = new RowDefinition();
                grid.RowDefinitions.Add(r);

                Canvas canvas = new Canvas();
                Grid.SetRow(canvas, i);
                canvas.Height = grid.ActualHeight / channel;
                canvas.HorizontalAlignment = HorizontalAlignment.Left;
                canvas.VerticalAlignment = VerticalAlignment.Bottom;

                lines[i] = new Line[waveData[i].Length / samplingRate * linesPerSecond - 1];
                int groupSize = samplingRate / linesPerSecond;
                for (int j = 0;j < lines[i].Length;j++)
                {
                    Line line = new()
                    {
                        Stroke = Brushes.OrangeRed,
                        StrokeThickness = lineWidth / 2,
                        X1 = j * lineWidth,
                        X2 = j * lineWidth,
                        Y1 = canvas.Height,
                        Y2 = canvas.Height - waveData[i].Skip(j * groupSize).Take(groupSize).Average() * factor * canvas.Height,
                    };
                    canvas.Children.Add(line);
                    lines[i][j] = line;
                }
                channelCanvas[i] = canvas;
                grid.Children.Add(channelCanvas[i]);
            }

            CrosserToTop();

            grid.Width = lines[0].Length * lineWidth;

            ScrollContainer.ScrollToHorizontalOffset(grid.Margin.Left * 0.5);

            return grid.Width;
        }

        private void RefreshWave()
        {
            for (int i = 0; i < channel; i++)
            {
                Canvas canvas = channelCanvas[i];
                canvas.Height = grid.ActualHeight / channel;

                int groupSize = samplingRate / linesPerSecond;
                for (int j = 0; j < lines[i].Length; j++)
                {
                    Line line = lines[i][j];
                    line.Stroke = Brushes.OrangeRed;
                    line.StrokeThickness = lineWidth / 2;
                    line.X1 = j * lineWidth;
                    line.X2 = j * lineWidth;
                    line.Y1 = canvas.Height;
                    line.Y2 = canvas.Height - waveData[i].Skip(j * groupSize).Take(groupSize).Average() * factor * canvas.Height;
                }
            }
            CrosserToTop();
        }

        private void CrosserToTop()
        {
            int maxZ = grid.Children.OfType<UIElement>()//linq语句，取Zindex的最大值
              .Where(x => x != crosser)
              .Select(x => Panel.GetZIndex(x))
              .Max();
            Panel.SetZIndex(crosser, maxZ + 1);
        }

        public void Clear()
        {
            for (int i = 0;i < channel;i++)
            {
                channelCanvas?[i].Children.Clear();
            }
            lines = null;
            channelCanvas = null;
            waveData = null;
        }

        public void UpdatePlayingProgress(TimeSpan currentTime)
        {
            crosser.Margin = new Thickness(currentTime.TotalSeconds * linesPerSecond * lineWidth - crosser.Width / 2, 0, 0, 0);
            if (autoScrolling)
            {
                double diff = crosser.TranslatePoint(new Point(), ScrollContainer).X - pointerOffset;
                ScrollContainer.ScrollToHorizontalOffset(ScrollContainer.HorizontalOffset + diff);
            }
        }

        private void grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point p = e.GetPosition(grid);
            if (p.X < 0)
            {
                return;
            }
            ProgressChange?.Invoke(TimeSpan.FromSeconds(p.X / lineWidth / linesPerSecond));
        }

        private void grid_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) //drag
            {
                Point p = e.GetPosition(grid);
                if (p.X < 0)
                {
                    return;
                }
                ProgressChange?.Invoke(TimeSpan.FromSeconds(p.X / lineWidth / linesPerSecond));
            }
        }

        public void StartAutoScrolling()
        {
            pointerOffset = crosser.TranslatePoint(new Point(), ScrollContainer).X;

            FakeCrosser.Margin = new Thickness(crosser.TranslatePoint(new Point(), ScrollContainer).X, 0, 0, scrollWidth);

            FakeCrosser.Visibility = Visibility.Visible;
            crosser.Visibility = Visibility.Hidden;
            autoScrolling = true;
        }

        public void StopAutoScrolling()
        {
            autoScrolling = false;
            FakeCrosser.Visibility = Visibility.Hidden;
            crosser.Visibility = Visibility.Visible;
        }

        public delegate void ProgressChangeHandler(TimeSpan currentTime);
        public event ProgressChangeHandler? ProgressChange;

        public delegate void ScrollChangeHandler(double offset);
        public event ScrollChangeHandler ScrollChange;

        private void ScrollContainer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            ScrollChange?.Invoke(e.HorizontalOffset);
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            grid.Margin = new Thickness(e.NewSize.Width / 2, 0, e.NewSize.Width, 0);
            scrollWidth = ScrollContainer.ActualHeight - grid.ActualHeight;

            if (e.HeightChanged && waveData != null)
            {
                RefreshWave();
            }
        }
    }
}
