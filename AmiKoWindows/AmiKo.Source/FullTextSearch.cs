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

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Permissions;
using System.Runtime.InteropServices;

namespace AmiKoWindows
{
    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    [ComVisible(true)]
    public class FullTextSearch : HtmlBase
    {
        #region Private Fields
        string _cssStr;
        string _jscriptStr;
        List<Article> _listOfArticles = null;
        Dictionary<string, HashSet<string>> _dictOfRegChapters = null;
        #endregion

        #region Constructors
        public FullTextSearch()
        {
            // Load important files
            string _jscriptPath = Path.Combine(Utilities.AppExecutingFolder(), Constants.JS_FOLDER, "main_callbacks.js");
            if (File.Exists(_jscriptPath))
            {
                _jscriptStr = "<script language=\"javascript\">" + File.ReadAllText(_jscriptPath) + "</script>";
            }
            string _cssFilePath = Path.Combine(Utilities.AppExecutingFolder(), Constants.FULLTEXT_SHEET);
            if (File.Exists(_cssFilePath))
            {
                _cssStr = "<style>" + File.ReadAllText(_cssFilePath) + "</style>";
            }

        }

        public FullTextSearch(List<Article> listOfArticles)
        {
            _listOfArticles = listOfArticles;
        }
        #endregion

        #region Dependency Properties
        public string Filter { get; set; }
        #endregion

        #region Public Methods
        public void UpdateTable()
        {
            ShowTableWithArticles(_listOfArticles, _dictOfRegChapters);
        }

        public void ShowTableWithArticles(List<Article> listOfArticles, Dictionary<string, HashSet<string>> dictOfRegChapters)
        {
            Dictionary<string, int> chaptersCountMap = new Dictionary<string, int>();

            _listOfArticles = listOfArticles;
            _dictOfRegChapters = dictOfRegChapters;

            string htmlStr = "";
            string headStr = "<head>"
                + "<meta http-equiv='Content-Type' content='text/html;charset=UTF-8'>"
                + _jscriptStr
                + _cssStr
                + "</head>";
            if (_listOfArticles != null)
            {
                int rows = 0;
                string content = "<body><div id=\"fulltext\"><ul>";
                foreach (var a in _listOfArticles)
                {
                    string content_style = "";
                    string content_title = a.Title;
                    string content_chapters = "";

                    string anchor = "?";
                    string key = "";
                    string regnr = a.Regnrs?.Split(',')[0];
                    content_title = "<a onclick=\"displayFachinfo('" + regnr + "','" + key + "','" + anchor + "')\">"
                        + "<span style=\"font-size:0.8em\"><b>" + a.Title + "</b></span></a>"
                        + "<span style=\"font-size:0.7em\"> | " + a.Author + "</span><br>";

                    bool filtered = true;
                    if (_dictOfRegChapters.ContainsKey(regnr))
                    {
                        var indexToTitlesMap = a.IndexToTitlesMap();
                        var chapters = _dictOfRegChapters[regnr];
                        foreach (string ch in chapters)
                        {
                            int c = Convert.ToInt32(ch);
                            if (indexToTitlesMap.ContainsKey(c))
                            {
                                string chapterStr = indexToTitlesMap[c];
                                if (chapterStr.Equals(Filter) || Filter.Length == 0)
                                {
                                    anchor = "section" + c;
                                    if (c > 100)
                                    {
                                        // These are "old" section titles, e.g. Section7900, Section8000, etc.
                                        anchor = "Section" + c;
                                    }
                                    content_chapters += "<span style=\"font-size:0.75em; color:#0099cc\">"
                                        + "<a onclick=\"displayFachinfo('" + regnr + "','" + key + "','" + anchor + "')\">" + chapterStr + "</a>"
                                        + "</span><br>";
                                    filtered = false;
                                }
                                int count = 0;
                                if (chaptersCountMap.ContainsKey(chapterStr))
                                    count = chaptersCountMap[chapterStr];
                                chaptersCountMap[chapterStr] = count + 1;
                            }
                        }
                    }

                    if (!filtered)
                    {
                        string firstLetter = a.Title.Substring(0, 1).ToUpper();
                        if (rows % 2 == 0)
                            content_style = "<li style=\"background-color:whitesmoke;\" id=\"" + firstLetter + "\">";
                        else
                            content_style = "<li style=\"background-color:white;\" id=\"" + firstLetter + "\">";

                        content += content_style + content_title + content_chapters + "</li>";
                        rows++;
                    }
                }
                content += "</ul></div></body>";
                htmlStr = content;
            }

            HtmlText = "<!DOCTYPE html>"
                + "<html>" 
                + headStr + htmlStr 
                + "</html>";

            // Section titles
            SectionTitles.Clear();
            List<TitleItem> listOfSectionTitles = new List<TitleItem>();
            foreach (var kvp in chaptersCountMap)
            {
                listOfSectionTitles.Add(new TitleItem()
                    {
                        Id = kvp.Key,
                        Title = kvp.Key + " (" + kvp.Value + ")"
                    });
            }
            SectionTitles.AddRange(listOfSectionTitles);
        }
        #endregion
    }
}
