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
using System.Reflection;

namespace AmiKoWindows
{
    class Utilities
    {
        public static string AppLanguage()
        {
            var culture = System.Globalization.CultureInfo.CurrentUICulture.ToString();
            if (culture.Equals("de-CH"))
                return "de";
            else if (culture.Equals("fr-CH"))
                return "fr";
            return "de";
        }

        public static string AppName()
        {
            object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
            if (attributes.Length > 0)
            {
                AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                if (titleAttribute.Title != "")
                {
                    return titleAttribute.Title;
                }
            }
            return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
        }

        public static string AppVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            if (version != "")
            {
                return version;
            }
            return "1.0.0";
        }

        public static string AppCompany()
        {
            object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
            if (attributes.Length > 0)
            {
                AssemblyCompanyAttribute versionAttribute = (AssemblyCompanyAttribute)attributes[0];
                if (versionAttribute.Company != "")
                {
                    return versionAttribute.Company;
                }
            }
            return "Ywesee";
        }

        public static string AppExecutingFolder()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        public static string AppLocalDataFolder()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppCompany(), AppName());
        }

        public static string AppRoamingDataFolder()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppCompany(), AppName());
        }

        public static string SQLiteDBPath()
        {
            return GetDbPath(Constants.AIPS_DB_BASE);
        }

        public static string FrequencyDBPath()
        {
            return GetDbPath(Constants.FREQUENCY_DB_BASE);
        }

        public static string PatientDBPath()
        {
            return GetDbPath(Constants.PATIENT_DB_BASE);
        }

        public static string InteractionsPath()
        {
            string interactionsName = Constants.INTERACTIONS_CSV_BASE + "de.csv";
            if (AppLanguage().Equals("fr"))
                interactionsName = Constants.INTERACTIONS_CSV_BASE + "fr.csv";

            string path = Path.Combine(AppRoamingDataFolder(), interactionsName);
            if (!File.Exists(path))
                path = Path.Combine(AppExecutingFolder(), "Data", interactionsName);
            return path;
        }

        public static string ReportPath()
        {
            string reportPath = "http://pillbox.oddb.org/amiko_report_de.html";
            if (AppLanguage().Equals("fr"))
                reportPath = "http://pillbox.oddb.org/amiko_report_fr.html";
            return reportPath;
        }

        private static string GetDbPath(string baseName)
        {
            string dbName = baseName + AppLanguage() + ".db";
            string dbPath = Path.Combine(AppRoamingDataFolder(), dbName);
            if (!File.Exists(dbPath))
                dbPath = Path.Combine(AppExecutingFolder(), "Data", dbName);
            return dbPath;
        }
    }
}
