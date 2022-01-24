using System;
using Bev.IO.MicroscopyScan;
using System.Drawing;
using System.Collections.Generic;
//using System.Drawing.Imaging;

namespace TestMicroScan
{
    class Program
    {
        static void Main(string[] args)
        {
            string fileName = "SNAP-150956-0013.bmp";

            MicroscopyScan ms = new MicroscopyScan(fileName);

            //ms.Crop(392, 910, 1872, 169);
            //ms.RotateFlip(RotateFlipType.Rotate180FlipNone);

            Console.WriteLine("Number of points per profiles: {0}", ms.NumberOfDataPoints);
            Console.WriteLine("Number of profiles: {0}", ms.NumberOfProfiles);

            ms.SelectChannel(Channel.Brightness);

            Console.WriteLine("Start line detection");
            EdgeDetector ed = new EdgeDetector(ms);
            ed.Evaluate();

            Console.WriteLine();
            Console.WriteLine("Lower bound:       {0:F3}", ed.LowerBound);
            Console.WriteLine("Upper bound:       {0:F3}", ed.UpperBound);
            Console.WriteLine("Threshold:         {0:F3}", ed.Threshold);
            Console.WriteLine("Reduced threshold: {0:F3}", ed.ReducedThreshold);
            Console.WriteLine("{0} valid profiles out of {1}", ed.ValidProfiles, ed.TotalProfiles);
            Console.WriteLine();

            ms.ScanFieldDeltaX = 0.5428433429183818301612782930732;
            ms.ScanFieldDeltaY = 0.5428433429183818301612782930732;

            double scale = ms.ScanFieldDeltaX;

            for (int i = 0; i < ed.LinePositionsSpan.Length; i++)
            {
                double pos = scale * ed.LinePositions[i];
                double wit = scale * ed.LineWidths[i];
                double Upos = scale * ed.LinePositionsSpan[i];
                double Uwit = scale * ed.LineWidthsSpan[i];  
                double dx = pos - 10.0 * i;
                //Console.WriteLine("{0,4} {1,7:F1} µm +/- {2,5:F1} µm", i, dx, Upos);

                Console.WriteLine($"{i,4} {dx,7:F3} {Upos,5:F3}");
            }

            Console.WriteLine();
            
            foreach (var s in ms.MetaData.Keys)
            {
                Console.WriteLine("{0} = {1}", s, ms.MetaData[s]);
            }

        }
    }
}
