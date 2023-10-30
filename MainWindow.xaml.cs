using ScottPlot.Control;
using ScottPlot;
using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DecodingGoldSequence
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Dictionary<string, int[]> _goldSequences;
        public MainWindow()
        {
            InitializeComponent();
            LoadGoldSequences();

        }

        private void OnLoadedMainWindow(object sender, RoutedEventArgs e)
        {
            SetUpChart(ChartIComponent, "I", "Время, с", "Амплитуда");
            SetUpChart(ChartQComponent, "Q", "Время, с", "Амплитуда");
            SetUpChart(ChartComplexSignal, "Комплексная огибающая", "Время, с", "Амплитуда");
        }


        private void OnClickGenerateSignals(object sender, RoutedEventArgs e)
        {
            GenerateSignal _gS = new GenerateSignal();

            _gS.BitsCount = (int)BitsCount.Value;
            _gS.SampleFreq = (int)SamplingFreq.Value;
            _gS.BaudRate = (int)BaudRate.Value;
            _gS.CarrierFreq = 500;
            int[] bits = _gS.BitSequence;

            var builder = new StringBuilder();
            Array.ForEach(bits, x => builder.Append(x));
            string s = builder.ToString();
            InputBits.Text = s;
            int[] res = GoldCodes.ConvertToGoldSequence(bits, _goldSequences);
            _gS.GetIQComponents(res);
            _gS.MakeNoise((double)SNR.Value);

           ChartIComponent.Plot.Clear();
            ChartIComponent.Plot.AddSignalXY(_gS.I.Select(p => p.X).ToArray(),
                _gS.I.Select(p => p.Y).ToArray(), color: Color.Blue);
            ChartIComponent.Refresh();

            ChartQComponent.Plot.Clear();
            ChartQComponent.Plot.AddSignalXY(_gS.Q.Select(p => p.X).ToArray(),
                _gS.Q.Select(p => p.Y).ToArray(), color: Color.Blue);
            ChartQComponent.Refresh();

            ChartComplexSignal.Plot.Clear();
            ChartComplexSignal.Plot.AddSignalXY(_gS.ComplexEnvelope.Select(p => p.X).ToArray(),
                _gS.ComplexEnvelope.Select(p => p.Y).ToArray(), color: Color.Blue);
            ChartComplexSignal.Refresh();

        }
        private void LoadGoldSequences()
        {
            if (_goldSequences == null)
            {
                int[] MSequence1 = GoldCodes.GenerateMSequence(new int[] { 1, 0, 0, 0, 0, 1 });
                int[] MSequence2 = GoldCodes.GenerateMSequence(new int[] { 1, 1, 0, 0, 1, 1 });

                _goldSequences = new Dictionary<string, int[]>
                {
                    ["00"] = GoldCodes.GetGoldCode(MSequence1, MSequence2),
                    ["10"] = GoldCodes.GetGoldCode(MSequence1, GoldCodes.ShiftedArray(MSequence2, 1)),
                    ["01"] = GoldCodes.GetGoldCode(MSequence1, GoldCodes.ShiftedArray(MSequence2, 2)),
                    ["11"] = GoldCodes.GetGoldCode(MSequence1, GoldCodes.ShiftedArray(MSequence2, 3))
                };
            }
        }
        private static void SetUpChart(IPlotControl chart, string title, string labelX, string labelY)
        {
            chart.Plot.Title(title);
            chart.Plot.XLabel(labelX);
            chart.Plot.YLabel(labelY);
            chart.Plot.XAxis.MajorGrid(enable: true, color: Color.FromArgb(50, Color.Black));
            chart.Plot.YAxis.MajorGrid(enable: true, color: Color.FromArgb(50, Color.Black));
            chart.Plot.XAxis.MinorGrid(enable: true, color: Color.FromArgb(30, Color.Black), lineStyle: LineStyle.Dot);
            chart.Plot.YAxis.MinorGrid(enable: true, color: Color.FromArgb(30, Color.Black), lineStyle: LineStyle.Dot);
            chart.Plot.Margins(x: 0.0, y: 0.8);
            chart.Plot.SetAxisLimits(xMin: 0);
            chart.Configuration.Quality = QualityMode.High;
            chart.Configuration.DpiStretch = false;
            chart.Refresh();
        }
    }
}
