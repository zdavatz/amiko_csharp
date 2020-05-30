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

namespace AmiKoWindows
{
    public class Constants
    {
        // Database (base) names
        public const string AIPS_DB_BASE = @"amiko_db_full_idx_";
        public const string FREQUENCY_DB_BASE = @"amiko_frequency_";
        public const string PATIENT_DB_BASE = @"amiko_patient_";

        // File directories
        public const string REPORT_FILE_BASE = @"amiko_report_";
        public const string INTERACTIONS_CSV_BASE = @"drug_interactions_csv_";

        // Resource files
        public const string IMG_FOLDER = @"Resources/img";
        public const string JS_FOLDER = @"Resources/js";
        public const string CSS_SHEET = @"Resources/css/fachinfo_css.css";
        public const string INTERACTIONS_SHEET = @"Resources/css/interactions_css.css";
        public const string FULLTEXT_SHEET = @"Resources/css/fulltext_style_css.css";
        public const string SHOPPING_SHEET = @"Resources/css/shopping_css.css";
        public const string ROSE_SHEET = @"Resources/css/zurrose_css.css";

        // User's profile (Account)
        public const string ACCOUNT_PICTURE_FILE = @"op_signature.png"; // (same as macOS version)

        // JSON values
        public const string JSON_GENDER_WOMAN = "woman";
        public const string JSON_GENDER_MAN = "man";

        // German section title abbreviations
        public static readonly string[] SectionTitlesDE = {
            "Zusammensetzung", "Galenische Form", "Kontraindikationen", "Indikationen",
            "Dosierung/Anwendung", "Vorsichtsmassnahmen", "Interaktionen", "Schwangerschaft", "Fahrtüchtigkeit", "Unerwünschte Wirk.",
            "Überdosierung", "Eig./Wirkung", "Kinetik", "Präklinik", "Sonstige Hinweise", "Zulassungsnummer", "Packungen", "Inhaberin",
            "Stand der Information"
        };

        // French section title abbrevations
        public static readonly string[] SectionTitlesFR = {
            "Composition", "Forme galénique", "Contre-indications", "Indications",
            "Posologie", "Précautions", "Interactions", "Grossesse/All.", "Conduite", "Effets indésir.", "Surdosage", "Propriétés/Effets",
            "Cinétique", "Préclinique", "Remarques", "Numéro d'autorisation", "Présentation", "Titulaire", "Mise à jour"
        };

#if AMIKO
        public static readonly string GoogleClientId = "<your amiko google client id>";
        public static readonly string GoogleClientSecret = "<your amiko google client secret>";
#elif COMED
        public static readonly string GoogleClientId = "<your comed google client id>";
        public static readonly string GoogleClientSecret = "<your comed google client secret>";
#endif
    }
}
