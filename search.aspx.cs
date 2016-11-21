using System;
using System.Collections.Generic;
using System.Drawing;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Colorissimo.Web;
using Colorissimo.Core;

namespace clxweb
{
    public partial class search : System.Web.UI.Page
    {
        private Parameters parameters;
        private static bool showThumblains = true;

        private const int PageSize = 20;

        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);

            string p = this.Context.Request.Params["parameters"];
            if (String.IsNullOrEmpty(p))
            {
                this.parameters = new Parameters();
                this.parameters["Color1Text"] = String.Empty;
                this.parameters["Color2Text"] = String.Empty;
                this.parameters.Set("Color1", -1L);
                this.parameters.Set("Color2", -1L);
                this.parameters.Set("Page", 1L);
                this.parameters.Set("Comparison", (long)ColorSearchWidth.Wide);
                this.parameters.Set("Sort", (long)PaletteListSortMode.Title);
                this.parameters.Set("BgColor", (long)BackgroundColor.White);
                this.parameters.Set("ShowAll", false);
                this.parameters.Set("ShowSimilar", false);
            }
            else
            {
                this.parameters = Parameters.Deserialize(p);
            }

            p = this.Context.Request.Params["color_1"];
            if (String.IsNullOrEmpty(p)) p = String.Empty;
            this.parameters["Color1Text"] = p;
            this.parameters.Set("Color1", (long)ColorParser.Parse(p));

            p = this.Context.Request.Params["color_2"];
            if (String.IsNullOrEmpty(p)) p = String.Empty;
            this.parameters["Color2Text"] = p;
            this.parameters.Set("Color2", (long)ColorParser.Parse(p));

            this.parameters.Set("Comparison", this.Param("comparison", (long)ColorSearchWidth.Wide));
            this.parameters.Set("Sort", this.Param("sort", (long)PaletteListSortMode.Title));
            this.parameters.Set("BgColor", this.Param("bgcolor",(long)BackgroundColor.White));
            this.parameters.Set("ShowAll", this.Exists("showall"));
            this.parameters.Set("ShowSimilar", this.Exists("showsimilar"));

