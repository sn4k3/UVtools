/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using BinarySerialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using UVtools.Core.EmguCV;
using UVtools.Core.Extensions;
using UVtools.Core.Layers;
using UVtools.Core.Operations;

namespace UVtools.Core.FileFormats
{
    [XmlRoot(ElementName = "svg", Namespace = "http://www.w3.org/2000/svg")]

    public class FlashForgeSVGXSvg
    {
        //public string Xmlns { get; set; } = "http://www.w3.org/2000/svg";

        //[XmlAttribute("svg", Namespace = "http://www.w3.org/2000/svg")] public string Svg { get; set; } = "http://www.w3.org/2000/svg";
        [XmlAttribute("version")] public string Version { get; set; } = "1.1";

        [XmlElement("printparams")] public FlashForgeSVGXSvgPrintParams PrintParameters { get; set; } = new();

        [XmlElement("g")] public List<FlashForgeSVGXSvgGroup> Groups { get; set; } = new();

        public override string ToString()
        {
            return $"{nameof(Version)}: {Version}, {nameof(PrintParameters)}: {PrintParameters}, {nameof(Groups)}: {Groups.Count}";
        }

        public string SerializeToString()
        {
            var settings = new XmlWriterSettings
            {
                // If set to true XmlWriter would close MemoryStream automatically and using would then do double dispose
                // Code analysis does not understand that. That's why there is a suppress message.
                CloseOutput = false,
                Encoding = new UTF8Encoding(false),
                Indent = true,
                NewLineChars = "\n"
            };

            var svg = XmlExtensions.SerializeObject(this, settings);
            return svg.Replace("  ", string.Empty)
                      .Replace("<g id=\"background\" area=\"0\" perimeter=\"0\" />", "<g id=\"background\">\n</g>")
                  + '\n';
        }
    }

    [XmlRoot("printparams")]
    public class FlashForgeSVGXSvgPrintParams
    {
        [XmlAttribute("machinename")] public string MachineName { get; set; } = "Unknown";
        [XmlAttribute("materialname")] public string MaterialName { get; set; } = "Unknown";
        [XmlAttribute("layerheight")] public float LayerHeight { get; set; }
        [XmlAttribute("volume")] public float MaterialMilliliters { get; set; }
        [XmlAttribute("layercount")] public uint LayerCount { get; set; }
        [XmlAttribute("lightintensity")] public float LightIntensity { get; set; } = 1;
        [XmlAttribute("resolutionx")] public uint ResolutionX { get; set; }
        [XmlAttribute("resolutiony")] public uint ResolutionY { get; set; }
        [XmlAttribute("displaywidth")] public float DisplayWidth { get; set; }
        [XmlAttribute("displayheight")] public float DisplayHeight { get; set; }
        [XmlAttribute("machinez")] public float MachineZ { get; set; }

        [XmlElement("projectiontime")] public FlashForgeSVGXSvgProjectionTime ProjectionTime { get; set; } = new();
        [XmlElement("projectionadjust")] public FlashForgeSVGXSvgProjectionAdjust ProjectionAdjust { get; set; } = new();
        [XmlElement("printrange")] public FlashForgeSVGXSvgPrintRange PrintRange { get; set; } = new();

        public override string ToString()
        {
            return $"{nameof(MaterialName)}: {MaterialName}, {nameof(LayerHeight)}: {LayerHeight}, {nameof(MaterialMilliliters)}: {MaterialMilliliters}, {nameof(LayerCount)}: {LayerCount}, {nameof(LightIntensity)}: {LightIntensity}, {nameof(ResolutionX)}: {ResolutionX}, {nameof(ResolutionY)}: {ResolutionY}, {nameof(DisplayWidth)}: {DisplayWidth}, {nameof(DisplayHeight)}: {DisplayHeight}, {nameof(MachineZ)}: {MachineZ}, {nameof(ProjectionTime)}: {ProjectionTime}, {nameof(ProjectionAdjust)}: {ProjectionAdjust}, {nameof(PrintRange)}: {PrintRange}";
        }
    }

    [XmlRoot("projectiontime")]
    public class FlashForgeSVGXSvgProjectionTime
    {
        [XmlAttribute("attachlayer")] public ushort BottomLayerCount { get; set; } = 3;
        [XmlAttribute("buildinlayer")] public ushort TransitionLayerCount { get; set; } = 5; 
        [XmlAttribute("attachtime")] public float BottomExposureTime { get; set; }
        [XmlAttribute("basetime")] public float ExposureTime { get; set; }

