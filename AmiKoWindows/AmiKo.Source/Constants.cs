/*
Copyright (c) 2016 Max Lungarella <cybrmx@gmail.com>

This file is part of AmiKoWindows.

AmiKoDesk is free software: you can redistribute it and/or modify
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
        // App 
        public const string APP_VERSION = "1.0";
        public const string GEN_DATE = "06.10.2016";
        public const string APP_NAME = "AmiKoWindows";
        public const string AMIKO_NAME = "AmiKoDesktop";
    	public const string COMED_NAME = "CoMedDesktop";

        // Important folders and files
        public const string IMG_FOLDER = @"./images/";	
	    public const string JS_FOLDER = @"./jscripts/";
        public const string CSS_SHEET = @"./css/amiko_stylesheet.css";
        public const string INTERACTIONS_SHEET = @"./css/interactions_css.css";
        public const string SHOPPING_SHEET = @"./css/shopping_css.css";
        public const string ROSE_SHEET = @"./css/zurrose_css.css";

        // German section title abbreviations
        public static readonly string[] SectionTitlesDE = { "Zusammensetzung", "Galenische Form", "Kontraindikationen", "Indikationen",
			"Dosierung/Anwendung", "Vorsichtsmassnahmen", "Interaktionen", "Schwangerschaft", "Fahrtüchtigkeit", "Unerwünschte Wirk.",
			"Überdosierung", "Eig./Wirkung", "Kinetik", "Präklinik", "Sonstige Hinweise", "Zulassungsnummer", "Packungen", "Inhaberin",
			"Stand der Information" };
        // French section title abbrevations
        public static readonly string[] SectionTitlesFR = { "Composition", "Forme galénique", "Contre-indications", "Indications",
			"Posologie", "Précautions", "Interactions", "Grossesse/All.", "Conduite", "Effets indésir.", "Surdosage", "Propriétés/Effets",
			"Cinétique", "Préclinique", "Remarques", "Numéro d'autorisation", "Présentation", "Titulaire", "Mise à jour" };
    }
}
