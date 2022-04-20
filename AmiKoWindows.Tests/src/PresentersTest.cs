using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using NUnit.Framework;

using AmiKoWindows;


namespace AmiKoWindows.Tests
{
    [TestFixture]
    public class PresentersTest
    {

        private static readonly Regex WHITESPACES = new Regex(@"\s*", RegexOptions.Compiled);
        
        [SetUp]
        public void SetUp()
        {
        }

        [TearDown]
        public void TearDown()
        {
            // pass
        }

        [Test]
        public void Test_ContactJSONPresenter_Serializability()
        {
            Contact contact;

            contact = new Contact();
            var presenter = new ContactJSONPresenter(contact);
            var expected = ExpectedJSONWithObject(contact);

            string resultJSON = JsonConvert.SerializeObject(presenter);
            Assert.AreEqual(expected, resultJSON);
            Assert.IsTrue(JsonConvert.DeserializeObject<Contact>(resultJSON) is Contact);
        }

        [Test]
        public void Test_ConactJSONPresenter_Properties()
        {
            Contact contact;

            contact = new Contact();
            Dictionary<string, string> properties = new Dictionary<string, string>();
            properties.Add("Uid", contact.GenerateUid());
            properties.Add("GivenName", "John");
            properties.Add("FamilyName", "Smith");
            properties.Add("Birthdate", "3.12.2001");
            properties.Add("Gender", "1");
            properties.Add("WeightKg", "70.3");
            properties.Add("HeightCm", "186");
            properties.Add("Zip", "123");
            properties.Add("Country", "Schweiz");
            properties.Add("Address", "Internetstrasse 42");
            properties.Add("Phone", "+00 123 456 789 0");
            properties.Add("Email", "john.smith@example.org");

            foreach (KeyValuePair<string, string> entry in properties)
                contact[entry.Key] = (string)entry.Value;

            var presenter = new ContactJSONPresenter(contact);
            string resultJSON = JsonConvert.SerializeObject(presenter);

            foreach (KeyValuePair<string, string> entry in properties)
                Assert.IsTrue(resultJSON.Contains(entry.Value));
        }

        [Test]
        public void Test_AccountJSONPresenter_Serializability()
        {
            Account account;

            account = new Account();
            var presenter = new AccountJSONPresenter(account);
            var expected = ExpectedJSONWithObject(account);

            string resultJSON = JsonConvert.SerializeObject(presenter);
            Assert.AreEqual(expected, resultJSON);
            Assert.IsTrue(JsonConvert.DeserializeObject<Account>(resultJSON) is Account);
        }

        [Test]
        public void Test_AccountJSONPresenter_Properties()
        {
            Account account;

            account = new Account();
            Dictionary<string, string> properties = new Dictionary<string, string>();
            properties.Add("Title", "Mr.");
            properties.Add("GivenName", "John");
            properties.Add("FamilyName", "Smith");
            properties.Add("Zip", "123");
            properties.Add("Address", "Internetstrasse 42");
            properties.Add("Phone", "+00 123 456 789 0");
            properties.Add("Email", "john.smith@example.org");

            foreach (KeyValuePair<string, string> entry in properties)
                account[entry.Key] = (string)entry.Value;

            var presenter = new AccountJSONPresenter(account);
            string resultJSON = JsonConvert.SerializeObject(presenter);

            foreach (KeyValuePair<string, string> entry in properties)
                Assert.IsTrue(resultJSON.Contains(entry.Value));

            Assert.IsTrue(resultJSON.Contains(account.Signature));
        }

        [Test]
        public void Test_PrescriptionJSONPresenter_Serializability()
        {
            var placeDate = "Bern, 20.10.2016 (17:01:31)";
            var hash = Utilities.GenerateUUID();
            var presenter = new PrescriptionJSONPresenter(hash, placeDate);

            var expected = String.Format(WHITESPACES.Replace(@"{{
                ""prescription_hash"":{0},
                ""place_date"":{1},
                ""operator"":{2},
                ""patient"":{3},
                ""medications"":{4}
            }}", ""),
                GetStringValue(hash),
                GetStringValue(placeDate),
                GetStringValue(null),
                GetStringValue(null),
                GetStringValue<Medication>(new List<Medication>())
            );

            string resultJSON = JsonConvert.SerializeObject(presenter);
            Assert.AreEqual(expected, resultJSON);
            Assert.IsTrue(JsonConvert.DeserializeObject<Account>(resultJSON) is Account);
        }