        public override string ToString()
        {
            return $"{nameof(BottomLayerCount)}: {BottomLayerCount}, {nameof(TransitionLayerCount)}: {TransitionLayerCount}, {nameof(BottomExposureTime)}: {BottomExposureTime}, {nameof(ExposureTime)}: {ExposureTime}";
        }
    }

    [XmlRoot("projectionadjust")]
    public class FlashForgeSVGXSvgProjectionAdjust
    {
        [XmlAttribute("x")] public float X { get; set; } = 100;
        [XmlAttribute("y")] public float Y { get; set; } = 100;

        public override string ToString()
        {
            return $"{nameof(X)}: {X}, {nameof(Y)}: {Y}";
        }
    }

    [XmlRoot("printrange")]
    public class FlashForgeSVGXSvgPrintRange
    {
        [XmlAttribute("minx")] public float MinX { get; set; }
        [XmlAttribute("miny")] public float MinY { get; set; }
        [XmlAttribute("minz")] public float MinZ { get; set; }
        [XmlAttribute("maxx")] public float MaxX { get; set; }
        [XmlAttribute("maxy")] public float MaxY { get; set; }
        [XmlAttribute("maxz")] public float MaxZ { get; set; }

        public override string ToString()
        {
            return $"{nameof(MinX)}: {MinX}, {nameof(MinY)}: {MinY}, {nameof(MinZ)}: {MinZ}, {nameof(MaxX)}: {MaxX}, {nameof(MaxY)}: {MaxY}, {nameof(MaxZ)}: {MaxZ}";
        }
    }

    [XmlRoot("g")]
    public class FlashForgeSVGXSvgGroup
    {
        [XmlAttribute("id")] public string Id { get; set; }
        [XmlAttribute("area")] public float Area { get; set; }
        [XmlAttribute("perimeter")] public float Perimeter { get; set; }
        [XmlElement("path")] public List<FlashForgeSVGXSvgPath> Paths { get; set; } = new();

        public FlashForgeSVGXSvgGroup() { }

        public FlashForgeSVGXSvgGroup(string id, float area = 0, float perimeter = 0)
        {
            Id = id;
            Area = area;
            Perimeter = perimeter;
        }

        public override string ToString()
        {
            return $"{nameof(Id)}: {Id}, {nameof(Area)}: {Area}, {nameof(Perimeter)}: {Perimeter}, {nameof(Paths)}: {Paths}";
        }
    }

    [XmlRoot("path")]
    public class FlashForgeSVGXSvgPath
    {
        [XmlAttribute("d")] public string Value { get; set; }
        [XmlAttribute("style")] public string Style { get; set; } = "fill:white";
        [XmlAttribute("fill-rule")] public string FillRule { get; set; } = "evenodd";

        public FlashForgeSVGXSvgPath() { }

        public FlashForgeSVGXSvgPath(string value)
        {
            Value = value;
        }

        public override string ToString()
        {
            return $"{nameof(Value)}: {Value}, {nameof(Style)}: {Style}, {nameof(FillRule)}: {FillRule}";
        }
    }


    public class FlashForgeSVGXFile : FileFormat
    {
        #region Constants
        #endregion

        #region Sub Classes
        #region Header
        public class Header
        {
            public const byte IdentifierLength = 16;
            public const string IdentifierText = "DLP-II 1.1\n";
            
            [FieldOrder(0)] [FieldLength(IdentifierLength)] [SerializeAs(SerializedType.TerminatedString)] public string Identifier { get; set; } = IdentifierText;
            [FieldOrder(1)] public uint Preview1Address { get; set; }
            [FieldOrder(2)] public uint Preview2Address { get; set; }
            [FieldOrder(3)] public uint SVGDocumentAddress { get; set; }

            public override string ToString()
            {
                return $"{nameof(Identifier)}: {Identifier}, {nameof(Preview1Address)}: {Preview1Address}, {nameof(Preview2Address)}: {Preview2Address}, {nameof(SVGDocumentAddress)}: {SVGDocumentAddress}";
            }
        }
        #endregion

