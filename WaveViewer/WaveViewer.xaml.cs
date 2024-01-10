using System;
using System.Collections;
using System.Collections.Generic;
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

namespace WaveViewer
{
    /// <summary>
    /// Interaction logic for WaveControl.xaml
    /// </summary>
    public partial class WaveControl : UserControl
    {

        byte[] waveData;
        double magnfication {  get; set; }
        int viewPosition { get; set; }
        int playPosition {  get; set; }
        int lineWidth = 10;
        public WaveControl()
        {
            InitializeComponent();
            magnfication = 1;
            viewPosition = 0;
            playPosition = 0;
        }

        public void LoadData(byte[] waveData)
        {
            this.waveData = waveData;
        }

        public void Refresh()
        {
            if (waveData != null)
            {
                int dotShowed = (int)(waveData.Length / magnfication);

                double groupSize = dotShowed / canvas.Children.Count;
                double fracPart = 0;
                int offset = viewPosition;
                for (int i = 0; i < canvas.Children.Count; i++)
                {
                    int actualGroupSize = (int)groupSize;
                    fracPart += groupSize - (int)groupSize;
                    if (fracPart > 1)
                    {
                        fracPart--;
                        actualGroupSize++;
                    }
                    setSpecWaveNum(i, (int)(Avg(offset, actualGroupSize) / 127 * canvas.ActualHeight));
                    offset += actualGroupSize;
                }
            }
        }

        private double Avg(int offset, int length)
        {
            double result = 0;
            for (int i = 0;i < length;i++)
            {
                result += waveData[offset + i];
            }
            return result / length;
        }

        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged)
            {
                int diff = (int)((canvas.ActualWidth - canvas.Children.Count) / lineWidth);
                int offset = canvas.Children.Count;
                if (diff > 0)
                {
                    for (int i = 0; i < diff; i++)
                    {
                        canvas.Children.Add(GetNewLine(i + offset));
                    }
                } else if (diff < 0)
                {
                    for (int i = 0; i < -diff; i++)
                    {
                        canvas.Children.RemoveAt(canvas.Children.Count - 1);
                    }
                }
                //Console.WriteLine(diff);
            }
            Refresh();
        }

        private void Canvas_Loaded(object sender, RoutedEventArgs e)
        {
            for (int i = 0;i < canvas.ActualWidth / lineWidth;i++)
            {
                canvas.Children.Add(GetNewLine(i));
            }
        }

        private Line GetNewLine(int index)
        {
            return new Line()
            {
                X1 = index * lineWidth,
                X2 = index * lineWidth,
                Y1 = canvas.ActualHeight,
                Y2 = canvas.ActualHeight,
                Stroke = Brushes.OrangeRed,
                StrokeThickness = lineWidth
            };
        }

        private void setSpecWaveNum(int index, int num)
        {
            ((Line)canvas.Children[index]).Y1 = canvas.ActualHeight;
            ((Line)canvas.Children[index]).Y2 = canvas.ActualHeight - num;
        }

    }
}