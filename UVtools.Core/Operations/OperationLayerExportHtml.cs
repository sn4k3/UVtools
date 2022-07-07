/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using UVtools.Core.EmguCV;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;

namespace UVtools.Core.Operations;

[Serializable]
public sealed class OperationLayerExportHtml : Operation
{
    #region Members
    private string _filePath = null!;
    private bool _exportThumbnails = true;
    private bool _exportLayerSettings = true;
    private bool _exportGCode = true;
    private bool _exportLayerPreview = true;
    private bool _exportRawData = true;

    #endregion

    #region Overrides

    public override LayerRangeSelection StartLayerRangeSelection => LayerRangeSelection.None;
    
    public override bool CanROI => false;
    public override bool CanMask => false;

    public override string IconClass => "fa-brands fa-html5";
    public override string Title => "Export layers to HTML";

    public override string Description =>
        "Export file information and layers to an html file.\n";

    public override string ConfirmationText =>
        $"export layers {LayerIndexStart} through {LayerIndexEnd}?";

    public override string ProgressTitle =>
        $"Exporting layers {LayerIndexStart} through {LayerIndexEnd}";

    public override string ProgressAction => "Exported layers";

    public override string ToString()
    {
        var result = $"[Thumbnails: {_exportThumbnails}]" +
                     $" [Raw data: {_exportRawData}]" +
                     $" [Layers: {_exportLayerSettings}]" +
                     $" [GCode: {_exportGCode}]" +
                     $" [Preview: {_exportLayerPreview}]";
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }

    #endregion

    #region Properties

    public string FilePath
    {
        get => _filePath;
        set => RaiseAndSetIfChanged(ref _filePath, value);
    }

    public bool ExportThumbnails
    {
        get => _exportThumbnails;
        set => RaiseAndSetIfChanged(ref _exportThumbnails, value);
    }

    public bool ExportRawData
    {
        get => _exportRawData;
        set => RaiseAndSetIfChanged(ref _exportRawData, value);
    }

    public bool ExportLayerSettings
    {
        get => _exportLayerSettings;
        set => RaiseAndSetIfChanged(ref _exportLayerSettings, value);
    }

    public bool ExportGCode
    {
        get => _exportGCode;
        set => RaiseAndSetIfChanged(ref _exportGCode, value);
    }

    public bool ExportLayerPreview
    {
        get => _exportLayerPreview;
        set => RaiseAndSetIfChanged(ref _exportLayerPreview, value);
    }

    #endregion

    #region Constructor

    public OperationLayerExportHtml()
    { }

    public OperationLayerExportHtml(FileFormat slicerFile) : base(slicerFile)
    { }

    public override void InitWithSlicerFile()
    {
        _filePath = SlicerFile.FileFullPath + ".html";
    }

    #endregion

