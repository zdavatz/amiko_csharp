using System.ComponentModel;
using System.Security.Permissions;
using System.Runtime.InteropServices;

namespace AmiKoWindows
{
    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    [ComVisible(true)]
    public class HtmlBase : INotifyPropertyChanged
    {
        #region Event Handlers
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Dependency Properties
        // Source object used for data binding, this is a property
        private string _htmlText;
        public string HtmlText
        {
            get { return _htmlText; }
            set
            {
                if (value != _htmlText)
                {
                    _htmlText = value;
                    OnPropertyChanged("HtmlText");
                }
            }
        }

        private TitlesObservableCollection _sectionTitles = new TitlesObservableCollection();
        public TitlesObservableCollection SectionTitles
        {
            get { return _sectionTitles; }
            private set
            {
                if (value != _sectionTitles)
                {
                    _sectionTitles = value;
                    // OnPropertyChanged is not necessary here...
                }
            }
        }
        #endregion
    }
}
