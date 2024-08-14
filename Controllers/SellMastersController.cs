using BarCodeInvoice.Models;
using System;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using ZXing;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace BarCodeInvoice.Controllers
{
    public class SellMastersController : Controller
    {
        private HPLVMSEntities db = new HPLVMSEntities();

        public ActionResult Index()
        {
            var sellMasters = db.SellMasters.ToList();

            foreach (var item in sellMasters)
            {
                var barcodeContent = $"{item.InvoiceNo}-{item.Id}";
                item.Barcode = GenerateBarcode(barcodeContent);
            }

            return View(sellMasters);
        }

        private string GenerateBarcode(string content)
        {
            const int maxLength = 15;
            if (content.Length > maxLength)
            {
                content = content.Substring(0, maxLength);
            }

            var barcodeWriter = new BarcodeWriter
            {
                Format = BarcodeFormat.CODE_128,
                Options = new ZXing.Common.EncodingOptions
                {
                    Height = 150,
                    Width = 300
                }
            };

            using (var bitmap = barcodeWriter.Write(content))
            {
                using (var stream = new MemoryStream())
                {
                    bitmap.Save(stream, ImageFormat.Png);
                    return "data:image/png;base64," + Convert.ToBase64String(stream.ToArray());
                }
            }
        }

        public ActionResult ExportBarcodesToPDF()
        {
            var sellMasters = db.SellMasters.ToList();
            using (MemoryStream ms = new MemoryStream())
            {
                Document document = new Document(PageSize.A4.Rotate(), 25, 25, 30, 30);
                PdfWriter writer = PdfWriter.GetInstance(document, ms);
                document.Open();

                PdfPTable table = new PdfPTable(4);
                table.WidthPercentage = 100;

                foreach (var item in sellMasters)
                {
                    var barcodeContent = $"{item.InvoiceNo}-{item.Id}";
                    var barcodeImage = GenerateBarcodeImage(barcodeContent);
                    Image img = Image.GetInstance(barcodeImage);
                    img.ScaleToFit(200, 150); // Scale the barcode image if necessary

                    PdfPCell cell = new PdfPCell(img);
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.Border = Rectangle.NO_BORDER;

                    table.AddCell(cell);
                }

                document.Add(table);
                document.Close();
                writer.Close();

                return File(ms.ToArray(), "application/pdf", "Barcodes.pdf");
            }
        }

        private byte[] GenerateBarcodeImage(string content)
        {
            const int maxLength = 15;
            if (content.Length > maxLength)
            {
                content = content.Substring(0, maxLength);
            }

            var barcodeWriter = new BarcodeWriter
            {
                Format = BarcodeFormat.CODE_128,
                Options = new ZXing.Common.EncodingOptions
                {
                    Height = 100,
                    Width = 400,
                    PureBarcode = true
                }
            };

            using (var bitmap = barcodeWriter.Write(content))
            {
                return AddTextBelowBarcode(bitmap, content);
            }
        }

        private byte[] AddTextBelowBarcode(System.Drawing.Bitmap barcode, string text)
        {
            int textHeight = 40; // Adjust this value as needed
            int totalHeight = barcode.Height + textHeight;

            var result = new System.Drawing.Bitmap(barcode.Width, totalHeight);
            using (var graphics = System.Drawing.Graphics.FromImage(result))
            {
                graphics.Clear(System.Drawing.Color.White);
                graphics.DrawImage(barcode, new System.Drawing.Point(0, 0));

                var font = new System.Drawing.Font("Arial", 18); // Adjust font size as needed
                var brush = new System.Drawing.SolidBrush(System.Drawing.Color.Black);
                var textSize = graphics.MeasureString(text, font);
                var position = new System.Drawing.PointF((barcode.Width - textSize.Width) / 2, barcode.Height);

                graphics.DrawString(text, font, brush, position);
            }

            using (var stream = new MemoryStream())
            {
                result.Save(stream, ImageFormat.Png);
                return stream.ToArray();
            }

        }
    }
}