        #region Preview
        /// <summary>
        /// The files contain two preview images.
        /// These are shown on the printer display when choosing which file to print, sparing the poor printer from needing to render a 3D image from scratch.
        /// </summary>
        public class Preview
        {
            public const byte IdentifierLength = 2;
            public const string IdentifierText = "BM";

            /// <summary>
            /// Gets or sets the identifier, BM = bitmap?
            /// </summary>
            [FieldOrder(0)] [FieldLength(IdentifierLength)] public string Identifier { get; set; } = IdentifierText; 
            
            /// <summary>
            /// Gets or sets the table total size
            /// </summary>
            [FieldOrder(1)] public uint TableSize { get; set; }
            [FieldOrder(2)] public uint Padding1 { get; set; }
            [FieldOrder(3)] public uint DpiX { get; set; } = 54;
            [FieldOrder(4)] public uint DpiY { get; set; } = 40;

            [FieldOrder(5)] public uint ResolutionX { get; set; }

            /// <summary>
            /// Gets the Y dimension of the preview image, in pixels. 
            /// </summary>
            [FieldOrder(6)] public uint ResolutionY { get; set; }

            [FieldOrder(7)] public uint Unknown1 { get; set; } = 0x180001;
            [FieldOrder(8)] public uint Padding2 { get; set; }
            [FieldOrder(9)] public uint DataSize { get; set; }
            [FieldOrder(10)] public uint Unknown2 { get; set; } = 3780;
            [FieldOrder(11)] public uint Unknown3 { get; set; } = 3780;
            [FieldOrder(12)] public uint Padding3 { get; set; }
            [FieldOrder(13)] public uint Padding4 { get; set; }

            [FieldOrder(14)] [FieldLength(nameof(DataSize))] public byte[] BGR { get; set; }

            public override string ToString()
            {
                return $"{nameof(Identifier)}: {Identifier}, {nameof(TableSize)}: {TableSize}, {nameof(Padding1)}: {Padding1}, {nameof(DpiX)}: {DpiX}, {nameof(DpiY)}: {DpiY}, {nameof(ResolutionX)}: {ResolutionX}, {nameof(ResolutionY)}: {ResolutionY}, {nameof(Unknown1)}: {Unknown1}, {nameof(Padding2)}: {Padding2}, {nameof(DataSize)}: {DataSize}, {nameof(Unknown2)}: {Unknown2}, {nameof(Unknown3)}: {Unknown3}, {nameof(Padding3)}: {Padding3}, {nameof(Padding4)}: {Padding4}, {nameof(BGR)}: {BGR.Length}";
            }
        }

        #endregion

        #endregion

        #region Properties

        public Header HeaderSettings { get; protected internal set; } = new();

        public FlashForgeSVGXSvg SVGDocument { get; protected internal set; } = new();

        public override FileFormatType FileType => FileFormatType.Binary;

        public override FileExtension[] FileExtensions { get; } = {
            new (typeof(FlashForgeSVGXFile), "svgx", "Flashforge SVGX"),
        };

        public override PrintParameterModifier[] PrintParameterModifiers { get; } =
        {
            PrintParameterModifier.BottomLayerCount,
            PrintParameterModifier.BottomExposureTime,
            PrintParameterModifier.ExposureTime,
            //PrintParameterModifier.LightPWM,
        };

        public override Size[] ThumbnailsOriginalSize { get; } =
        {
            new(128, 128), 
            new(200, 240)
        };

        public override uint ResolutionX
        {
            get => SVGDocument.PrintParameters.ResolutionX;
            set
            {
                SVGDocument.PrintParameters.ResolutionX = value;
                RaisePropertyChanged();
            }
        }

        public override uint ResolutionY
        {
            get => SVGDocument.PrintParameters.ResolutionY;
            set
            {
                SVGDocument.PrintParameters.ResolutionY = value;
                RaisePropertyChanged();
            }
        }

        public override float DisplayWidth
        {
            get => SVGDocument.PrintParameters.DisplayWidth;
            set
            {
                SVGDocument.PrintParameters.DisplayWidth = (float)Math.Round(value, 2);
                RaisePropertyChanged();
            }
        }


        public override float DisplayHeight
        {
            get => SVGDocument.PrintParameters.DisplayHeight;
            set
            {
                SVGDocument.PrintParameters.DisplayHeight = (float)Math.Round(value, 2);
                RaisePropertyChanged();
            }
        }
        
