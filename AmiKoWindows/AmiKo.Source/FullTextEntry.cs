/*
Copyright (c) 2017 Max Lungarella <cybrmx@gmail.com>

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

namespace AmiKoWindows
{
    public class FullTextEntry
    {
        public string Hash { get; set; }
        public string Keyword { get; set; }
        public string Regnrs { get; set; }
        public Dictionary<string, HashSet<string>> RegChaptersDict { get; set; }
        public bool IsFavorite { get; set; }

        public string GetKeywordPlus()
        {
            return Keyword + " (" + RegChaptersDict.Count + ")";
        }

        public string GetRegnrs()
        {
            string regsStr = "";
            foreach (var k in RegChaptersDict.Keys)
            {
                regsStr += k + ",";
            }
            return regsStr;
        }

        public List<string> GetRegnrsAsList()
        {
            return new List<string>(RegChaptersDict.Keys);
        }

        public HashSet<string> GetChapters(string regnr)
        {
            if (RegChaptersDict.ContainsKey(regnr))
                return RegChaptersDict[regnr];
            return null;
        }
    }
}
