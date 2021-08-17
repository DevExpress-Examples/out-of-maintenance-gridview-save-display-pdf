using DXWebApplication1.Models;
using System;
using System.Web;

namespace DXWebApplication1.Models {
    public class FileDownloadHandler : IHttpHandler {
        DXWebApplication1.Models.PdfBaseEntities db = new DXWebApplication1.Models.PdfBaseEntities();
        public bool IsReusable {
            get { return true; }
        }

        public void ProcessRequest(HttpContext context) {
            string id = context.Request["id"];
            byte[] fileBytes = GetFileBytesByKey(context, id);

            if (fileBytes != null)
                ExportToResponse(context, fileBytes, "file_" + id);
        }

        private byte[] GetFileBytesByKey(HttpContext context, object key) {
            int value;
            if (key == null || !Int32.TryParse(key.ToString(), out value)) return null;
            TableWithPdf record = db.TableWithPdfs.Find(Convert.ToInt32(key));            
            return record.PdfFile;
        }

        public void ExportToResponse(HttpContext context, byte[] content, string fileName) {
            context.Response.Clear();
            context.Response.ContentType = "application/pdf";
            context.Response.AddHeader("Content-Disposition", string.Format("{0}; filename={1}.pdf","Inline",
                fileName));
            context.Response.AddHeader("Content-Length", content.Length.ToString());
            context.Response.BinaryWrite(content);
            context.Response.Flush();
            context.Response.Close();
            context.Response.End();
        }        
    }
}
