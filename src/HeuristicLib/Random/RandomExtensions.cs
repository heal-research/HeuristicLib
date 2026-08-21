namespace HEAL.HeuristicLib.Random;

public static class RandomExtensions
{
    extension(IRandomNumberGenerator random)
    {
        public int NextInt(int high, bool inclusiveHigh = false) => random.NextInt(0, high, inclusiveHigh);

        public int NextInt(int low, int high, bool inclusiveHigh = false)
        {
            if (high <= low)
                return low;

            var range = (long)high - low + (inclusiveHigh ? 1L : 0L);
            return low + (int)(random.NextDouble() * range);
        }

        public bool NextBool(double probability = 0.5) => random.NextDouble() < probability;

        public bool[] NextBools(int length, double probability = 0.5)
        {
            var values = new bool[length];
            random.NextBools(values, probability);

            return values;
        }

        public void NextBools(Span<bool> destination, double probability = 0.5)
        {
            for (var i = 0; i < destination.Length; i++)
            {
                destination[i] = random.NextBool(probability);
            }
        }

        public double NextDouble(double low, double high)
        {
            if (high <= low)
                return low;

            return random.NextDouble() * (high - low) + low;
        }

        public double NextNormal(double mu = 0, double sigma = 1)
        {
            double u;
            double s;
            do
            {
                u = (random.NextDouble() * 2) - 1;
                var v = (random.NextDouble() * 2) - 1;
                s = (u * u) + (v * v);
            } while (s is > 1 or 0);

            s = Math.Sqrt(-2.0 * Math.Log(s) / s);
            return mu + (sigma * u * s);
        }

        public double[] NextNormals(int length, double mu = 0, double sigma = 1)
        {
            var values = new double[length];
            random.NextNormals(values, mu, sigma);

            return values;
        }

        public void NextNormals(Span<double> destination, double mu = 0, double sigma = 1)
        {
            for (var i = 0; i < destination.Length; i++)
            {
                destination[i] = random.NextNormal(mu, sigma);
            }
        }

        public double[] NextDoubles(int length)
        {
            var values = new double[length];
            random.NextDoubles(values);

            return values;
        }

        public void NextDoubles(Span<double> destination)
        {
            for (var i = 0; i < destination.Length; i++)
            {
                destination[i] = random.NextDouble();
            }
        }

        public double[] NextDoubles(int length, double low, double high)
        {
            var values = new double[length];
            random.NextDoubles(values, low, high);

            return values;
        }

        public void NextDoubles(Span<double> destination, double low, double high)
        {
            if (high <= low)
            {
                destination.Fill(low);
                return;
            }

            var width = high - low;
            for (var i = 0; i < destination.Length; i++)
            {
                destination[i] = random.NextDouble() * width + low;
            }
        }

        public int[] NextInts(int length, int high, bool inclusiveHigh = false) =>
            random.NextInts(length, 0, high, inclusiveHigh);

        public void NextInts(Span<int> destination, int high, bool inclusiveHigh = false) =>
            random.NextInts(destination, 0, high, inclusiveHigh);

        public int[] NextInts(int length, int low, int high, bool inclusiveHigh = false)
        {
            var values = new int[length];
            random.NextInts(values, low, high, inclusiveHigh);

            return values;
        }

        public void NextInts(Span<int> destination, int low, int high, bool inclusiveHigh = false)
        {
            if (high <= low)
            {
                destination.Fill(low);
                return;
            }

            var range = (long)high - low + (inclusiveHigh ? 1L : 0L);
            for (var i = 0; i < destination.Length; i++)
            {
                destination[i] = low + (int)(random.NextDouble() * range);
            }
        }
    }
}
