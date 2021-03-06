﻿/*
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
using System.Security.Permissions;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace AmiKoWindows
{
    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    [ComVisible(true)]
    public class FullTextSearch : HtmlBase
    {
        #region Private Fields
        string _cssStr;
        string _jsStr;
        List<Article> _listOfArticles = null;
        Dictionary<string, HashSet<string>> _dictOfRegChapters = null;
        #endregion

        #region Constructors
        public FullTextSearch()
        {
            // Load important files
            string _jsPath = Path.Combine(Utilities.AppExecutingFolder(), Constants.JS_FOLDER, "main_callbacks.js");
            if (File.Exists(_jsPath))
            {
                _jsStr = $"<script language=\"javascript\">{File.ReadAllText(_jsPath)}</script>";
            }
            string _cssFilePath = Path.Combine(Utilities.AppExecutingFolder(), Constants.FULLTEXT_SHEET);
            if (File.Exists(_cssFilePath))
            {
                _cssStr = $"<style>{File.ReadAllText(_cssFilePath)}</style>";
            }

            // Default blank page
            UpdateHtml(Colors.ReplaceStyleForDarkMode("<!DOCTYPE html><head>"
                + "<meta http-equiv='Content-Type' content='text/html;charset=UTF-8'>"
                + _cssStr
                + "</head></html>"));

            UserPreferenceChangedEventHandler e3 = (o, e) =>
            {
                ReloadColors();
            };
            SystemEvents.UserPreferenceChanged += e3;
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
            string headStr = $"<head><meta charset='UTF-8'><meta http-equiv='X-UA-Compatible' content='IE=10'>{_jsStr}{_cssStr}</head>";
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
                    string regnr = a.Regnrs?.Split(',')[0];
                    content_title = $"<a onclick=\"displayFachinfo('{regnr}','{anchor}')\"> <span style=\"font-size:0.8em\"><b>{a.Title}</b></span></a> <span style=\"font-size:0.7em\"> | {a.Author}</span><br>";

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
                                    content_chapters += $"<span style=\"font-size:0.75em; color:#0088BB\"> <a onclick=\"displayFachinfo('{regnr}','{anchor}')\">{chapterStr}</a></span><br>";
                                    filtered = false;
                                }
                                int count = 0;
                                if (chaptersCountMap.ContainsKey(chapterStr))
                                    count = chaptersCountMap[chapterStr];
                                chaptersCountMap[chapterStr] = count + 1;
                            }
                        }
                    }

                    if (!filtered && a.Title != null && a.Title.Length > 1)
                    {
                        string firstLetter = a.Title.Substring(0, 1).ToUpper();
                        if (rows % 2 == 0)
                            content_style = $"<li style=\"background-color:var(--background-color-gray);\" id=\"{firstLetter}\">";
                        else
                            content_style = $"<li style=\"background-color:var(--background-color-normal);\" id=\"{firstLetter}\">";

                        content += content_style + content_title + content_chapters + "</li>";
                        rows++;
                    }
                }
                content += "</ul></div></body>";
                htmlStr = content;
            }

            UpdateHtml($"<!DOCTYPE html><html>{headStr}{htmlStr}</html>");

            // Section titles
            SectionTitleListItems.Clear();
            List<TitleItem> listOfSectionTitles = new List<TitleItem>();
            foreach (var kvp in chaptersCountMap)
            {
                listOfSectionTitles.Add(new TitleItem()
                    {
                        Id = kvp.Key,
                        Title = kvp.Key + " (" + kvp.Value + ")"
                    });
            }
            SectionTitleListItems.AddRange(listOfSectionTitles);
        }

        private void UpdateHtml(string html)
        {
            HtmlText = Colors.ReplaceStyleForDarkMode(html);
        }

        private void ReloadColors()
        {
            if (_listOfArticles != null && _dictOfRegChapters != null)
            {
                ShowTableWithArticles(_listOfArticles, _dictOfRegChapters);
            }
        }
        #endregion
    }
}
