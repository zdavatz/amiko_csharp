/*
Copyright (c) 2016 Max Lungarella <cybrmx@gmail.com>

This file is part of AmiKoWindows.

AmiKoDesk is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program. If not, see <http://www.gnu.org/licenses/>.
*/

using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace AmiKoWindows
{
    class FachInfo : INotifyPropertyChanged
    {
        #region Private Fields
        string _cssStr;
        #endregion

        #region Properties
        string CssFilePath { get; set; }
        #endregion

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

        #region Constructors
        public FachInfo()
        {
            // Load important files
            CssFilePath = Utilities.AppExecutingFolder() + Constants.CSS_SHEET;
            if (File.Exists(CssFilePath))
            {
                _cssStr = "<style>" + File.ReadAllText(CssFilePath) + "</style>";
            }
          }
        #endregion

        #region Public Methods
        public void ShowFull(Article a)
        {
            string htmlStr = a.Content;

            string headStr = "<head>" 
                + "<meta http-equiv='Content-Type' content='text/html;charset=UTF-8'>" 
                // + "<link rel='stylesheet' type='text/css' href='http://fonts.googleapis.com/css?family=Roboto&subset=latin,latin-ext'>"
                + _cssStr
                + "</head>"; 
            HtmlText = headStr + htmlStr;

            SetSectionTitles(a);
        }

        public async Task ShowReport()
        {
            string reportPath = Utilities.ReportPath();
            if (File.Exists(reportPath))
            {
                await Task.Run(() =>
                {
                    System.Diagnostics.Process.Start(reportPath);
                });
            }
        }

        public void SetSectionTitles(Article a)
        {
            SectionTitles.Clear();  
            // List of section titles
            List<TitleItem> listOfSectionTitles = a.ListOfSectionTitleItems();
            SectionTitles.AddRange(listOfSectionTitles);
        }

        public void LoadHtmlFromFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                string html = File.ReadAllText(filePath);
                HtmlText = html;
            }
        }
        #endregion
    }
}
