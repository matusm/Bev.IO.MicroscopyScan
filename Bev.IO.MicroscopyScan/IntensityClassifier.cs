namespace Bev.IO.MicroscopyScan
{
    internal class IntensityClassifier
    {
        internal IntensityClassifier(MicroscopyScan microscopyScan)
        {
            this.microscopyScan = microscopyScan;
            histogram = new int[numberOfBins];
            ReEvaluate();
        }

        internal double LowerBound => lowerBound;
        internal double UpperBound => upperBound;
        internal int[] Histogram => histogram;

        internal void ReEvaluate()
        {
            UpdateHistogram();
            lowerBound = GetLowerBound();
            upperBound = GetUpperBound();
        }

        private void UpdateHistogram()
        {
            // clear the histogram
            for (int i = 0; i < histogram.Length; i++)
                histogram[i] = 0;
            int bin;
            for (int x = 0; x < microscopyScan.Width; x++)
            {
                for (int y = 0; y < microscopyScan.Height; y++)
                {
                    bin = (int)(microscopyScan.GetNormalizedPixelValue(x, y, microscopyScan.GlobalMinimum, microscopyScan.GlobalMaximum) * (numberOfBins - 1));
                    histogram[bin]++;
                }
            }
        }

        private double GetLowerBound()
        {
            double intensity = (double)FindPeakPosition(0, numberOfBins / 2 - 1);
            intensity = intensity / (double)(numberOfBins - 1);
            return intensity;
        }

        private double GetUpperBound()
        {
            double intensity = (double)FindPeakPosition(numberOfBins / 2 + 1, numberOfBins);
            intensity = intensity / (double)(numberOfBins - 1);
            return intensity;
        }

        private int FindPeakPosition(int lower, int upper)
        {
            // rudimentary error check
            if (lower < 0) lower = 0; ;
            if (upper > numberOfBins) upper = numberOfBins;
            if (upper < lower)
            {
                int temp = upper;
                upper = lower;
                lower = temp;
            }

            int histMaximum = 0;
            int peakPosition = 0;
            for (int i = lower; i < upper; i++)
            {
                if (histogram[i] > histMaximum)
                {
                    histMaximum = histogram[i];
                    peakPosition = i;
                }
            }
            return peakPosition;
        }

        private readonly MicroscopyScan microscopyScan;
        private const int numberOfBins = 256;
        private readonly int[] histogram;
        private double lowerBound;
        private double upperBound;
    }
}
