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

using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Threading.Tasks;

namespace AmiKoWindows
{
    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    [ComVisible(true)]
    public class FachInfo : HtmlBase
    {
        #region Private Fields
        string _jsStr;
        string _cssStr;
        MainWindow _mainWindow;
        MainSqlDb _sqlDb;
        string htmlPreColor;
        Article currentArticle;
        #endregion

        #region Properties
        string JsPath { get; set; }
        string CssFilePath { get; set; }
        #endregion

        #region Constructors
        public FachInfo(MainWindow mainWindow, MainSqlDb sqlDb)
        {
            _mainWindow = mainWindow;
            _sqlDb = sqlDb;

            // Load important files
            JsPath = Path.Combine(Utilities.AppExecutingFolder(), Constants.JS_FOLDER, "main_callbacks.js");
            if (File.Exists(JsPath))
            {
                _jsStr = "<script language=\"javascript\">" + File.ReadAllText(JsPath) + "</script>";
            }
            CssFilePath = Path.Combine(Utilities.AppExecutingFolder(), Constants.CSS_SHEET);
            if (File.Exists(CssFilePath))
            {
                _cssStr = "<style>" + File.ReadAllText(CssFilePath) + "</style>";
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
        #endregion

        #region Public Methods
        public async void JSNotify(string cmd, object o1, object o2)
        {
            if (cmd.Equals("displayFachinfo"))
            {
                string ean = o1 as string;
                string anchor = o2 as string;
                if (ean != null && anchor != null)
                {
                    ean = ean.Trim();
                    Article a = await _sqlDb.GetArticleWithRegnr(ean);
                    if (a != null)
                    {
                        _mainWindow.BringFachinfoIntoView();
                        ShowFull(a, _mainWindow.SelectedFullTextSearchKey());
                        await Task.Delay(100);
                        if (anchor != null && anchor != "?")
                        {
                            string jsCode = "moveToHighlight('" + anchor + "');";
                            _mainWindow.InjectJS(jsCode);
                        }

                    }
                }
            }
        }

        private string HighlightContent(string content, string highlight)
        {
            if (highlight != null && highlight.Length > 3)
            {
                // Marks the keyword in the html
                string firstUpper = highlight.Substring(0, 1).ToUpper() + highlight.Substring(1, highlight.Length - 1);

                content += "<script>highlightText(document.body, '" + highlight + "')</script>";
                content += "<script>highlightText(document.body, '" + firstUpper + "')</script>";
            }

            return content;
        }

        public void ShowFull(Article a, string highlight = "")
        {
            string htmlStr = HighlightContent(a.Content, highlight);

            string headStr = "<!DOCTYPE html><head>"
                + "<meta http-equiv='Content-Type' content='text/html;charset=UTF-8'>"
                + _jsStr
                + _cssStr
                + "</head>";
            UpdateHtml(headStr + htmlStr);
            UpdateSectionTitleList(a);
        }

        public async Task ShowReport()
        {
            /* This is a solution to the local storage issue for Windows Store Apps which is not using UWP
             */
            string reportPath = Utilities.ReportPath();
            // First part could be omitted... WEIRD!!
            if (File.Exists(reportPath))
            {
                await Task.Run(() =>
                {
                    System.Diagnostics.Process.Start(reportPath);
                });
            }
            else
            {
                await Task.Run(() =>
                {
                    System.Diagnostics.Process.Start(reportPath);
                });
            }
        }

        public void UpdateSectionTitleList(Article a)
        {
            currentArticle = a;
            SectionTitleListItems.Clear();
            // List of section titles
            if (a != null)
            {
                List<TitleItem> listOfSectionTitles = a.ListOfSectionTitleItems();
                SectionTitleListItems.AddRange(listOfSectionTitles);
            }
        }

        public void UpdateHtml(string html)
        {
            htmlPreColor = html;
            HtmlText = Colors.ReplaceStyleForDarkMode(html);
        }

        #endregion

        private void ReloadColors()
        {
            HtmlText = Colors.ReplaceStyleForDarkMode(htmlPreColor);
            UpdateSectionTitleList(currentArticle);
        }
    }
}
