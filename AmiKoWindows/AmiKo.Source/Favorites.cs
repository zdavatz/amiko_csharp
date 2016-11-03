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
using System.IO;
using System.Threading.Tasks;

namespace AmiKoWindows
{
    class Favorites
    {
        HashSet<string> _setOfRegNrs = new HashSet<string>();
        HashSet<Article> _setOfArticles = new HashSet<Article>();
        string _userDataDir;

        public Favorites()
        {
            _userDataDir = Utilities.AppRoamingDataFolder();
            CheckDir(_userDataDir);
            // Load favorites from persistent storage
            Load();
        }

        private static string CheckDir(string dir)
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
        }

        /*
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        */
        public void Load()
        {
            string favoritesFile = Path.Combine(_userDataDir, "favorites.txt");
            if (File.Exists(favoritesFile))
                _setOfRegNrs = FileOps.ReadFromXmlFile<HashSet<string>>(favoritesFile);
        }

        public async Task Save()
        {
            await Task.Run(() =>
            {
                if (_setOfRegNrs.Count > 0)
                {
                    string favoritesFile = Path.Combine(_userDataDir, "favorites.txt");
                    FileOps.WriteToXmlFile<HashSet<string>>(favoritesFile, _setOfRegNrs, false);
                }
            });
        }

        public void Add(Article article)
        {
            _setOfRegNrs.Add(article?.Regnrs);
            _setOfArticles.Add(article);
        }

        public void Remove(Article article)
        {
            _setOfRegNrs.Remove(article?.Regnrs);
            _setOfArticles.RemoveWhere(a => a.Equals(article.Regnrs));
        }

        public bool Contains(string reg)
        {
            return _setOfRegNrs.Contains(reg);
        }

        public bool Contains(Article article)
        {
            return _setOfArticles.Contains(article);
        }
    }
}
