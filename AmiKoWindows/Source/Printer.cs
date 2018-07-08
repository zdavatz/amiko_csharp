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
using System.IO;
using System.Linq;
using System.Printing;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;

namespace AmiKoWindows
{
    using ControlExtensions;

    class Printer
    {
        public const double INCH = 96;
        public const double DIP = 37.8;

        public static void PrintPrescription(Prescription prescription)
        {
            var dialog = new PrintDialog();
            dialog.PageRangeSelection = PageRangeSelection.AllPages;
            dialog.UserPageRangeEnabled = false;

            // PageMediaSizeName.ISOA4 will be null in some case :'(
            var pageSize = new PageMediaSize(8.29 * INCH, 11.69 * INCH);
            dialog.PrintTicket.PageMediaSize = pageSize;

            PrintTicket ticket = dialog.PrintTicket;
            Double printableWidth = ticket.PageMediaSize.Width.Value;
            Double printableHeight = ticket.PageMediaSize.Height.Value;

            Log.WriteLine("{0} dip", printableWidth);
            Log.WriteLine("{0} dip", printableHeight);

            // 1cm = 37.8dip
            Log.WriteLine("{0} cm", printableWidth / DIP);
            Log.WriteLine("{0} cm", printableHeight / DIP);

            // scale
            double xMargin = 0;
            double yMargin = 0;
            Double xScale = ((printableWidth - xMargin * 2) / printableWidth);
            Double yScale = ((printableHeight - yMargin * 2) / printableHeight);

            var matrix =  new MatrixTransform(xScale, 0, 0, yScale, xMargin, yMargin);
            prescription.RenderTransform = matrix;
            prescription.LayoutTransform = matrix;

            List<Prescription> pages = new List<Prescription>();

            prescription.SetAccountPicture();
            prescription.MedicationList.DataContext = prescription;
            prescription.UpdateMedicationList();
            prescription.MedicationList.UpdateLayout();
            prescription.MedicationList.ApplyTemplate();
            prescription.MedicationList.BringIntoView();

            // first page
            var w = (printableWidth - (prescription.lMargin + prescription.rMargin));
            var h = (printableHeight - (prescription.tMargin + prescription.bMargin));
            Log.WriteLine("ListBox (max) Width: {0}", w);
            Log.WriteLine("ListBox (max) Height: {0}", prescription.MedicationListBoxMaxHeight);
            prescription.MedicationList.Measure(new Size(w, h));
            prescription.MedicationList.Arrange(new Rect());

            var aw = prescription.MedicationList.ActualWidth;
            var ah = prescription.MedicationList.ActualHeight;
            Log.WriteLine("ListBox ActualWidth: {0}", aw);
            Log.WriteLine("ListBox ActualHeight: {0}", ah);

            List<Medication> toNext = new List<Medication>();
            while (ah >= prescription.MedicationListBoxMaxHeight)
            {
                var medication = prescription.PopMedication();
                prescription.MedicationList.DataContext = null;
                prescription.MedicationList.DataContext = prescription;
                prescription.UpdateMedicationList();
                prescription.MedicationList.UpdateLayout();
                prescription.MedicationList.ApplyTemplate();
                prescription.MedicationList.BringIntoView();

                prescription.MedicationList.Measure(new Size(w, h));
                prescription.MedicationList.Arrange(new Rect());

                ah = prescription.MedicationList.ActualHeight;
                Log.WriteLine("ListBox ActualHeight: {0}", ah);
                toNext.Add(medication);
            }

            prescription.SetAccountPicture();

            // Without these lines, page losts their elements if the document is
            // oen page ... why?
            prescription.AccountInfo.UpdateLayout();
            prescription.AccountInfo.BringIntoView();
            prescription.AccountInfo.UpdateLayout();
            prescription.AccountInfo.BringIntoView();

            prescription.RenderTransform = matrix;
            prescription.LayoutTransform = matrix;

            pages.Add(prescription);

            var filename = prescription.FileName.Text;
            var placeDate = prescription.PlaceDate.Text;

            // >= 2 pages
            var i = 2;
            Log.WriteLine("toNext.Count: {0}", toNext.Count);
            while (toNext.Count > 0)
            {
                toNext.Reverse();
                var prescriptionB = new Prescription(filename, placeDate)
                {
                    ActiveContact = prescription.ActiveContact,
                    ActiveAccount = prescription.ActiveAccount,
                    Medications = toNext,
                    PageNumber = i
                };
                prescriptionB.Info.Visibility = Visibility.Collapsed;

                prescriptionB.RenderTransform = matrix;
                prescriptionB.LayoutTransform = matrix;

                prescriptionB.MedicationList.MaxHeight = h;
                prescriptionB.MedicationList.DataContext = prescriptionB;
                prescriptionB.UpdateMedicationList();
                prescriptionB.MedicationList.UpdateLayout();
                prescriptionB.MedicationList.ApplyTemplate();
                prescriptionB.MedicationList.BringIntoView();

                prescriptionB.MedicationList.Measure(new Size(w, h + 1));
                prescriptionB.MedicationList.Arrange(new Rect());

                var aw2 = prescriptionB.MedicationList.ActualWidth;
                var ah2 = prescriptionB.MedicationList.ActualHeight;
                Log.WriteLine("(page {0}) ListBox ActualWidth: {1}", i, aw2);
                Log.WriteLine("(page {0}) ListBox ActualHeight: {1}", i, ah2);
                Log.WriteLine("h: {0}", h);

                var toNextB = new List<Medication>();
                while (ah2 >= h)
                {
                    var medication = prescriptionB.PopMedication();
                    prescriptionB.MedicationList.DataContext = null;
                    prescriptionB.MedicationList.DataContext = prescriptionB;
                    prescriptionB.UpdateMedicationList();
                    prescriptionB.MedicationList.UpdateLayout();
                    prescriptionB.MedicationList.ApplyTemplate();
                    prescriptionB.MedicationList.BringIntoView();

                    prescriptionB.MedicationList.Measure(new Size(w, h + 1));
                    prescriptionB.MedicationList.Arrange(new Rect());

                    ah2 = prescriptionB.MedicationList.ActualHeight;
                    Log.WriteLine("(page {0}) ListBox ActualHeight: {1}", i, ah2);
                    toNextB.Add(medication);
                }
                prescriptionB.UpdateLayout();
                pages.Add(prescriptionB);

                Log.WriteLine("toNextB.Count: {0}", toNextB.Count);
                if (toNextB.Count < 1)
                    toNext.Clear();
                else
                {
                    toNext = toNextB;
                    i++;
                }
            }
            Log.WriteLine("pages.Count: {0}", pages.Count);

            pages = pages.Select(p => {
                p.TotalPages = pages.Count;
                p.Number();
                return p;
            }).ToList();

            var paginator = new Paginator<Prescription>(pages);
            paginator.PageSize = new Size(printableWidth, printableHeight);
            PrintPages(paginator, Size.Empty);
        }

