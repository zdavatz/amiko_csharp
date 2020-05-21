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
using System.Linq;
using System.Web.Script.Serialization;

// JSON Data Presenters
namespace AmiKoWindows
{
    // NOTE
    //
    // json properties for prescription
    // ```
    // Contact -> patient
    // Account -> operator
    // Medication[] -> medications
    // ```
    public class PrescriptionJSONPresenter
    {
        public string prescription_hash { get; set; }
        public string place_date { get; set; }

        private AccountJSONPresenter _accountPresenter;
        public AccountJSONPresenter @operator {
            get { return _accountPresenter; }
            set { this._accountPresenter = value; }
        }

        private ContactJSONPresenter _contactPresenter;
        public ContactJSONPresenter patient {
            get { return _contactPresenter; }
            set { this._contactPresenter = value; }
        }

        private MedicationJSONPresenter[] _medications;
        public MedicationJSONPresenter[] medications {
            get {
                if (_medications == null)
                    return new MedicationJSONPresenter[]{};
                return _medications;
            }
            set { this._medications = value; }
        }

        #region Accessors for Internal Objects
        [ScriptIgnore]
        public Account Account {
            set { _accountPresenter = new AccountJSONPresenter(value); }
            get {
                if (_accountPresenter == null)
                    return null;

                var account = new Account();
                account.Title = _accountPresenter.title;
                account.GivenName = _accountPresenter.given_name;
                account.FamilyName = _accountPresenter.family_name;
                account.Address = _accountPresenter.postal_address;
                account.City = _accountPresenter.city;
                account.Zip = _accountPresenter.zip_code;
                account.Phone = _accountPresenter.phone_number;
                account.Email = _accountPresenter.email_address;
                account.Signature = _accountPresenter.signature;
                return account;
            }
        }

        [ScriptIgnore]
        public Contact Contact {
            set { _contactPresenter = new ContactJSONPresenter(value); }
            get {
                if (_contactPresenter == null)
                    return null;

                // NOTE:
                // There are no fields like `_Id` and `TimeStamp` for Patient (Contact) in json
                var contact = new Contact();
                contact.Uid = _contactPresenter.patient_id;
                contact.GivenName = _contactPresenter.given_name;
                contact.FamilyName = _contactPresenter.family_name;
                contact.Birthdate = _contactPresenter.birth_date;
                contact.IsFemale = _contactPresenter.gender.Equals(Constants.JSON_GENDER_WOMAN); // Gender
                contact.WeightKg = _contactPresenter.weight_kg;
                contact.HeightCm = _contactPresenter.height_cm;
                contact.Zip = _contactPresenter.zip_code;
                contact.City = _contactPresenter.city;
                contact.Country = _contactPresenter.country;
                contact.Address = _contactPresenter.postal_address;
                contact.Phone = _contactPresenter.phone_number;
                contact.Email = _contactPresenter.email_address;
                return contact;
            }
        }

        [ScriptIgnore]
        public List<Medication> MedicationsList {
            set { _medications = value.Select(m => new MedicationJSONPresenter(m)).ToArray(); }
            get {
                if (_medications == null)
                    return null;
                else if (_medications.Length == 0)
                    return new List<Medication>();

                // NOTE: There is no field `TimeStamp` for Medication in json
                return _medications.Select(m => {
                    var medication = new Medication();
                    medication.Regnrs = m.regnrs;
                    medication.Owner = m.owner;
                    medication.Atccode = m.atccode;
                    medication.Title = m.title;
                    medication.Package = m.package;
                    medication.Comment = m.comment;
                    medication.Eancode = m.eancode;
                    medication.ProductName = m.product_name;
                    return medication;
                }).ToList();
            }
        }
        #endregion

        public PrescriptionJSONPresenter()
        {
            // pass (for deserialization)
        }

        public PrescriptionJSONPresenter(string hash , string placeDate)
        {
            this.prescription_hash = hash;
            this.place_date = placeDate;
        }
    }

    // Operator in Prescription
    public class AccountJSONPresenter
    {
        public string title { get; set; }
        public string given_name { get; set; }
        public string family_name { get; set; }
        public string postal_address { get; set; }
        public string city { get; set; }
        public string zip_code { get; set; }
        public string phone_number { get; set; }
        public string email_address { get; set; }
        public string signature { get; set; }