            p = this.Context.Request.Params["form_event"];
            if (String.IsNullOrEmpty(p)) p = null;
            if (p != null)
            {
                Parameters evp;
                string ev = Utils.UnpackEvent(p, out evp);
                if (ev != null)
                    this.ProcessEvent(ev, evp);
            }
        }

        protected string Escape(string s)
        {
            return HtmlRenderer.Escape(s);
        }

        private void ProcessEvent(string ev, Parameters param)
        {
            if (ev == "search")
            {
                this.parameters["_Color1"] = this.parameters["Color1Text"];
                this.parameters["_Color2"] = this.parameters["Color2Text"];
                this.parameters["_ShowSimilar"] = this.parameters["ShowSimilar"];
                this.parameters["_ShowAll"] = this.parameters["ShowAll"];
                this.parameters["_Comparison"] = this.parameters["Comparison"];
                this.parameters["_Sort"] = this.parameters["Sort"];
                this.parameters["_BgColor"] = this.parameters["BgColor"];
                this.parameters.Set("Page", 1L);
                return;
            }
            if (ev == "page")
            {
                long p = param.Get("Page", -1L);
                if (p > 0) this.parameters.Set("Page", p);
                return;
            }
        }

        protected string Param(string name)
        {
            return this.parameters[name, String.Empty];
        }

        protected string Param()
        {
            return this.parameters.Serialize();
        }

        private long Param(string name, long defaultValue)
        {
            string s = this.Context.Request.Params[name];
            if (String.IsNullOrEmpty(s)) return defaultValue;
            long l;
            if (Int64.TryParse(s, out l)) return l;
            return defaultValue;
        }

        private bool Exists(string name)
        {
            string[] n = this.Context.Request.Params.AllKeys;
            for (int i = 0; i < n.Length; i++)
            {
                if (String.Compare(n[i], name, true) == 0)
                    return true;
            }
            return false;
        }

        protected string RenderComparisonList()
        {
            HtmlRenderer html = new HtmlRenderer();

            ColorSearchWidth opt = (ColorSearchWidth)this.parameters.Get("Comparison", (long)ColorSearchWidth.Wide);

            html.SelectItem("Exact", ((int)ColorSearchWidth.Exact).ToString(), opt == ColorSearchWidth.Exact);
            html.SelectItem("Narrow", ((int)ColorSearchWidth.Narrow).ToString(), opt == ColorSearchWidth.Narrow);
            html.SelectItem("Wide", ((int)ColorSearchWidth.Wide).ToString(), opt == ColorSearchWidth.Wide);
            html.SelectItem("Very wide", ((int)ColorSearchWidth.VeryWide).ToString(), opt == ColorSearchWidth.VeryWide);
            html.SelectItem("Widest", ((int)ColorSearchWidth.Widest).ToString(), opt == ColorSearchWidth.Widest);

            return html.ToString();
        }

        protected string RenderSortList()
        {
            HtmlRenderer html = new HtmlRenderer();

            PaletteListSortMode opt = (PaletteListSortMode)this.parameters.Get("Sort", (long)PaletteListSortMode.Title);

            html.SelectItem("Title", ((int)PaletteListSortMode.Title).ToString(), opt == PaletteListSortMode.Title);
            html.SelectItem("Color", ((int)PaletteListSortMode.PaletteColor).ToString(), opt == PaletteListSortMode.PaletteColor);
            html.SelectItem("Cluster", ((int)PaletteListSortMode.ClusterId).ToString(), opt == PaletteListSortMode.ClusterId);
            html.SelectItem("Cluster color", ((int)PaletteListSortMode.ClusterColor).ToString(), opt == PaletteListSortMode.ClusterColor);

            return html.ToString();
        }

        protected string RenderBgColorList()
        {
            HtmlRenderer html = new HtmlRenderer();

            BackgroundColor opt = (BackgroundColor)this.parameters.Get("BgColor", (long)BackgroundColor.White);

            html.SelectItem("White", ((int)BackgroundColor.White).ToString(), opt == BackgroundColor.White);
            html.SelectItem("Black", ((int)BackgroundColor.Black).ToString(), opt == BackgroundColor.Black);

            return html.ToString();
        }

        protected string RenderCheckbox(string name, string paramName)
        {
            HtmlRenderer html = new HtmlRenderer();
            html.Add("<input");
            html.Add("type", "checkbox");
            html.Add("name", name);
            html.Add("value", "yes");
            if (this.parameters.Get(paramName, false))
                html.Add("checked", "yes");
            html.Add("/>");
            return html.ToString();
        }

        protected string RenderSearchResults()
        {
            Color color1 = Color.Transparent;
            Color color2 = Color.Transparent;
            bool ok = true;
            string s = this.parameters["_Color1", null];
            if (!String.IsNullOrEmpty(s))
            {
                long c = ColorParser.Parse(s);
                if (c == ColorParser.InvalidColor)
                {
                    ok = false;
                }
                else
                {
                    color1 = ColorTransform.IntToRgb((int)c);
                }
            }
            s = this.parameters["_Color2", null];
            if (!String.IsNullOrEmpty(s))
            {
                long c = ColorParser.Parse(s);
                if (c == ColorParser.InvalidColor)
                {
                    ok = false;
                }
                else
                {
                    color2 = ColorTransform.IntToRgb((int)c);
                }
            }

            HtmlRenderer html = new HtmlRenderer();
            
            if (!ok)
            {
                search.EmptyResultTable(html, "One of the colors is invalid");
                return html.ToString();
            }
            if ((color1 == Color.Transparent) && (color2 == Color.Transparent))
            {
                search.EmptyResultTable(html, "One of the colors must be specified");
                return html.ToString();
            }

            PaletteManager mgr = new PaletteManager(Utils.ConnectionString);
            List<PaletteItem> result = null;
            string msg = null;
            try
            {
                DateTime start = DateTime.Now;

                result = mgr.Find(color1, color2,
                    (ColorSearchWidth)this.parameters.Get("_Comparison", (long)ColorSearchWidth.Wide),
                    this.parameters.Get("_ShowSimilar", false),
                    1000);

                if (result != null)
                {
                    if (result.Count > 0)
                    {
                        PaletteListSortMode mode = (PaletteListSortMode)this.parameters.Get("_Sort", (long)PaletteListSortMode.Title);
                        PaletteManager.Sort(result, mode, mode == PaletteListSortMode.ClusterColor ? mgr.LoadClusters() : null);
                    }
                    else
                    {
                        result = null;
                    }
                }
                double sec = DateTime.Now.Subtract(start).TotalSeconds;
                if (result != null)
                    msg = String.Format("{0} item(s) found, {1} seconds", result.Count, sec.ToString("#####0.0##"));
            }
            catch
            {
                result = null;
            }
            if (result == null)
            {
                search.EmptyResultTable(html, "No palette(s) found or error occured");
                return html.ToString();
            }

            bool paging = !this.parameters.Get("_ShowAll", false);

            int first = 0;
            int last = result.Count - 1;
            int pageCount = 0;
            int page = 0;
            if (paging)
            {
                pageCount = result.Count / search.PageSize;
                if ((pageCount * search.PageSize) < result.Count) pageCount++;

                page = (int)this.parameters.Get("Page", 1L);

                if (page < 1) page = 1;
                if (page > pageCount) page = pageCount;

                this.parameters.Set("Page", (long)page);

                first = (page - 1) * search.PageSize;
                last = first + search.PageSize;
                if (last >= result.Count) last = result.Count - 1;
                
            }

            html.Add("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\">");

            if (msg != null)
            {
                html.Add("<tr><td colspan=\"2\" align=\"center\" style=\"font-size:small;color:#909090;padding-bottom:2px;\">");
                html.Text(msg);
                html.Add("</td></tr>");
            }

            string key = Utils.GetTimeKey(60);
            bool black = (BackgroundColor)this.parameters.Get("BgColor", (long)BackgroundColor.White) == BackgroundColor.Black;

            if (paging) search.RenderPager(html, page, pageCount);

            for (int i = first; i <= last; i++)
                search.RenderPaletteItem(html, result[i], key, black);

            if (paging) search.RenderPager(html, page, pageCount);

            html.Add("</table>");

            return html.ToString();
        }

        private static void RenderPager(HtmlRenderer html, int page, int lastPage)
        {
            if (lastPage == 1) return;

            html.Add("\r\n<tr><td align=\"left\" colspan=\"2\">");
            html.Add("<table border=\"0\" cellpadding=\"0\" cellspacing=\"4\"><tr valign=\"middle\">");

            if (lastPage <= 5)
            {
                for (int i = 1; i <= lastPage; i++)
                    search.RenderPageMarker(html, i, i == page);
            }
            else
            {
                int l = page - 2;
                int h = page + 2;
                if (l < 1)
                {
                    l = 1;
                    h = l + 4;
                }
                if (h > lastPage)
                {
                    h = lastPage;
                    l = h - 4;
                }
                if (l > 1)
                {
                    search.RenderPageMarker(html, 1, 1 == page);
                    if (l > 2)
                        html.Add("<td style=\"font-size:20px;color:#909090;\">&nbsp;&nbsp;...&nbsp;&nbsp;</td>");
                }
                for (int i = l; i <= h; i++)
                    search.RenderPageMarker(html, i, i == page);
                if (h < lastPage)
                {
                    if ((h+1) < lastPage)
                        html.Add("<td style=\"font-size:20px;color:#909090;\">&nbsp;&nbsp;...&nbsp;&nbsp;</td>");
                    search.RenderPageMarker(html, lastPage, lastPage == page);
                }

            }
            html.Add("</tr></table></td></tr>");
        }

        private static void RenderPageMarker(HtmlRenderer html, int page, bool current)
        {
            
            Parameters attrs = new Parameters();
            attrs["align"] = "center";
            attrs["class"] = current ? "pager_selected" : "pager_page";
            if (!current)
            {
                Parameters p = new Parameters();
                p.Set("Page", (long)page);
                attrs["onclick"] = "submitEvent('" + Utils.PackEvent("page", p) + "');";
            }
            html.Add("td", attrs, true);

            html.Text(page.ToString());

            html.Add("</td>");
        }

        public static void RenderPaletteItem(HtmlRenderer html, PaletteItem item, string key, bool black)
        {
            html.Add("\r\n<tr");
            html.Add("valign", "middle");
            html.Add("bgcolor", black ? "#000000" : "#ffffff");
            html.Add("><td align=\"center\"");

            Parameters style = new Parameters();
            style["border-bottom"] = "2px solid #909090";
            style["padding"] = "8px";
            style["font-size"] = "small";
            style["color"] = black ? "#c0c0c0" : "#404040";

            html.Style(style);

            html.Add(">");

            if (search.showThumblains)
            {
                Parameters par = new Parameters();
                par["Id"] = item.Id.ToString();
                par["Key"] = key;
                html.Image("handler.ashx?q=thumbnail&t=" + HttpUtility.UrlEncode(par.Serialize()), "Palette source image thumbnail");
            }
            else
            {
                html.Text("No image");
            }

            html.Add("</td>\r\n<td");
            html.Add("align", "left");

            html.Style(style);

            html.Add(">");

            html.Add("<table border=\"0\" cellpadding=\"2\" cellspacing=\"0\"><tr><td align=\"left\"");

            style.Clear();
            style["padding"] = "8px";
            style["border-bottom"] = "1px solid #909090";

            html.Style(style);

            html.Add(">");

            if (item.Colors.Length > 0)
            {

                html.Add("<table border=\"0\" cellpadding=\"0\" cellspacing=\"4\"><tr valign=\"middle\">");

                style.Clear();
                style["width"] = "96px";
                style["height"] = "56px";
                style["padding-top"] = "40px";
                style["text-align"] = "center";

                for (int i = 0; i < item.Colors.Length; i++)
                {
                    html.Add("<td><div");

                    string c = "#" + (item.Colors[i].ToArgb() & 0xffffff).ToString("x6");

                    style["background-color"] = c;

                    HlsColor hls = ColorTransform.RgbToHls(item.Colors[i]);

                    style["color"] = hls.L > 500 ? "#202020" : "#f0f0f0";

                    html.Style(style);

                    html.Add(">");

                    html.Text(c);

                    html.Add("</div></td>");
                }


                html.Add("</tr></table>");

            }
            else
            {
                html.Text("Empty color list");
            }

            html.Add("</td></tr>\r\n<tr><td");

            style.Clear();
            style["font-size"] = "medium";
            style["padding"] = "8px";
            style["color"] = black ? "#c0c0c0" : "#404040";

            html.Style(style);

            html.Add(">");

            html.Text(item.Title);

            html.Add("</td></tr></table></td></tr>");
        }

        private static void EmptyResultTable(HtmlRenderer html, string message)
        {
            html.Clear();
            html.Add("<table border=\"0\" cellpadding=\"2\" cellspacing=\"0\">");
            html.Add("<tr valign=\"middle\"><td class=\"empty_results\">");
            html.Image("img/error.png", "Warning");
            html.Add("</td><td class=\"empty_results\">");
            html.Text(message);
            html.Add("</td></tr></table>");
        }
    }
}