        public static void PrintMedicationLabel(MedicationLabel medicationLabel)
        {
            var dialog = new PrintDialog();
            dialog.PageRangeSelection = PageRangeSelection.AllPages;
            dialog.UserPageRangeEnabled = false;

            var pageSize = new PageMediaSize(3.5 * INCH, 1.41 * INCH);
            dialog.PrintTicket.PageMediaSize = pageSize;

            PrintTicket ticket = dialog.PrintTicket;
            Double printableWidth = ticket.PageMediaSize.Width.Value;
            Double printableHeight = ticket.PageMediaSize.Height.Value;

            Log.WriteLine("{0} dip", printableWidth);
            Log.WriteLine("{0} dip", printableHeight);

            // 1cm = 37.8dip
            Log.WriteLine("{0} cm", printableWidth / DIP);
            Log.WriteLine("{0} cm", printableHeight / DIP);

            medicationLabel.Layout();

            // scale
            double xMargin = 0;
            double yMargin = 0;
            Double xScale = ((printableWidth - xMargin * 2) / printableWidth);
            Double yScale = ((printableHeight - yMargin * 2) / printableHeight);

            var matrix =  new MatrixTransform(xScale, 0, 0, yScale, xMargin, yMargin);
            medicationLabel.RenderTransform = matrix;
            medicationLabel.LayoutTransform = matrix;

            List<MedicationLabel> pages = new List<MedicationLabel>();

            var w = (printableWidth - (medicationLabel.lMargin + medicationLabel.rMargin));
            var h = (printableHeight - (medicationLabel.tMargin + medicationLabel.bMargin));
            Log.WriteLine("ListBox (max) Width: {0}", w);
            Log.WriteLine("ListBox (max) Height: {0}", h);

            pages.Add(medicationLabel);
            Log.WriteLine("pages.Count: {0}", pages.Count);

            var paginator = new Paginator<MedicationLabel>(pages);
            paginator.PageSize = new Size(printableWidth, printableHeight);

            var viewerSize = new Size(336 + 45, 135 + 120);
            PrintPages(paginator, viewerSize);
        }

