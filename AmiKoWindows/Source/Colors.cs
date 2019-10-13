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
using System;
using System.Windows;
using System.Windows.Media;

namespace AmiKoWindows
{
    // See also Style.xaml
    class Colors
    {
        public const string SearchBoxItems = "DarkSlateGray"; // #FF2F4F4F
        //public const string SearchBoxChildItems = "DarkSlateGray"; // #FF2F4F4F
        public static Brush SearchBoxChildItems()
        {
            var r = Colors.themeResources;
            var c = r["GrayBrush2"];
            return (Brush)c;
        }
        public static Brush SectionTitles()
        {
            var r = Colors.themeResources;
            var c = r["TextBrush"];
            return (Brush)c;
        }
        public static Brush Background()
        {
            var r = Colors.themeResources;
            var c = r["WhiteBrush"];
            return (Brush)c;
        }
        public static Brush TextBoxBorder()
        {
            var r = Colors.themeResources;
            var c = r["GrayBrush7"];
            return (Brush)c;
        }

        public const string Originals = "Red"; // #FFFF0000
        public const string Generics = "Green"; // #FF008000

        // Fields
        public static string ErrorFieldColor = "#fad3d3";
        public static string ErrorBrushColor = "#df1e43";

        // General names
        public const string PaleGray = "#f2f2f2";
        public const string ModestBlack = "#888888";

        public static bool IsLightMode()
        {
            return 1 == (int)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1);
        }

        public static void ReloadColors()
        {
            if (IsLightMode())
            {
                themeResources = new ResourceDictionary
                {
                    Source = new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Accents/BaseLight.xaml", UriKind.RelativeOrAbsolute)
                };
            }
            else
            {
                themeResources = new ResourceDictionary
                {
                    Source = new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Accents/BaseDark.xaml", UriKind.RelativeOrAbsolute)
                };
            }
        }

        private static ResourceDictionary themeResources = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Accents/BaseDark.xaml", UriKind.RelativeOrAbsolute)
            };
    }
}