    #region Methods

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        using TextWriter html = new StreamWriter(_filePath);
        html.WriteLine("<!doctype html>");
        html.WriteLine("<html lang=\"en\" class=\"h-100\">");
        html.WriteLine("  <head>");
        html.WriteLine("    <meta charset=\"utf-8\">");
        html.WriteLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        html.WriteLine($"    <meta name=\"description\" content=\"{About.SoftwareWithVersion}\">");
        html.WriteLine($"    <meta name=\"author\" content=\"{About.SoftwareWithVersion}\">");
        html.WriteLine($"    <meta name=\"generator\" content=\"{About.SoftwareWithVersion}\">");
        html.WriteLine($"    <title>{About.Software} · {SlicerFile.Filename}</title>");
        html.WriteLine();
        html.WriteLine($"    <link rel=\"UVtools\" href=\"{About.Website}\">");
        html.WriteLine($"    <link rel=\"icon\" type=\"image/png\" sizes=\"32x32\" href=\"data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAYAAABzenr0AAAAGXRFWHRTb2Z0d2FyZQBBZG9iZSBJbWFnZVJlYWR5ccllPAAAAwpJREFUeNrsV01IG0EU/pJILFJxIRCqeFi8BQkMBARPDQqCp20FpbfUgJ5EUfBsDQTEi8ab5pB6LKLGk61iY045RdaTIBo25lAbsKwI2gZCOzNJ1vy4mxTUpdDvseyb2WTeN+9vdywoYhzjXnqbotcbPC2i9AqtYe2QDSxF4+/pLYLnxTIlMW0p7jwGc/DWWnS7WZhjBLwmEiCMgGAiAVhhMv4T0CVwQ0UPuaI0iisqDRNghgciAwimgpC2JUCsfC76REweTfKL6UbIC3m+RvhHmK/JxtVoqp5w+9wY8g0VjIki7tQ77I3uaTtZWFqAIBQKh0QIpHUJ7VQeQs9UDwalQa6zNTNyBiehE2MPOESH7pi5vWS8EVSv1SK01A9Bcj0JVVW18cHywf2CVBLxhDZmOpvTQ2I9UTE+jZ/W/Ia9C35XT94KtwgcBTDbNwuH4qjJEf+2H9fqNTZHN9FKxTAPSB7hozCGu4Zr1nowB7ir1Bak02nklNpMZwZTckrT6yErZwvhU3KNlyHzQFtbG5zE+WB5sth2kS7DUi3BLtr5vZN0NkiAVtZKagWEEO46p+SsSMKJ2ATP6H6pn+tG/aDV24qN1AbXg1+DsBFb/RB4fJ6KTPcv+bFP9rn+i0rv617tGdMXqOiVoUtyaTpbk0gESTlpTOBKqexa58fnWvYyAqxCygkyI9mdLOxUvlF5SYXnBvWki7gqQ6ve1pC0eeD5UD6ROc7AIlrQLrYj9iWGrekt5OQcT6K8ksfZ5RmPJyMSng9jZGoE3e+60Uyasbi7yD2oCipmIjOIrkZx8f0CbuLG7s4u4vNxWH9a65dhqevp1Xgp+dhOme6d82Jsbkx7rigKAn0B2JVCAjLP6IWpSbeLGTSY8vJjenWDYSVcMs6gZ/zRXseqrOJEvu/xiY+Jhv+rG4K/BQtFh7eDJ3H57uuh6bE+LHg+HN7wavjnvohUswlETbQfZQRCJhII2ZJIXtJumC6ekF48o/FRejiN8tcTJSFTEp/KTkmvnsioTK/P7FBaOp7/EWAAocsV0JqWvDkAAAAASUVORK5CYII=\" />");