        public override Enumerations.FlipDirection DisplayMirror
        {
            get => Enumerations.FlipDirection.Vertically;
            set {}
        }

        public override float MachineZ 
        {
            get => SVGDocument.PrintParameters.MachineZ > 0 ? SVGDocument.PrintParameters.MachineZ : base.MachineZ;
            set => base.MachineZ = SVGDocument.PrintParameters.MachineZ = (float)Math.Round(value, 2);
        }

        public override float LayerHeight
        {
            get => SVGDocument.PrintParameters.LayerHeight;
            set
            {
                SVGDocument.PrintParameters.LayerHeight = Layer.RoundHeight(value);
                RaisePropertyChanged();
            }
        }

        public override float PrintHeight
        {
            get => base.PrintHeight;
            set => base.PrintHeight = SVGDocument.PrintParameters.PrintRange.MaxZ = base.PrintHeight;
        }

        public override uint LayerCount
        {
            get => base.LayerCount;
            set => SVGDocument.PrintParameters.LayerCount = base.LayerCount;
        }

        public override ushort BottomLayerCount
        {
            get => SVGDocument.PrintParameters.ProjectionTime.BottomLayerCount;
            set => base.BottomLayerCount = SVGDocument.PrintParameters.ProjectionTime.BottomLayerCount = value;
        }

        public override float BottomExposureTime
        {
            get => SVGDocument.PrintParameters.ProjectionTime.BottomExposureTime;
            set => base.BottomExposureTime = SVGDocument.PrintParameters.ProjectionTime.BottomExposureTime = (float)Math.Round(value, 2);
        }


        public override float ExposureTime
        {
            get => SVGDocument.PrintParameters.ProjectionTime.ExposureTime;
            set => base.ExposureTime = SVGDocument.PrintParameters.ProjectionTime.ExposureTime = (float)Math.Round(value, 2);
        }

        /*public override float BottomLiftHeight
        {
            get => HeaderSettings.BottomLiftHeight;
            set => base.BottomLiftHeight = HeaderSettings.BottomLiftHeight = (float)Math.Round(value, 2);
        }

        public override float LiftHeight
        {
            get => HeaderSettings.LiftHeight;
            set => base.LiftHeight = HeaderSettings.LiftHeight = (float)Math.Round(value, 2);
        }

        public override float BottomLiftSpeed
        {
            get => HeaderSettings.BottomLiftSpeed;
            set => base.BottomLiftSpeed = HeaderSettings.BottomLiftSpeed = (float)Math.Round(value, 2);
        }

        public override float LiftSpeed
        {
            get => HeaderSettings.LiftSpeed;
            set => base.LiftSpeed = HeaderSettings.LiftSpeed = (float)Math.Round(value, 2);
        }

        public override float BottomRetractSpeed => RetractSpeed;

        public override float RetractSpeed
        {
            get => HeaderSettings.RetractSpeed;
            set => base.RetractSpeed = HeaderSettings.RetractSpeed = (float)Math.Round(value, 2);
        }

        public override byte BottomLightPWM
        {
            get => (byte) HeaderSettings.BottomLightPWM;
            set => base.BottomLightPWM = (byte) (HeaderSettings.BottomLightPWM = value);
        }

        public override byte LightPWM
        {
            get => (byte) HeaderSettings.LightPWM;
            set => base.LightPWM = (byte) (HeaderSettings.LightPWM = value);
        }*/

        public override float MaterialMilliliters 
        {
            get => SVGDocument.PrintParameters.MaterialMilliliters;
            set
            {
                base.MaterialMilliliters = value;
                SVGDocument.PrintParameters.MaterialMilliliters = base.MaterialMilliliters;
            }
        }

        public override string MaterialName
        {
            get => SVGDocument.PrintParameters.MaterialName;
            set => base.MaterialName = SVGDocument.PrintParameters.MaterialName = value;
        }

        public override string MachineName
        {
            get => SVGDocument.PrintParameters.MachineName;
            set => base.MachineName = SVGDocument.PrintParameters.MachineName = value;
        }

        public override object[] Configs => new object[] { HeaderSettings, SVGDocument.PrintParameters, SVGDocument.PrintParameters.ProjectionAdjust, SVGDocument.PrintParameters.PrintRange, SVGDocument.PrintParameters.ProjectionTime  };

