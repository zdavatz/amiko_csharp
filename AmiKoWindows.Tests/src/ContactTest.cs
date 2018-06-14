using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Threading;
using NUnit.Framework;

using AmiKoWindows;


namespace AmiKoWindows.Tests
{
    [TestFixture]
    public class ContactTest
    {
        [TearDown]
        public void TearDown()
        {
            // pass
        }

        [Test]
        public void Test_ContactToParameters()
        {
            var contact = new Contact();
            contact.Uid = "123";
            contact.GivenName = "John";
            contact.FamilyName = "Smith";
            contact.Address = "Strasse 42";
            contact.City = "Unknown";
            contact.Zip = "1";
            contact.Country = "None";
            contact.Birthdate = "31.05.2018";
            contact.Gender = "1";
            contact.WeightKg = "60.5";
            contact.HeightCm = "175.5";
            contact.Phone = "+00 00 0000 0000";
            contact.Email = "john@example.com";
            contact.TimeStamp = "";

            Dictionary<string, string> expected = null;

            // ignored
            expected = new Dictionary<string, string>();
            Assert.AreEqual(expected, contact.ToParameters(new string[]{"_id", "item"}));

            expected = new Dictionary<string, string>();
            expected.Add("@uid", "123");
            Assert.AreEqual(expected, contact.ToParameters(new string[]{"uid",}));

            expected = new Dictionary<string, string>();
            expected.Add("@given_name", "John");
            expected.Add("@family_name", "Smith");
            expected.Add("@birthdate", "31.05.2018");
            Assert.AreEqual(expected, contact.ToParameters(
                new string[]{"given_name", "family_name", "birthdate"}));
        }

        [Test]
        public void Test_GenerateUid_Equality()
        {
            var contactA = new Contact();
            contactA.GivenName = "John";
            contactA.FamilyName = "Smith";
            contactA.Birthdate = "31.5.2018";

            var expected = contactA.GenerateUid();

            var contactB = new Contact();
            contactB.GivenName = "JOHN";
            contactB.FamilyName = "SMITH";
            contactB.Birthdate = "31.5.2018";

            var contactC = new Contact();
            contactC.GivenName = "John";
            contactC.FamilyName = "Smith";
            contactC.Birthdate = "31.5.2018";

            Assert.AreEqual(expected, contactB.GenerateUid());
            Assert.AreEqual(expected, contactC.GenerateUid());

            var contactD = new Contact();
            contactD.GivenName = "John";
            contactD.FamilyName = "Smith";
            contactD.Birthdate = "9.11.1972";

            Assert.AreNotEqual(expected, contactD.GenerateUid());
        }
    }
}