        html.WriteLine($"    <link href=\"https://cdn.jsdelivr.net/npm/bootstrap@5.2.0-beta1/dist/css/bootstrap.min.css\" rel=\"stylesheet\" integrity=\"sha384-0evHe/X+R7YkIZDRvuzKMRqM+OrBnVFBL6DOitfPri4tjfHxaWutUpFmBp4vmVor\" crossorigin=\"anonymous\">");
        html.WriteLine("    <style>");
        // Start Styles
        html.WriteLine("      main > .container {padding: 30px 0 50px 0;}");
        html.WriteLine("      .table > thead > tr > th, .table > tbody > tr > td {vertical-align: middle;}");
        // End Styles
        html.WriteLine("    </style>");
        html.WriteLine("  </head>");
        html.WriteLine("  <body class=\"d-flex flex-column h-100\">");
        html.WriteLine();
        html.WriteLine($"    <header>");
        // Start Navbar
        html.WriteLine($"      <nav class=\"navbar navbar-expand-md navbar-dark fixed-top bg-dark\">");
        html.WriteLine($"        <div class=\"container\">");
        html.WriteLine($"          <a class=\"navbar-brand\" href=\"#\"><img src=\"data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAYAAABzenr0AAAAGXRFWHRTb2Z0d2FyZQBBZG9iZSBJbWFnZVJlYWR5ccllPAAAAwpJREFUeNrsV01IG0EU/pJILFJxIRCqeFi8BQkMBARPDQqCp20FpbfUgJ5EUfBsDQTEi8ab5pB6LKLGk61iY045RdaTIBo25lAbsKwI2gZCOzNJ1vy4mxTUpdDvseyb2WTeN+9vdywoYhzjXnqbotcbPC2i9AqtYe2QDSxF4+/pLYLnxTIlMW0p7jwGc/DWWnS7WZhjBLwmEiCMgGAiAVhhMv4T0CVwQ0UPuaI0iisqDRNghgciAwimgpC2JUCsfC76REweTfKL6UbIC3m+RvhHmK/JxtVoqp5w+9wY8g0VjIki7tQ77I3uaTtZWFqAIBQKh0QIpHUJ7VQeQs9UDwalQa6zNTNyBiehE2MPOESH7pi5vWS8EVSv1SK01A9Bcj0JVVW18cHywf2CVBLxhDZmOpvTQ2I9UTE+jZ/W/Ia9C35XT94KtwgcBTDbNwuH4qjJEf+2H9fqNTZHN9FKxTAPSB7hozCGu4Zr1nowB7ir1Bak02nklNpMZwZTckrT6yErZwvhU3KNlyHzQFtbG5zE+WB5sth2kS7DUi3BLtr5vZN0NkiAVtZKagWEEO46p+SsSMKJ2ATP6H6pn+tG/aDV24qN1AbXg1+DsBFb/RB4fJ6KTPcv+bFP9rn+i0rv617tGdMXqOiVoUtyaTpbk0gESTlpTOBKqexa58fnWvYyAqxCygkyI9mdLOxUvlF5SYXnBvWki7gqQ6ve1pC0eeD5UD6ROc7AIlrQLrYj9iWGrekt5OQcT6K8ksfZ5RmPJyMSng9jZGoE3e+60Uyasbi7yD2oCipmIjOIrkZx8f0CbuLG7s4u4vNxWH9a65dhqevp1Xgp+dhOme6d82Jsbkx7rigKAn0B2JVCAjLP6IWpSbeLGTSY8vJjenWDYSVcMs6gZ/zRXseqrOJEvu/xiY+Jhv+rG4K/BQtFh7eDJ3H57uuh6bE+LHg+HN7wavjnvohUswlETbQfZQRCJhII2ZJIXtJumC6ekF48o/FRejiN8tcTJSFTEp/KTkmvnsioTK/P7FBaOp7/EWAAocsV0JqWvDkAAAAASUVORK5CYII=\" alt=\"\" width=\"32\" height=\"32\" class=\"d-inline-block align-text-top\"> {About.Software}</a>");
        html.WriteLine($"          <button class=\"navbar-toggler\" type=\"button\" data-bs-toggle=\"collapse\" data-bs-target=\"#navbarCollapse\" aria-controls=\"navbarCollapse\" aria-expanded=\"false\" aria-label=\"Toggle navigation\">");
        html.WriteLine($"            <span class=\"navbar-toggler-icon\"></span>");
        html.WriteLine($"          </button>");
        html.WriteLine($"          <div class=\"collapse navbar-collapse\" id=\"navbarCollapse\">");
        html.WriteLine($"            <ul class=\"navbar-nav me-auto mb-2 mb-md-0\">");
        html.WriteLine($"              <li class=\"nav-item\">");
        html.WriteLine($"                <a class=\"nav-link\" href=\"#Information\">Information</a>");
        html.WriteLine($"              </li>");
        html.WriteLine($"              <li class=\"nav-item\">");
        html.WriteLine($"                <a class=\"nav-link\" href=\"#Settings\">Settings</a>");
        html.WriteLine($"              </li>");
        
        if (_exportRawData && SlicerFile.Configs?.Length > 0)
        {
            html.WriteLine($"              <li class=\"nav-item\">");
            html.WriteLine($"                <a class=\"nav-link\" href=\"#RawData\">Raw data</a>");
            html.WriteLine($"              </li>");
        }
        
        if (_exportLayerSettings && SlicerFile.PrintParameterPerLayerModifiers is not null)
        {
            html.WriteLine($"              <li class=\"nav-item\">");
            html.WriteLine($"                <a class=\"nav-link\" href=\"#Layers\">Layers</a>");
            html.WriteLine($"              </li>");
        }

        if (_exportGCode && SlicerFile.HaveGCode)
        {
            html.WriteLine($"              <li class=\"nav-item\">");
            html.WriteLine($"                <a class=\"nav-link\" href=\"#GCode\">GCode</a>");
            html.WriteLine($"              </li>");
        }

        if (_exportLayerPreview)
        {
            html.WriteLine($"              <li class=\"nav-item\">");
            html.WriteLine($"                <a class=\"nav-link\" href=\"#LayerPreview\">Preview</a>");
            html.WriteLine($"              </li>");
        }

        html.WriteLine($"            </ul>");
        html.WriteLine($"            <span class=\"navbar-text\">{SlicerFile.Filename}</span>");
        html.WriteLine($"          </div>");
        // End Navbar
        html.WriteLine($"        </div>");
        html.WriteLine($"      </nav>");
        // Start Header
        // End Header
        html.WriteLine($"    </header>");
        html.WriteLine();
        html.WriteLine("    <div class=\"p-5 bg-light rounded-3\">");
        html.WriteLine("      <div class=\"container pt-5\">");
        html.WriteLine($"        <h1 class=\"display-5 fw-bold\">{SlicerFile.Filename}</h1>");

