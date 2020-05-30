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

    enum OverlayMode
    {
        None = 0,
        Book = 1,
        Card = 2,
        Settings = 3,
    }
    private OverlayMode _overlayMode;

    public bool HasBook
    {
        get { return this._overlayMode == OverlayMode.Book; }
        set { this._overlayMode = value ? OverlayMode.Book : OverlayMode.None; NotifyChanged("HasBook"); }
    }

    public bool HasCard
    {
        get { return this._overlayMode == OverlayMode.Card; }
        set { this._overlayMode = value ? OverlayMode.Card : OverlayMode.None; NotifyChanged("HasCard"); }
    }

    public bool HasSettings
    {
        get { return this._overlayMode == OverlayMode.Settings; }
        set { this._overlayMode = value ? OverlayMode.Settings : OverlayMode.None; NotifyChanged("HasSettings"); }
    }

    public ViewType(string modeName) {
      this.Mode = modeName;
      this._overlayMode = OverlayMode.None;
    }

    public ViewType(string mode, bool hasBook) {
      this.Mode = mode;
      this.HasBook = hasBook;
    }

    public event PropertyChangedEventHandler PropertyChanged;
    void NotifyChanged(string property)
    {
        if (PropertyChanged != null)
            PropertyChanged(this, new PropertyChangedEventArgs(property));
    }
}
