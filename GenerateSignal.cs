using DevExpress.XtraPrinting.Native;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using MathNet.Numerics.Statistics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DecodingGoldSequence
{
    internal class GenerateSignal
    {
        private Random? _randomGenerator;
        public List<PointD> I, Q;
        public List<PointD> ComplexEnvelope;
        public Dictionary<string, List<PointD>> convolutions;
        public int BitsCount { get; set; }
        public int SampleFreq { get; set; }
        public int CarrierFreq { get; set; }
        public int BaudRate { get; set; }
        private double dt => 1d / (double)(SampleFreq * 1000);
        private int TBit => (SampleFreq * 1000) / BaudRate;
        public GenerateSignal(Dictionary<string, object> _param)
        {
            BitsCount = (int)_param["bitsCount"];
            SampleFreq = (int)_param["sampleFreq"];
            CarrierFreq = (int)_param["baudRate"];
            BaudRate = (int)_param["carrierFreq"];
        }

        public int[] BitSequence
        {
            get
            {
                _randomGenerator = new Random(Guid.NewGuid().GetHashCode());
                return Enumerable
               .Range(0, BitsCount)
               .Select(i => Convert.ToInt32(_randomGenerator.Next(2) == 0))
               .ToArray();
            }
        }

        public void GetIQComponents(int[] GoldBits)
        {
            I = new List<PointD>();
            Q = new List<PointD>();
            ComplexEnvelope = new List<PointD>();

            int counter = 0;
            int size = 0;
            if (GoldBits.Length % 2 == 0) size = GoldBits.Length;
            else size = GoldBits.Length - 1;

            for (int i = 0; i < size; i += 2)
            {
                for (int j = 0; j < TBit * 2; j++)
                {
                    I.Add(new PointD(counter * dt, GoldBits[i] - 0.5));
                    Q.Add(new PointD(counter * dt, GoldBits[i + 1] - 0.5));
                    double temp = (GoldBits[i] - 0.5) * Math.Cos(2 * Math.PI * CarrierFreq * counter * dt) -
                        (GoldBits[i + 1] - 0.5) * Math.Sin(2 * Math.PI * CarrierFreq * counter * dt);
                    ComplexEnvelope.Add(new PointD(counter * dt, temp));
                    counter++;
                }
            }
        }
        public void GetConvolution(Dictionary<string, int[]> goldCodes)
        {
            convolutions = new Dictionary<string, List<PointD>>();
            int counter = 0;
            foreach (var k in goldCodes)
            {
                List<PointD> signSequence = new();
                for (int i = 0; i < k.Value.Length - 1; i++)
                {
                    int t1 = k.Value[i], t2 = k.Value[i + 1];
                    for (int j = 0; j < TBit; j++)
                    {
                        double temp = (t1 - 0.5) * Math.Cos(2 * Math.PI * CarrierFreq * counter * dt) -
                            (t2 - 0.5) * Math.Sin(2 * Math.PI * CarrierFreq * counter * dt);
                        signSequence.Add(new PointD(counter * dt, temp));
                        counter++;
                    }
                }
                convolutions[k.Key] = new();
                convolutions[k.Key] = CalculateCorrelation(signSequence);//CrossCorrelate(ComplexEnvelope, signSequence);
            }
        }
        
        public List<PointD> CalculateCorrelation(List<PointD> filter)
        {
            var result = new List<PointD>();
            for (var i = 0; i < ComplexEnvelope.Count - filter.Count + 1; i++)
            {
                var corr = 0d;
                for (var j = 0; j < filter.Count; j++)
                    corr += ComplexEnvelope[i + j].Y * filter[j].Y;
                result.Add(new PointD(ComplexEnvelope[i].X, corr / filter.Count));
            }
            return result;
        }
        public List<string> DecodeSignal()
        {
            int signCount = (BitsCount / 2)-1,
                length = convolutions["00"].Count,
                interval = TBit*62,
                startEnd = (length-(interval*(signCount-1))-1)/2;

            List<string> map = new();

            int range = 0;
            for (int i = 0; i < length-1;)
            {
                if (i == 0 || i == (length - startEnd)-1) range = startEnd;
                else range = interval;

                Dictionary<string, double> max = new Dictionary<string, double>();
                foreach (var k in convolutions)
                {
                    max[k.Key] = k.Value.GetRange(i, range).Select(p => p.Y).Max();
                }
                map.Add(max.MaxBy(kvp=>kvp.Value).Key);
                if (i == 0 || i == (length - startEnd)-1) i += startEnd;
                else i += interval;
            }
            return map;
        }
        /// <summary>
        /// Наложение шума на сигнал
        /// </summary>
        /// <param name="snrDb">SNR в дБ</param>
        public void MakeNoise(double snrDb)
        {

            // Наложение шума на исследуемый сигнал.
            ComplexEnvelope = ComplexEnvelope.Zip(
                    GenerateNoise(ComplexEnvelope.Count, ComplexEnvelope.Sum(p => p.Y * p.Y), snrDb),
                    (p, n) => new PointD(p.X, p.Y + n))
                .ToList();
        }
        /// <summary>
        /// Генерация случайного числа по нормальному распределению
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        private static double GetNormalRandom(double min, double max, int n = 20)
        {
            var rnd = new Random(Guid.NewGuid().GetHashCode());
            var sum = 0d;
            for (var i = 0; i < n; i++)
                sum += rnd.NextDouble() * (max - min) + min;
            return sum / n;
        }
        /// <summary>
        /// Генерация белого гауссовского шума
        /// </summary>
        /// <param name="countNumbers">Число отсчетов</param>
        /// <param name="energySignal">Энергия сигнала</param>
        /// <param name="snrDb">SNR в дБ</param>
        /// <returns></returns>
        private static IEnumerable<double> GenerateNoise(int countNumbers, double energySignal, double snrDb)
        {
            var noise = new List<double>();
            for (var i = 0; i < countNumbers; i++)
                noise.Add(GetNormalRandom(-1d, 1d));

            // Нормировка шума.
            var snr = Math.Pow(10, -snrDb / 10);
            var norm = Math.Sqrt(energySignal / noise.Sum(y => y * y)*snr);

            return noise.Select(y => y * norm).ToList();
        }

    }
}
