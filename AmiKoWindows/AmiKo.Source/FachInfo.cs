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
using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace AmiKoWindows
{
    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    [ComVisible(true)]
    public class FachInfo : HtmlBase
    {
        #region Private Fields
        string _jscriptStr;
        string _cssStr;
        MainWindow _mainWindow;
        MainSqlDb _sqlDb;
        #endregion

        #region Properties
        string JscriptPath { get; set; }
        string CssFilePath { get; set; }
        #endregion

        #region Constructors
        public FachInfo(MainWindow mainWindow, MainSqlDb sqlDb)
        {
            _mainWindow = mainWindow;
            _sqlDb = sqlDb;

            // Load important files
            JscriptPath = Path.Combine(Utilities.AppExecutingFolder(), Constants.JS_FOLDER, "main_callbacks.js");
            if (File.Exists(JscriptPath))
            {
                _jscriptStr = "<script language=\"javascript\">" + File.ReadAllText(JscriptPath) + "</script>";
            }
            CssFilePath = Path.Combine(Utilities.AppExecutingFolder(), Constants.CSS_SHEET);
            if (File.Exists(CssFilePath))
            {
                _cssStr = "<style>" + File.ReadAllText(CssFilePath) + "</style>";
            }
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
                        _mainWindow.SetState(UIState.State.Compendium);
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
                content = content.Replace(highlight, "<span class=\"mark\" style=\"background-color: yellow\">" + highlight + "</span>");
                string firstUpper = highlight.Substring(0, 1).ToUpper() + highlight.Substring(1, highlight.Length - 1);
                content = content.Replace(firstUpper, "<span class=\"mark\" style=\"background-color: yellow\">" + firstUpper + "</span>");
            }

            return content;
        }

        public void ShowFull(Article a, string highlight = "")
        {
            string htmlStr = HighlightContent(a.Content, highlight);

            string headStr = "<!DOCTYPE html><head>"
                + "<meta http-equiv='Content-Type' content='text/html;charset=UTF-8'>" 
                // + "<link rel='stylesheet' type='text/css' href='http://fonts.googleapis.com/css?family=Roboto&subset=latin,latin-ext'>"
                + _jscriptStr
                + _cssStr
                + "</head>"; 
            HtmlText = headStr + htmlStr;

            SetSectionTitles(a);
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
