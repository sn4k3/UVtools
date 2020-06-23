/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using ApplicationManagement;
using Emgu.CV;
using UVtools.Core;
using UVtools.Parser;
using UVtools.GUI.Forms;

namespace UVtools.GUI
{
    static class Program
    {
        /// <summary>
        /// Changes fonts of controls contained in font collection recursively. <br/>
        /// <b>Usage:</b> <c><br/>
        /// SetAllControlsFont(this.Controls, 20); // This makes fonts 20% bigger. <br/>
        /// SetAllControlsFont(this.Controls, -4, false); // This makes fonts smaller by 4.</c>
        /// </summary>
        /// <param name="ctrls">Control collection containing controls</param>
        /// <param name="amount">Amount to change: posive value makes it bigger, 
        /// negative value smaller</param>
        /// <param name="amountInPercent">True - grow / shrink in percent, 
        /// False - grow / shrink absolute</param>
        /// <param name="amountType">0 = Absolute | 1 = Partial | 2 = Percent</param>
        public static void SetAllControlsFontSize(
            Control.ControlCollection ctrls,
            int amount = 0, byte amountType = 0)
        {
            if (amount == 0) return;
            foreach (Control ctrl in ctrls)
            {
                // recursive
                SetAllControlsFontSize(ctrl.Controls, amount, amountType);

                var oldSize = ctrl.Font.Size;
                float newSize;
                switch (amountType)
                {
                    case 1:
                        newSize = oldSize + amount;
                        break;
                    case 2:
                        newSize = oldSize + oldSize * (amount / 100);
                        break;
                    default:
                        newSize = amount;
                        break;
                }
                
                if (newSize < 8) newSize = 8; // don't allow less than 8
                var fontFamilyName = ctrl.Font.FontFamily.Name;
                var fontStyle = ctrl.Font.Style;
                ctrl.Font = new Font(fontFamilyName, newSize, fontStyle);
            };
        }

        public static Matrix<byte> KernelStar3x3 { get; } = new Matrix<byte>(new byte[,]
        {
            { 0, 1, 0 },
            { 1, 0, 1 }, 
            { 0, 1, 0 }
        });

        public static Matrix<sbyte> KernelFindIsolated { get; } = new Matrix<sbyte>(new sbyte[,]
        {
            { 0, 1, 0 },
            { 1, -1, 1 },
            { 0, 1, 0 }
        });

        public static FileFormat SlicerFile { get; set; }
        public static FrmMain FrmMain { get; private set; }
        public static FrmAbout FrmAbout { get; private set; }

        public static ExceptionHandler ExceptionHandler { get; private set; }

        public static string[] Args { get; private set; }
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Args = args;
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-GB");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            ExceptionHandler = new ExceptionHandler {MessageBoxButtons = MessageBoxButtons.OK};
            ExceptionHandler.StartHandlingExceptions();

            FrmMain = new FrmMain();
            FrmAbout = new FrmAbout();

            Application.Run(FrmMain);
        }

        public static void NewInstance(string filePath)
        {
            var info = new ProcessStartInfo(Application.ExecutablePath, $"\"{filePath}\"");
            Process.Start(info);
        }
    }
}
