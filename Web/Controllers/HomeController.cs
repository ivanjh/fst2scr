using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Web.Util;

namespace Web.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ActionName("Index")]
        [HttpContentType("multipart/form-data")]
        public FileStreamResult FomPost(HttpPostedFileBase file)
        {
            var fileStreamResult = FileStreamResult(file.InputStream);
            fileStreamResult.FileDownloadName = file.FileName.Split('\\', '/').Last() + ".scr";
            return fileStreamResult;
        }

        [HttpPost]
        [ActionName("Index")]
        [HttpContentType("text/html")]
        public FileStreamResult TextPost()
        {
            return FileStreamResult(Request.InputStream);
        }

        private static FileStreamResult FileStreamResult(Stream file)
        {
            var memoryStream = new MemoryStream();
            Fst2Scr.Fst2Scr.Convert(file, memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            var fileStreamResult = new FileStreamResult(memoryStream, "application/octet-stream");
            return fileStreamResult;
        }
    }
}