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
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace AmiKoWindows
{
    class Utilities
    {
        public static string AppCultureInfoName()
        {
            return System.Globalization.CultureInfo.CurrentUICulture.ToString();
        }

        public static string AppLanguage()
        {
            var culture = AppCultureInfoName();
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

        #region Path Utilities
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

        public static string PrescriptionsPath()
        {
            string path = Path.Combine(AppRoamingDataFolder(), "amk");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        public static string GetInboxPath()
        {
            return Application.Current.Properties["InboxPath"] as string;
        }

        // returns always new (temp) inbox directory
        public static string NewInboxPath()
        {
            string path = GetTempDir("inbox");
            while (Directory.Exists(path))
                path = GetTempDir("inbox");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        public static string AccountPictureFilePath()
        {
            string path = Path.Combine(
                AppRoamingDataFolder(), Constants.ACCOUNT_PICTURE_FILE);
            return path;
        }
        #endregion

        public static void CleanupInbox()
        {
            var inbox = GetInboxPath();
            Log.WriteLine("InboxPath: {0}", inbox);
            if (inbox != null)
            {
                if (Directory.Exists(inbox))
                {
                    var info = new DirectoryInfo(inbox);
                    Directory.Delete(info.FullName, true);
                }
            }
        }

        public async static Task DeleteAll(string[] dirs)
        {
            await Task.Run(() =>
            {
                foreach (var dir in dirs)
                {
                    if (!EnforceDir(dir))
                        continue;

                    var info = new DirectoryInfo(dir);
                    foreach (FileInfo file in info.GetFiles())
                        file.Delete();

                    info.Delete(true);
                }
            });
        }

        public static void SaveImageFileAsPng(Stream input, Stream output)
        {
            using (var image = System.Drawing.Image.FromStream(input))
            using (var bitmap = new Bitmap(image.Width, image.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CompositingQuality = CompositingQuality.HighSpeed;
                graphics.SmoothingMode = SmoothingMode.HighSpeed;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                // save as same width/height
                graphics.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height));
                bitmap.Save(output, ImageFormat.Png);
            }
        }

        public static bool EnforceDir(string dir)
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return Directory.Exists(dir);
        }

        public static string Base64Encode(string input)
        {
            if (input == null)
                return null;
            var bytes = Encoding.UTF8.GetBytes(input);
            return Convert.ToBase64String(bytes);
        }

        public static string Base64Decode(string input)
        {
            if (input == null)
                return null;
            var bytes = Convert.FromBase64String(input);
            return Encoding.UTF8.GetString(bytes);
        }

        #region Private Directory Utilities
        private static string GetDbPath(string baseName)
        {
            string dbName = baseName + AppLanguage() + ".db";
            string dbPath = Path.Combine(AppRoamingDataFolder(), dbName);
            if (!File.Exists(dbPath))
                dbPath = Path.Combine(AppExecutingFolder(), "Data", dbName);
            return dbPath;
        }

        private static string GetTempDir(string name)
        {
            return Path.Combine(
                Path.GetTempPath(),
                String.Format("amiko.{0}.{1}.tmp", name, Path.GetRandomFileName())
            );
        }
        #endregion

        #region General Functions
        // TitleCase -> snake_case
        public static string ConvertTitleCaseToSnakeCase(string text)
        {
            if (text == null || text.Equals(string.Empty))
                return "";

            return string.Concat(text.Select(
                (x, i) => {
                    if (i == 0)
                        return x.ToString().ToLower();
                    else if (char.IsUpper(x))
                        return "_" + x.ToString().ToLower();
                    else
                        return x.ToString();
                }
            ));
        }

        // snake_case -> TitleCase
        public static string ConvertSnakeCaseToTitleCase(string text)
        {
            if (text == null || text.Equals(string.Empty))
                return "";

            string newText = "";
            foreach (string word in text.Split('_'))
            {
                if (!word.Equals(string.Empty))
                    newText += word[0].ToString().ToUpper() + word.Substring(1).ToLower();
            }
            if (newText.Length > 0)
                return newText[0].ToString().ToUpper() + newText.Substring(1);
            return newText;
        }

        public static string Concat(params string[] parts)
        {
            return ConcatWith(" ", parts);
        }

        // Makes a string using arguments concatenated as string
        public static string ConcatWith(string delimiter, params string[] parts)
        {
            return String.Join(delimiter, parts.Where(v => {
                return (v != null && !v.Equals(string.Empty));
            }));
        }

        // Microsoft's GUID is UUID version 4 (RFC 4122)
        public static string GenerateUUID()
        {
            var uuid = Guid.NewGuid().ToString();
            // convert UPPERCASE (same as macOS Version of AmiKo)
            return uuid.ToUpper();
        }

        public static string GenerateHash(string baseString)
        {
            long hash = Hash(baseString);
            // cast signed long to unsigned long (same as macOS Version of AmiKo)
            return String.Format("{0}", (ulong)Hash(baseString));
        }

        // Returns hashed long number same as NSString (CFString)'s Hash Implementation.
        //
        // On Swift 3.0.1, it seems that `hash` property is same result with output by hash implementation in *CFString.c* (CF-1151.16)
        // See also `UtilityTest.cs`.
        //
        // ## Note
        //
        // [Repl](https://repl.it/) may be useful to check the value (on Swift)
        //
        // ```swift
        // import Foundation
        //
        // var s: NSString = "Hoi"
        // print(s.hash)
        //
        // var t: NSString = "Zäme"
        // print(t.hash)
        //
        // var r: NSString = "FooBarBaz"
        // print(r.hash)
        // print(NSString(format: "%lu", r.hash))
        //
        // var i: NSString = "FooBarBazQuxQuuxFooBarBazQuxQuux"
        // print(i.hash)
        //
        // var n = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
        // print(n.hash)
        // ```
        //
        // ## References
        //
        // * https://opensource.apple.com/source/CF/CF-1151.16/CFString.c.auto.html
        // * https://developer.apple.com/documentation/foundation/nsstring/1417245-hash
        // * https://developer.apple.com/documentation/foundation/nsstring/1417245-hash?language=objc
        // * https://developer.apple.com/documentation/objectivec/1418956-nsobject/1418859-hash?language=objc
        public static long Hash(string baseString)
        {
            char[] chars = baseString.ToCharArray();
            int len = baseString.Length;
            int i = 0;
            long result = len;

            // updates `result` and `i` in the scope
            Action Calc = () =>
            {
                result = result * 67503105 + chars[i] * 16974593 + chars[i + 1] * 66049 + chars[i + 2] * 257 + chars[i + 3];
                i += 4;
            };

            if (len <= 96)
            {
                int to4 = (len & ~3);
                int end = len;
                while (i < to4)
                    Calc();

                while (i < end)
                    result = result * 257 + chars[i++];
            }
            else
            {
                int end;
                end = 29;
                while (i < end)
                    Calc();

                i = ((len / 2) - 16);
                end = ((len / 2) + 15);
                while (i < end)
                    Calc();

                i = (len - 32);
                end = (i + 29);
                while (i < end)
                    Calc();
            }
            return (result + (result << (len & 31)));
        }
        #endregion

        #region Time Functions
        // GetUTCTimeAsString("yyyy-MM-dd'THH:mm.ss");
        public static string GetUTCTimeAsString(string formatString)
        {
            DateTime datetime = DateTime.UtcNow;
            return datetime.ToString(formatString);
        }
        // GetLocalTimeAsString("dd.MM.yyyy (HH:mm:ss)");
        // GetLocalTimeAsString("yyyy-MM-ddTHHmmss");
        public static string GetLocalTimeAsString(string formatString)
        {
            DateTime datetime = DateTime.Now;
            return datetime.ToString(formatString);
        }

        // GetLocalTimeAsString("2018-05-01T10:00.00", "dd.MM.yyyy (HH:mm:ss)");
        public static string ConvertUTCToLocalTimeAsString(string utcString, string formatString)
        {
            DateTime utcTime = DateTime.Parse(utcString);
            DateTime time = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);
            return time.ToLocalTime().ToString(formatString);
        }
        #endregion

        #region UI Functions
        // Loads image on memory (to make it deletable on another view)
        public static bool LoadPictureInto(System.Windows.Controls.Image image, string path)
        {
            var loaded = false;
            if ((image is System.Windows.Controls.Image) && path != null && !path.Equals(string.Empty) && File.Exists(path))
            {
                var source = new System.Windows.Media.Imaging.BitmapImage();
                source.BeginInit();
                source.CacheOption = BitmapCacheOption.OnLoad;
                source.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                source.UriSource = new Uri(path, UriKind.Absolute);
                source.EndInit();
                image.Source = source;
                source.Freeze();
                loaded = true;
            }
            return loaded;
        }

        public static Xceed.Wpf.Toolkit.MessageBox MessageDialog(string text, string caption, string buttonType)
        {
            var box = new Xceed.Wpf.Toolkit.MessageBox();
            // ugh :'(
            var button = box.GetType().GetField("_button", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo field = typeof(MessageBoxButton).GetField(buttonType, BindingFlags.Public | BindingFlags.Static);
            button.SetValue(box, (MessageBoxButton)field.GetValue(null));
            System.Windows.VisualStateManager.GoToState(box, buttonType, false);
            box.Text = text;
            box.Caption = caption;
            box.OkButtonContent = Properties.Resources.ok;
            box.CancelButtonContent = Properties.Resources.cancel;
            return box;
        }
        #endregion

        #region Shell Functions
        [DllImport("shell32.dll", EntryPoint="#261", CharSet=CharSet.Unicode, PreserveSig=false)]
        public static extern void GetUserAvatarFilePath(string username, UInt32 whatever, StringBuilder picpath, int maxLength);

        public static string GetUserAvatarFilePath(string username)
        {
            var builder = new StringBuilder(1000);
            GetUserAvatarFilePath(username, 0x80000000, builder, builder.Capacity);
            return builder.ToString();
        }
        #endregion
    }
}