        #endregion

        #region Constructors
        public FlashForgeSVGXFile()
        {
        }
        #endregion

        #region Methods
        protected override void EncodeInternally(OperationProgress progress)
        {
            if (SVGDocument.PrintParameters.ResolutionX == 0 || SVGDocument.PrintParameters.ResolutionY == 0 ||
                SVGDocument.PrintParameters.DisplayWidth == 0 || SVGDocument.PrintParameters.DisplayHeight == 0)
            {
                throw new FileLoadException("This file does not contain a resolution and/or display size information needed to generate the layer images.\n" +
                                            "Note that FlashDLPrint slicer is unable to output files with the required information to load in here.\n" +
                                            "Please use other compatible slicer capable of output the correct information to load the file in here.", FileFullPath);
            }

            using var outputFile = new FileStream(FileFullPath, FileMode.Create, FileAccess.Write);
            outputFile.Seek(Helpers.Serializer.SizeOf(HeaderSettings), SeekOrigin.Begin);

            HeaderSettings.Preview1Address = 0;
            HeaderSettings.Preview2Address = 0;

            progress.Reset(OperationProgress.StatusEncodePreviews, (uint)ThumbnailsOriginalSize.Length);
            if (Thumbnails is not null)
            {
                foreach (var mat in Thumbnails)
                {
                    if (HeaderSettings.Preview2Address > 0) break;
                    if(mat is null) continue;

                    var preview = new Preview
                    {
                        ResolutionX = (uint)mat.Width,
                        ResolutionY = (uint)mat.Height
                    };

                    using var matFlip = new Mat();
                    CvInvoke.Flip(mat, matFlip, FlipType.Vertical);

                    var bytes = EncodeImage(DATATYPE_BGR888, matFlip);
                    preview.BGR = bytes;
                    preview.DataSize = (uint)bytes.Length;
                    preview.TableSize = (uint)Helpers.Serializer.SizeOf(preview);

                    if (HeaderSettings.Preview1Address == 0)
                    {
                        HeaderSettings.Preview1Address = (uint)outputFile.Position;
                    }
                    else
                    {
                        HeaderSettings.Preview2Address = (uint)outputFile.Position;
                    }

                    outputFile.WriteSerialize(preview);
                    progress++;
                }
            }

            var halfDisplay = Display.Half();
            var ppmm = Ppmm;
            var pixelUm = PixelSizeMicronsMax;

            progress.Reset(OperationProgress.StatusEncodeLayers, LayerCount);
            SVGDocument.Groups = new List<FlashForgeSVGXSvgGroup> { new("background") };
            var groups = new FlashForgeSVGXSvgGroup[LayerCount];

            Parallel.For(0, LayerCount, CoreSettings.ParallelOptions, layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;

                groups[layerIndex] = new FlashForgeSVGXSvgGroup($"layer-{layerIndex}");

                using var mat = this[layerIndex].LayerMat;
                //CvInvoke.Threshold(mat, mat, 127, 255, ThresholdType.Binary); // no AA

                using var contours = mat.FindContours(out var hierarchy, RetrType.Tree);

                float minx = SVGDocument.PrintParameters.PrintRange.MinX;
                float miny = SVGDocument.PrintParameters.PrintRange.MinY;
                float maxx = SVGDocument.PrintParameters.PrintRange.MaxX;
                float maxy = SVGDocument.PrintParameters.PrintRange.MaxY;

                var path = new StringBuilder();
                for (int i = 0; i < contours.Size; i++)
                {
                    if (hierarchy[i, EmguContour.HierarchyParent] == -1) // Top hierarchy
                    {
                        if (path.Length > 0)
                        {
                            groups[layerIndex].Paths.Add(new FlashForgeSVGXSvgPath(path.ToString()));
                        }
                        path.Clear();

                        groups[layerIndex].Area = (float)Math.Round(Math.Cbrt(CvInvoke.ContourArea(contours[i]) / pixelUm), 3);
                        groups[layerIndex].Perimeter = (float)Math.Round(CvInvoke.ArcLength(contours[i], true) / pixelUm, 3);
                    }
                    else
                    {
                        path.Append(' ');
                    }

                    var mmX = (float)Math.Round(contours[i][0].X / ppmm.Width - halfDisplay.Width, 3);
                    var mmY = (float)Math.Round(contours[i][0].Y / ppmm.Height - halfDisplay.Height, 3);

                    minx = Math.Min(minx, mmX);
                    miny = Math.Min(miny, mmY);
                    maxx = Math.Max(maxx, mmX);
                    maxy = Math.Max(maxy, mmY);

                    path.Append($"M {mmX} {mmY} L");
                    for (int x = 1; x < contours[i].Size; x++)
                    {
                        mmX = (float)Math.Round(contours[i][x].X / ppmm.Width - halfDisplay.Width, 3);
                        mmY = (float)Math.Round(contours[i][x].Y / ppmm.Height - halfDisplay.Height, 3);
                        path.Append($" {mmX} {mmY}");

                        minx = Math.Min(minx, mmX);
                        miny = Math.Min(miny, mmY);
                        maxx = Math.Max(maxx, mmX);
                        maxy = Math.Max(maxy, mmY);
                    }
                    path.Append(" Z");
                }

                if (path.Length > 0) // Left over
                {
                    groups[layerIndex].Paths.Add(new FlashForgeSVGXSvgPath(path.ToString()));
                }

                lock (progress.Mutex)
                {
                    SVGDocument.PrintParameters.PrintRange.MinX = Math.Min(SVGDocument.PrintParameters.PrintRange.MinX, minx);
                    SVGDocument.PrintParameters.PrintRange.MinY = Math.Min(SVGDocument.PrintParameters.PrintRange.MinY, miny);
                    SVGDocument.PrintParameters.PrintRange.MaxX = Math.Max(SVGDocument.PrintParameters.PrintRange.MaxX, maxx);
                    SVGDocument.PrintParameters.PrintRange.MaxY = Math.Max(SVGDocument.PrintParameters.PrintRange.MaxY, maxy);
                    progress++;
                }
            });

