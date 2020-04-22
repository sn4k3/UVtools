/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Globalization;
using System.Threading;
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
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-GB");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            FrmMain = new FrmMain();
            FrmAbout = new FrmAbout();
            Application.Run(FrmMain);
        }
    }
}
