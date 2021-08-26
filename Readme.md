<!-- default badges list -->
![](https://img.shields.io/endpoint?url=https://codecentral.devexpress.com/api/v1/VersionRange/397242559/21.1.4%2B)
[![](https://img.shields.io/badge/Open_in_DevExpress_Support_Center-FF7200?style=flat-square&logo=DevExpress&logoColor=white)](https://supportcenter.devexpress.com/ticket/details/T1022389)
[![](https://img.shields.io/badge/📖_How_to_use_DevExpress_Examples-e9f6fc?style=flat-square)](https://docs.devexpress.com/GeneralInformation/403183)
<!-- default badges end -->
# GridView for MVC - How to save a pdf document to a database and display it
<!-- run online -->
**[[Run Online]](https://codecentral.devexpress.com/397242559/)**
<!-- run online end -->

<p>A pdf file is stored in a database as a byte array. To display this file as a pdf, it is necessary to pass this byte array to the Response. This can be done by implementing your own <a href="https://docs.microsoft.com/en-us/troubleshoot/aspnet/http-modules-handlers">HttpHandler</a>: </p> 

```cs
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

```
<p>This handler should be added to the server.webServer section of web.config:</p>

```xml
<system.webServer>  
  <handlers>
     ....
    <add name="FileDownloadHandler" verb="*" path="*" type="DXWebApplication1.Models.FileDownloadHandler"/>  
  </handlers>
</system.webServer>
```
<p>On the GridView level, you can implement the SetDataItemTemplateContent and SetEditItemTemplateContent templates to show/hide hyper links and UploadControl:</p>

```cs
settings.Columns.Add(column => {
    column.Caption = "PDF file";
    column.SetDataItemTemplateContent(container => {
        var obj = DataBinder.Eval(container.DataItem, "PdfFile");
        if (obj == null) {
            Html.DevExpress().Label(
                edtSettings => {
                    edtSettings.Text = "No file";
                }
            ).Render();
        } else {
            Html.DevExpress().HyperLink(hyperlink => {
                var visibleIndex = container.VisibleIndex;
                var keyValue = container.KeyValue;
                hyperlink.Name = "hld" + keyValue.ToString();
                hyperlink.Properties.Text = "Open pdf";
                hyperlink.Properties.Target = "_blank";
                hyperlink.NavigateUrl = string.Format("FileDownloadHandler.ashx?id={0}", container.KeyValue);
            }).Render();
        }
    });
    column.SetEditItemTemplateContent(container => {
        using (Html.BeginForm("ImageUpload", "Home", FormMethod.Post)) {
            Html.DevExpress().UploadControl(
                ucSettings => {
                    ucSettings.Name = "uploadControl";
                    ucSettings.ShowUploadButton = true;
                    ucSettings.AddUploadButtonsSpacing = 0;
                    ucSettings.AddUploadButtonsHorizontalPosition = AddUploadButtonsHorizontalPosition.InputRightSide;
                    ucSettings.CallbackRouteValues = new { Controller = "Home", Action = "ImageUpload" };
                    ucSettings.ValidationSettings.Assign(DXWebApplication1.Controllers.UploadControlHelper.ValidationSettings);
                    ucSettings.ClientSideEvents.FileUploadComplete = string.Format("function(s, e) {{onFileUploadComplete(s,e,{0});}}", container.KeyValue);
                }
            ).Render();
        }
        var obj = DataBinder.Eval(container.DataItem, "PdfFile");
        Html.DevExpress().HyperLink(hyperlink => {
            var visibleIndex = container.VisibleIndex;
            var keyValue = container.KeyValue;
            hyperlink.Properties.EnableClientSideAPI = true;
            hyperlink.ClientVisible = !(obj == null);
            hyperlink.Name = "hle";
            hyperlink.Properties.Text = "Open pdf";
            hyperlink.Properties.Target = "_blank";
            hyperlink.NavigateUrl = string.Format("FileDownloadHandler.ashx?id={0}", container.KeyValue);
        }).Render();

        Html.DevExpress().Label(
            edtSettings => {
                edtSettings.Name = "lble";
                edtSettings.Properties.EnableClientSideAPI = true;
                edtSettings.Text = obj == null ? "No file" : "";
            }
        ).Render();
    });
});

```
```js
function onFileUploadComplete(s, e, keyvalue) {
    if (e.isValid) {                
        if(hle) hle.SetClientVisible(false);
        lble.SetText('A new file is uploaded, but is not saved')
    }
}
```
<p>A new uploaded file is saved to the Session variable in the FileUploadComplete event:</p>

```cs
public static void uploadControl_FileUploadComplete(object sender, FileUploadCompleteEventArgs e) {
    if (e.UploadedFile.IsValid) {
        HttpContext.Current.Session["file"] = e.UploadedFile.FileBytes;
    }
}
```
<p>Then, in the Update/Insert actions of the data source, the byte array is stored to the database:</p>

```cs
[HttpPost, ValidateInput(false)]
public ActionResult GridViewPartialAddNew(DXWebApplication1.Models.TableWithPdf item) {
    var model = db.TableWithPdfs;
    if (ModelState.IsValid) {
        try {
            item.PdfFile = (byte[])Session["file"];
            model.Add(item);
            db.SaveChanges();
            Session["file"] = null;
        } catch (Exception e) {
            ViewData["EditError"] = e.Message;
        }
    } else
        ViewData["EditError"] = "Please, correct all errors.";
    return PartialView("_GridViewPartial", model.ToList());
}
[HttpPost, ValidateInput(false)]
public ActionResult GridViewPartialUpdate(DXWebApplication1.Models.TableWithPdf item) {
    var model = db.TableWithPdfs;
    if (ModelState.IsValid) {
        try {
            var modelItem = model.FirstOrDefault(it => it.Id == item.Id);
            if (modelItem != null) {
                modelItem.PdfFile = (byte[])Session["file"];
                this.UpdateModel(modelItem);
                db.SaveChanges();
                Session["file"] = null;
            }
        } catch (Exception e) {
            ViewData["EditError"] = e.Message;
        }
    } else
        ViewData["EditError"] = "Please, correct all errors.";
    return PartialView("_GridViewPartial", model.ToList());
}
```

<img src="./Images/video.gif">

**See also:**

<a href="https://supportcenter.devexpress.com/ticket/details/t578527/aspxcardview-how-to-upload-files-in-the-edit-mode-and-save-them-in-a-database">ASPxCardView - How to upload files in the Edit mode and save them in a database</a> 

<br/>
<!-- default file list -->
Files to look at:

* [FileDownloadHandler.cs](./CS/DXWebApplicationPDF/DXWebApplication1/FileDownloadHandler.cs)
* [_GridViewPartial.cshtml](./CS/DXWebApplicationPDF/DXWebApplication1/Views/Home/_GridViewPartial.cshtml)
* [_Layout.cshtml](./CS/DXWebApplicationPDF/DXWebApplication1/Views/Shared/_Layout.cshtml)
* [HomeController.cs](./CS/DXWebApplicationPDF/DXWebApplication1/Controllers/HomeController.cs)
<!-- default file list end -->


