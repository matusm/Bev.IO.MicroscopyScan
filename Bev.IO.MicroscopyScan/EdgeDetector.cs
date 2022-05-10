using System;
using System.Collections.Generic;

namespace Bev.IO.MicroscopyScan
{
    public class EdgeDetector
    {
        private const double requiredContrast = 0.05;

        public EdgeDetector(MicroscopyScan microscopyScan)
        {
            this.microscopyScan = microscopyScan;
            Prepare();
        }

        public int ValidProfiles => validProfiles;
        public int TotalProfiles => totalProfiles;
        public double LowerBound => lowerBound;
        public double UpperBound => upperBound;
        public double Threshold => threshold;
        public double ReducedThreshold => reducedThreshold;
        public double[] LinePositions => linePositions;
        public double[] LineWidths => lineWidths;
        public double[] LinePositionsSpan => linePositionsSpan; 
        public double[] LineWidthsSpan => lineWidthsSpan; 
        public double[] LeftEdgePositions => leftEdgePositions; 
        public double[] RightEdgePositions => rightEdgePositions; 
        
        public void Evaluate()
        {
            Evaluate(0.5); 
        }

        public void Evaluate(double threshold)
        {
            Evaluate(0, threshold);
        }

        public void Evaluate(int linesExpected, double threshold)
        {
            Evaluate(linesExpected, 0, threshold);
        }

        public void Evaluate(int linesExpected, int referenceLineIndex, double threshold)
        {
            validProfiles = 0;
            totalProfiles = microscopyScan.NumberOfProfiles;
            this.threshold = threshold;
            reducedThreshold = contrast * threshold + lowerBound;

            if (contrast < requiredContrast) return;

            // estimate # of lines if not stated
            if (linesExpected <= 0)
            {
                linesExpected = GetLeftEdgePositionForProfil(totalProfiles / 2, reducedThreshold).Length;
                if (linesExpected == 0) return;
            }

            if (referenceLineIndex > linesExpected) return;

            leftEdgePositions = new double[linesExpected];
            rightEdgePositions = new double[linesExpected];
            linePositions = new double[linesExpected];
            lineWidths = new double[linesExpected];
            linePositionsSpan = new double[linesExpected];
            lineWidthsSpan = new double[linesExpected];
            double[] linePositionsMin = new double[linesExpected];
            double[] linePositionsMax = new double[linesExpected];
            double[] lineWidthsMin = new double[linesExpected];
            double[] lineWidthsMax = new double[linesExpected];

            for (int i = 0; i < linesExpected; i++)
            {
                linePositionsMax[i] = double.MinValue;
                lineWidthsMax[i] = double.MinValue;
                linePositionsMin[i] = double.MaxValue;
                lineWidthsMin[i] = double.MaxValue;
                lineWidths[i] = 0;
                linePositions[i] = 0;
                leftEdgePositions[0] = 0;
                rightEdgePositions[0] = 0;
            }

            double[] linePosTemp = new double[linesExpected];
            double[] lineWidthTemp = new double[linesExpected];
            // main loop over profiles
            for (int iProfile = 0; iProfile < totalProfiles; iProfile++)
            {
                double[] lEdge = GetLeftEdgePositionForProfil(iProfile, reducedThreshold);
                double[] rEdge = GetRightEdgePositionForProfil(iProfile, reducedThreshold);
                if (lEdge.Length == linesExpected && rEdge.Length == linesExpected)
                {
                    // estimate line positions and widths
                    for (int i = 0; i < lEdge.Length; i++)
                    {
                        linePosTemp[i] = (lEdge[i] + rEdge[i]) * 0.5;
                        lineWidthTemp[i] = Math.Abs(lEdge[i] - rEdge[i]);
                    }
                    // reference all lines to selected line
                    double lOffset = lEdge[referenceLineIndex];
                    double rOffset = rEdge[referenceLineIndex];
                    double pOffset = linePosTemp[referenceLineIndex];
                    for (int i = 0; i < lEdge.Length; i++)
                    {
                        lEdge[i] = lEdge[i] - lOffset;
                        rEdge[i] = rEdge[i] - rOffset;
                        linePosTemp[i] = linePosTemp[i] - pOffset;
                    }
                    // sum up all results
                    for (int i = 0; i < lEdge.Length; i++)
                    {
                        leftEdgePositions[i] += lEdge[i];
                        rightEdgePositions[i] += rEdge[i];
                        linePositions[i] += linePosTemp[i];
                        lineWidths[i] += lineWidthTemp[i];
                        if (linePosTemp[i] < linePositionsMin[i]) linePositionsMin[i] = linePosTemp[i];
                        if (linePosTemp[i] > linePositionsMax[i]) linePositionsMax[i] = linePosTemp[i];
                        if (lineWidthTemp[i] < lineWidthsMin[i]) lineWidthsMin[i] = lineWidthTemp[i];
                        if (lineWidthTemp[i] > lineWidthsMax[i]) lineWidthsMax[i] = lineWidthTemp[i];
                    }
                    validProfiles++;
                }
            }
            // calculate average and spans
            if (validProfiles == 0) return;
            for (int i = 0; i < leftEdgePositions.Length; i++)
            {
                leftEdgePositions[i] /= (double)validProfiles;
                rightEdgePositions[i] /= (double)validProfiles;
                linePositions[i] /= (double)validProfiles;
                lineWidths[i] /= (double)validProfiles;
                linePositionsSpan[i] = linePositionsMax[i] - linePositionsMin[i];
                lineWidthsSpan[i] = lineWidthsMax[i] - lineWidthsMin[i];
            }
        }

        void Prepare()
        {
            IntensityClassifier ic = new IntensityClassifier(microscopyScan);
            lowerBound = ic.LowerBound;
            upperBound = ic.UpperBound;
            contrast = upperBound - lowerBound;
        }

        double[] GetLeftEdgePositionForProfil(int profileNumber, double threshold)
        {
            double[] profile = microscopyScan.GetNormalizedProfileLine(profileNumber);
            List<double> edgeList = new List<double>();
            for (int i = 1; i < profile.Length - 1; i++)
            {
                if (profile[i - 1] < threshold && profile[i] >= threshold)
                {
                    double x = (double)(i - 1) + (threshold - profile[i - 1]) / (profile[i] - profile[i - 1]);
                    edgeList.Add(x);
                }
            }
            return edgeList.ToArray();
        }

        double[] GetRightEdgePositionForProfil(int profileNumber, double threshold)
        {
            double[] profile = microscopyScan.GetNormalizedProfileLine(profileNumber);
            List<double> edgeList = new List<double>();
            for (int i = 1; i < profile.Length - 1; i++)
            {
                if (profile[i + 1] < threshold && profile[i] >= threshold)
                {
                    double x = (double)(i + 1) - (threshold - profile[i + 1]) / (profile[i] - profile[i + 1]);
                    edgeList.Add(x);
                }
            }
            return edgeList.ToArray();
        }

        private readonly MicroscopyScan microscopyScan;
        private int validProfiles = 0;
        private int totalProfiles = 0;
        private double lowerBound, upperBound;
        private double contrast;
        private double threshold;
        private double reducedThreshold;
        private double[] leftEdgePositions;
        private double[] rightEdgePositions;
        private double[] linePositions;
        private double[] lineWidths;
        private double[] linePositionsSpan;
        private double[] lineWidthsSpan;

    }
}
