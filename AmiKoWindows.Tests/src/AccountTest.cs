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
    public class AccountTest
    {
        [TearDown]
        public void TearDown()
        {
            // pass
        }

        [Test]
        public void Test_VirtualField_Fullname()
        {
            Account account;

            account = new Account();
            account.GivenName = "John";
            Assert.AreEqual("John", account.Fullname);

            account = new Account();
            account.FamilyName = "Smith";
            Assert.AreEqual("Smith", account.Fullname);

            account = new Account();
            account.Title = "Dr.";
            account.GivenName = "John";
            account.FamilyName = "Smith";
            Assert.AreEqual("Dr. John Smith", account.Fullname);
        }

        [Test]
        public void Test_VirtualField_Place()
        {
            Account account;

            account = new Account();
            account.Zip = "123";
            account.City = "Zürich";
            Assert.AreEqual("123 Zürich", account.Place);

            account = new Account();
            account.City = "Bern";
            Assert.AreEqual("Bern", account.Place);
        }
    }
}
