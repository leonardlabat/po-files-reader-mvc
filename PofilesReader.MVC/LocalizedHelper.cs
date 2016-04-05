using PofilesReader.Localization;
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
        //TODO : IOC would be better and life managing container for objects
        public const string basePath = "~/App_Data/Localization/";
        private static IPathsBuilder pathsBuilder = new WebDirectoryFinder(basePath);
        private static ILocalizedStringManager _manager = new DefaultLocalizedStringManager(pathsBuilder);

        public static MvcHtmlString T(this HtmlHelper helper, string text)
        {
            string result = text;
            result = _manager.GetLocalizedString(string.Empty, text);
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
            result = _manager.GetLocalizedString(context, text);
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
            var manager = new DefaultLocalizedStringManager(pathsBuilder);
            return new MvcHtmlString(result);
        }


        public static MvcHtmlString TPlural(this HtmlHelper helper, string text, params string[] parammss)
        {
            var result = helper.TPlural(text).ToString();
            return new MvcHtmlString(string.Format(result, parammss));
        }

    }
}
