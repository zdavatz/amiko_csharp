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
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AmiKoWindows
{
    public class UIState : INotifyPropertyChanged
    {
        public enum State
        {
            Compendium=0x01, Favorites=0x02, Interactions=0x04, FullTextSearch=0x08
        };

        public enum Query
        {
            Title, Author, AtcCode, Ingredient, Regnr, Application, EanCode, Fulltext
        }

        private State _uiState = State.Compendium;
        private Query _query = Query.Title;

        /**
         * Properties
         */
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged(
        [CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _searchTextBoxWaterMark;
        public string SearchTextBoxWaterMark
        {
            get { return _searchTextBoxWaterMark; }
            set
            {
                if (value != _searchTextBoxWaterMark)
                {
                    _searchTextBoxWaterMark = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /**
         * Constructors
         */
        public UIState()
        {
        }

        public UIState(Query query)
        {
            _query = query;
        }

        /**
         * Public functions
         */
        public void SetState(State uiState)
        {
            _uiState = uiState;
        }

        public State GetState()
        {
            return _uiState;
        }

        public bool IsCompendium()
        {
            return _uiState == State.Compendium;
        }

        public bool IsFavorites()
        {
            return _uiState == State.Favorites;
        }

        public bool IsInteractions()
        {
            return _uiState == State.Interactions;
        }

        public bool IsFullTextSearch()
        {
            return _uiState == State.FullTextSearch;
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

        public bool FullTextQueryEnabled()
        {
            return _query == Query.Fulltext;
        }

        public Query GetQuery()
        {
            return _query;
        }

        public string SearchQueryType()
        {
            if (_query == Query.Title)
                return "title";
            else if (_query == Query.Author)
                return "author";
            else if (_query == Query.AtcCode)
                return "atc";
            else if (_query == Query.Regnr)
                return "regnr";
            else if (_query == Query.Ingredient)
                return "ingredient";
            else if (_query == Query.Application)
                return "application";
            else if (_query == Query.Fulltext)
                return "fulltext";
            return "title";
        }
    }
}
