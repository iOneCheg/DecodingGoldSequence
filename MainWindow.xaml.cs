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
using DevExpress.XtraPrinting.Native;
using System.ComponentModel;

namespace DecodingGoldSequence
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Dictionary<string, int[]> _goldSequences;
        Dictionary<string, object> _gSParameters;
        Dictionary<string, object> _researchParameters;
        List<PointD> _snrResearch;
        private readonly BackgroundWorker _bgResearch;
        public MainWindow()
        {
            InitializeComponent();
            LoadGoldSequences();
            _bgResearch = (BackgroundWorker)FindResource("BackgroundWorkerConductResearch");
        }

        private void OnLoadedMainWindow(object sender, RoutedEventArgs e)
        {
            SetUpChart(ChartIComponent, "I", "Время, с", "Амплитуда");
            SetUpChart(ChartQComponent, "Q", "Время, с", "Амплитуда");
            SetUpChart(ChartComplexSignal, "Комплексная огибающая", "Время, с", "Амплитуда");
            SetUpChart(ChartConvolution, "Отклики согласованных фильтров", "Время, с", "Амплитуда");
            SetUpChart(ChartResearch, "Зависимость вероятности ошибки определения символа от ОСШ", "Уровень шума, дБ", "Вероятность ошибки");
        }
        private void UpdateParameters()
        {
            var bitsCount = (int)BitsCount.Value;
            var sampleFreq = (int)SamplingFreq.Value;
            var baudRate = (int)BaudRate.Value;
            var carrierFreq = (int)((double)CarrierFreq.Value * 1000);

            _gSParameters = new Dictionary<string, object>
            {
                ["bitsCount"] = bitsCount,
                ["sampleFreq"] = sampleFreq,
                ["baudRate"] = baudRate,
                ["carrierFreq"] = carrierFreq
            };
        }

        private void OnClickGenerateSignals(object sender, RoutedEventArgs e)
        {
            UpdateParameters();
            GenerateSignal _gS = new GenerateSignal(_gSParameters);

            int[] bits = _gS.BitSequence;

            var builder = new StringBuilder();
            Array.ForEach(bits, x => builder.Append(x));
            string s = builder.ToString();
            InputBits.Text = s;
            int[] res = GoldCodes.ConvertToGoldSequence(bits, _goldSequences);
            _gS.GetIQComponents(res);
            _gS.MakeNoise((double)SNR.Value);
            _gS.GetConvolution(_goldSequences);
            ResultBits.Text = string.Join("", _gS.DecodeSignal());

            ChartIComponent.Visibility = Visibility.Visible;
            ChartQComponent.Visibility = Visibility.Visible;
            ChartComplexSignal.Visibility = Visibility.Visible;
            ChartConvolution.Visibility = Visibility.Visible;
            ChartResearch.Visibility = Visibility.Collapsed;

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

            ChartConvolution.Plot.Clear();
            ChartConvolution.Plot.AddSignalXY(
                _gS.convolutions["00"].Select(p => p.X).ToArray(),
                _gS.convolutions["00"].Select(p => p.Y).ToArray(),
                Color.Red,
                "00"
            );
            ChartConvolution.Plot.AddSignalXY(
                _gS.convolutions["01"].Select(p => p.X).ToArray(),
                _gS.convolutions["01"].Select(p => p.Y).ToArray(),
                Color.Green,
                "01"
            );
            ChartConvolution.Plot.AddSignalXY(
                _gS.convolutions["10"].Select(p => p.X).ToArray(),
                _gS.convolutions["10"].Select(p => p.Y).ToArray(),
                Color.Blue,
                "10"
            );
            ChartConvolution.Plot.AddSignalXY(
                _gS.convolutions["11"].Select(p => p.X).ToArray(),
                _gS.convolutions["11"].Select(p => p.Y).ToArray(),
                Color.Indigo,
                "11"
            );
            ChartConvolution.Plot.Legend();
            ChartConvolution.Refresh();

        }
        private void LoadGoldSequences()
        {
            if (_goldSequences == null)
            {
                int[] MSequence1 = GoldCodes.GenerateMSequence(new int[] { 1, 0, 0, 0, 0, 1 });
                int[] MSequence2 = GoldCodes.GenerateMSequence(new int[] { 1, 1, 0, 0, 1, 1 });

                _goldSequences = new Dictionary<string, int[]>
                {
                    ["00"] = GoldCodes.GetGoldCode(MSequence1, GoldCodes.ShiftedArray(MSequence2, 0)),
                    ["10"] = GoldCodes.GetGoldCode(MSequence1, GoldCodes.ShiftedArray(MSequence2, 10)),
                    ["01"] = GoldCodes.GetGoldCode(MSequence1, GoldCodes.ShiftedArray(MSequence2, 20)),
                    ["11"] = GoldCodes.GetGoldCode(MSequence1, GoldCodes.ShiftedArray(MSequence2, 30))
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

        private void OnClickConductResearch(object sender, RoutedEventArgs e)
        {
            ConductResearch.Visibility = Visibility.Collapsed;
            ProgressResearch.Visibility = Visibility.Visible;

            _snrResearch = new List<PointD>();

            UpdateParameters();
            _researchParameters = new Dictionary<string, object>
            {
                ["meanOrder"] = (int)MeanCount.Value,
                ["minSNR"] = (int)DownBorder.Value,
                ["maxSNR"] = (int)UpBorder.Value,
                ["snrStep"] = (double)Step.Value
            };
            ProgressResearch.Value = 0;
            ProgressResearch.Maximum = 2 * (int)MeanCount.Value * ((int)UpBorder.Value - (int)DownBorder.Value) / ((double)Step.Value + 1);
            _bgResearch.RunWorkerAsync();
        }
        #region #BackgroundWorker Methods#
        private void OnDoWorkBackgroundWorkerConductResearch(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            try
            {
                var meanOrder = (int)_researchParameters["meanOrder"];
                var minSNR = (int)_researchParameters["minSNR"];
                var maxSNR = (int)_researchParameters["maxSNR"];
                var snrStep = (double)_researchParameters["snrStep"];

                var index = 0;
                Parallel.For(0, (int)((maxSNR - minSNR) / snrStep + 1), n =>
                {
                    double p = 0;
                    var snr = maxSNR - n * snrStep;
                    Parallel.For(0, meanOrder, i =>
                    {
                        int P = 0;
                        var _gS = new GenerateSignal(_gSParameters);
                        int[] bits = _gS.BitSequence;
                        _gS.GetIQComponents(GoldCodes.ConvertToGoldSequence(bits, _goldSequences));
                        _gS.MakeNoise(snr);
                        _gS.GetConvolution(_goldSequences);
                        string t = string.Join("", _gS.DecodeSignal());
                        for (int k = 0; k < bits.Length; k++)
                        {
                            if (bits[k].ToString() != t[k].ToString()) P++;
                        }
                        p += (double)P / (double)bits.Length;
                        _bgResearch.ReportProgress(++index);
                    });
                    _snrResearch.Add(new PointD(snr, (double)p / meanOrder));
                });
                _snrResearch = _snrResearch.OrderBy(p => p.X).ToList();
            }
            catch (Exception exception)
            {
                MessageBox.Show("Ошибка!", exception.Message);
            }
        }

        private void OnRunWorkerCompletedBackgroundWorkerConductResearch(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            ConductResearch.Visibility = Visibility.Visible;
            ProgressResearch.Visibility = Visibility.Collapsed;
            ChartResearch.Visibility = Visibility.Visible;

            ChartResearch.Plot.Clear();
            ChartResearch.Plot.AddSignalXY(
                _snrResearch.Select(p => p.X).ToArray(),
                _snrResearch.Select(p => p.Y).ToArray(),
                Color.Red
            );
            ChartResearch.Refresh();
        }

        private void OnProgressChangedBackgroundWorkerConductResearch(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            ProgressResearch.Value = e.ProgressPercentage;
        }
        #endregion
    }
}
