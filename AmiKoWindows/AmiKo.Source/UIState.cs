using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AmiKoWindows
{
    public class UIState : INotifyPropertyChanged
    {
        public enum State
        {
            Compendium, Favorites, Interactions, Shopping, FullTextSearch
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
