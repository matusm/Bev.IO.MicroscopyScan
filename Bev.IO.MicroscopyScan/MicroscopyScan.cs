using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Xml.Linq;
using System.Globalization;

namespace Bev.IO.MicroscopyScan
{
    public class MicroscopyScan
    {
        private readonly CultureInfo culture = CultureInfo.InvariantCulture;

        public MicroscopyScan(string fileName) : this(new Bitmap(fileName, true))
        {
            string metaDataFileName = fileName + "_meta.xml";
            ApendMetaData("FileName", fileName);
            ReadMetaDataFile(metaDataFileName);
        }

        public MicroscopyScan(Bitmap bitmap)
        {
            BitmapData = bitmap;
            InvalidateMinMax();
            ApendMetaData("ModificationDate", DateTime.UtcNow.ToString(culture)); //TODO format
            scanFieldDeltaX = double.NaN;
            scanFieldDeltaY = double.NaN;
            SelectChannel(Channel.Brightness);
            UpdateDimensionsInMetaData();
        }


        // geometry parameters
        public int NumberOfProfiles => Height;
        public int NumberOfDataPoints => Width;
        // metric parameters
        public double ScanFieldDeltaX { get { return scanFieldDeltaX; } set { SetScanFieldDeltaX(value); } }
        public double ScanFieldDeltaY { get { return scanFieldDeltaY; } set { SetScanFieldDeltaY(value); } }
        public double ScanFieldDimensionX => scanFieldDeltaX * (NumberOfDataPoints - 1);
        public double ScanFieldDimensionY => scanFieldDeltaY * (NumberOfProfiles - 1);
        // additional metadata
        public Dictionary<string, string> MetaData => metaData;
        // specific properties
        public Bitmap BitmapData { get; private set; }
        public int Width => BitmapData.Width;
        public int Height => BitmapData.Height;
        public double GlobalMinimum { get { UpdateMinMax(); return minimumValue; } }
        public double GlobalMaximum { get { UpdateMinMax(); return maximumValue; } }
        public Channel Channel { get { return channel; } set { SelectChannel(value); } }


        public void ApendMetaData(string key, string value)
        {
            if (metaData.ContainsKey(key))
            {
                value = metaData[key] + " + " + value;
            }
            metaData[key] = value;
        }

        public void SelectChannel(Channel channel)
        {
            this.channel = channel;
            InvalidateMinMax();
            metaData["SelectedChannel"] = channel.ToString();
        }
        
        public double GetPixelValue(int x, int y)
        {
            if (x < 0) return double.NaN;
            if (y < 0) return double.NaN;
            if (x >= Width) return double.NaN;
            if (y >= Height) return double.NaN;
            return ExtractChannelFrom(BitmapData.GetPixel(x, y));
        }

        public double GetNormalizedPixelValue(int x, int y)
        {
            return GetNormalizedPixelValue(x, y, GlobalMinimum, GlobalMaximum);
        }

        public double GetNormalizedPixelValue(int x, int y, double low, double high)
        {
            if (low > high)
            {
                double tempHigh = high;
                high = low;
                low = tempHigh;
            }
            return (GetPixelValue(x, y) - low) / (high - low);
        }

        public void RotateFlip(RotateFlipType rotateFlipType)
        {
            BitmapData.RotateFlip(rotateFlipType);
            ApendMetaData("RotateAndFlip", rotateFlipType.ToString());
            UpdateDimensionsInMetaData();
        }

        public void Crop(int fromX, int fromY, int width, int height)
        {
            Rectangle rectangle = new Rectangle(fromX, fromY, width, height);
            Crop(rectangle);
        }

        public void Crop(Rectangle rect)
        {
            PixelFormat format = BitmapData.PixelFormat;
            Bitmap cropped = BitmapData.Clone(rect, format);
            BitmapData = new Bitmap(cropped);
            InvalidateMinMax();
            string rectString = string.Format("({0};{1};{2};{3})", rect.X, rect.Y, rect.Width, rect.Height);
            ApendMetaData("CropedTo", rectString);
            UpdateDimensionsInMetaData();
        }

        public double[] GetProfileLine(int y)
        {
            double[] zValues = new double[Width];
            for (int x = 0; x < Width; x++)
            {
                zValues[x] = GetPixelValue(x, y);
            }
            return zValues;
        }

        public double[] GetLocalNormalizedProfileLine(int y)
        {
            double[] zValues = GetProfileLine(y);
            double low = zValues.Min();
            double high = zValues.Max();
            for (int i = 0; i < zValues.Length; i++)
            {
                zValues[i] = (zValues[i] - low) / (high - low);
            }
            return zValues;
        }

        public double[] GetNormalizedProfileLine(int y)
        {
            double[] zValues = GetProfileLine(y);
            double low = GlobalMinimum;
            double high = GlobalMaximum;
            for (int i = 0; i < zValues.Length; i++)
            {
                zValues[i] = (zValues[i] - low) / (high - low);
            }
            return zValues;
        }

