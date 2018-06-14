using System;
using System.IO;
using System.Globalization;
using System.Threading;
using NUnit.Framework;

using AmiKoWindows;


namespace AmiKoWindows.Tests
{
    [TestFixture]
    public class PatientDbTest
    {
        [TearDown]
        public void TearDown()
        {
            // pass
        }

        [Test]
        public void Test_BirthdateValidationFailure()
        {
            var patientDb = new PatientDb();
            Assert.IsFalse(patientDb.ValidateField("birthdate", ""));
            Assert.IsFalse(patientDb.ValidateField("birthdate", "invalid"));
            Assert.IsFalse(patientDb.ValidateField("birthdate", "00.00.0000"));
            Assert.IsFalse(patientDb.ValidateField("birthdate", "32.05.2018"));
        }

        [Test]
        public void Test_BirthdateValidationSuccess()
        {
            var patientDb = new PatientDb();
            Assert.IsTrue(patientDb.ValidateField("birthdate", "01.01.1970"));
            Assert.IsTrue(patientDb.ValidateField("birthdate", "31.05.2018"));
        }
    }
}
