using System;
using System.Collections.Generic;
using System.Text;

namespace Colorissimo.Web
{
    /*
     "Segoe UI","Helvetica",Garuda,Arial,sans-serif
    */

    public sealed class HtmlRenderer
    {
        private StringBuilder html;

        public const char UnicodeNbsp = (char)0xa000;

        public HtmlRenderer()
        {
            this.html = new StringBuilder();
        }

        public void Clear()
        {
            this.html.Length = 0;
        }

        public void Image(string src, string alt)
        {
            this.Add("<img");
            this.Add("src", src);
            if (!String.IsNullOrEmpty(alt))
                this.Add("alt", alt);
            this.Add("/>");
        }

        public void SelectItem(string text, string value, bool selected)
        {
            this.Add("<option");
            this.Add("value", value);
            if (selected)
                this.Add("selected", "yes");
            this.Add(">");
            this.Text(text);
            this.Add("</option>");
        }

        public void Error(string message, string details)
        {
            this.html.Append(@"<table border=""0"" cellpadding=""16"" cellspacing=""0"">
<tr valign=""top""><td style=""background:#B41B03;""><img src=""img/error.png"" /></td><td align=""left"" style=""background:#B41B03;color:#ffffff;font-size:x-large;"">");
            this.html.Append("\r\n");
            this.html.Append(HtmlRenderer.Escape(message));
            if (details != null)
            {
                this.html.Append(@"<br/><div style=""font-size:medium;"">");
                this.html.Append(HtmlRenderer.Escape(details, true, false));
                this.html.Append("</div>");
            }
            this.html.Append("</td></tr></table>");
        }

        public void Style(Parameters styles)
        {
            if (styles == null) return;
            string[] names = styles.AllNames;
            if (names.Length == 0)
                return;
            this.html.Append(" style=\"");
            for (int i = 0; i < names.Length; i++)
            {
                html.Append(names[i]);
                html.Append(':');
                html.Append(styles[names[i]]);
                html.Append(';');
            }
            this.html.Append('"');
        }

        public void Add(string attrName, string attrValue)
        {
            this.html.Append(' ');
            this.html.Append(attrName);
            this.html.Append("=\"");
            this.html.Append(HtmlRenderer.Escape(attrValue));
            this.html.Append('"');
        }

        public void Add(Parameters attrs)
        {
            if (attrs == null) return;
            string[] names = attrs.AllNames;
            for (int i = 0; i < names.Length; i++)
                this.Add(names[i], attrs[names[i]]);
        }

        public void Text(string s)
        {
            this.html.Append(HtmlRenderer.Escape(s));
        }

        public void Add(string s)
        {
            this.html.Append(s);
        }

        public void Add(string name, Parameters attrs, bool close)
        {
            this.html.Append('<');
            this.html.Append(name);
            this.Add(attrs);
            if (close) this.html.Append('>');
        }

        public void Add(string name, bool close)
        {
            this.html.Append('<');
            this.html.Append(name);
            if (close) this.html.Append('>');
        }

        public void End(string name)
        {
            this.html.Append("</");
            this.html.Append(name);
            this.html.Append(">");
        }

        public override string ToString()
        {
            return this.html.ToString();
        }

        public static string Escape(string s)
        {
            return HtmlRenderer.Escape(s, false, false);
        }

        public static string Escape(string s, bool br, bool nbsp)
        {
            if (s == null)
                return String.Empty;
            if (s.Length == 0)
                return String.Empty;

            StringBuilder sb = new StringBuilder();
            bool lf = false;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == HtmlRenderer.UnicodeNbsp)
                {
                    sb.Append("&nbsp;");
                    continue;
                }
                switch (s[i])
                {
                    case '&':
                        sb.Append("&amp;");
                        lf = false;
                        break;

                    case ' ':
                    case '\t':
                        if (nbsp)
                        {
                            sb.Append("&nbsp;");
                        }
                        else
                        {
                            sb.Append(' ');
                        }
                        lf = false;
                        break;

                    case '<':
                        sb.Append("&lt;");
                        lf = false;
                        break;

                    case '>':
                        sb.Append("&gt;");
                        lf = false;
                        break;

                    case '"':
                        sb.Append("&quot;");
                        lf = false;
                        break;

                    case '\r':
                        if (!lf)
                        {
                            if (br) { sb.Append("<br>"); }
                            else { sb.Append(' '); }
                            lf = true;
                        }
                        break;

                    case '\n':
                        if (!lf)
                        {
                            if (br) { sb.Append("<br>"); }
                            else { sb.Append(' '); }
                            lf = true;
                        }
                        break;

                    default:
                        sb.Append(s[i]);
                        lf = false;
                        break;
                }
            }
            return sb.ToString();
        }
    }

}