        [Test]
        public void Test_PrescriptionJSONPresenter_Properties()
        {
            Contact contact = new Contact();
            Dictionary<string, string> contactProperties = new Dictionary<string, string>();
            contactProperties.Add("Uid", contact.GenerateUid());
            contactProperties.Add("GivenName", "John");
            contactProperties.Add("FamilyName", "Smith");
            contactProperties.Add("Birthdate", "3.12.2001");
            contactProperties.Add("Gender", "1");
            contactProperties.Add("WeightKg", "70.3");
            contactProperties.Add("HeightCm", "186");
            contactProperties.Add("Zip", "123");
            contactProperties.Add("Country", "Schweiz");
            contactProperties.Add("Address", "Internetstrasse 42");
            contactProperties.Add("Phone", "+00 123 456 789 0");
            contactProperties.Add("Email", "john.smith@example.org");

            foreach (KeyValuePair<string, string> entry in contactProperties)
                contact[entry.Key] = (string)entry.Value;

            Account account = new Account();
            Dictionary<string, string> accountProperties = new Dictionary<string, string>();
            accountProperties.Add("Title", "Dr.");
            accountProperties.Add("GivenName", "John");
            accountProperties.Add("FamilyName", "Smith");
            accountProperties.Add("Zip", "123");
            accountProperties.Add("Address", "Internetstrasse 42");
            accountProperties.Add("Phone", "+00 123 456 789 0");
            accountProperties.Add("Email", "john.smith@example.org");

            foreach (KeyValuePair<string, string> entry in accountProperties)
                account[entry.Key] = (string)entry.Value;

            var placeDate = "Bern, 20.10.2016 (17:01:31)";
            var hash = Utilities.GenerateUUID();
            var presenter = new PrescriptionJSONPresenter(hash, placeDate);

            // set properties
            presenter.Contact = contact;
            presenter.Account = account;
            presenter.MedicationsList = new List<Medication>();

            string resultJSON = JsonConvert.SerializeObject(presenter);

            Assert.IsTrue(resultJSON.Contains(placeDate));
            Assert.IsTrue(resultJSON.Contains(hash));

            foreach (KeyValuePair<string, string> entry in contactProperties)
                Assert.IsTrue(resultJSON.Contains(entry.Value));

            foreach (KeyValuePair<string, string> entry in accountProperties)
                Assert.IsTrue(resultJSON.Contains(entry.Value));

            Assert.IsTrue(resultJSON.Contains(account.Signature));
        }

        #region Private Utilities
        private string GetStringValue(object input)
        {
            var result = input as string;
            if (result == null)
                return "null";
            else
                return String.Format(@"""{0}""", result);
        }

        private string GetStringValue<T>(List<T> input)
        {
            var result = input as List<T>;
            if (result == null || result.Count == 0)
                return "[]";
            else
                return String.Format(@"""{0}""", result.ToString());
        }

        private string ExpectedJSONWithObject(Contact contact)
        {
            return String.Format(WHITESPACES.Replace(@"{{
                ""patient_id"":{0},
                ""given_name"":{1},
                ""family_name"":{2},
                ""birth_date"":{3},
                ""gender"":{4},
                ""weight_kg"":{5},
                ""height_cm"":{6},
                ""zip_code"":{7},
                ""city"":{8},
                ""country"":{9},
                ""postal_address"":{10},
                ""phone_number"":{11},
                ""email_address"":{12}
            }}", ""),
                GetStringValue(contact.Uid),
                GetStringValue(contact.GivenName),
                GetStringValue(contact.FamilyName),
                GetStringValue(contact.Birthdate),
                GetStringValue(contact.IsMale ? "man" : "woman"),
                GetStringValue(contact.WeightKg),
                GetStringValue(contact.HeightCm),
                GetStringValue(contact.Zip),
                GetStringValue(contact.City),
                GetStringValue(contact.Country),
                GetStringValue(contact.Address),
                GetStringValue(contact.Phone),
                GetStringValue(contact.Email)
            );
        }

        private string ExpectedJSONWithObject(Account account)
        {
            return String.Format(WHITESPACES.Replace(@"{{
                ""title"":{0},
                ""given_name"":{1},
                ""family_name"":{2},
                ""postal_address"":{3},
                ""city"":{4},
                ""zip_code"":{5},
                ""phone_number"":{6},
                ""email_address"":{7},
                ""signature"":{8}
            }}", ""),
                GetStringValue(account.Title),
                GetStringValue(account.GivenName),
                GetStringValue(account.FamilyName),
                GetStringValue(account.Address),
                GetStringValue(account.City),
                GetStringValue(account.Zip),
                GetStringValue(account.Phone),
                GetStringValue(account.Email),
                GetStringValue(account.Signature)
            );
        }
        #endregion
    }
}
