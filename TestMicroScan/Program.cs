using System;
using Bev.IO.MicroscopyScan;
using System.IO;
using System.Globalization;

namespace TestMicroScan
{
    class Program
    {
        static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            //string fileName = "SNAP-113807-0016.bmp";
            string fileName = Path.ChangeExtension(args[0], ".bmp");
            string outFileName = Path.ChangeExtension(fileName, ".prn");

            MicroscopyScan ms = new MicroscopyScan(fileName);

            //ms.Crop(392, 910, 1872, 169);
            //ms.RotateFlip(RotateFlipType.Rotate180FlipNone);

            Console.WriteLine($"Number of points per profiles: {ms.NumberOfDataPoints}");
            Console.WriteLine($"Number of profiles: {ms.NumberOfProfiles}");

            ms.SelectChannel(Channel.Brightness);

            Console.WriteLine("Start line detection");
            EdgeDetector ed = new EdgeDetector(ms);
            ed.Evaluate();

            Console.WriteLine();
            Console.WriteLine($"Lower bound:       {ed.LowerBound:F3}");
            Console.WriteLine($"Upper bound:       {ed.UpperBound:F3}");
            Console.WriteLine($"Threshold:         {ed.Threshold:F3}");
            Console.WriteLine($"Reduced threshold: { ed.ReducedThreshold:F3}");
            Console.WriteLine($"{ed.ValidProfiles} valid profiles out of {ed.TotalProfiles}");
            Console.WriteLine();

            ms.ScanFieldDeltaX = 0.5428433429183818301612782930732;
            ms.ScanFieldDeltaY = 0.5428433429183818301612782930732;

            using (StreamWriter writer = new StreamWriter(outFileName, false))
            {
                writer.WriteLine("===============");
                foreach (var s in ms.MetaData.Keys)
                {
                    writer.WriteLine("{0} = {1}", s, ms.MetaData[s]);
                }
                writer.WriteLine("===============");
                writer.WriteLine($"Lower bound = {ed.LowerBound:F3}");
                writer.WriteLine($"Upper bound = {ed.UpperBound:F3}");
                writer.WriteLine($"Threshold = {ed.Threshold:F3}");
                writer.WriteLine($"Reduced threshold = {ed.ReducedThreshold:F3}");
                writer.WriteLine($"{ed.ValidProfiles} valid profiles out of {ed.TotalProfiles}");
                writer.WriteLine("===============");
                double scale = ms.ScanFieldDeltaX;
                for (int i = 0; i < ed.LinePositionsSpan.Length; i++)
                {
                    double pos = scale * ed.LinePositions[i];
                    double wit = scale * ed.LineWidths[i];
                    double Upos = scale * ed.LinePositionsSpan[i];
                    double Uwit = scale * ed.LineWidthsSpan[i];
                    double dx = pos - 10.0 * i;
                    //Console.WriteLine("{0,4} {1,7:F1} µm +/- {2,5:F1} µm", i, dx, Upos);
                    writer.WriteLine($"{i,4} {dx,7:F3} {Upos,5:F3}");
                    Console.WriteLine($"{i,4} {dx,7:F3} {Upos,5:F3}");
                }
            }
        }
    }
}
