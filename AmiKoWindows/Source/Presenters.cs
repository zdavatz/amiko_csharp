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

// JSON Data Presenters
namespace AmiKoWindows
{
    // NOTE
    //
    // json properties for prescription
    // ```
    // Contact -> patient
    // Account -> operotar
    // Medication[] -> medications
    // ```
    class PrescriptionJSONPresenter
    {
        public string prescription_hash { get; set; }
        public string place_date { get; set; }

        private AccountJSONPresenter _accountPresenter;
        public Account Account {
            set { _accountPresenter = new AccountJSONPresenter(value); }
        }
        public AccountJSONPresenter @operator {
            get { return _accountPresenter; }
            set { this._accountPresenter = value; }
        }

        private ContactJSONPresenter _contactPresenter;
        public Contact Contact {
            set { _contactPresenter = new ContactJSONPresenter(value); }
        }
        public ContactJSONPresenter patient {
            get { return _contactPresenter; }
            set { this._contactPresenter = value; }
        }

        private Medication[] _medicationsArray;
        public Medication[] medications {
            get {
                if (_medicationsArray == null)
                    return new Medication[]{};
                return _medicationsArray;
            }
            set { this._medicationsArray = value; }
        }

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
    class AccountJSONPresenter
    {
        // NOTE: the type of all fields is string
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

    // Patient in Prescription
    class ContactJSONPresenter
    {
        // NOTE: the type of all fields is string
        public string patient_id { get; set; }
        public string family_name { get; set; }
        public string given_name { get; set; }
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
            this.patient_id = patient.Uid;
            this.family_name = patient.FamilyName;
            this.given_name = patient.GivenName;
            this.birth_date = patient.Birthdate;
            this.gender = patient.IsMale ? "men" : "women";
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
