﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AmiKoWindows.Source.HINClient
{
    class SDSProfileResponse
    {
        public string LoginName;
        public string Email;
        public string FirstName;
        public string MiddleName;
        public string LastName;
        public string Gender; // "M" / "F"
        public DateTime DateOfBirth;
        public string Address;
        public string PostalCode;
        public string City;
        public string CountryCode;
        public string PhoneNumber;
        public string GLN;
        public string VerifiactionLevel;

        static SDSProfileResponse FromJSON(string jsonStr)
        {
            SDSProfileResponseJSONPresenter o = JsonConvert.DeserializeObject<SDSProfileResponseJSONPresenter>(jsonStr);
            var p = new SDSProfileResponse();
            p.LoginName = o.loginName;
            p.Email = o.email;
            p.FirstName = o.contactId.firstName;
            p.MiddleName = o.contactId.middleName;
            p.LastName = o.contactId.lastName;
            p.Gender = o.contactId.gender;
            p.DateOfBirth = o.contactId.dateOfBirth;
            p.Address = o.contactId.address;
            p.PostalCode = o.contactId.postalCode;
            p.City = o.contactId.city;
            p.CountryCode = o.contactId.countryCode;
            p.PhoneNumber = o.contactId.phoneNr;
            p.GLN = o.contactId.gln;
            p.VerifiactionLevel = o.contactId.verificationLevel;
            return p;
        }
    }

    class SDSProfileResponseJSONPresenter {
        public string loginName;
        public string email;
        public SDSProfileResponseContactIdJSONPresenter contactId;
    }
    class SDSProfileResponseContactIdJSONPresenter
    {
        public string firstName;
        public string middleName;
        public string lastName;
        public string gender; // M / F
        public DateTime dateOfBirth;
        public string address;
        public string postalCode;
        public string city;
        public string countryCode;
        public string phoneNr;
        public string gln;
        public string verificationLevel;
    }
}
