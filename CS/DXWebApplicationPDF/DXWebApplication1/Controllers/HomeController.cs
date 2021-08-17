using DevExpress.Web;
using DevExpress.Web.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;

namespace DXWebApplication1.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        DXWebApplication1.Models.PdfBaseEntities db = new DXWebApplication1.Models.PdfBaseEntities();

        [ValidateInput(false)]
        public ActionResult GridViewPartial() {
            var model = db.TableWithPdfs;
            return PartialView("_GridViewPartial", model.ToList());
        }

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
        [HttpPost, ValidateInput(false)]
        public ActionResult GridViewPartialDelete(System.Int32 Id) {
            var model = db.TableWithPdfs;
            if (Id >= 0) {
                try {
                    var item = model.FirstOrDefault(it => it.Id == Id);
                    if (item != null)
                        model.Remove(item);
                    db.SaveChanges();
                } catch (Exception e) {
                    ViewData["EditError"] = e.Message;
                }
            }
            return PartialView("_GridViewPartial", model.ToList());
        }

        public ActionResult ImageUpload() {
            UploadControlExtension.GetUploadedFiles("uploadControl", UploadControlHelper.ValidationSettings, UploadControlHelper.uploadControl_FileUploadComplete);
            return null;
        }
    }

    public class UploadControlHelper {
        public static readonly UploadControlValidationSettings ValidationSettings = new UploadControlValidationSettings {
            AllowedFileExtensions = new string[] { ".pdf" },
            MaxFileSize = 4000000
        };

        public static void uploadControl_FileUploadComplete(object sender, FileUploadCompleteEventArgs e) {
            if (e.UploadedFile.IsValid) {
                HttpContext.Current.Session["file"] = e.UploadedFile.FileBytes;
            }
        }
    }
}