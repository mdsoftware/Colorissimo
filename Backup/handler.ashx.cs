using System;
using System.Collections.Generic;
using System.Web;
using System.Text;
using System.Drawing;
using System.Threading;
using Colorissimo.Web;
using Colorissimo.Core;

namespace clxweb
{
    /// <summary>
    /// Summary description for handler
    /// </summary>
    public class handler : IHttpHandler
    {

        private static byte[] emptyGif = new byte[51] {
0x47,0x49,0x46,0x38,0x39,0x61,0x01,0x00,0x01,0x00,0x91,0x00,0x00,0xff,0xff,0xff,
0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x21,0xf9,0x04,0x09,0x00,0x00,0x00,
0x00,0x2c,0x00,0x00,0x00,0x00,0x01,0x00,0x01,0x00,0x00,0x08,0x04,0x00,0x01,0x04,
0x04,0x00,0x3b};

        private static Random rnd = new Random();

        public void ProcessRequest(HttpContext context)
        {
            HtmlRenderer html = new HtmlRenderer();
            bool error = false;
            try
            {
                string q = context.Request.Params["q"];
                if (String.IsNullOrEmpty(q))
                    throw new Exception("Empty query");
                switch (q.ToLower())
                {
                    case "colorsample":
                        handler.ColorSample(html, context.Request.Params["t"]);
                        break;

                    case "colorinfo":
                        handler.ColorInfo(html, context.Request.Params["t"]);
                        break;

                    case "thumbnail":
                        if (handler.Thumbnail(context)) return;
                        error = true;
                        break;

                    default:
                        throw new Exception("Unsupported query");
                }

            }
            catch
            {
                error = true;
            }
            if (error)
            {
                /*
                html.Clear();
                html.Add("<div style=\"color:#ff0000;\">");
                html.Text("Error occured: " + e.GetBaseException().Message);
                html.Add("</div>");
                 */
                context.Response.Clear();
                context.Response.StatusCode = 500;
                context.Response.Status = "Internal server error";
                return;
            }
            handler.SendHtml(context, html.ToString());
        }

        private static bool Thumbnail(HttpContext context)
        {
            Parameters p = Parameters.Deserialize(context.Request.Params["t"]);
            if (!Utils.CheckTimeKey(p["Key", "?"]))
                return false;
            long id = p.Get("Id", -1L);
            if (id < 0L)
                return false;
            PaletteManager mgr = new PaletteManager(Utils.ConnectionString);
            byte[] image = mgr.GetPaletteThumbnail((int)id);
            if (image == null)
            {
                handler.SendBytes(context, "image/gif", handler.emptyGif);
            }
            else
            {
                handler.SendBytes(context, "image/jpeg", image);
            }
            image = null;
            return true;
        }

        private static void ColorSample(HtmlRenderer html, string s)
        {
            int c = ColorParser.Parse(s);
            Parameters style = new Parameters();
            if (c == ColorParser.InvalidColor)
            {
                style["color"] = "#000000";
                style["background-color"] = "#ffffff";
            }
            else
            {
                s = "#" + c.ToString("x6");
                style["color"] = s;
                style["background-color"] = s;
                HlsColor hls = ColorTransform.RgbToHls(ColorTransform.IntToRgb(c));
                if (hls.L > 800)
                    style["border"] = "1px solid #808080";
            }
            html.Add("<div");
            html.Style(style);
            html.Add(">");
            html.Add(String.IsNullOrEmpty(s) ? "X" : "?");
            html.Add("</div>");
        }

        private static void ColorInfo(HtmlRenderer html, string s)
        {
            if (String.IsNullOrEmpty(s))
            {
                html.Text("Color not specified");
                return;
            }
            int c = ColorParser.Parse(s);
            if (c == ColorParser.InvalidColor)
            {
                html.Text("Invalid color string");
                return;
            }
            
            html.Text("WEB #" + c.ToString("x6"));

            Color cc = Color.FromArgb(c);
            html.Text(String.Format(", RGB({0},{1},{2}),",
               ((double)cc.R / 255f).ToString("######0.0##"),
               ((double)cc.G / 255f).ToString("######0.0##"),
               ((double)cc.B / 255f).ToString("######0.0##")));

            HlsColor hls = ColorTransform.RgbToHls(cc);
            html.Add("<br>");
            html.Text(String.Format("HLS({0},{1},{2})",
               ((double)hls.H / 100f).ToString("######0.0##"),
               ((double)hls.L / 1000f).ToString("######0.0##"),
               ((double)hls.S / 1000f).ToString("######0.0##")));

            LabColor lab = ColorTransform.RgbToLab(cc);
            html.Text(String.Format(", LAB({0},{1},{2})",
               lab.L.ToString("######0.0##"),
               lab.A.ToString("######0.0##"),
               lab.B.ToString("######0.0##")));
        }

        private static void SendHtml(HttpContext context, string html)
        {
            context.Response.AddHeader("Content-Type", "text/html");
            byte[] buf = Encoding.ASCII.GetBytes(html);
            context.Response.AddHeader("Content-Length", buf.Length.ToString());
            context.Response.OutputStream.Write(buf, 0, buf.Length);
            buf = null;
        }

        private static void SendBytes(HttpContext context, string contentType, byte[] image)
        {
            context.Response.AddHeader("Content-Type", contentType);
            context.Response.AddHeader("Content-Length", image.Length.ToString());
            context.Response.OutputStream.Write(image, 0, image.Length);
        }

        public bool IsReusable
        {
            get
            {
                return true;
            }
        }
    }
}