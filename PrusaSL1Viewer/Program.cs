/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Windows.Forms;
using PrusaSL1Reader;

namespace PrusaSL1Viewer
{
    static class Program
    {
        public static FileFormat SlicerFile { get; set; }
        public static FrmMain FrmMain { get; private set; }
        public static FrmAbout FrmAbout { get; private set; }
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            FrmMain = new FrmMain();
            FrmAbout = new FrmAbout();
            Application.Run(FrmMain);

            //CbddlpFile file = new CbddlpFile();

            //file.Decode(@"D:\Tiago\Desktop\_Coronavirus-v6-HIRES-Supports_NOAA.cbddlp");
            /*file.Decode(@"D:\Tiago\Desktop\coronanew11.cbddlp");
            file.GetLayerImage(0).Save(@"D:\img-new-0.png");
            file.GetLayerImage(10).Save(@"D:\img-new-10.png");
            file.GetLayerImage(20).Save(@"D:\img-new-20.png");
            file.GetLayerImage(50).Save(@"D:\img-new-50.png");*/
        }
    }
}
