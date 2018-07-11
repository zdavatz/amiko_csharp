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
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AmiKoWindows
{
    public class UIState : INotifyPropertyChanged
    {
        public enum State
        {
            Compendium=0x01, Favorites=0x02, Interactions=0x03, Prescriptions=0x04,
            FullTextSearch=0x08,
        };

        public enum Query
        {
            Title, Author, AtcCode, Ingredient, Regnr, Application, EanCode,
            Fulltext
        }

        #region private Fields
        private State _uiState = State.Compendium;
        private Query _query = Query.Title;
        #endregion

        #region Public Fields
        private string _searchTextBoxWaterMark;
        public string SearchTextBoxWaterMark
        {
            get { return _searchTextBoxWaterMark; }
            set
            {
                if (value != _searchTextBoxWaterMark)
                {
                    _searchTextBoxWaterMark = value;
                    OnPropertyChanged(string.Empty);
                }
            }
        }

        public bool IsCompendium
        {
            get { return _uiState == State.Compendium; }
        }

        public bool IsFavorites
        {
            get { return _uiState == State.Favorites; }
        }

        public bool IsInteractions
        {
            get { return _uiState == State.Interactions; }
        }

        public bool IsPrescriptions
        {
            get { return _uiState == State.Prescriptions; }
        }

        public bool FullTextQueryEnabled
        {
            get { return _query == Query.Fulltext; }
        }
        #endregion

        #region Event Handlers
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        public UIState()
        {
        }

        public UIState(Query query)
        {
            _query = query;
        }

        /**
         * Static functions
         */
        // see GetQueryTypeAsName
        public static Query QueryBySourceName(string sourceName)
        {
            string name = sourceName.Trim().ToLower();
            //Log.WriteLine("name: {0}", name);

            if (name.Equals("title"))
                return Query.Title;
            else if (name.Equals("author"))
                return Query.Author;
            else if (name.Equals("atccode"))
                return Query.AtcCode;
            else if (name.Equals("regnr"))
                return Query.Regnr;
            else if (name.Equals("ingredient"))
                return Query.Ingredient;
            else if (name.Equals("application"))
                return Query.Application;
            else if (name.Equals("eancode"))
                return Query.EanCode;
            else if (name.Equals("fulltext"))
                return Query.Fulltext;
            return Query.Title;
        }

        public void SetState(State uiState)
        {
            _uiState = uiState;
        }

        public State GetState()
        {
            return _uiState;
        }

        public void SetQuery(Query query)
        {
            string search = Properties.Resources.search + " ";

            _query = query;
            switch (query)
            {
                case Query.Title:
                    SearchTextBoxWaterMark = search + Properties.Resources.butTitle + "...";
                    break;
                case Query.Author:
                    SearchTextBoxWaterMark = search + Properties.Resources.butAuthor + "...";
                    break;
                case Query.AtcCode:
                    SearchTextBoxWaterMark = search + Properties.Resources.butAtccode + "...";
                    break;
                case Query.Regnr:
                    SearchTextBoxWaterMark = search + Properties.Resources.butRegnr + "...";
                    break;
                case Query.Application:
                    SearchTextBoxWaterMark = search + Properties.Resources.butTherapy + "...";
                    break;
                case Query.Fulltext:
                    SearchTextBoxWaterMark = search + Properties.Resources.butFulltext + "...";
                    break;
                default:
                    break;
            }
        }

        public Query GetQuery()
        {
            return _query;
        }

        public string GetQueryTypeAsName()
        {
            if (_query == Query.Title)
                return "title";
            else if (_query == Query.Author)
                return "author";
            else if (_query == Query.AtcCode)
                return "atccode";
            else if (_query == Query.Regnr)
                return "regnr";
            else if (_query == Query.Ingredient)
                return "ingredient";
            else if (_query == Query.Application)
                return "application";
            else if (_query == Query.EanCode)
                return "eancode";
            else if (_query == Query.Fulltext)
                return "fulltext";
            return "title";
        }
    }
}
