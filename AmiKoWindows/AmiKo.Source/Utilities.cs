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

using System;
using System.IO;
using System.Reflection;

namespace AmiKoWindows
{
    class Utilities
    {
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
            object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyVersionAttribute), false);
            if (attributes.Length > 0)
            {
                AssemblyVersionAttribute versionAttribute = (AssemblyVersionAttribute)attributes[0];
                if (versionAttribute.Version != "")
                {
                    return versionAttribute.Version;
                }
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
            return Path.Combine(Utilities.AppRoamingDataFolder(), @"amiko_db_full_idx_de.db");
        }

        public static string InteractionsPath()
        {
            return Path.Combine(Utilities.AppRoamingDataFolder(), @"drug_interactions_csv_de.csv");
        }
    }
}
