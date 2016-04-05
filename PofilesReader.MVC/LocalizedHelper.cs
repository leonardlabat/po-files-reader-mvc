﻿using PofilesReader.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace PofilesReader.MVC.Helpers
{
    /// <summary>
    /// looks for app_data/localization/{dir_folder}/*.po
    /// if culture en-US folder not available will look up for parents, en folder then no sub folder.... 
    /// </summary>
    public static class LocalizedHelper
    {
        //TODO for IOC , make parameter constructor with IWebSitefolder..
        private static ILocalizedStringManager GetLocalizedManager()
        {
            var res = DependencyResolver.Current.GetService<ILocalizedStringManager>();
            return res ?? new DefaultLocalizedStringManager();
        }

        public static MvcHtmlString T(this HtmlHelper helper, string text)
        {
            string result = text;
            string paths = GetResourcesDirectory(helper);
            if (!string.IsNullOrEmpty(paths))
            {
                var manager = new DefaultLocalizedStringManager(paths);
                result = manager.GetLocalizedString(string.Empty, text);
            }
            return new MvcHtmlString(result);
        }
        public static MvcHtmlString T(this HtmlHelper helper, string text, params string[] paramss) 
        {
            var result = helper.T(text).ToString();
            return new MvcHtmlString(string.Format(result, paramss));
        }
        public static MvcHtmlString T(this HtmlHelper helper, string text, string context)
        {
            string result = text;
            string paths = GetResourcesDirectory(helper);
            if (!string.IsNullOrEmpty(paths))
            {
                var manager = new DefaultLocalizedStringManager(paths);
                result = manager.GetLocalizedString(context, text);
            }
            return new MvcHtmlString(result);
        }

        /// <summary>
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static MvcHtmlString TPlural(this HtmlHelper helper, string text)
        {
            string result = text;
            string paths = GetResourcesDirectory(helper);
            if (!string.IsNullOrEmpty(paths))
            {
                var manager = new DefaultLocalizedStringManager(paths);
                result = manager.GetLocalizedString(string.Empty, text, true,1)??text;
            }
            return new MvcHtmlString(result);
        }


        public static MvcHtmlString TPlural(this HtmlHelper helper, string text, params string[] parammss)
        {
            var result = helper.TPlural(text).ToString();
            return new MvcHtmlString(string.Format(result, parammss));
        }

        /// <summary>
        /// can go up to parent
        /// </summary>
        /// <param name="helper"></param>
        /// <returns></returns>
        private static string GetResourcesDirectory(HtmlHelper helper)
        {
            var dirPath = helper.ViewContext.HttpContext.Server.MapPath($"~/App_Data/Localization/{CultureInfo.CurrentUICulture}/");
            var parent = CultureInfo.CurrentUICulture.Parent;
            var dirExist = Directory.Exists(dirPath);
            while (parent != null && parent.Parent !=parent && !dirExist)
            {
                dirPath = helper.ViewContext.HttpContext.Server.MapPath($"~/App_Data/Localization/{parent}/");
                dirExist= Directory.Exists(dirPath);
                parent = parent.Parent;
            }
            return dirExist? dirPath:string.Empty;
        }
    }
}
