
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AvnetNullSearchBot
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
        private List<string> results = new List<string>();
        private List<string> searchTerms = new List<string>();
        private int searchCounter = 0;
        private string m_ExcelSheet = "";
        public List<string> GetTerms(string filePath)
        {
            List<string> terms = new List<string>();
            string value = "";
            int nextNumber = 2;
            using (SpreadsheetDocument document = SpreadsheetDocument.Open(filePath, true))
            {
                do
                {

                    string reference = "A" + nextNumber;
                    value = GetCellValue(document, "Sheet1", "A" + nextNumber.ToString());
                    nextNumber = nextNumber + 1;
                    terms.Add(value);
                } while (!string.IsNullOrWhiteSpace(value));
            }


            return terms;
        }

        public static string GetCellValue(SpreadsheetDocument document, string sheetName, string column)
        {
            string value = "";

            // Retrieve a reference to the workbook part.
            WorkbookPart wbPart = document.WorkbookPart;

            // Find the sheet with the supplied name, and then use that 
            // Sheet object to retrieve a reference to the first worksheet.
            Sheet theSheet = wbPart.Workbook.Descendants<Sheet>().
              Where(s => s.Name == sheetName).FirstOrDefault();

            // Throw an exception if there is no sheet.
            if (theSheet == null)
            {
                throw new ArgumentException("sheetName");
            }

            // Retrieve a reference to the worksheet part.
            WorksheetPart wsPart =
                (WorksheetPart)(wbPart.GetPartById(theSheet.Id));
            // Use its Worksheet property to get a reference to the cell 
            // whose address matches the address you supplied.
            Cell theCell = wsPart.Worksheet.Descendants<Cell>().
              Where(c => c.CellReference == column).FirstOrDefault();

            // If the cell does not exist, return an empty string.
            if (theCell != null)
            {
                value = theCell.Descendants<CellValue>().FirstOrDefault().Text;

                if (theCell.DataType != null)
                {
                    switch (theCell.DataType.Value)
                    {
                        case CellValues.SharedString:

                            // For shared strings, look up the value in the
                            // shared strings table.
                            var stringTable =
                                wbPart.GetPartsOfType<SharedStringTablePart>()
                                .FirstOrDefault();

                            // If the shared string table is missing, something 
                            // is wrong. Return the index that is in
                            // the cell. Otherwise, look up the correct text in 
                            // the table.
                            if (stringTable != null)
                            {
                                value =
                                    stringTable.SharedStringTable
                                    .ElementAt(int.Parse(value)).InnerText;
                            }
                            break;

                        case CellValues.Boolean:
                            switch (value)
                            {
                                case "0":
                                    value = "FALSE";
                                    break;
                                default:
                                    value = "TRUE";
                                    break;
                            }
                            break;
                    }
                }
            }
            return value;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
            m_ExcelSheet = openFileDialog1.FileName;
            searchTerms = GetTerms(m_ExcelSheet);
            //searchTerms = new List<string>() { "dogfood" };
            TriggerNextEvent();
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (e.Url.AbsolutePath != (sender as WebBrowser).Url.AbsolutePath)
                return;
            if (webBrowser1.Url.AbsolutePath.Contains("NoSearchResults"))
            {
                results.Add("YES");
            }
            else
            {
                results.Add("NO");
            }
            searchCounter = searchCounter + 1;
            Thread.Sleep(1000);
            TriggerNextEvent();
        }

        
        private void TriggerNextEvent()
        {
            if(searchCounter < searchTerms.Count)
            {
                string nextTerm = searchTerms.ElementAt(searchCounter);
                try
                {
                    
                    string url = $"https://www.avnet.com/shop/SearchDisplay?searchTerm={nextTerm}";
                    //webBrowser1.ScriptErrorsSuppressed = true;
                    //GetWebPage(url);
                    webBrowser1.Navigate(url, "_self", null, "Referer: Avnet-ASU-BOT");
                    Application.DoEvents();
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
                
                //webBrowser1.ScriptErrorsSuppressed = true;
              
               
            }
            else
            {
                UpdateList();
                MessageBox.Show("Record Completed!");
            }
           
        }

        private void UpdateList()
        {
            int nextNumber = 2;
            using (SpreadsheetDocument document = SpreadsheetDocument.Open(m_ExcelSheet, true))
            {
                foreach (string result in results)
                {
                    string reference = "B" + nextNumber;
                    UpdateCellValue(document, reference, result, (uint)nextNumber);
                    nextNumber = nextNumber + 1;
                }
            }
        }

        private void UpdateCellValue(SpreadsheetDocument document, string reference, string result, uint rowIndex)
        {
            WorkbookPart wbPart = document.WorkbookPart;

            // Find the sheet with the supplied name, and then use that 
            // Sheet object to retrieve a reference to the first worksheet.
            Sheet theSheet = wbPart.Workbook.Descendants<Sheet>().
              Where(s => s.Name == "Sheet1").FirstOrDefault();

            // Throw an exception if there is no sheet.
            if (theSheet == null)
            {
                throw new ArgumentException("sheetName");
            }

            // Retrieve a reference to the worksheet part.
            WorksheetPart wsPart =
                (WorksheetPart)(wbPart.GetPartById(theSheet.Id));
            // Use its Worksheet property to get a reference to the cell 
            // whose address matches the address you supplied.
            Cell theCell = wsPart.Worksheet.Descendants<Cell>().
              Where(c => c.CellReference == reference).FirstOrDefault();
            if(theCell == null)
            {
                theCell = new Cell();
                theCell.CellReference = reference;
            
                Row row = GetRow(wsPart.Worksheet, rowIndex);
                Cell refCell = null;
                foreach (Cell cell in row.Elements<Cell>())
                {
                    if (string.Compare(cell.CellReference.Value, reference, true) > 0)
                    {
                        refCell = cell;
                        break;
                    }
                }
                theCell.CellValue = new CellValue(result);

                row.InsertBefore(theCell, refCell);


            }
            theCell.DataType = new DocumentFormat.OpenXml.EnumValue<CellValues>(CellValues.String);
            theCell.CellValue = new CellValue(result);
            wsPart.Worksheet.Save();
        }

        private static Cell GetCell(Worksheet worksheet,
                 string columnName, uint rowIndex)
        {
            Row row = GetRow(worksheet, rowIndex);

            if (row == null)
                return null;

            return row.Elements<Cell>().Where(c => string.Compare
                   (c.CellReference.Value, columnName +
                   rowIndex, true) == 0).First();
        }

        private static Row GetRow(Worksheet worksheet, uint rowIndex)
        {
            return worksheet.GetFirstChild<SheetData>().
              Elements<Row>().Where(r => r.RowIndex == rowIndex).First();
        }

        private void webBrowser1_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            int x = 3;
        }
    }
}