        if (_exportThumbnails)
        {
            var thumbnailCount = 0;
            foreach (var thumbnail in SlicerFile.Thumbnails)
            {
                if (thumbnail is null) continue;
                thumbnailCount++;
                html.WriteLine($"        <img src=\"data:image/png;base64,{Convert.ToBase64String(thumbnail.GetPngByes())}\" class=\"img-thumbnail\" alt=\"Thumbnail {thumbnailCount}\">");
            }
        }


        html.WriteLine("      </div>");
        html.WriteLine("    </div>");
        html.WriteLine();
        html.WriteLine("    <main class=\"flex-shrink-0\">");
        html.WriteLine("      <div class=\"container\">");
        // Start Content



        html.WriteLine("      <div class=\"accordion\" id=\"accordionStructure\">");
        html.WriteLine("        <div class=\"accordion-item\">");
        html.WriteLine("          <h2 class=\"accordion-header\" id=\"accordionStructure-InformationHeader\">");
        html.WriteLine("            <a id=\"Information\"></a>");
        html.WriteLine("            <button class=\"accordion-button\" type=\"button\" data-bs-toggle=\"collapse\" data-bs-target=\"#accordionStructure-Information\" aria-expanded=\"true\" aria-controls=\"accordionStructure-Information\">");
        html.WriteLine("            <h1>Information</h1>");
        html.WriteLine("            </button>");
        html.WriteLine("          </h2>");

        html.WriteLine("          <div id=\"accordionStructure-Information\" class=\"accordion-collapse collapse show\" aria-labelledby=\"accordionStructure-InformationHeader\">");
        html.WriteLine("            <div class=\"accordion-body\" style=\"padding:0\">");
        html.WriteLine("              <table class=\"table table-bordered table-striped table-hover table-fluid\">");
        html.WriteLine("                <thead>");
        html.WriteLine("                  <tr>");
        html.WriteLine("                    <th scope=\"col\">Property</th>");
        html.WriteLine("                    <th scope=\"col\">Value</th>");
        html.WriteLine("                  </tr>");
        html.WriteLine("                </thead>");
        html.WriteLine("                <tbody class=\"table-group-divider\">");
                                      
        html.WriteLine("                  <tr>");
        html.WriteLine($"                    <td scope=\"row\">Class</td>");
        html.WriteLine($"                    <td>{SlicerFile.GetType().Name}, Version: {SlicerFile.Version}</td>");
        html.WriteLine("                  </tr>");
                                      
        html.WriteLine("                  <tr>");
        html.WriteLine($"                    <td scope=\"row\">Image resolution</td>");
        html.WriteLine($"                    <td>{SlicerFile.Resolution} px</td>");
        html.WriteLine("                  </tr>");

        if (SlicerFile.DisplayWidth > 0 && SlicerFile.DisplayHeight > 0)
        {
            html.WriteLine("                  <tr>");
            html.WriteLine($"                    <td scope=\"row\">Display size</td>");
            html.WriteLine($"                    <td>{SlicerFile.Display} mm</td>");
            html.WriteLine("                  </tr>");
                                          
            html.WriteLine("                  <tr>");
            html.WriteLine($"                    <td scope=\"row\">Pixel size</td>");
            html.WriteLine($"                    <td>{SlicerFile.PixelSizeMicrons} µm</td>");
            html.WriteLine("                  </tr>");
                                          
            html.WriteLine("                  <tr>");
            html.WriteLine($"                    <td scope=\"row\">Pixels per millimeters</td>");
            html.WriteLine($"                    <td>{SlicerFile.Ppmm} px</td>");
            html.WriteLine("                  </tr>");
        }


        html.WriteLine("                 <tr>");
        html.WriteLine($"                   <td scope=\"row\">Print time</td>");
        html.WriteLine($"                   <td>{SlicerFile.PrintTimeString}</td>");
        html.WriteLine("                 </tr>");


        if (SlicerFile.MaterialMilliliters > 0)
        {
            html.WriteLine("                  <tr>");
            html.WriteLine($"                    <td scope=\"row\">Material milliliters</td>");
            html.WriteLine($"                    <td>{SlicerFile.MaterialMilliliters} ml</td>");
            html.WriteLine("                  </tr>");
        }

        if (SlicerFile.MaterialCost > 0)
        {
            html.WriteLine("                  <tr>");
            html.WriteLine($"                    <td scope=\"row\">Material cost</td>");
            html.WriteLine($"                    <td>{SlicerFile.MaterialCost} $</td>");
            html.WriteLine("                  </tr>");
        }

