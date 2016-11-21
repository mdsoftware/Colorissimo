using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Colorissimo.Core;
using Colorissimo.Core.Clustering;
using Colorissimo.Web;

namespace clxweb
{
    public partial class add : System.Web.UI.Page
    {
        private Parameters parameters;
        private string errorMessage = null;
        private string message = null;
        private int paletteId = 0;

        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected string RenderError()
        {
            if (this.errorMessage == null) return String.Empty;



            HtmlRenderer html = new HtmlRenderer();

            html.Add("<tr><td align=\"center\" style=\"padding:16px;\" colspan=\"2\">");
            html.Error(this.errorMessage, null);
            html.Add("</td></tr>");

            return html.ToString();
        }

        protected string RenderMessage()
        {
            if (this.message == null) return String.Empty;



            HtmlRenderer html = new HtmlRenderer();

            html.Add("<tr><td align=\"center\" style=\"padding:8px;font-size:small;color:#909090;\" colspan=\"2\">");
            html.Text(this.message);
            html.Add("</td></tr>");

            if (this.paletteId > 0)
            {
                PaletteManager mgr = new PaletteManager(Utils.ConnectionString);
                PaletteItem pi = mgr.Load(this.paletteId);

                if (pi.Id == this.paletteId)
                {
                    html.Add("<tr><td align=\"center\" style=\"padding:2px;font-size:small;color:#909090; border:2px solid #909090;\" colspan=\"2\">");
                    html.Add("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\">");
                    search.RenderPaletteItem(html, pi, this.parameters["Key", String.Empty], false);
                    search.RenderPaletteItem(html, pi, this.parameters["Key", String.Empty], true);
                    html.Add("</table></td></tr>");
                }
            }

            return html.ToString();
        }

        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);

            string p = this.Context.Request.Params["parameters"];
            if (String.IsNullOrEmpty(p))
            {
                this.parameters = new Parameters();
                this.parameters["Key"] = Utils.GetTimeKey(300);
                this.parameters["Login"] = String.Empty;
                this.parameters["Description"] = String.Empty;
                this.parameters.Set("Processing", 1L);
            }
            else
            {
                this.parameters = Parameters.Deserialize(p);
            }

            string s = this.Context.Request.Params["login"];
            this.parameters["Login"] = String.IsNullOrEmpty(s) ? String.Empty : Utils.SecureString(s, 32);
            s = this.Context.Request.Params["description"];
            this.parameters["Description"] = String.IsNullOrEmpty(s) ? String.Empty : Utils.SecureString(s, 255);
            this.parameters.Set("Processing", this.Param("processing", 1L));

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



        private void ProcessEvent(string ev, Parameters param)
        {
            if (ev == "addfile")
            {
                if (!Utils.CheckTimeKey(this.parameters["Key", String.Empty])) return;
                /*
                 * Default user: 1234
                 * Default password: 1234
                 */
                string hash = "7f271e5f7da0199841293ff2d5b7e677";
                if (Utils.GetLogonHash(this.parameters["Login"], this.Context.Request.Params["password"]) != hash)
                    return;
                try
                {
                    DateTime start = DateTime.Now;

                    string fname;
                    byte[] file = this.GetFile(out fname);
                    if (file == null) return;

                    string desc = this.parameters["Description", null];
                    if (String.IsNullOrEmpty(desc))
                        desc = String.Format("File {0}, uploaded {1}", fname, start.ToString());

                    RawImage img = RawImage.Load(file);
                    file = null;

                    PaletteManager mgr = new PaletteManager(Utils.ConnectionString);
                    
                    IClustering clustering = this.parameters.Get("", 1L) == 1L ?
                        Clustering.KMeansClustering(150) :
                        Clustering.MedianCutClustering();

                    int id = mgr.CreatePalette(8, 2, 600, desc, img, clustering, 96);

                    img = null;

                    double sec = DateTime.Now.Subtract(start).TotalSeconds;

                    this.paletteId = id;
                    this.message = String.Format("Palette #{0} created, {1} sec.", id, sec.ToString("###0.0#"));

                }
                catch (Exception e)
                {
                    this.errorMessage = "FATAL ERROR: " + e.GetBaseException().Message;
                }
                return;
            }
            if (ev == "clustering")
            {
                if (!Utils.CheckTimeKey(this.parameters["Key", String.Empty])) return;
                /*
                 * Default user: 1234
                 * Default password: 1234
                 */
                string hash = "7f271e5f7da0199841293ff2d5b7e677";
                if (Utils.GetLogonHash(this.parameters["Login"], this.Context.Request.Params["password"]) != hash)
                    return;

                try
                {
                    DateTime start = DateTime.Now;

                    PaletteManager mgr = new PaletteManager(Utils.ConnectionString);
                    Clustering.ClusterPalettes(mgr, 20, 60);

                    double sec = DateTime.Now.Subtract(start).TotalSeconds;
                    this.message = String.Format("Clustering completed, {1} sec.", sec.ToString("###0.0#"));
                }
                catch (Exception e)
                {
                    this.errorMessage = "FATAL ERROR: " + e.GetBaseException().Message;
                }
                return;
            }
        }

