/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Diagnostics;
using UVtools.Core.Extensions;
using UVtools.Core.Objects;

namespace UVtools.Core.Network
{
    public class RemotePrinter : BindableBase
    {
        #region Members
        private bool _isEnabled;
        private string _name;
        private string _host = "0.0.0.0";
        private string _compatibleExtensions;
        private ushort _port;

        private RemotePrinterRequest _requestUploadFile  = new(RemotePrinterRequest.RequestType.UploadFile,  RemotePrinterRequest.RequestMethod.PUT);
        private RemotePrinterRequest _requestPrintFile   = new(RemotePrinterRequest.RequestType.PrintFile,   RemotePrinterRequest.RequestMethod.GET);
        private RemotePrinterRequest _requestDeleteFile  = new(RemotePrinterRequest.RequestType.DeleteFile,  RemotePrinterRequest.RequestMethod.GET);
        private RemotePrinterRequest _requestPausePrint  = new(RemotePrinterRequest.RequestType.PausePrint,  RemotePrinterRequest.RequestMethod.GET);
        private RemotePrinterRequest _requestResumePrint = new(RemotePrinterRequest.RequestType.ResumePrint, RemotePrinterRequest.RequestMethod.GET);
        private RemotePrinterRequest _requestStopPrint   = new(RemotePrinterRequest.RequestType.StopPrint,   RemotePrinterRequest.RequestMethod.GET);
        private RemotePrinterRequest _requestGetFiles    = new(RemotePrinterRequest.RequestType.GetFiles,    RemotePrinterRequest.RequestMethod.GET);
        private RemotePrinterRequest _requestPrintStatus = new(RemotePrinterRequest.RequestType.PrintStatus, RemotePrinterRequest.RequestMethod.GET);
        private RemotePrinterRequest _requestPrinterInfo = new(RemotePrinterRequest.RequestType.PrinterInfo, RemotePrinterRequest.RequestMethod.GET);

        //private List<RemotePrinterRequest> _requests = new();
        #endregion

        #region Properties

        public bool IsEnabled
        {
            get => _isEnabled;
            set => RaiseAndSetIfChanged(ref _isEnabled, value);
        }

        /// <summary>
        /// Gets or sets the alias name for this printer.
        /// Not used on requests
        /// </summary>
        public string Name
        {
            get => _name;
            set => RaiseAndSetIfChanged(ref _name, value);
        }

        /// <summary>
        /// Gets or sets the host/ip for the requests
        /// </summary>
        public string Host
        {
            get => _host;
            set => RaiseAndSetIfChanged(ref _host, value?.Trim());
        }

        /// <summary>
        /// Gets or sets the host port for the requests.
        /// Use 0 to not use a port
        /// </summary>
        public ushort Port
        {
            get => _port;
            set => RaiseAndSetIfChanged(ref _port, value);
        }

        public string HostUrl
        {
            get
            {
                var result = $"http://{_host}";
                if (_port > 0) result += $":{_port}";
                return result;
            }
        }

        /// <summary>
        /// Gets or sets the compatible extensions with this device.
        /// Empty or null to be compatible with everything
        /// </summary>
        public string CompatibleExtensions
        {
            get => _compatibleExtensions;
            set => RaiseAndSetIfChanged(ref _compatibleExtensions, value);
        }

        /// <summary>
        /// Gets if this host is valid
        /// </summary>
        public bool IsValid => !string.IsNullOrEmpty(_host) && _host != "0.0.0.0";

        public RemotePrinterRequest RequestUploadFile
        {
            get => _requestUploadFile;
            set => RaiseAndSetIfChanged(ref _requestUploadFile, value);
        }

        public RemotePrinterRequest RequestPrintFile
        {
            get => _requestPrintFile;
            set => RaiseAndSetIfChanged(ref _requestPrintFile, value);
        }

        public RemotePrinterRequest RequestDeleteFile
        {
            get => _requestDeleteFile;
            set => RaiseAndSetIfChanged(ref _requestDeleteFile, value);
        }

        public RemotePrinterRequest RequestPausePrint
        {
            get => _requestPausePrint;
            set => RaiseAndSetIfChanged(ref _requestPausePrint, value);
        }

        public RemotePrinterRequest RequestResumePrint
        {
            get => _requestResumePrint;
            set => RaiseAndSetIfChanged(ref _requestResumePrint, value);
        }

        public RemotePrinterRequest RequestStopPrint
        {
            get => _requestStopPrint;
            set => RaiseAndSetIfChanged(ref _requestStopPrint, value);
        }

        public RemotePrinterRequest RequestGetFiles
        {
            get => _requestGetFiles;
            set => RaiseAndSetIfChanged(ref _requestGetFiles, value);
        }

        public RemotePrinterRequest RequestPrintStatus
        {
            get => _requestPrintStatus;
            set => RaiseAndSetIfChanged(ref _requestPrintStatus, value);
        }

        public RemotePrinterRequest RequestPrinterInfo
        {
            get => _requestPrinterInfo;
            set => RaiseAndSetIfChanged(ref _requestPrinterInfo, value);
        }


        public RemotePrinterRequest[] Requests => new []
        {
            _requestUploadFile, 
            _requestPrintFile,
            _requestDeleteFile,
            _requestPausePrint,
            _requestResumePrint,
            _requestStopPrint,
            _requestGetFiles,
            _requestPrintStatus,
            _requestPrinterInfo,
        };

        /*
        /// <summary>
        /// Gets or sets the available requests for this printer
        /// </summary>
        public List<RemotePrinterRequest> Requests
        {
            get => _requests;
            set => RaiseAndSetIfChanged(ref _requests, value);
        }*/

        #endregion

        #region Constructor

        public RemotePrinter()
        {
        }

        public RemotePrinter(string host = "0.0.0.0", ushort port = 0, string name = "", bool isEnabled = false)
        {
            _isEnabled = isEnabled;
            _name = name;
            _host = host;
            _port = port;
        }

        #endregion

        #region Methods

        public override string ToString()
        {
            var result = HostUrl;
            if (!string.IsNullOrWhiteSpace(_name)) result += $"  ({_name})";
            return result;
        }

        public RemotePrinter Clone()
        {
            return this.CloneByXmlSerialization();
        }

        #endregion


    }
}
