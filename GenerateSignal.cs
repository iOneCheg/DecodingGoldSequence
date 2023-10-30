using DevExpress.XtraPrinting.Native;
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
        public int BitsCount { get; set; }
        public int SampleFreq { get; set; }
        public int CarrierFreq { get; set; }
        public int BaudRate { get; set; }
        public double dt => 1d / (double)(SampleFreq * 1000);
        public int TBit => (SampleFreq * 1000) / BaudRate;
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
            for (int i = 0; i < GoldBits.Length; i += 2)
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
        private static double GetNormalRandom(double min, double max, int n = 12)
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
            var norm = Math.Sqrt(snr * energySignal / noise.Sum(y => y * y));

            return noise.Select(y => y * norm).ToList();
        }

    }
}