        public AccountJSONPresenter()
        {
            // pass (for deserialization)
        }

        public AccountJSONPresenter(Account account)
        {
            if (account != null)
            {
                this.title = account.Title;
                this.given_name = account.GivenName;
                this.family_name = account.FamilyName;
                this.postal_address = account.Address;
                this.city = account.City;
                this.zip_code = account.Zip;
                this.phone_number = account.Phone;
                this.email_address = account.Email;
                this.signature = account.Signature;
            }
        }
    }

    // Patient in Prescription
    public class ContactJSONPresenter
    {
        public string patient_id { get; set; }
        public string given_name { get; set; }
        public string family_name { get; set; }
        public string birth_date { get; set; }
        public string gender { get; set; }
        public string weight_kg { get; set; }
        public string height_cm { get; set; }
        public string zip_code { get; set; }
        public string city { get; set; }
        public string country { get; set; }
        public string postal_address { get; set; }
        public string phone_number { get; set; }
        public string email_address { get; set; }

        public ContactJSONPresenter()
        {
            // pass (for deserialization)
        }

        public ContactJSONPresenter(Contact patient)
        {
            if (patient != null)
            {
                this.patient_id = patient.Uid;
                this.given_name = patient.GivenName;
                this.family_name = patient.FamilyName;
                this.birth_date = patient.Birthdate;
                this.gender = patient.IsMale ? Constants.JSON_GENDER_MAN : Constants.JSON_GENDER_WOMAN;
                this.weight_kg = patient.WeightKg;
                this.height_cm = patient.HeightCm;
                this.zip_code = patient.Zip;
                this.city = patient.City;
                this.country = patient.Country;
                this.postal_address = patient.Address;
                this.phone_number = patient.Phone;
                this.email_address = patient.Email;
            }
        }
    }

    public class MedicationJSONPresenter
    {
        public string regnrs { get; set; }
        public string owner { get; set; }
        public string atccode { get; set; }
        public string title { get; set; }
        public string package { get; set; }
        public string comment { get; set; }
        public string eancode { get; set; }
        public string product_name { get; set; }

        public MedicationJSONPresenter()
        {
            // pass (for deserialization)
        }

        public MedicationJSONPresenter(Medication medication)
        {
            if (medication != null)
            {
                this.regnrs = medication.Regnrs;
                this.owner = medication.Owner;
                this.atccode = medication.Atccode;
                this.title = medication.Title;
                this.package = medication.Package;
                this.comment = medication.Comment;

                // Optional (same as macOS Version, v3.4.4)
                // https://github.com/zdavatz/amiko-osx/blob/23ab3a89aa4e40c1a503fbad3d9fb33a9270fd31/MLPrescriptionsAdapter.m#L258-L259
                if (medication.Eancode != null && !medication.Eancode.Equals(string.Empty))
                    this.eancode = medication.Eancode;

                this.product_name = medication.ProductName;
            }
        }
    }

    // The "account setting" of the doctor, not the operator in amk files
    public class SettingAccountJSONPresenter
    {
        public string title { get; set; }
        public string name { get; set; }
        public string surname { get; set; }
        public string street { get; set; }
        public string city { get; set; }
        public string zip { get; set; }
        public string phone { get; set; }
        public string email { get; set; }

        public SettingAccountJSONPresenter()
        {
            // pass (for deserialization)
        }

        public SettingAccountJSONPresenter(Account account)
        {
            this.Account = account;
        }

        [ScriptIgnore]
        public Account Account
        {
            get
            {
                var account = new Account();
                account.Title = this.title;
                account.GivenName = this.name;
                account.FamilyName = this.surname;
                account.Address = this.street;
                account.City = this.city;
                account.Zip = this.zip;
                account.Phone = this.phone;
                account.Email = this.email;
                return account;
            }

            set
            {
                var account = value;
                this.title = account.Title;
                this.name = account.GivenName;
                this.surname = account.FamilyName;
                this.street = account.Address;
                this.city = account.City;
                this.zip = account.Zip;
                this.phone = account.Phone;
                this.email = account.Email;
            }
        }
    }
}
