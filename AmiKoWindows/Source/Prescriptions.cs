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
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Threading.Tasks;

namespace AmiKoWindows
{
    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    [ComVisible(true)]
    class Prescriptions : HtmlBase
    {
        HashSet<string> _setOfIds = new HashSet<string>();
        HashSet<string> _setOfEntries = new HashSet<string>();

        string _userDataDir;

        public Prescriptions()
        {
            _userDataDir = Utilities.AppRoamingDataFolder();
            CheckDir(_userDataDir);
            // Load prescriptions from persistent storage
            Load();
            // TODO
            HtmlText = "";
        }

        private static string CheckDir(string dir)
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
        }

        public List<string> Ids()
        {
            return new List<string>(_setOfIds);
        }

        public void Load()
        {
            // TODO
        }

        public void ShowDetail()
        {
            HtmlText = "";
        }

        public async Task Save()
        {
            await Task.Run(() =>
            {
              // TODO
            });
        }

        public void Add(string entry)
        {
            // TODO
        }

        public void Remove(string entry)
        {
            // TODO
        }

        public bool Contains(string key)
        {
            return _setOfIds.Contains(key);
        }
    }
}
