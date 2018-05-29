using System;
using System.IO;
using System.Globalization;
using System.Threading;
using NUnit.Framework;

using AmiKoWindows;


namespace AmiKoWindows.Tests
{
    [TestFixture]
    public class UtilityTest
    {
        [TearDown]
        public void TearDown()
        {
            // default
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("de-CH");
        }

        [Test]
        public void Test_PatientDBPath_in_DE()
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("de-CH");
            string dbPath = AmiKoWindows.Utilities.PatientDBPath();
            Assert.AreEqual("amiko_patient_de.db", Path.GetFileName(dbPath));
        }

        [Test]
        public void Test_PatientDBPath_in_FR()
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("fr-CH");
            string dbPath = AmiKoWindows.Utilities.PatientDBPath();
            Assert.AreEqual("amiko_patient_fr.db", Path.GetFileName(dbPath));
        }
    }
}
