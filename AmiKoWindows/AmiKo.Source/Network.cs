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

using System.ComponentModel;
using System.Net;
using System.Net.NetworkInformation;

namespace AmiKoWindows
{
    class Network : INotifyPropertyChanged
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

        // 
        // Statics
        //
        public static bool CheckForInternetConnection()
        {
            try
            {
                using (var client = new WebClient())
                using (var stream = client.OpenRead("http://www.google.com"))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool CheckForInternetConnection2()
        {
            try
            {
                Ping ping = new Ping();
                byte[] buffer = new byte[32];
                PingReply reply = ping.Send("google.com", 1000, buffer, new PingOptions());
                return (reply.Status == IPStatus.Success);
            }
            catch
            {
                return false;
            }
        }

        //
        // Functions
        //
        public long PingRoundTripTimeInMilliSeconds(string url)
        {
            Ping ping = new Ping();
            byte[] buffer = new byte[32];
            PingReply reply = ping.Send(url, 1000, buffer, new PingOptions());
            return reply.RoundtripTime;
        }
    }
}
