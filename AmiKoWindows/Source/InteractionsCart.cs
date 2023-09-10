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
using System.IO;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Microsoft.Win32;
using System.Threading.Tasks;
using System.Diagnostics;

namespace AmiKoWindows
{
    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    [ComVisible(true)]
    public class InteractionsCart : HtmlBase
    {
        public InteractionsCart()
        {
            UserPreferenceChangedEventHandler e3 = (o, e) =>
            {
                ReloadColors();
            };
            SystemEvents.UserPreferenceChanged += e3;
        }

        #region Private Fields
        Dictionary<string, string> _interactionsDict = new Dictionary<string, string>();
        // Dictionary of title to article
        Dictionary<string, Article> _articleBasket = new Dictionary<string, Article>();
        string _jsStr;
        string _cssStr;
        string _imgFolder;
        string _htmlPreColor;
        #endregion

        #region Public Methods
        public async void JSNotify(string cmd, object o)
        {
            if (cmd.Equals("deleteSingleRow"))
            {
                string row = o as string;
                row = row.Trim();
                RemoveArticle(row);
            }
            else if (cmd.Equals("deleteAllRows"))
            {
                RemoveAllArticles();
            } else if (cmd.StartsWith("openLink:"))
            {
                var startInfo = new ProcessStartInfo { FileName = cmd.Replace("openLink:", ""), UseShellExecute = true };
                Process.Start(startInfo);
            }
        }

        public async Task<Dictionary<string, dynamic>>CallEPha()
        {
            if (_articleBasket.Count == 0) return null;
            var dicts = new List<Dictionary<string, string>>();
            foreach (var kvp in _articleBasket)
            {
                Article a = kvp.Value;
                var parts = Regex.Split(a.Packages, "\\|");
                var dict = new Dictionary<string, string>();
                dict["type"] = "drug";
                dict["gtin"] = parts[9];
                dicts.Add(dict);
            }
            var jsonStr = JsonConvert.SerializeObject(dicts, Formatting.None);
            var content = new StringContent(jsonStr, Encoding.UTF8, "application/json");

            var client = new HttpClient();
            var endpoint = "https://api.epha.health/clinic/advice/" + Utilities.AppLanguage() + "/";
            var result = await client.PostAsync(endpoint, content);
            var responseStr = await result.Content.ReadAsStringAsync();
            dynamic deserialized = JsonConvert.DeserializeObject(responseStr);
            var resultDict = deserialized as Dictionary<string, object>;
            var code = deserialized["meta"]["code"];
            if (code >= 200 && code < 300)
            {
                return resultDict["data"] as Dictionary<string, dynamic>;
            }
            return null;
        }