        if (!string.IsNullOrWhiteSpace(SlicerFile.MaterialName))
        {
            html.WriteLine("                  <tr>");
            html.WriteLine($"                    <td scope=\"row\">Material name</td>");
            html.WriteLine($"                    <td>{SlicerFile.MaterialName}</td>");
            html.WriteLine("                  </tr>");
        }

        if (!string.IsNullOrWhiteSpace(SlicerFile.MachineName))
        {
            html.WriteLine("                  <tr>");
            html.WriteLine($"                    <td scope=\"row\">Machine name</td>");
            html.WriteLine($"                    <td>{SlicerFile.MachineName}</td>");
            html.WriteLine("                  </tr>");
        }

        html.WriteLine("                  <tr>");
        html.WriteLine($"                    <td scope=\"row\">Layer count</td>");
        html.WriteLine($"                    <td>{SlicerFile.LayerCount} layers</td>");
        html.WriteLine("                  </tr>");

        html.WriteLine("                  <tr>");
        html.WriteLine($"                    <td scope=\"row\">Layer height</td>");
        html.WriteLine($"                    <td>{SlicerFile.LayerHeight} mm</td>");
        html.WriteLine("                  </tr>");

        html.WriteLine("                  <tr>");
        html.WriteLine($"                    <td scope=\"row\">Print height</td>");
        html.WriteLine($"                    <td>{SlicerFile.PrintHeight} mm</td>");
        html.WriteLine("                  </tr>");

        html.WriteLine("                  <tr>");
        html.WriteLine($"                    <td scope=\"row\">Print bounding rectangle</td>");
        html.WriteLine($"                    <td>{SlicerFile.BoundingRectangleMillimeters} mm</td>");
        html.WriteLine("                  </tr>");

        html.WriteLine("                  <tr>");
        html.WriteLine($"                    <td scope=\"row\">Print volume</td>");
        html.WriteLine($"                    <td>{SlicerFile.Volume} mm³</td>");
        html.WriteLine("                  </tr>");



        html.WriteLine("                </tbody>");
        html.WriteLine("              </table>");


        html.WriteLine("            </div>");
        html.WriteLine("          </div>");
        html.WriteLine("        </div>");



        if (SlicerFile.PrintParameterModifiers is not null)
        {
            html.WriteLine("        <div class=\"accordion-item\">");
            html.WriteLine("          <h2 class=\"accordion-header\" id=\"accordionStructure-SettingsHeader\">");
            html.WriteLine("            <a id=\"Settings\"></a>");
            html.WriteLine("            <button class=\"accordion-button\" type=\"button\" data-bs-toggle=\"collapse\" data-bs-target=\"#accordionStructure-Settings\" aria-expanded=\"true\" aria-controls=\"accordionStructure-Settings\">");
            html.WriteLine("            <h1>Settings</h1>");
            html.WriteLine("            </button>");
            html.WriteLine("          </h2>");

            html.WriteLine("          <div id=\"accordionStructure-Settings\" class=\"accordion-collapse collapse show\" aria-labelledby=\"accordionStructure-SettingsHeader\">");
            html.WriteLine("            <div class=\"accordion-body\" style=\"padding:0\">");

            html.WriteLine("              <table class=\"table table-bordered table-striped table-hover table-fluid\">");
            html.WriteLine("                <thead>");
            html.WriteLine("                  <tr>");
            html.WriteLine("                    <th scope=\"col\">Property</th>");
            html.WriteLine("                    <th scope=\"col\">Value</th>");
            html.WriteLine("                  </tr>");
            html.WriteLine("                </thead>");
            html.WriteLine("                <tbody class=\"table-group-divider\">");
            SlicerFile.RefreshPrintParametersModifiersValues();
            foreach (var modifier in SlicerFile.PrintParameterModifiers)
            {
                html.WriteLine("                  <tr>");
                html.WriteLine($"                    <td scope=\"row\" data-bs-toggle=\"tooltip\" title=\"{modifier.Description}\">{modifier.Name}</td>");
                html.WriteLine($"                    <td>{modifier.Value.ToString($"F{modifier.DecimalPlates}")} {modifier.ValueUnit}</td>");
                html.WriteLine("                  </tr>");

            }
            html.WriteLine("                </tbody>");
            html.WriteLine("              </table>");
            html.WriteLine("            </div>");
            html.WriteLine("          </div>");
            html.WriteLine("        </div>");
        }


