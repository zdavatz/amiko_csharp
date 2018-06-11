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

        [Test]
        public void Test_AppCultureInfoName_in_DE()
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("de-CH");
            string cultureName = AmiKoWindows.Utilities.AppCultureInfoName();
            Assert.AreEqual("de-CH", cultureName);
        }

        [Test]
        public void Test_AppCultureInfoName_in_FR()
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("fr-CH");
            string cultureName = AmiKoWindows.Utilities.AppCultureInfoName();
            Assert.AreEqual("fr-CH", cultureName);
        }

        [Test]
        public void Test_ConvertTitleCaseToSnakeCase()
        {
            Assert.AreEqual(
                "given_name", AmiKoWindows.Utilities.ConvertTitleCaseToSnakeCase("GivenName"));
            Assert.AreEqual(
                "given_name_family_name", AmiKoWindows.Utilities.ConvertTitleCaseToSnakeCase("GivenNameFamilyName"));

            Assert.AreEqual(
                "phone", AmiKoWindows.Utilities.ConvertTitleCaseToSnakeCase("Phone"));
        }

        [Test]
        public void Test_ConvertSnakeCaseToTitleCase()
        {
            Assert.AreEqual(
                "Id", AmiKoWindows.Utilities.ConvertSnakeCaseToTitleCase("_id"));

            Assert.AreEqual(
                "FamilyName", AmiKoWindows.Utilities.ConvertSnakeCaseToTitleCase("family_name"));
            Assert.AreEqual(
                "FamilyNameGivenName", AmiKoWindows.Utilities.ConvertSnakeCaseToTitleCase("family_name_given_name"));

            Assert.AreEqual(
                "Email", AmiKoWindows.Utilities.ConvertSnakeCaseToTitleCase("email"));
        }

        [Test]
        public void Test_GenerateHash_Equality()
        {
            string hashA, hashB;
            hashA = Utilities.GenerateHash("test");
            hashB = Utilities.GenerateHash("test");
            Assert.AreEqual(hashA, hashB);

            hashA = Utilities.GenerateHash("Test");
            hashB = Utilities.GenerateHash("test");
            Assert.AreNotEqual(hashA, hashB);
        }
    }
}