        public string HtmlForEPhaResponse(Dictionary<string, dynamic> dict)
        {
            int safety = dict["safety"];
            int kinetic = dict["risk"]["kinetic"];
            int qtc = dict["risk"]["qtc"];
            int warning = dict["risk"]["warning"];
            int serotonerg = dict["risk"]["serotonerg"];
            int anticholinergic = dict["risk"]["anticholinergic"];
            int adverse = dict["risk"]["adverse"];
            string lang = Utilities.AppLanguage();

            string html_str = "";

            if (lang == "de")
            {
                html_str += "Sicherheit<BR>";
                html_str += "<p class='risk-description'>Je höher die Sicherheit, desto sicherer die Kombination.</p>";
            }
            else
            {
                html_str += "Sécurité<BR>";
                html_str += "<p class='risk-description'>Plus la sécurité est élevée, plus la combinaison est sûre.</p>";
            }

            html_str += "<div class='risk'>100";
            html_str += "<div class='gradient'>" +
                    "<div class='pin' style='left: " + (100 - safety) + "%'>" + safety + "</div>" +
                "</div>";
            html_str += "0</div><BR><BR>";

            if (lang == "de")
            {
                html_str += "Risikofaktoren<BR>";
                html_str += "<p class='risk-description'>Je tiefer das Risiko, desto sicherer die Kombination.</p>";
            }
            else
            {
                html_str += "Facteurs de risque<BR>";
                html_str += "<p class='risk-description'>Plus le risque est faible, plus la combinaison est sûre.</p>";
            }

            html_str += "<table class='risk-table'>";
            html_str += "<tr><td class='risk-name'>";
            html_str += lang == "de" ? "Pharmakokinetik" : "Pharmacocinétique";
            html_str += "</td>";
            html_str += "<td>";
            html_str += "<div class='risk'>0";
            html_str += "<div class='gradient'><div class='pin' style='left: " + kinetic + "%'>" + kinetic + "</div></div>";
            html_str += "100</div>";
            html_str += "</td></tr>";
            html_str += "<tr><td class='risk-name'>";
            html_str += lang == "de" ? "Verlängerung der QT-Zeit" : "Allongement du temps QT";
            html_str += "</td>";
            html_str += "<td>";
            html_str += "<div class='risk'>0";
            html_str += "<div class='gradient'><div class='pin' style='left: " + qtc + "%'>" + qtc + "</div></div>";
            html_str += "100</div>";
            html_str += "</td></tr>";
            html_str += "<tr><td class='risk-name'>";
            html_str += lang == "de" ? "Warnhinweise" : "Avertissements";
            html_str += "</td>";
            html_str += "<td>";
            html_str += "<div class='risk'>0";
            html_str += "<div class='gradient'><div class='pin' style='left: " + warning + "%'>" + warning + "</div></div>";
            html_str += "100</div>";
            html_str += "</td></tr>";
            html_str += "<tr><td class='risk-name'>";
            html_str += lang == "de" ? "Serotonerge Effekte" : "Effets sérotoninergiques";
            html_str += "</td>";
            html_str += "<td>";
            html_str += "<div class='risk'>0";
            html_str += "<div class='gradient'><div class='pin' style='left: " + serotonerg + "%'>" + serotonerg + "</div></div>";
            html_str += "100</div>";
            html_str += "</td></tr>";
            html_str += "<tr><td class='risk-name'>";
            html_str += lang == "de" ? "Anticholinerge Effekte" : "Effets anticholinergiques";
            html_str += "</td>";
            html_str += "<td>";
            html_str += "<div class='risk'>0";
            html_str += "<div class='gradient'><div class='pin' style='left: " + anticholinergic + "%'>" + anticholinergic + "</div></div>";
            html_str += "100</div>";
            html_str += "</td></tr>";
            html_str += "<tr><td class='risk-name'>";
            html_str += lang == "de" ? "Allgemeine Nebenwirkungen" : "Effets secondaires généraux";
            html_str += "</td>";
            html_str += "<td>";
            html_str += "<div class='risk'>0";
            html_str += "<div class='gradient'><div class='pin' style='left: " + adverse + "%'>" + adverse + "</div></div>";
            html_str += "100</div>";
            html_str += "</td></tr>";
            html_str += "</table>";

            return html_str;
        }

        public void LoadFiles()
        {
            _interactionsDict = ReadInteractions(Utilities.InteractionsPath());

            Log.WriteLine(">> OK: Opened interactions db located in {0}\n", Utilities.InteractionsPath());

            string path = Path.Combine(Utilities.AppExecutingFolder(), Constants.JS_FOLDER, "interaction_callbacks.js");
            _jsStr = File.ReadAllText(path);

            path = Path.Combine(Utilities.AppExecutingFolder(), Constants.INTERACTIONS_SHEET);
            _cssStr = File.ReadAllText(path);

            _imgFolder = Path.Combine(Utilities.AppExecutingFolder(), Constants.IMG_FOLDER);
        }