        private static void PrintPages<T>(Paginator<T> paginator, Size viewerSize) where T: Page
        {
            string filename = Path.GetTempFileName();
            try
            {
                File.Delete(filename);

                // create multiple pages as 1 xps document
                XpsDocument document;
                using (document = new XpsDocument(filename, FileAccess.ReadWrite))
                {
                    XpsDocumentWriter writer = XpsDocument.CreateXpsDocumentWriter(document);
                    writer.Write(paginator);
                }

                using (document = new XpsDocument(filename, FileAccess.Read))
                {
                    DocumentViewer viewer = new DocumentViewer
                    {
                        Document = document.GetFixedDocumentSequence()
                    };

                    Window window = new Window();

                    if (!viewerSize.IsEmpty)
                    {
                        window.Width = viewerSize.Width;
                        window.Height = viewerSize.Height;
                    }

                    window.Content = viewer;

                    // actual size viewer window
                    //window.SizeToContent = SizeToContent.WidthAndHeight;

                    window.Title = Properties.Resources.appTitle;
                    window.ShowDialog();
                    window.Close();
                }
            }
            finally
            {
                if (File.Exists(filename))
                {
                    try
                    {
                        File.Delete(filename);
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine(ex.Message);
                    }
                }
            }
        }
    }

    public class Paginator<T> : DocumentPaginator, IDocumentPaginatorSource where T: Page
    {
        private List<T> _pages;
        private Size _size;
        private int _currentPage = 0;

        public Paginator(List<T> pages)
        {
            this._pages = pages;
        }

        DocumentPaginator IDocumentPaginatorSource.DocumentPaginator {
            get { return this; }
        }

        public override IDocumentPaginatorSource Source
        {
            get { return this; }
        }

        public override bool IsPageCountValid
        {
            get { return (_pages.Count == 0); }
        }

        public override int PageCount
        {
            get { return _currentPage; }
        }

        public override Size PageSize {
            get { return _size; }
            set { this._size = value; }
        }

        public override DocumentPage GetPage(int pageNumber)
        {
            this._currentPage = pageNumber + 1; // next
            Log.WriteLine("pageNumber: {0}", pageNumber);
            T page = _pages[0];
            _pages.RemoveAt(0);
            page.Measure(_size);
            page.Arrange(new Rect(_size));
            // It seems that we can't pass `Page` itself ... :'(
            var dp = new DocumentPage(page.Content as Visual, _size, new Rect(_size), new Rect(_size));
            Log.WriteLine("Size: {0}", dp.Size);
            return dp;
        }
    }
}
