﻿using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VaultItemProcessor
{
    public static class ProcessPDF
    {
        public static bool CopyPDF(string inputFolder, List<string> filesToCopy, List<string> watermarks, string outputFolder, string orderNumber="", string jobName = "")
        {
            // copy pdf file(s) from input folder to output folder and stamp them with a watermark.  If there is more than one file in the list, they are combined into one, 
            //  with the filename matching the name of the first file to copy.  There needs to be an equal amount of files and watermarks passed in.
            try
            {
                if (filesToCopy.Count != watermarks.Count)
                    return false;

                PdfDocument outputDocument = new PdfDocument();
                PdfDocument inputDocument = new PdfDocument();
                PdfDocument editedDocument = new PdfDocument();

                XUnit height = new XUnit();
                XUnit width = new XUnit();
                List<XUnit[]> xUnitArrayList = new List<XUnit[]>();

                int fileCount = 0;
                foreach (string fileName in filesToCopy)
                {
                    string inputPdfName = inputFolder + fileName;
                    xUnitArrayList.Clear();
                    if (File.Exists(inputPdfName))
                    {
                        inputDocument = PdfReader.Open(inputPdfName, PdfDocumentOpenMode.Modify);

                        int count = inputDocument.PageCount;
                        for (int idx = 0; idx < count; idx++)
                        {
                            PdfPage page = inputDocument.Pages[idx];
                            PdfPage watermarkPage = new PdfPage(inputDocument);
                            
                            // store page width and height in array list so we can reference again when we are producing output
                            height = page.Height;
                            width = page.Width;
                            watermarkPage.Height = page.Height;
                            watermarkPage.Width = page.Width;

                            XUnit[] pageDims = new XUnit[] { page.Height, page.Width };
                            xUnitArrayList.Add(pageDims);       // drawing page
                            xUnitArrayList.Add(pageDims);       // watermark page

                            XGraphics gfx = XGraphics.FromPdfPage(watermarkPage, XGraphicsPdfPageOptions.Prepend);

                            XFont font = new XFont("Times New Roman", 15, XFontStyle.Bold);
                            XTextFormatter tf = new XTextFormatter(gfx);

                            XRect rect = new XRect(40, 75, width - 40, height - 75);
                            XBrush brush = new XSolidBrush(XColor.FromArgb(128, 255, 0, 0));
                            tf.DrawString(watermarks[fileCount], font, brush, rect, XStringFormats.TopLeft);

                            //inputDocument.AddPage(watermarkPage);
                            inputDocument.InsertPage(idx*2+1, watermarkPage);
                        }

                        string randomFileName = Path.GetTempFileName();
                        inputPdfName = randomFileName;
                        inputDocument.Save(randomFileName);


                        editedDocument = PdfReader.Open(randomFileName, PdfDocumentOpenMode.Import);

                        // Iterate pages
                        count = editedDocument.PageCount;
                        for (int idx = 0; idx < count; idx++)
                        {
                            // Get the page from the external document...
                            PdfPage editedPage = editedDocument.Pages[idx];

                            XUnit[] outputPageDims = xUnitArrayList[idx];
                            editedPage.Height = outputPageDims[0];
                            editedPage.Width = outputPageDims[1];

                            // ...and add it to the output document.
                            outputDocument.AddPage(editedPage);

                        }
                    }

                    if (!File.Exists(inputPdfName))
                    {
                        watermarks[fileCount] = "No Drawing Found For:\n" + watermarks[fileCount];

                        string randomFileName = Path.GetTempFileName();
                        inputPdfName = randomFileName;

                        // Create a new PDF document
                        PdfDocument document = new PdfDocument();
                        document.Info.Title = "Created with PDFsharp";

                        // Create an empty page
                        PdfPage page = document.AddPage();
                        PdfPage pageBack = document.AddPage();
                        page.Orientation = PageOrientation.Landscape;
                        pageBack.Orientation = PageOrientation.Landscape;

                        height = page.Height;
                        width = page.Width;

                        // Get an XGraphics object for drawing
                        XGraphics gfx = XGraphics.FromPdfPage(page);

                        // Create a font
                        XFont font = new XFont("Times New Roman", 15, XFontStyle.Bold);

                        // Create point for upper-left corner of drawing.
                        PointF Line1Point = new PointF(50.0F, 50.0F);
                        PointF Line2Point = new PointF(50.0F, 70.0F);
                        PointF Line3Point = new PointF(50.0F, 90.0F);
                        PointF Line4Point = new PointF(50.0F, 110.0F);
                        PointF Line5Point = new PointF(50.0F, 130.0F);

                        XBrush brush = new XSolidBrush(XColor.FromArgb(128, 255, 0, 0));
                        XTextFormatter tf = new XTextFormatter(gfx);
                        XRect rect = new XRect(40, 75, width - 40, height - 75);

                        tf.DrawString(watermarks[fileCount], font, brush, rect, XStringFormats.TopLeft);

                        // Save the document...
                        string newPDFName = inputPdfName;
                        document.Save(newPDFName);

                        inputDocument = PdfReader.Open(newPDFName, PdfDocumentOpenMode.Import);

                        // Iterate pages
                        int count = inputDocument.PageCount;
                        for (int idx = 0; idx < count; idx++)
                        {
                            // Get the page from the external document...
                            page = inputDocument.Pages[idx];

                            // ...and add it to the output document.
                            outputDocument.AddPage(page);
                        }
                    }
                    fileCount++;
                }


                bool successfulPrint = false;
                do
                {
                    try
                    {
                        // Save the document...
                        string outputFileName = "";
                        if (orderNumber == "")
                            outputFileName = outputFolder + filesToCopy[0];
                        else
                        {
                            if (outputFolder.Contains("Batch") || inputFolder.Contains("batch"))
                                outputFileName = outputFolder + "\\" + orderNumber + "-" + filesToCopy[0];
                            else
                                outputFileName = outputFolder + "\\" + orderNumber + "-" + jobName + ".pdf";

                        }

                        outputDocument.Save(outputFileName);
                        successfulPrint = true;
                    }
                    catch (Exception ex)
                    {
                        DialogResult result = MessageBox.Show("Problem in printing PDF for " + jobName + "\n" + "The file name may have an invalid character or the file may be open in Windows Explorer. Please close it and click OK to try again.  If you cancel, the drawing will not get printed.", "Confirm", MessageBoxButtons.RetryCancel);
                        if (result == DialogResult.Cancel)
                            successfulPrint = true; ;
                    }
                } while (!successfulPrint);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static string CalculateSubFolder(string pdfInputPath, string rootOutputPath, AggregateLineItem item, bool isBatch)
        {
            string inputPdfName = pdfInputPath + item.Number + ".pdf";
            string outputPdfPath = rootOutputPath;
            

            if (item.StructCode != "")
                item.StructCode = item.StructCode.Replace('/', '-');

            // route to the proper plant
            if(item.Category == "Part")
            {
                if (item.PlantID == "Plant 2")
                    outputPdfPath += "\\Plant 2\\";
                else if (item.PlantID == "Plant 1&2")
                    outputPdfPath += "\\Plant 1&2\\";
                else    // default to plant 1
                    outputPdfPath += "\\Plant 1\\";
            }

            if (!isBatch)
            {
                // route to stock or make to order folders for part items on orders
                if (item.Category == "Part")
                {
                    if (item.IsStock == true)
                        outputPdfPath += "\\Stock\\";
                    else
                        outputPdfPath += "\\Make To Order\\";
                }
            }
            else
            {
                // route to stock or make to order folders for part items on batches
                if (item.Category == "Part")
                {
                    if (item.IsStock == true)
                        outputPdfPath += "\\Parts To Make for Batch\\";
                    else
                        outputPdfPath += "\\Parts To Make as Ordered\\";
                }
            }

            System.IO.Directory.CreateDirectory(outputPdfPath);

            // sort out laser parts
            if (item.Operations == "Laser")
            {
                outputPdfPath = outputPdfPath + "\\" + item.Operations + "\\";
                if (item.MaterialThickness == "") item.MaterialThickness = "Unknown Thickness";
                outputPdfPath += item.MaterialThickness.ToString();
                System.IO.Directory.CreateDirectory(outputPdfPath);
                outputPdfPath += "\\" + item.Number + ".pdf";
            }

            // sort out bandsaw parts
            else if (item.Operations == "Bandsaw" || item.Operations == "Iron Worker")
            {
                outputPdfPath = outputPdfPath + "\\" + item.Operations + "\\";
                if (item.StructCode == "") item.StructCode = "Unknown Material Type";
                outputPdfPath += item.StructCode;
                System.IO.Directory.CreateDirectory(outputPdfPath);
                outputPdfPath += "\\" + item.Number + ".pdf";
            }

            // sort out machine shop parts
            else if(item.Operations == "Machine Shop")
            {
                outputPdfPath = outputPdfPath + "\\" + item.Operations + "\\";
                if (item.StructCode == "") item.StructCode = "Unknown Material Type";
                outputPdfPath += item.StructCode;
                System.IO.Directory.CreateDirectory(outputPdfPath);
                outputPdfPath += "\\" + item.Number + ".pdf";
            }

            // sort out sheared parts
            else if (item.Operations == "Shear")
            {
                outputPdfPath = outputPdfPath + "\\" + item.Operations + "\\";
                if (item.StructCode == "") item.StructCode = "Unknown Material Type";
                outputPdfPath += item.StructCode;
                System.IO.Directory.CreateDirectory(outputPdfPath);
                outputPdfPath += "\\" + item.Number + ".pdf";
            }

            // assemblies should drop through above logic down into here...
            else
            {
                outputPdfPath += "\\" + item.Number + ".pdf";
            }

            return outputPdfPath;
        }

        public static bool AddWatermark(string fileName, string watermark)
        {
            try
            {
                PdfDocument outputDocument = new PdfDocument();
                PdfDocument inputDocument = new PdfDocument();
                PdfDocument editedDocument = new PdfDocument();

                XUnit height = new XUnit();
                XUnit width = new XUnit();
                List<XUnit[]> xUnitArrayList = new List<XUnit[]>();

                string inputPdfName = fileName;
                xUnitArrayList.Clear();
                if (File.Exists(inputPdfName))
                {
                    inputDocument = PdfReader.Open(inputPdfName, PdfDocumentOpenMode.Modify);

                    int count = inputDocument.PageCount;
                    for (int idx = 0; idx < count; idx++)
                    {
                        PdfPage page = inputDocument.Pages[idx];
                        // store page width and height in array list so we can reference again when we are producing output
                        height = page.Height;
                        width = page.Width;
                        XUnit[] pageDims = new XUnit[] { page.Height, page.Width };
                        xUnitArrayList.Add(pageDims);       // drawing page
                        xUnitArrayList.Add(pageDims);       // watermark page

                        PdfPage watermarkPage = new PdfPage(inputDocument);
                        watermarkPage.Height = page.Height;
                        watermarkPage.Width = page.Width;

                        XGraphics gfx = XGraphics.FromPdfPage(watermarkPage, XGraphicsPdfPageOptions.Prepend);

                        XFont font = new XFont("Times New Roman", 15, XFontStyle.Bold);
                        XTextFormatter tf = new XTextFormatter(gfx);

                        XRect rect = new XRect(40, 75, width - 40, height - 75);
                        XBrush brush = new XSolidBrush(XColor.FromArgb(128, 255, 0, 0));
                        tf.DrawString(watermark, font, brush, rect, XStringFormats.TopLeft);

                        //inputDocument.AddPage(watermarkPage);
                        inputDocument.InsertPage(idx * 2 + 1, watermarkPage);
                    }

                    string randomFileName = Path.GetTempFileName();
                    inputPdfName = randomFileName;
                    inputDocument.Save(randomFileName);


                    editedDocument = PdfReader.Open(randomFileName, PdfDocumentOpenMode.Import);

                    // Iterate pages
                    count = editedDocument.PageCount;
                    for (int idx = 0; idx < count; idx++)
                    {
                        // Get the page from the external document...
                        PdfPage editedPage = editedDocument.Pages[idx];

                        XUnit[] outputPageDims = xUnitArrayList[idx];
                        editedPage.Height = outputPageDims[0];
                        editedPage.Width = outputPageDims[1];

                        // ...and add it to the output document.
                        outputDocument.AddPage(editedPage);
                    }

                    // save the watermarked file
                    outputDocument.Save(fileName);

                }
                return true;
            }
            catch(Exception)
            {
                return false;
            }
        }

        public static bool CreateEmptyPageWithWatermark(string fileName, string watermark)
        {
            try
            {
                PdfDocument outputDocument = new PdfDocument();
                PdfDocument newDocument = new PdfDocument();
                PdfDocument editedDocument = new PdfDocument();

                XUnit height = new XUnit();
                XUnit width = new XUnit();
                List<XUnit[]> xUnitArrayList = new List<XUnit[]>();

                string inputPdfName = fileName;
                xUnitArrayList.Clear();
                newDocument = new PdfDocument();

                PdfPage page = newDocument.AddPage();
                // store page width and height in array list so we can reference again when we are producing output
                height = page.Height;
                width = page.Width;
                XUnit[] pageDims = new XUnit[] { page.Height, page.Width };
                xUnitArrayList.Add(pageDims);

                XGraphics gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Prepend);

                XFont font = new XFont("Times New Roman", 15, XFontStyle.Bold);
                XTextFormatter tf = new XTextFormatter(gfx);

                XRect rect = new XRect(40, 75, width - 40, height - 75);
                XBrush brush = new XSolidBrush(XColor.FromArgb(128, 255, 0, 0));
                tf.DrawString(watermark, font, brush, rect, XStringFormats.TopLeft);

                string randomFileName = Path.GetTempFileName();
                inputPdfName = randomFileName;
                newDocument.Save(randomFileName);


                editedDocument = PdfReader.Open(randomFileName, PdfDocumentOpenMode.Import);

                // Get the page from the external document...
                PdfPage editedPage = editedDocument.Pages[0];

                XUnit[] outputPageDims = xUnitArrayList[0];
                editedPage.Height = outputPageDims[0];
                editedPage.Width = outputPageDims[1];

                // ...and add it to the output document.
                outputDocument.AddPage(editedPage);

                // save the watermarked file
                outputDocument.Save(fileName);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
