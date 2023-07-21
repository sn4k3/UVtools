using Avalonia;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;
using Projektanker.Icons.Avalonia.MaterialDesign;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using UVtools.Core;
using UVtools.Core.SystemOS;

namespace UVtools.UI;

public static class Program
{
    private static bool _isDebug;

    public static string[] Args = Array.Empty<string>();
    
    public static bool IsDebug
    {
        get
        {
#if DEBUG
            return true;
#else
            return _isDebug;
#endif
        }
        set => _isDebug = value;
    }

    public static bool IsCrashReport;

    public static Stopwatch ProgramStartupTime = null!;
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentUICulture =
        CultureInfo.DefaultThreadCurrentCulture = CoreSettings.OptimalCultureInfo;
        ProgramStartupTime = Stopwatch.StartNew();
        Args = args;
        try
        {
            if (ConsoleArguments.ParseArgs(args)) return;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return;
        }

        if (Args.Length >= 1)
        {
            switch (Args[0])
            {
                case "--debug":
                    IsDebug = true;
                    break;
                case "--crash-report" when Args.Length >= 3:
                    IsCrashReport = true;
                    break;
            }
        }


        /*using var mat = CvInvoke.Imread(@"D:\layer0.png", ImreadModes.Grayscale);
        var contours = mat.FindContours(out var hierarchy, RetrType.Tree, ChainApproxMethod.ChainApproxTc89Kcos);

        var triangulator = new Incremental();

        var poly = new Polygon();

        // Generate mesh.
        var points = new List<Vertex>();

        for (int i = 0; i < contours.Size; i++)
        for (int x = 0; x < contours[i].Size; x++)
        {
            points.Add(new Vertex(contours[i][x].X, contours[i][x].Y));
        }
        
        //var mesh = triangulator.Triangulate(points, new Configuration());
        //var triangles = mesh.Triangles.ToArray();


        using var stl = new STLMeshFile(@"D:\test.stl", FileMode.Create);
        stl.BeginWrite();

        var groups = EmguContours.GetPositiveContoursInGroups(contours, hierarchy);

        foreach (var group in groups)
        {
            var z = 0;
            for (int i = 0; i < group.Size; i++)
            {
                var list = new List<Vertex>();

                
                for (int x = 0; x < contours[i].Size; x++)
                {
                    //list.Add(contours[i][x]);
                    list.Add(new Vertex(contours[i][x].X, contours[i][x].Y));
                }

                poly.Add(new Contour(list), i > 0);

                /*using var sub = new Subdiv2D(list.ToArray());
                var triangles = sub.GetDelaunayTriangles();

                foreach (var triangle2Df in triangles)
                {
                    stl.WriteTriangle(
                        new Vector3(triangle2Df.V0.X, triangle2Df.V0.Y, z),
                        new Vector3(triangle2Df.V1.X, triangle2Df.V1.Y, z),
                        new Vector3(triangle2Df.V2.X, triangle2Df.V2.Y, z),
                        new Vector3(triangle2Df.Centeroid.X, triangle2Df.Centeroid.Y, z)
                    );
                }*/



        //z++;
        /* }


         //break;
     }

     var mesh = poly.Triangulate(new ConstraintOptions());
     for (int i = 0; i < 50; i++)
     {
         foreach (var meshTriangle in mesh.Triangles)
         {
             stl.WriteTriangle(
                 new Vector3((float)meshTriangle.GetVertex(0).X, (float)meshTriangle.GetVertex(0).Y, i),
                 new Vector3((float)meshTriangle.GetVertex(1).X, (float)meshTriangle.GetVertex(1).Y, i),
                 new Vector3((float)meshTriangle.GetVertex(2).X, (float)meshTriangle.GetVertex(2).Y, i),
                 new Vector3(1f, 1f, i)
             );
         }
     }


     stl.EndWrite();
     return;*/

        /*Slicer slicer = new(Size.Empty, SizeF.Empty, "D:\\Cube100x100x100.stl");
        var slices = slicer.SliceModel(0.05f);
    
        foreach (var slice in slices)
        {
            using var mat = EmguExtensions.InitMat(new Size(1000, 1000));
            var contour = slice.Value.ToContour();
            using var vec = new VectorOfPoint(contour);
            CvInvoke.FillPoly(mat, vec, EmguExtensions.WhiteColor, LineType.AntiAlias);
            mat.Save(@$"D:\SLICE\{slice.Key}.png");
        }*/

        // PrusaSlicer to Machine.cs
        //var machines = Machine.GetMachinesFromPrusaSlicer();
        //var machinesText = Machine.GenerateMachinePresetsFromPrusaSlicer();

        // Add the event handler for handling non-UI thread exceptions to the event.
        AppDomain.CurrentDomain.UnhandledException += (sender, e) => HandleUnhandledException("Non-UI", (Exception)e.ExceptionObject);
        TaskScheduler.UnobservedTaskException += (sender, e) => HandleUnhandledException("Task", e.Exception);
        //AppDomain.CurrentDomain.FirstChanceException += CurrentDomainOnFirstChanceException;

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e)
        {
            HandleUnhandledException("Application", e);
        }
        

        // Closing
    }

    private static void HandleUnhandledException(string category, Exception ex)
    {
        ErrorLog.AppendLine($"Fatal {category} Error", ex.ToString());

        if (!IsCrashReport)
        {
            try
            {
                string? file = null;
                if (App.SlicerFile is not null)
                {
                    file = $"{App.SlicerFile.Filename}  [Version: {App.SlicerFile.Version}] [Class: {App.SlicerFile.GetType().Name}]";
                }
                SystemAware.StartThisApplication($"--crash-report \"{category}\" \"{ex}\" \"{file}\"");
                //var errorMsg = $"An application error occurred. Please contact the administrator with the following information:\n\n{ex}";
                //await App.MainWindow.MessageBoxError(errorMsg, "Fatal Non-UI Error");
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
                Console.WriteLine(exception);
            }
        }

        Environment.Exit(-1);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        IconProvider.Current
            .Register<FontAwesomeIconProvider>()
            .Register<MaterialDesignIconProvider>();

        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new SkiaOptions { MaxGpuResourceSizeBytes = 256_000_000 })
            .With(new Win32PlatformOptions())
            .With(new X11PlatformOptions())
            .With(new MacOSPlatformOptions { ShowInDock = true })
            .With(new AvaloniaNativePlatformOptions())
            //.UseSkia()
            .LogToTrace();
    }
}