/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using PrusaSL1Reader;

namespace PrusaSL1Viewer
{
    static class Program
    {
        public static SL1File SL1File { get; } = new SL1File();
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

            //file.Load(@"D:\Tiago\Desktop\_Coronavirus-v6-HIRES-Supports.cbddlp");



        //LayerPositionZ: 0, LayerExposure: 35, LayerOffTimeSeconds: 0, DataAddress: 283641, DataSize: 30804, Unknown1: 0, Unknown2: 0, Unknown3: 0, Unknown4: 0
        }
    }
}
