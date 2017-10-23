using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using testCloneOnLinux.Models;

namespace testCloneOnLinux.Controllers
{
    public class MyModelTestController : Controller
    {

        [DllImport("libc", SetLastError = true)]
        private static extern int chmod(string pathname, int mode);

        // user permissions
        const int S_IRUSR = 0x100;
        const int S_IWUSR = 0x80;
        const int S_IXUSR = 0x40;

        // group permission
        const int S_IRGRP = 0x20;
        const int S_IWGRP = 0x10;
        const int S_IXGRP = 0x8;

        // other permissions
        const int S_IROTH = 0x4;
        const int S_IWOTH = 0x2;
        const int S_IXOTH = 0x1;

        public ActionResult Post()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Post([FromForm]MyModelGen model)
        {
            try
            {
                if (!Directory.Exists(model.Source))
                {
                    return Json(new {message = "Nhập sai đường dẫn nguồn"});
                }
                if (!Directory.Exists(model.Destination))
                {
                    Directory.CreateDirectory(model.Destination);
                }
                this.Copy(model.Source, model.Destination);
            }
            catch (Exception ex)
            {
                return Json(new {message = ex.Message, data = ex});
            }
            return Json(new {message = "Sao chép thành công!"});
        }

        private void Copy(string sourceDirectory, string targetDirectory)
        {
            DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);
            DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);

            CopyAll(diSource, diTarget);
        }

        private void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                // Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }
        private void DeleteFolder(string path)
        {
            System.IO.DirectoryInfo di = new DirectoryInfo(path);

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }
            di.Delete();
        }
    }
}