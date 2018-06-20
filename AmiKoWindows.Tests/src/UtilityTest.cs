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
        public void Test_Contact()
        {
            Assert.AreEqual("Foo Bar Baz", Utilities.Concat("Foo", "Bar", "Baz"));
            Assert.AreEqual("Foo Baz", Utilities.Concat("Foo", "", "Baz"));
            Assert.AreEqual("Baz", Utilities.Concat("", "", "Baz"));
            Assert.AreEqual("Foo", Utilities.Concat("Foo", "", ""));

            Assert.AreEqual("Foo Baz", Utilities.Concat("Foo", null, "Baz"));
            Assert.AreEqual("Bar Baz", Utilities.Concat(null, "Bar", "Baz"));
            Assert.AreEqual("", Utilities.Concat(null, null, null));
        }

        [Test]
        public void Test_Hash()
        {
            Assert.AreEqual(501371451, Utilities.Hash("Hoi"));
            Assert.AreEqual(30817822292, Utilities.Hash("Zäme"));
            Assert.AreEqual(-1575337182486860193, Utilities.Hash("FooBarBaz"));
            Assert.AreEqual(2098610509559846236, Utilities.Hash("FooBarBazQuxQuuxFooBarBazQuxQuux"));
            Assert.AreEqual(6539383804322955524, Utilities.Hash(new String('a', 100)));
        }

        [Test]
        public void Test_GenerateHash()
        {
            Assert.AreEqual("501371451", Utilities.GenerateHash("Hoi"));
            Assert.AreEqual("30817822292", Utilities.GenerateHash("Zäme"));
            Assert.AreEqual("16871406891222691423", Utilities.GenerateHash("FooBarBaz"));
            Assert.AreEqual("2098610509559846236", Utilities.GenerateHash("FooBarBazQuxQuuxFooBarBazQuxQuux"));
            Assert.AreEqual("6539383804322955524", Utilities.GenerateHash(new String('a', 100)));
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
