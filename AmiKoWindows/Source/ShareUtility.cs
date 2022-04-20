using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace AmiKoWindows
{
    class ShareUtility
    {
        /**
         * This class uses API that only exists on Windows 10,
         * Creating an instance on older version throws TypeLoadException with InnerException of PlatformNotSupportedException.
         */
        // https://msdn.microsoft.com/en-us/library/windows/desktop/jj542488(v=vs.85).aspx
        // https://github.com/arunjeetsingh/Build2015/blob/master/Win32ShareSourceSamples/WpfShareSource/MainWindow.xaml.cs

        private string AmkFilePath;
        private Account ActiveAccount;
        private Contact ActiveContact;

        private IDataTransferManagerInterOp _interop = null;
        private IntPtr _handle;
        private List<StorageFile> _outbox = null;
        private DataTransferManager _dataTransferManager;

        public ShareUtility(string amkFilePath, Account ActiveAccount, Contact ActiveContact)
        {
            this.AmkFilePath = amkFilePath;
            this.ActiveAccount = ActiveAccount;
            this.ActiveContact = ActiveContact;
        }

        [System.Runtime.InteropServices.ComImport]
        [System.Runtime.InteropServices.Guid("3A3DCD6C-3EAB-43DC-BCDE-45671CE800C8")]
        [System.Runtime.InteropServices.InterfaceType(
            System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
        interface IDataTransferManagerInterop
        {
            IntPtr GetForWindow([System.Runtime.InteropServices.In] IntPtr appWindow,
                [System.Runtime.InteropServices.In] ref Guid riid);
            void ShowShareUIForWindow(IntPtr appWindow);
        }

        static readonly Guid _dtm_iid =
            new Guid(0xa5caee9b, 0x8708, 0x49d1, 0x8d, 0x36, 0x67, 0xd2, 0x5a, 0x8d, 0xa0, 0x0c);

        public async void Share()
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

            IDataTransferManagerInterop interop = Windows.ApplicationModel.DataTransfer.DataTransferManager.As
                <IDataTransferManagerInterop>();

            IntPtr result = interop.GetForWindow(hWnd, _dtm_iid);
            var dataTransferManager = WinRT.MarshalInterface
                <Windows.ApplicationModel.DataTransfer.DataTransferManager>.FromAbi(result);

            dataTransferManager.DataRequested += OnDataRequested;

            this._outbox = new List<StorageFile>();
            StorageFile file = await StorageFile.GetFileFromPathAsync(this.AmkFilePath);
            _outbox.Add(file);

            // Show the Share UI
            interop.ShowShareUIForWindow(hWnd);
        }

        private void OnDataRequested(DataTransferManager sender, DataRequestedEventArgs e)
        {
            Log.WriteLine(sender.GetType().Name);

            DataRequest req = e.Request;

            if (_outbox.Count > 0)
            {
                var file = _outbox[0] as StorageFile;
                req.Data.Properties.Title = Utilities.GetMailSubject(
                    ActiveContact.Fullname, ActiveContact.Birthdate, ActiveAccount.Fullname
                );
                req.Data.Properties.Description = Path.GetFileName(file.Path);
                req.Data.SetText(Utilities.GetMailBody());
                req.Data.SetStorageItems(_outbox);
            }
        }
    }
}