        public async Task ShowBasket(bool skipEPha = false)
        {
            if (!skipEPha)
            {
                // If we want to load from EPha, we better run once without EPha first
                // so the user can see results faster.
                await ShowBasket(true);
            }
            if (_articleBasket.Count > 0)
            {
                int medCount = 1;
                // Build interaction basket table
                string basketHtmlStr = "<h3>Interaktionenkorb</h3>";
                if (Utilities.AppLanguage().Equals("fr"))
                    basketHtmlStr = "<h3>Panier de médicaments</h3>";

                basketHtmlStr += "<table id=\"Interaktionen\" width=\"98%25\">";

                foreach (var kvp in _articleBasket)
                {
                    string title = kvp.Key;
                    Article a = kvp.Value;
                    Tuple<string, string> atcInfo = GetAtcInfo(a);

                    if (medCount % 2 == 0)
                        basketHtmlStr += "<tr style=\"background-color:color: var(--background-color-gray);\">";
                    else
                        basketHtmlStr += "<tr style=\"background-color:var(--background-color-normal);\">";
                    basketHtmlStr += "<td>" + medCount + "</td>"
                            + "<td>" + title + " </td> "
                            + "<td>" + atcInfo.Item1 + "</td>"
                            + "<td>" + atcInfo.Item2 + "</td>"
                            + "<td style=\"text-align:center;\">" + "<button type=\"button\" style=\"border:none;\" onclick=\"deleteRow('Interaktionen',this)\"><img src=\""
                            + _imgFolder + "/trash_icon_2.png\" /></button>" + "</td>";

                    basketHtmlStr += "</tr>";
                    medCount++;
                }
                basketHtmlStr += "</table>";

                Dictionary<string, dynamic> ephaResponse = skipEPha ? null : await CallEPha();
                if (ephaResponse != null)
                {
                    basketHtmlStr += HtmlForEPhaResponse(ephaResponse);
                }

                string interactionsHtmlStr = "";
                string deleteAllButtonStr = "";
                string ephaButtonStr = "";
                bool interactionsPresent = false;
                if (medCount > 1)
                {
                    interactionsHtmlStr = Interactions();
                    if (interactionsHtmlStr.Length > 0)
                        interactionsPresent = true;

                    if (Utilities.AppLanguage().Equals("de"))
                    {
                        deleteAllButtonStr = "alle löschen";
                    }
                    else if (Utilities.AppLanguage().Equals("fr"))
                    {
                        deleteAllButtonStr = "tout supprimer";
                    }
                    deleteAllButtonStr = "<div id=\"Delete_all\"><input type=\"button\" value=\"" + deleteAllButtonStr + "\" onclick=\"deleteRow('DeleteAll',this)\" /></div>";
                }
                if (ephaResponse != null)
                {
                    var buttonString = Utilities.AppLanguage() == "de" ? "EPha API Details anzeigen" : "Afficher les détails de l'API EPha";
                    ephaButtonStr = "<input type=\"button\" value=\""+ buttonString + "\" style=\"cursor: pointer; float:right;\" onclick=\"openLink('" + ephaResponse["link"] +"')\" />";
                }

                string topNoteHtmlStr = "";
                string legendHtmlStr = "";
                if (!interactionsPresent && medCount > 1)
                {
                    // Add note to indicate that there are no interactions
                    if (Utilities.AppLanguage().Equals("de"))
                        topNoteHtmlStr = "<p class=\"paragraph0\">Zur Zeit sind keine Interaktionen zwischen diesen Medikamenten in der EPha.ch-Datenbank vorhanden. Weitere Informationen finden Sie in der Fachinformation.</p><br><br>";
                    else if (Utilities.AppLanguage().Equals("fr"))
                        topNoteHtmlStr = "<p class=\"paragraph0\">Il n’y a aucune information dans la banque de données EPha.ch à propos d’une interaction entre les médicaments sélectionnés. Veuillez consulter les informations professionelles.</p><br><br>";
                }
                else if (medCount > 1)
                {
                    legendHtmlStr = ColorLegend();
                }

                string bottomNoteHtmlStr = "";
                if (Utilities.AppLanguage().Equals("de"))
                {
                    bottomNoteHtmlStr += "<p class=\"footnote\">1. Datenquelle: Public Domain Daten von EPha.ch.</p> " +
                        "<p class=\"footnote\">2. Unterstützt durch:  IBSA Institut Biochimique SA.</p>";
                }
                else if (Utilities.AppLanguage().Equals("fr"))
                {
                    bottomNoteHtmlStr += "<p class=\"footnote\">1. Source des données: données du domaine publique de EPha.ch</p> " +
                        "<p class=\"footnote\">2. Soutenu par: IBSA Institut Biochimique SA.</p>";
                }

                UpdateHtml("<!DOCTYPE html><head><meta http-equiv=\"content-type\" content=\"text/html; charset=UTF-8\" />"
                    + "<script language=\"javascript\">" + _jsStr + "</script>"
                    + "<style>" + _cssStr + "</style>" + "</head>"
                    + "<body><div id=\"interactions\">"
                    + basketHtmlStr + deleteAllButtonStr + ephaButtonStr + "<br><br>"
                    + topNoteHtmlStr
                    + interactionsHtmlStr + "<br>"
                    + legendHtmlStr + "<br>"
                    + bottomNoteHtmlStr
                    + "</div></body></html>");
            }
            else
            {
                string basketHtmlStr = "<p class=\"paragraph0\">Ihr Medikamentenkorb ist leer.</p>";
                // Medikamentenkorb ist leer
                if (Utilities.AppLanguage().Equals("fr"))
                    basketHtmlStr = "<p class=\"paragraph0\">Votre panier de médicaments est vide.</p>";

                UpdateHtml("<html><head><meta http-equiv=\"content-type\" content=\"text/html; charset=UTF-8\" />"
                    + "<style>" + _cssStr + "</style>" + "</head>"
                    + "<body><div id=\"interactions\">"
                    + basketHtmlStr
                    + "</div></body></html>");
            }
        }

