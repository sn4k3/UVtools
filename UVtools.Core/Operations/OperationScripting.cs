/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.IO;
using System.Xml.Serialization;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using UVtools.Core.FileFormats;
using UVtools.Core.Scripting;

namespace UVtools.Core.Operations
{
    [Serializable]
    public sealed class OperationScripting : Operation
    {
        #region Members

        public event EventHandler OnScriptReload;
        private string _filePath;
        private string _scriptText;
        private ScriptState _scriptState;

        #endregion

        #region Overrides

        public override bool CanRunInPartialMode => true;

        public override bool CanHaveProfiles => false;

        public override string Title => "Scripting";

        public override string Description =>
            $"Run external scripts to manipulate the loaded file.\n" +
            $"The scripts have wide access to your system and able to do modifications, read/write files, etc. " +
            $"Make sure to run only the scripts you trust! Or run UVtools in a sandbox while executing this.";

        public override string ConfirmationText =>
            $"run the {ScriptGlobals.Script.Name} script from layers {LayerIndexStart} through {LayerIndexEnd}?";

        public override string ProgressTitle =>
            $"Scripting from layers {LayerIndexStart} through {LayerIndexEnd}";

        public override string ProgressAction => "Scripted layers";

        public override string ValidateInternally()
        {
            if (!CanExecute)
            {
                return "Script is not loaded.";
            }

            var scriptValidation = _scriptState.ContinueWithAsync<string>("return ScriptValidate();").Result;
            return scriptValidation.ReturnValue;
        }

        #endregion

        #region Enums

        #endregion

        #region Properties

        public ScriptGlobals ScriptGlobals { get; private set; }

        public string FilePath
        {
            get => _filePath;
            set
            {
                if (value is null)
                {
                    RaiseAndSetIfChanged(ref _filePath, null);
                }
                else
                {
                    if (!value.EndsWith(".csx") && !value.EndsWith(".cs")) return;
                    if (!File.Exists(value)) return;
                    if (!RaiseAndSetIfChanged(ref _filePath, value)) return;
                }

                RaisePropertyChanged(nameof(HaveFile));
            }
        }

        [XmlIgnore]
        public string ScriptText
        {
            get => _scriptText;
            set => RaiseAndSetIfChanged(ref _scriptText, value);
        }

        [XmlIgnore]
        public bool CanExecute => !string.IsNullOrWhiteSpace(_filePath) && _scriptState is not null;

        [XmlIgnore]
        public bool HaveFile => !string.IsNullOrWhiteSpace(_filePath);

        /*public override string ToString()
        {
            var result = $"[{_infillType}] [Wall: {_wallThickness}px] [B: {_infillBrightness}px] [T: {_infillThickness}px] [S: {_infillSpacing}px]" + LayerRangeString;
            if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
            return result;
        }*/



        #endregion

        #region Constructor

        public OperationScripting() { }

        public OperationScripting(FileFormat slicerFile) : base(slicerFile)
        { }

        #endregion

        #region Equality
        
        #endregion

        #region Methods

        public void ReloadScriptFromFile(string filePath = null)
        {
            if (!string.IsNullOrWhiteSpace(filePath)) FilePath = filePath;
            if (string.IsNullOrWhiteSpace(_filePath) || !File.Exists(_filePath)) return;
            ReloadScriptFromText(File.ReadAllText(_filePath));
        }

        public void ReloadScriptFromText(string text = null)
        {
            if (!string.IsNullOrWhiteSpace(text)) ScriptText = text;
            if (string.IsNullOrWhiteSpace(_scriptText)) return;


            ScriptText = ScriptParser.ParseScriptFromText(_scriptText);

            ScriptGlobals = new ScriptGlobals { SlicerFile = SlicerFile, Operation = this };
            _scriptState = CSharpScript.RunAsync(_scriptText,
                ScriptOptions.Default.AddReferences(typeof(About).Assembly).WithAllowUnsafe(true),
                ScriptGlobals).Result;

            var result = _scriptState.ContinueWithAsync("ScriptInit();").Result;

            RaisePropertyChanged(nameof(CanExecute));
            OnScriptReload?.Invoke(this, EventArgs.Empty);
        }



        protected override bool ExecuteInternally(OperationProgress progress)
        {
            ScriptGlobals.Progress = progress;
            var scriptExecute = _scriptState.ContinueWithAsync<bool>("return ScriptExecute();").Result;
            return !progress.Token.IsCancellationRequested && scriptExecute.ReturnValue;
        }

        #endregion
    }
}
