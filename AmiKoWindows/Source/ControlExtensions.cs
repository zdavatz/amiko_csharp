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
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AmiKoWindows
{
    namespace ControlExtensions
    {
        /// <summary>
        /// The extension methods for the finding in visual tree.
        /// </summary>
        public static class VisualTreeFinder
        {
            public static IEnumerable<T> FindVisualChildren<T>(this DependencyObject obj) where T: DependencyObject
            {
                if (obj == null)
                    yield break;

                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                    if (child != null && child is T)
                        yield return (T)child;

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                        yield return childOfChild;
                }
            }

            public static T FindVisualChild<T>(this DependencyObject obj) where T: DependencyObject
            {
                foreach (T child in FindVisualChildren<T>(obj))
                {
                    return child;
                }
                return null;
            }

            public static T FindVisualAncestor<T>(this Control _control, FrameworkElement element) where T: FrameworkElement
            {
                var parent = VisualTreeHelper.GetParent(element);
                if (parent != null && !(parent is T))
                    return (T)FindVisualAncestor<T>(_control, parent as FrameworkElement);
                return (T)parent;
            }
        }

        /// <summary>
        /// The extension methods for user feedback on custom user control.
        /// </summary>
        public static class FeedbackExtension
        {
            public static void FeedbackField<T>(this UserControl _control, T element, bool hasError)
            {
                // TODO:
                // Refactor
                // TexBox and Border are FrameworkElement, but TextBox is Control, Border is not Control...
                if (element == null || !(element is T))
                    return;

                var converter = new BrushConverter();
                if (element is TextBox)
                {
                    var box = element as TextBox;
                    if (hasError)
                    {
                        box.BorderBrush = converter.ConvertFrom(Colors.ErrorBrushColor) as Brush;
                        box.Background = converter.ConvertFrom(Colors.ErrorFieldColor) as Brush;
                    }
                    else
                    {
                        box.BorderBrush = Brushes.DarkGray;
                        box.Background = Brushes.White;
                    }
                }
                else if (element is Image)
                {
                    // NOTE: Assumes Image's parent element is Border
                    var img = element as Image;
                    var border = img.Parent as Border;
                    if (border == null)
                        return;

                    if (hasError)
                    {
                        border.BorderBrush = converter.ConvertFrom(Colors.ErrorBrushColor) as Brush;
                        border.Background = converter.ConvertFrom(Colors.ErrorFieldColor) as Brush;
                    }
                    else
                    {
                        border.BorderBrush = Brushes.LightGray;
                        border.Background = converter.ConvertFrom(Colors.PaleGray) as Brush;
                    }
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
