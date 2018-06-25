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
using System.Linq;

namespace AmiKoWindows
{
    public class Medication
    {
        public long articleId { get; set; }
        public long timestamp { get; set; }

        public string Regnrs { get; set; }
        public string Owner { get; set; }
        public string Atccode { get; set; }
        public string Title { get; set; }
        public string Package { get; set; }
        public string Comment { get; set; }
        public string Eancode { get; set; }
        public string ProductName { get; set; }

        public Medication(string eancode, Article article)
        {
            string package = "";
            string ean = "";
            using (var e1 = article.PackInfo.Split('\n').ToList().GetEnumerator())
            using (var e2 = article.Packages.Split('\n').ToList().GetEnumerator())
            {
                while (e1.MoveNext() && e2.MoveNext())
                {
                    string[] packs = e2.Current.Split('|');
                    if (packs.Length > 9)
                        ean = packs[9];

                    if (!ean.Equals(eancode))
                        continue;

                    eancode = ean;
                    package = e1.Current;
                }
            }
            if (!package.Equals(string.Empty) || !ean.Equals(string.Empty))
            {
                this.Regnrs = article.Regnrs;
                this.Owner = article.Author;
                this.Atccode = article.AtcCode;
                this.Title = article.Title;

                this.Package = package;
                this.Comment = "";
                this.Eancode = ean;
                this.ProductName = package.Split(',')?[0];
            }
        }
    }
}