        private byte[] GetFile(out string fileName)
        {
            fileName = null;
            string[] keys = this.Context.Request.Files.AllKeys;
            if (keys == null)
            {
                this.errorMessage = "File must be specified";
                return null;
            }
            if (keys.Length != 1)
            {
                this.errorMessage = "File must be specified";
                return null;
            }
            HttpPostedFile f = this.Context.Request.Files[keys[0]];
            switch (f.ContentType.ToLower())
            {
                case "image/jpeg":
                case "image/jpg":
                case "image/pjpeg":
                case "application/octet-stream":
                    break;

                default:
                    this.errorMessage = String.Format("Only Jpeg images are accepted ({0})", f.ContentType);
                    return null;
            }
            if (f.ContentLength > (2 * 1024 * 1024))
            {
                this.errorMessage = "File is too large";
                return null;
            }
            string fname = f.FileName;
            switch (Path.GetExtension(fname).ToLower())
            {
                case ".jpg":
                case ".jpeg":
                    break;

                default:
                    this.errorMessage = "Invalid file expension, only .jpg or .jpeg are accepted";
                    return null;
            }
            fileName = Path.GetFileName(fname);
            Stream s = f.InputStream;
            byte[] file = new byte[s.Length];
            s.Read(file, 0, file.Length);
            s = null;

            /*
             FFD8FFFE00, .JPEG;.JPE;.JPG, "JPG Graphic File"
             FFD8FFE000, .JPEG;.JPE;.JPG, "JPG Graphic File"
             */

            if (file.Length < 16)
            {
                this.errorMessage = "File is too short";
                return null;
            }

            if (!add.Match(file, 0, new byte[3] { 0xff, 0xd8, 0xff }) ||
                !add.Match(file, 6, new byte[5] { 0x4a, 0x46, 0x49, 0x46, 0x00 }))
            {
                this.errorMessage = "Invalid Jpeg file content/signature";
                return null;
            }

            return file;
        }

        private static bool Match(byte[] buffer, int offset, byte[] key)
        {
            int p = offset;
            for (int i = 0; i < key.Length; i++)
                if (buffer[p++] != key[i]) return false;
            return true;
        }

        protected string Param()
        {
            return this.parameters.Serialize();
        }

        protected string Param(string name)
        {
            return this.parameters[name, String.Empty];
        }

        protected string Escape(string s)
        {
            return HtmlRenderer.Escape(s);
        }

        protected string RenderProcessingList()
        {
            HtmlRenderer html = new HtmlRenderer();

            long p = this.parameters.Get("Processing", 1L);

            html.SelectItem("Clustering", "1", p == 1);
            html.SelectItem("Median Cut", "2", p == 2);

            return html.ToString();
        }

        private long Param(string name, long defaultValue)
        {
            string s = this.Context.Request.Params[name];
            if (String.IsNullOrEmpty(s)) return defaultValue;
            long l;
            if (Int64.TryParse(s, out l)) return l;
            return defaultValue;
        }
    }
}