            SVGDocument.Groups.AddRange(groups);

            HeaderSettings.SVGDocumentAddress = (uint)outputFile.Position;

            outputFile.WriteString(SVGDocument.SerializeToString());

            outputFile.Seek(0, SeekOrigin.Begin);
            outputFile.WriteSerialize(HeaderSettings);
            
            Debug.WriteLine("Encode Results:");
            Debug.WriteLine(HeaderSettings);
            Debug.WriteLine(SVGDocument);
            Debug.WriteLine("-End-");
        }

        protected override void DecodeInternally(OperationProgress progress)
        {
            using var inputFile = new FileStream(FileFullPath, FileMode.Open, FileAccess.Read);
            HeaderSettings = Helpers.Deserialize<Header>(inputFile);
            if (HeaderSettings.Identifier != Header.IdentifierText)
            {
                throw new FileLoadException("Not a valid Flashforge SVGX file!", FileFullPath);
            }

            progress.Reset(OperationProgress.StatusDecodePreviews, ThumbnailsCount);
            Debug.Write("Header -> ");
            Debug.WriteLine(HeaderSettings);

            byte thumbnailCount = 0;
            if (HeaderSettings.Preview1Address > 0)
            {
                inputFile.Seek(HeaderSettings.Preview1Address, SeekOrigin.Begin);
                var preview = Helpers.Deserialize<Preview>(inputFile);
                Thumbnails[thumbnailCount] = DecodeImage(DATATYPE_BGR888, preview.BGR, preview.ResolutionX, preview.ResolutionY);
                CvInvoke.Flip(Thumbnails[thumbnailCount], Thumbnails[thumbnailCount], FlipType.Vertical);
                Debug.Write($"Preview[{thumbnailCount}] -> ");
                Debug.WriteLine(preview);
                thumbnailCount++;

            }
            progress++;
            if (HeaderSettings.Preview2Address > 0)
            {
                inputFile.Seek(HeaderSettings.Preview2Address, SeekOrigin.Begin);
                var preview = Helpers.Deserialize<Preview>(inputFile);
                Thumbnails[thumbnailCount] = DecodeImage(DATATYPE_BGR888, preview.BGR, preview.ResolutionX, preview.ResolutionY);
                CvInvoke.Flip(Thumbnails[thumbnailCount], Thumbnails[thumbnailCount], FlipType.Vertical);
                Debug.Write($"Preview[{thumbnailCount}] -> ");
                Debug.WriteLine(preview);
                thumbnailCount++;

            }
            progress++;

            inputFile.Seek(HeaderSettings.SVGDocumentAddress, SeekOrigin.Begin);
            string svgDocument = Encoding.UTF8.GetString(inputFile.ReadToEnd());
            SVGDocument = XmlExtensions.DeserializeObject<FlashForgeSVGXSvg>(svgDocument);

            Debug.WriteLine(SVGDocument);

            if (SVGDocument.PrintParameters.ResolutionX == 0 || SVGDocument.PrintParameters.ResolutionY == 0 ||
                SVGDocument.PrintParameters.DisplayWidth == 0 || SVGDocument.PrintParameters.DisplayHeight == 0)
            {
                throw new FileLoadException("This file does not contain a resolution and/or display size information needed to generate the layer images.\n" +
                                            "Note that FlashDLPrint slicer is unable to output files with the required information to load in here.\n" +
                                            "Please use other compatible slicer capable of output the correct information to load the file in here.", FileFullPath);
            }

            var halfDisplay = Display.Half();
            var ppmm = Ppmm;

            LayerManager.Init(SVGDocument.PrintParameters.LayerCount, DecodeType == FileDecodeType.Partial);

            if (DecodeType != FileDecodeType.Full) return;
            progress.Reset(OperationProgress.StatusDecodeLayers, LayerCount);
            Parallel.For(0, LayerCount, CoreSettings.ParallelOptions, layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;

                var mat = EmguExtensions.InitMat(Resolution);

                var group = SVGDocument.Groups.FirstOrDefault(g => g.Id == $"layer-{layerIndex}");

                if (@group is not null)
                {
                    var pointsOfPoints = new List<Point[]>();
                    var points = new List<Point>();
                    foreach (var path in @group.Paths)
                    {
                        if (progress.Token.IsCancellationRequested) break;
                        var spaceSplit = path.Value.Split(' ',
                            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                        for (int i = 0; i < spaceSplit.Length; i++)
                        {
                            if (spaceSplit[i] == "M")
                            {
                                if (points.Count > 0)
                                {
                                    pointsOfPoints.Add(points.ToArray());
                                    points.Clear();
                                }

                                continue;
                            }

                            if (spaceSplit[i] == "Z")
                            {
                                if (points.Count > 0)
                                {
                                    pointsOfPoints.Add(points.ToArray());
                                    points.Clear();
                                }

                                continue;
                            }

                            if (spaceSplit[i].Length == 1 && !char.IsDigit(spaceSplit[i][0]))
                                continue; // Ignore any other not processed 1 char that's not a digit (L)

                            if (i + 1 >= spaceSplit.Length) break; // No more to see


                            if (!float.TryParse(spaceSplit[i], out var mmX)) continue;
                            if (!float.TryParse(spaceSplit[++i], out var mmY)) continue;


                            var mmAbsX = Math.Clamp(halfDisplay.Width + mmX, 0, DisplayWidth);
                            var mmAbsY = Math.Clamp(halfDisplay.Height + mmY, 0, DisplayHeight);

                            int x = (int)(mmAbsX * ppmm.Width);
                            int y = (int)(mmAbsY * ppmm.Height);

                            points.Add(new Point(x, y));
                        }

                        if (points.Count > 0) // Leftovers, still this should never happen!
                        {
                            pointsOfPoints.Add(points.ToArray());
                            points.Clear();
                        }
                    }

                    if (pointsOfPoints.Count > 0)
                    {
                        using var vecPoints = new VectorOfVectorOfPoint(pointsOfPoints.ToArray());
                        CvInvoke.DrawContours(mat, vecPoints, -1, EmguExtensions.WhiteColor, -1);
                    }

                }

                progress.Token.ThrowIfCancellationRequested();

                this[layerIndex] = new Layer((uint)layerIndex, mat, this);
                progress.LockAndIncrement();
            });
        }

        protected override void PartialSaveInternally(OperationProgress progress)
        {
            using var outputFile = new FileStream(FileFullPath, FileMode.Open, FileAccess.Write);
            outputFile.Seek(HeaderSettings.SVGDocumentAddress, SeekOrigin.Begin);
            outputFile.SetLength(outputFile.Position);
            outputFile.WriteString(SVGDocument.SerializeToString());
        }

        #endregion
    }
}