        public double[] GetProfileDistances(double startValue)
        {
            double[] xValues = new double[Width];
            xValues[0] = startValue;
            for (int x = 1; x < Width; x++)
            {
                xValues[x] = xValues[x - 1] + scanFieldDeltaX;
            }
            return xValues;
        }

        public double[] GetProfileDistances()
        {
            return GetProfileDistances(0.0);
        }


        private void UpdateDimensionsInMetaData()
        {
            metaData["PointsPerProfile"] = Width.ToString();
            metaData["Profiles"] = Height.ToString();
        }

        private void SetScanFieldDeltaX(string delta)
        {
            if (double.TryParse(delta, NumberStyles.Number, culture, out double dx))
            {
                SetScanFieldDeltaX(dx);
            }
        }

        private void SetScanFieldDeltaX(double delta)
        {
            scanFieldDeltaX = delta;
            metaData["ScanFieldDeltaX"] = delta.ToString(culture);
        }

        private void SetScanFieldDeltaY(string delta)
        {
            if (double.TryParse(delta, NumberStyles.Number, culture, out double dy))
            {
                SetScanFieldDeltaY(dy);
            }
        }

        private void SetScanFieldDeltaY(double delta)
        {
            scanFieldDeltaY = delta;
            metaData["ScanFieldDeltaY"] = delta.ToString(culture);
        }

        double ExtractChannelFrom(Color color)
        {
            switch (channel)
            {
                case Channel.Brightness:
                    return color.GetBrightness();
                case Channel.Hue:
                    return color.GetHue();
                case Channel.Saturation:
                    return color.GetSaturation();
                case Channel.Red:
                    return color.R;
                case Channel.Green:
                    return color.G;
                case Channel.Blue:
                    return color.B;
                case Channel.Gray:
                    return ((double)color.R + (double)color.G + (double)color.B) / 3.0;
                default:
                    return double.NaN;
            }
        }

        void InvalidateMinMax()
        {
            minimumValue = double.NaN;
            maximumValue = double.NaN;
            minMaxNeedsUpdate = true;
        }

        void UpdateMinMax()
        {
            if (!minMaxNeedsUpdate)
                return;
            InvalidateMinMax();
            if (channel == Channel.None)
                return;
            minimumValue = double.MaxValue;
            maximumValue = double.MinValue;
            double temporaryValue;
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    temporaryValue = GetPixelValue(x, y);
                    if (temporaryValue < minimumValue) minimumValue = temporaryValue;
                    if (temporaryValue > maximumValue) maximumValue = temporaryValue;
                }
            }
            minMaxNeedsUpdate = false;
        }

        double ConvertDpiToMeter(float dpi)
        {
            return 2.54e-2 / dpi;
        }

        void ReadMetaDataFile(string fileName)
        {
            try
            {
                XDocument xdoc = XDocument.Load(fileName);
                foreach (XElement ele in xdoc.Root.Elements())
                {
                    foreach (XElement element in ele.Elements())
                    {
                        switch (element.Name.ToString())
                        {
                            case "V5":
                                metaData["OriginalFileName"] = element.Value;
                                break;
                            case "V13":
                                metaData["Operator"] = element.Value;
                                break;
                            case "V19":
                                metaData["Organisation"] = element.Value;
                                break;
                            case "V25":
                                metaData["PxWidth"] = element.Value;
                                break;
                            case "V26":
                                metaData["PxHeight"] = element.Value;
                                break;
                            case "V29":
                                SetScanFieldDeltaX(element.Value);
                                break;
                            case "V32":
                                SetScanFieldDeltaY(element.Value);
                                break;
                            case "V42":
                                metaData["CaptureDate"] = element.Value;
                                break;
                            case "V43":
                                metaData["CameraType"] = element.Value;
                                break;
                            case "V72":
                                metaData["Camera"] = element.Value;
                                break;
                            case "V74":
                                metaData["Microscope"] = element.Value;
                                break;
                            case "V84":
                                metaData["Objective"] = element.Value;
                                break;
                            case "V85":
                                metaData["ObjectiveType"] = element.Value;
                                break;
                            case "V86":
                                metaData["ObjectiveMagnification"] = element.Value;
                                break;
                            case "V87":
                                metaData["ObjectiveNA"] = element.Value;
                                break;
                            case "V103":
                                metaData["Reflector"] = element.Value;
                                break;
                            case "V104":
                                metaData["IlluminationType"] = element.Value;
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            catch { }
        }


        private readonly Dictionary<string, string> metaData = new Dictionary<string, string>();

        private Channel channel = Channel.None;
        private bool minMaxNeedsUpdate = true;
        private double minimumValue;
        private double maximumValue;

        // metric parameters
        public double scanFieldDeltaX = 1.0;
        public double scanFieldDeltaY = 1.0;
        public double scanFieldDimensionX;
        public double scanFieldDimensionY;

    }

    public enum Channel
    {
        None,
        Brightness,
        Hue,
        Saturation,
        Red,
        Green,
        Blue,
        Gray
    }

}