        if (_exportRawData && SlicerFile.Configs?.Length > 0)
        {
            html.WriteLine("        <div class=\"accordion-item\">");
            html.WriteLine("          <h2 class=\"accordion-header\" id=\"accordionStructure-RawDataHeader\">");
            html.WriteLine("            <a id=\"RawData\"></a>");
            html.WriteLine("            <button class=\"accordion-button collapsed\" type=\"button\" data-bs-toggle=\"collapse\" data-bs-target=\"#accordionStructure-RawData\" aria-expanded=\"false\" aria-controls=\"accordionStructure-RawData\">");
            html.WriteLine("            <h1>Raw data</h1>");
            html.WriteLine("            </button>");
            html.WriteLine("          </h2>");

            html.WriteLine("          <div id=\"accordionStructure-RawData\" class=\"accordion-collapse collapse\" aria-labelledby=\"accordionStructure-RawDataHeader\">");
            html.WriteLine("            <div class=\"accordion-body\" style=\"padding:0\">");

            html.WriteLine("              <table class=\"table table-bordered table-striped table-hover table-fluid\">");
            html.WriteLine("                <thead>");
            html.WriteLine("                  <tr>");
            html.WriteLine("                    <th scope=\"col\">Class</th>");
            html.WriteLine("                    <th scope=\"col\">Property</th>");
            html.WriteLine("                    <th scope=\"col\">Value</th>");
            html.WriteLine("                  </tr>");
            html.WriteLine("                </thead>");
            html.WriteLine("                <tbody class=\"table-group-divider\">");

            foreach (var config in SlicerFile.Configs)
            {
                var type = config.GetType();
                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (property.Name.Equals("Item")) continue;
                    html.WriteLine("                  <tr>");
                    html.WriteLine($"                    <td>{type.Name}</td>");
                    html.WriteLine($"                    <td>{property.Name}</td>");
                    var value = property.GetValue(config);
                    switch (value)
                    {
                        case null:
                            continue;
                        case IList list:
                            html.WriteLine($"                    <td>{list.Count}</td>");
                            break;
                        default:
                            html.WriteLine($"                    <td>{value}</td>");
                            break;
                    }
                    
                    html.WriteLine("                  </tr>");
                }
            }

