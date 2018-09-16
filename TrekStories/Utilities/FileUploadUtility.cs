﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace TrekStories.Utilities
{
    public static class FileUploadUtility
    {
        private const int MAX_FILESIZE = 7000 * 1024;  //7MB converted in bytes

        private static string[] supportedFileExtensions = new[] { ".png", ".jpg", ".gif", ".pdf", ".msg", ".txt" };

        public static bool ValidFileExtension(HttpPostedFileBase file)
        {
            string extension = Path.GetExtension(file.FileName);
            if (supportedFileExtensions.Contains(extension))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool ValidFileSize(HttpPostedFileBase file)
        {
            int size = file.ContentLength;
            if (size > MAX_FILESIZE)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}