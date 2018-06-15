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

// View state holder for AddressBook (Book) and Doctor's ProfileCard (Card)
public class ViewType : INotifyPropertyChanged
{
    private string _mode;

    public string Mode
    {
        get { return this._mode; }
        set { this._mode = value; NotifyChanged("Mode"); }
    }

    private bool _hasBook;
    private bool _hasCard;

    public bool HasBook
    {
        get { return this._hasBook; }
        set { this._hasBook = value; NotifyChanged("HasBook"); }
    }

    public bool HasCard
    {
        get { return this._hasCard; }
        set { this._hasCard = value; NotifyChanged("HasCard"); }
    }

    public ViewType(string modeName) {
      this.Mode = modeName;
      this.HasBook = false;
      this.HasCard = false;
    }

    public ViewType(string mode, bool hasBook) {
      this.Mode = mode;
      this.HasBook = hasBook;
      this.HasCard = false;
    }

    public event PropertyChangedEventHandler PropertyChanged;
    void NotifyChanged(string property)
    {
        if (PropertyChanged != null)
            PropertyChanged(this, new PropertyChangedEventArgs(property));
    }
}