            html.WriteLine("                </tbody>");
            html.WriteLine("              </table>");
            html.WriteLine("            </div>");
            html.WriteLine("          </div>");
            html.WriteLine("        </div>");
        }
        

        if (_exportLayerSettings && SlicerFile.PrintParameterPerLayerModifiers is not null)
        {
            html.WriteLine("        <div class=\"accordion-item\">");
            html.WriteLine("          <h2 class=\"accordion-header\" id=\"accordionStructure-LayersHeader\">");
            html.WriteLine("            <a id=\"Layers\"></a>");
            html.WriteLine("            <button class=\"accordion-button collapsed\" type=\"button\" data-bs-toggle=\"collapse\" data-bs-target=\"#accordionStructure-Layers\" aria-expanded=\"false\" aria-controls=\"accordionStructure-Layers\">");
            html.WriteLine("            <h1>Layers</h1>");
            html.WriteLine("            </button>");
            html.WriteLine("          </h2>");

            html.WriteLine("          <div id=\"accordionStructure-Layers\" class=\"accordion-collapse collapse\" aria-labelledby=\"accordionStructure-LayersHeader\">");
            html.WriteLine("            <div class=\"accordion-body\" style=\"padding:0\">");

            html.WriteLine("              <table class=\"table table-bordered table-striped table-hover table-responsive\">");
            html.WriteLine("                <thead>");
            html.WriteLine("                  <tr>");
            html.WriteLine("                    <th scope=\"col\">#</th>");

            foreach (var modifier in SlicerFile.PrintParameterPerLayerModifiers)
            {
                html.WriteLine($"                    <th scope=\"col\">{modifier.Name}</th>");
            }

            html.WriteLine("                  </tr>");
            html.WriteLine("                </thead>");
            html.WriteLine("                <tbody class=\"table-group-divider\">");
            SlicerFile.RefreshPrintParametersModifiersValues();
            foreach (var layer in SlicerFile)
            {
                html.WriteLine("                  <tr>");
                html.WriteLine($"                    <td scope=\"row\">{layer.Index}</td>");
                SlicerFile.RefreshPrintParametersPerLayerModifiersValues(layer.Index);

                foreach (var modifier in SlicerFile.PrintParameterPerLayerModifiers)
                {
                    html.WriteLine($"                    <td>{modifier.Value.ToString($"F{modifier.DecimalPlates}")} {modifier.ValueUnit}</td>");
                }

                html.WriteLine("                  </tr>");

            }

            html.WriteLine("                </tbody>");
            html.WriteLine("              </table>");
            html.WriteLine("            </div>");
            html.WriteLine("          </div>");
            html.WriteLine("        </div>");
        }

        if (_exportGCode && SlicerFile.HaveGCode)
        {
            html.WriteLine("        <div class=\"accordion-item\">");
            html.WriteLine("          <h2 class=\"accordion-header\" id=\"accordionStructure-GCodeHeader\">");
            html.WriteLine("            <a id=\"GCode\"></a>");
            html.WriteLine("            <button class=\"accordion-button collapsed\" type=\"button\" data-bs-toggle=\"collapse\" data-bs-target=\"#accordionStructure-GCode\" aria-expanded=\"false\" aria-controls=\"accordionStructure-GCode\">");
            html.WriteLine("            <h1>GCode</h1>");
            html.WriteLine("            </button>");
            html.WriteLine("          </h2>");

            html.WriteLine("          <div id=\"accordionStructure-GCode\" class=\"accordion-collapse collapse\" aria-labelledby=\"accordionStructure-GCodeHeader\">");
            html.WriteLine("            <div class=\"accordion-body\">");

            html.WriteLine("              <pre><code>");
            html.WriteLine(SlicerFile.GCodeStr);
            html.WriteLine("              </code></pre>");

            html.WriteLine("            </div>");
            html.WriteLine("          </div>");
            html.WriteLine("        </div>");
        }

        if(_exportLayerPreview)
        {
            html.WriteLine($"        <a id=\"LayerPreview\"></a>");
            html.WriteLine($"        <h1 class=\"mt-4\">Layer preview</h1>");
            html.WriteLine($"        <table class=\"table table-borderless table-sm align-middle w-100\">");
            html.WriteLine($"          <tbody>");
            html.WriteLine($"            <tr>");
            html.WriteLine($"              <td width=\"30\"><a href=\"javascript:;\" onclick=\"showLayer(0)\">0</a></td>");
            html.WriteLine($"              <td><input type=\"range\" class=\"form-range\" min=\"0\" max=\"{SlicerFile.LastLayerIndex}\" step=\"1\" value=\"0\" id=\"layerIndexSlider\" oninput=\"showLayer(this.value)\"></td>");
            html.WriteLine($"              <td class=\"text-end\" width=\"50\"><a href=\"javascript:;\" onclick=\"showLayer({SlicerFile.LastLayerIndex})\">{SlicerFile.LastLayerIndex}</a></td>");
            html.WriteLine($"            </tr>");
            html.WriteLine($"            <tr>");
            html.WriteLine($"              <td id=\"currentLayerIndexText\" colspan=\"3\" class=\"text-center\">0</td>");
            html.WriteLine($"            </tr>");
            html.WriteLine($"          </tbody>");
            html.WriteLine($"        </table>");

            html.WriteLine($"        <svg xmlns=\"http://www.w3.org/2000/svg\" id=\"svgLayer\" viewBox=\"0 0 {SlicerFile.ResolutionX} {SlicerFile.ResolutionY}\">");
            html.WriteLine("          <defs>");
            html.WriteLine("            <style>");
            html.WriteLine("              .background { fill: #000000; }");
            html.WriteLine("              path { fill: #FFFFFF; fill-rule: evenodd; }");
            html.WriteLine("            </style>");
            html.WriteLine("          </defs>");
            html.WriteLine($"         <g id=\"svgLayerGroup\">");
            html.WriteLine($"           <rect class=\"background\" width=\"{SlicerFile.ResolutionX}\" height=\"{SlicerFile.ResolutionY}\"/>");
            html.WriteLine("          </g>");
            html.WriteLine("        </svg>");
        }

        html.WriteLine("      </div>");

        // End Content
        html.WriteLine("      </div>");
        html.WriteLine("    </main>");
        html.WriteLine();
        html.WriteLine("    <footer class=\"footer mt-auto py-3 bg-light\">");
        html.WriteLine("      <div class=\"container\">");
        // Start Footer
        html.WriteLine($"        <span class=\"text-muted\"><a href=\"{About.Website}\" target=\"_blank\">{About.SoftwareWithVersion}</a> · {SlicerFile.Filename}</span>");
        html.WriteLine($"        <span class=\"text-muted float-end\">{DateTime.Now}</span>");
        // End Footer
        html.WriteLine("      </div>");
        html.WriteLine("    </footer>");
        html.WriteLine();
        html.WriteLine("    <script src=\"https://cdn.jsdelivr.net/npm/bootstrap@5.2.0-beta1/dist/js/bootstrap.bundle.min.js\" integrity=\"sha384-pprn3073KE6tl6bjs2QrFaJGz5/SUsLqktiwsUTF55Jfv3qYSDhgCecCxMW52nD2\" crossorigin=\"anonymous\"></script>");
        html.WriteLine("    <script>");
        html.WriteLine("      const firstLayerIndex = 0;");
        html.WriteLine($"      const lastLayerIndex = {SlicerFile.LastLayerIndex};");
        html.WriteLine($"      const layerCount = {SlicerFile.LayerCount};");
        html.WriteLine($"      var currentLayerIndex = firstLayerIndex;");
        html.WriteLine("      const tooltipTriggerList = document.querySelectorAll('[data-bs-toggle=\"tooltip\"]')");
        html.WriteLine("      const tooltipList = [...tooltipTriggerList].map(tooltipTriggerEl => new bootstrap.Tooltip(tooltipTriggerEl))");


        if (_exportLayerPreview)
        {
            html.WriteLine("      const layerSvg = {");

            var layerSvgPath = new string[SlicerFile.LayerCount];
            Parallel.For(0, SlicerFile.LayerCount, CoreSettings.ParallelOptions, layerIndex =>
            {
                using var mat = SlicerFile[layerIndex].LayerMat;
                CvInvoke.Threshold(mat, mat, 127, byte.MaxValue, ThresholdType.Binary); // Remove AA

                using var contours = mat.FindContours(out var hierarchy, RetrType.Tree);
                bool firstTime = true;

                var sb = new StringBuilder();

                sb.AppendFormat($" {layerIndex}: '");
                for (int i = 0; i < contours.Size; i++)
                {
                    if (hierarchy[i, EmguContour.HierarchyParent] == -1) // Top hierarchy
                    {
                        if (firstTime)
                        {
                            firstTime = false;
                        }
                        else
                        {
                            sb.AppendFormat("\"/>");
                        }

                        sb.AppendFormat("<path d=\"");
                    }
                    else
                    {
                        sb.AppendFormat(" ");
                    }

                    sb.AppendFormat($"M {contours[i][0].X} {contours[i][0].Y} L");
                    for (int x = 1; x < contours[i].Size; x++)
                    {
                        sb.AppendFormat($" {contours[i][x].X} {contours[i][x].Y}");
                    }
                    sb.AppendFormat(" Z");
                }

                if (!firstTime) sb.AppendFormat("\"/>");
                sb.AppendFormat("'");
                layerSvgPath[layerIndex] = sb.ToString();
                progress.LockAndIncrement();
            });

            html.WriteLine($"                       {string.Join(',', layerSvgPath)}");
            html.WriteLine("                       };");
            html.WriteLine();
            html.WriteLine("      function showLayer(layerIndex) {");
            html.WriteLine("        if(layerIndex < firstLayerIndex) return false;");
            html.WriteLine("        if(layerIndex > lastLayerIndex) return false;");
            html.WriteLine($"        document.getElementById('svgLayerGroup').innerHTML = '<rect class=\"background\" width=\"{SlicerFile.ResolutionX}\" height=\"{SlicerFile.ResolutionY}\"></rect>' + layerSvg[layerIndex];");
            html.WriteLine($"        document.getElementById('layerIndexSlider').value = layerIndex;");
            html.WriteLine($"        document.getElementById('currentLayerIndexText').innerHTML = layerIndex;");
            html.WriteLine($"        currentLayerIndex = layerIndex;");
            html.WriteLine("        return true;");
            html.WriteLine("      }");
            html.WriteLine("      showLayer(0);");
            html.WriteLine();
        }


        html.WriteLine("    </script>");
        html.WriteLine("  </body>");
        html.WriteLine("</html>");

        return !progress.Token.IsCancellationRequested;
    }


    #endregion

    #region Equality

    private bool Equals(OperationLayerExportHtml other)
    {
        return _filePath == other._filePath && _exportThumbnails == other._exportThumbnails && _exportLayerSettings == other._exportLayerSettings && _exportGCode == other._exportGCode && _exportLayerPreview == other._exportLayerPreview && _exportRawData == other._exportRawData;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is OperationLayerExportHtml other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_filePath, _exportThumbnails, _exportLayerSettings, _exportGCode, _exportLayerPreview, _exportRawData);
    }

    #endregion
}