        public void UpdateHtml(string html)
        {
            _htmlPreColor = html;
            HtmlText = Colors.ReplaceStyleForDarkMode(html);
        }

        public void ReloadColors()
        {
            if (_htmlPreColor != null)
            {
                HtmlText = Colors.ReplaceStyleForDarkMode(_htmlPreColor);
            }
        }

        public void AddArticle(Article a)
        {
            // Key-Value pair with key = article title and value = article
            string title = ClampedTitle(a);
            if (!_articleBasket.ContainsKey(title))
                _articleBasket.Add(title, a);
        }

        public void RemoveArticle(string row)
        {
            if (_articleBasket.ContainsKey(row))
            {
                _articleBasket.Remove(row);
                // Refresh UI
                ShowBasket();
            }
        }

        public void RemoveArticle(Article a)
        {

            string title = ClampedTitle(a);
            if (_articleBasket.ContainsKey(title))
            {
                _articleBasket.Remove(title);
                // Refresh UI
                ShowBasket();
            }
        }

        public void RemoveAllArticles()
        {
            _articleBasket.Clear();
            ShowBasket();
        }
        #endregion

        #region Private Methods
        string ClampedTitle(Article a)
        {
            string title = a.Title;
            if (title.Length > 30)
                title = title.Substring(0, 30) + "...";
            return title;
        }

        Tuple<string, string> GetAtcInfo(Article a)
        {
            string atcCode = "k.A.";
            string atcClass = "k.A.";
            string[] s = a?.AtcCode.Split(';');
            if (s.Length > 0)
            {
                atcCode = s[0];
                if (s.Length > 1)
                    atcClass = s[1];
            }
            return new Tuple<string, string>(atcCode, atcClass);
        }

