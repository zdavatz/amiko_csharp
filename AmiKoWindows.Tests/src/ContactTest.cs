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

        [Test]
        public void Test_VirtualField_Fullname()
        {
            Contact contact;

            contact = new Contact();
            contact.GivenName = "John";
            Assert.AreEqual("John", contact.Fullname);

            contact = new Contact();
            contact.FamilyName = "Smith";
            Assert.AreEqual("Smith", contact.Fullname);

            contact = new Contact();
            contact.GivenName = "John";
            contact.FamilyName = "Smith";
            Assert.AreEqual("John Smith", contact.Fullname);
        }

        [Test]
        public void Test_VirtualField_Place()
        {
            Contact contact;

            contact = new Contact();
            contact.Zip = "123";
            contact.City = "Zürich";
            Assert.AreEqual("123 Zürich", contact.Place);

            contact = new Contact();
            contact.City = "Bern";
            Assert.AreEqual("Bern", contact.Place);
        }

        [Test]
        public void Test_VirtualField_PersonalInfo()
        {
            Contact contact;

            contact = new Contact();
            contact.IsFemale = true;
            contact.RawWeightKg = 45.9f;
            contact.RawHeightCm = 168.3f;
            contact.Birthdate = "13.6.1993";
            contact.Uid = contact.GenerateUid();
            Assert.AreEqual("45.9kg/168.3cm F 13.6.1993", contact.PersonalInfo);

            contact = new Contact();
            contact.IsMale = true;
            contact.RawWeightKg = 70.3f;
            contact.RawHeightCm = 186.7f;
            contact.Birthdate = "30.11.1990";
            contact.Uid = contact.GenerateUid();
            Assert.AreEqual("70.3kg/186.7cm M 30.11.1990", contact.PersonalInfo);

            // F default, without decimal points
            contact = new Contact();
            contact.RawWeightKg = 60.0f;
            contact.RawHeightCm = 170.0f;
            contact.Birthdate = "1.1.1970";
            contact.Uid = contact.GenerateUid();
            Assert.AreEqual("60kg/170cm F 1.1.1970", contact.PersonalInfo);

            // without size properties
            contact = new Contact();
            contact.Birthdate = "1.1.1970";
            contact.Uid = contact.GenerateUid();
            Assert.AreEqual("F 1.1.1970", contact.PersonalInfo);

            // without weight
            contact = new Contact();
            contact.IsMale = true;
            contact.RawHeightCm = 191.0f;
            contact.Birthdate = "3.6.2000";
            contact.Uid = contact.GenerateUid();
            Assert.AreEqual("191cm M 3.6.2000", contact.PersonalInfo);

            // without height
            contact = new Contact();
            contact.IsFemale = true;
            contact.RawWeightKg = 58.3f;
            contact.Birthdate = "22.2.2002";
            contact.Uid = contact.GenerateUid();
            Assert.AreEqual("58.3kg F 22.2.2002", contact.PersonalInfo);
        }
    }
}
