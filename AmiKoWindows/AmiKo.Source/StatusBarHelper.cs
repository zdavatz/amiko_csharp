/*
Copyright (c) ywesee GmbH

This file is part of AmiKo for Windows.

AmiKo for Windows is free software: you can redistribute it and/or modify
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

using System;
using System.ComponentModel;

namespace AmiKoWindows
{
    /// <summary>
    /// Helper class to bind UI properties to different classes
    /// </summary>
    class StatusBarHelper : INotifyPropertyChanged
    {
        // 
        // Properties
        // 
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _networkStatusText;
        public string NetworkStatusText
        {
            get { return _networkStatusText; }
            set
            {
                if (value != _networkStatusText)
                {
                    _networkStatusText = value;
                    OnPropertyChanged("NetworkStatusText");
                }
            }
        }

        private string _databaseStatusText;
        public string DatabaseStatusText
        {
            get { return _databaseStatusText; }
            set
            {
                if (value != _databaseStatusText)
                {
                    _databaseStatusText = value;
                    OnPropertyChanged("DatabaseStatusText");
                }
            }
        }

        // 
        // Functions
        // 
        public void IsConnectedToInternet()
        {
            NetworkStatusText = Network.CheckForInternetConnection2() ? "Online" : "Offline";
        }

        public void UpdateDatabaseSearchText(Tuple<long, double> result)
        {
            DatabaseStatusText = string.Format("{0} Suchresultate in {1:0.000} Sekunden", result.Item1, result.Item2);
        }
    }
}
