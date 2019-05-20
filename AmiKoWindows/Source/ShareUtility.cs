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

        public async void Share()
        {
            var factory = WindowsRuntimeMarshal.GetActivationFactory(typeof(DataTransferManager));
            this._interop = (IDataTransferManagerInterOp)factory;

            Guid guid = new Guid("a5caee9b-8708-49d1-8d36-67d25a8da00c");
            this._handle = new WindowInteropHelper(Application.Current.MainWindow).Handle;
            DataTransferManager m = null;
            this._interop.GetForWindow(_handle, guid, out m);
            Log.WriteLine("m: {0}", m);
            if (m != null)
            {
                m.DataRequested += OnDataRequested;
                this._dataTransferManager = m;
            }

            this._outbox = new List<StorageFile>();
            StorageFile file = await StorageFile.GetFileFromPathAsync(this.AmkFilePath);
            _outbox.Add(file);

            _interop.ShowShareUIForWindow(_handle);
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
