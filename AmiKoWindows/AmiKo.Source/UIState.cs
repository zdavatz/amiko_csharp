using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AmiKoWindows
{
    class UIState : INotifyPropertyChanged
    {
        public enum State
        {
            Compendium, Favorites, Interactions, Shopping
        };

        public enum Query
        {
            Title, Author, AtcCode, Ingredient, Regnr, Application, EanCode
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

        public void SetQuery(Query query)
        {
            _query = query;
            switch (query)
            {
                case Query.Title:
                    SearchTextBoxWaterMark = "Suche Präparat...";
                    break;
                case Query.Author:
                    SearchTextBoxWaterMark = "Suche Inhaberin...";
                    break;
                case Query.AtcCode:
                    SearchTextBoxWaterMark = "Suche Wirkstoff / ATC...";
                    break;
                case Query.Regnr:
                    SearchTextBoxWaterMark = "Suche Zulassungsnummer...";
                    break;
                case Query.Application:
                    SearchTextBoxWaterMark = "Suche Therapie...";
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
            return "title";
        }
    }
}