        Dictionary<string, string> ReadInteractions(string filePath)
        {
            Dictionary<string, string> strDict = new Dictionary<string, string>();
            if (File.Exists(filePath))
            {
                try
                {
                    var contents = File.ReadAllText(filePath, Encoding.UTF8).Split('\n');
                    foreach (var row in contents)
                    {
                        var token = Regex.Split(row, "\\|\\|");
                        if (token.Length > 2)
                        {
                            var key = token[0] + '-' + token[1];
                            if (!strDict.ContainsKey(key))
                            {
                                strDict.Add(key, token[2]);
                            }
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                    //
                }
            }
            return strDict;
        }

        string Interactions()
        {
            string htmlStr = "";
            List<TitleItem> listOfSectionTitles = new List<TitleItem>();

            SectionTitleListItems.Clear();

            Dictionary<string, string> listOfATCCodePairs = new Dictionary<string, string>();
            foreach (var kvp1 in _articleBasket)
            {
                Article a1 = kvp1.Value;
                string title1 = ClampedTitle(a1);
                string atc1 = GetAtcInfo(a1)?.Item1;

                foreach (var kvp2 in _articleBasket)
                {
                    Article a2 = kvp2.Value;
                    string title2 = ClampedTitle(a2);
                    string atc2 = GetAtcInfo(a2)?.Item1;

                    if (!atc1.Equals(atc2))
                    {
                        string key = atc1 + "-" + atc2;
                        string title = title1 + " ➞ " + title2;

                        if (_interactionsDict.ContainsKey(key))
                        {
                            htmlStr += _interactionsDict[key];
                            listOfSectionTitles.Add(new TitleItem() { Id = key, Title = title });
                        }
                    }
                }
            }

            if (listOfSectionTitles.Count > 0)
            {
                listOfSectionTitles.Insert(0, new TitleItem() { Id = "Interaktionen", Title = "Interaktionen" });
                listOfSectionTitles.Add(new TitleItem { Id = "Legende", Title = "Legende" });
            }

            SectionTitleListItems.AddRange(listOfSectionTitles);

            return htmlStr;
        }

        string ColorLegend()
        {
            string legend = "<table id=\"Legende\" width=\"98%25\">";
            /*
             Risikoklassen
             -------------
                 A: Keine Massnahmen notwendig (grün)
                 B: Vorsichtsmassnahmen empfohlen (gelb)
                 C: Regelmässige Überwachung (orange)
                 D: Kombination vermeiden (pinky)
                 X: Kontraindiziert (hellrot)
                 0: Keine Angaben (grau)
            */

            if (Utilities.AppLanguage().Equals("de"))
            {
                legend = "<table id=\"Legende\" width=\"98%25\">";
                legend += "<tr><td bgcolor=\"#caff70\"></td>" +
                        "<td>A</td>" +
                        "<td>Keine Massnahmen notwendig</td></tr>";
                legend += "<tr><td bgcolor=\"#ffec8b\"></td>" +
                        "<td>B</td>" +
                        "<td>Vorsichtsmassnahmen empfohlen</td></tr>";
                legend += "<tr><td bgcolor=\"#ffb90f\"></td>" +
                        "<td>C</td>" +
                        "<td>Regelmässige Überwachung</td></tr>";
                legend += "<tr><td bgcolor=\"#ff82ab\"></td>" +
                        "<td>D</td>" +
                        "<td>Kombination vermeiden</td></tr>";
                legend += "<tr><td bgcolor=\"#ff6a6a\"></td>" +
                        "<td>X</td>" +
                        "<td>Kontraindiziert</td></tr>";
            }
            else if (Utilities.AppLanguage().Equals("fr"))
            {
                legend = "<table id=\"Légende\" width=\"98%25\">";
                legend += "<tr><td bgcolor=\"#caff70\"></td>" +
                        "<td>A</td>" +
                        "<td>Aucune mesure nécessaire</td></tr>";
                legend += "<tr><td bgcolor=\"#ffec8b\"></td>" +
                        "<td>B</td>" +
                        "<td>Mesures de précaution sont recommandées</td></tr>";
                legend += "<tr><td bgcolor=\"#ffb90f\"></td>" +
                        "<td>C</td>" +
                        "<td>Doit être régulièrement surveillée</td></tr>";
                legend += "<tr><td bgcolor=\"#ff82ab\"></td>" +
                        "<td>D</td>" +
                        "<td>Eviter la combinaison</td></tr>";
                legend += "<tr><td bgcolor=\"#ff6a6a\"></td>" +
                        "<td>X</td>" +
                        "<td>Contre-indiquée</td></tr>";
            }

            legend += "</table>";

            return legend;
        }
        #endregion
    }
}
