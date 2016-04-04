using PofilesReader.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace PofilesReader.MVC.Helpers
{
    public static class LocalizedHelper
    {
        public static MvcHtmlString T(this HtmlHelper helper, string text)
        {
            var path = $"~/App_Data/Localization/{CultureInfo.CurrentUICulture}/";
            var paths = helper.ViewContext.HttpContext.Server.MapPath(path);
            var manager = new DefaultLocalizedStringManager(paths);
            var t = manager.GetLocalizedString(string.Empty, text);
            return new MvcHtmlString(t);
        }
        public static MvcHtmlString T(this HtmlHelper helper, string text, string context)
        {

            var path = $"~/App_Data/Localization/{CultureInfo.CurrentUICulture}/";
            var paths = helper.ViewContext.HttpContext.Server.MapPath(path);
            var manager = new DefaultLocalizedStringManager(paths);
            var t = manager.GetLocalizedString(context, text);
            return new MvcHtmlString(t);
        }
    }
}
