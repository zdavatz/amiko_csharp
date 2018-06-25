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
    public class OperatorTest
    {
        [TearDown]
        public void TearDown()
        {
            // pass
        }

        [Test]
        public void Test_VirtualField_Fullname()
        {
            Operator o;

            o = new Operator();
            o.GivenName = "John";
            Assert.AreEqual("John", o.Fullname);

            o = new Operator();
            o.FamilyName = "Smith";
            Assert.AreEqual("Smith", o.Fullname);

            o = new Operator();
            o.Title = "Dr.";
            o.GivenName = "John";
            o.FamilyName = "Smith";
            Assert.AreEqual("Dr. John Smith", o.Fullname);
        }

        [Test]
        public void Test_VirtualField_Place()
        {
            Operator o;

            o = new Operator();
            o.Zip = "123";
            o.City = "ZÃrich";
            Assert.AreEqual("123 ZÃrich", o.Place);

            o = new Operator();
            o.City = "Bern";
            Assert.AreEqual("Bern", o.Place);
        }
    }
}
