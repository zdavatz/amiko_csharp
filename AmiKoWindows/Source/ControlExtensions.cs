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
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AmiKoWindows
{
    namespace ControlExtensions
    {
        /// <summary>
        /// The extension methods for user feedback on custom user control.
        /// </summary>
        public static class FeedbackExtension
        {
            public static void FeedbackField(this UserControl _control, TextBox box, bool hasError)
            {
                if (box == null)
                    return;

                if (hasError)
                {
                    var converter = new BrushConverter();
                    Brush errFieldColor = converter.ConvertFrom(Constants.ErrorFieldColor) as Brush;
                    Brush errBrushColor = converter.ConvertFrom(Constants.ErrorBrushColor) as Brush;

                    box.Background = errFieldColor;
                    box.BorderBrush = errBrushColor;
                }
                else
                {
                    box.Background = Brushes.White;
                    box.BorderBrush = Brushes.DarkGray;
                }
            }

            public static void FeedbackMessage(this UserControl _control, TextBlock block, bool needsDisplay)
            {
                if (block == null)
                    return;

                if (!needsDisplay)
                    block.Visibility = Visibility.Hidden;
                else
                    block.Visibility = Visibility.Visible;
            }
        }
    }
}
