using System.Globalization;
using System.Text;
using LawFirm.Core;
using LawFirm.Models;
using SkyNet;

namespace LawFirm.codes
{
    public class Contact : WebPage
    {
        private static readonly Dictionary<string, List<DateTime>> Recent = new Dictionary<string, List<DateTime>>();
        private string pre = string.Empty;

        public override async Task OnInitialized()
        {
            PublicData d = await Store.LoadPublic();
            pre = Pick(QueryValue("for"), d.Areas.Select(a => a.Key));
            HtmlDoc.SetTitle("Contact | " + d.Site.Legal);
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("description", "Request a free consultation with " + d.Site.Legal + " in " + d.Site.City + ", " + d.Site.Region + ".");
            HtmlDoc.HtmlBodyText = HtmlDoc.HtmlBodyText
                .Replace("{plhd_head}", HeadHtml(d, "Contact"))
                .Replace("{plhd_main}", PageHtml(d, "contact"))
                .Replace("{plhd_foot}", FootHtml(d));
        }

        private static string Pick(string? value, IEnumerable<string> allowed)
        {
            string v = (value ?? string.Empty).Trim().ToLowerInvariant();
            return allowed.Contains(v) ? v : string.Empty;
        }

        private string DataPart(PublicData d, Block b, bool first)
        {
            if (b.Source != "contactform")
            {
                return string.Empty;
            }
            SiteData s = d.Site;
            StringBuilder sb = new StringBuilder(PageHead(s, b, first));
            sb.Append("<section class=\"ct-wrap ct-sec ct-ctwo\"><div class=\"ct-details\"><h2>Our office</h2><ul class=\"ct-contact\"><li>" + Icon("pin") + "<span>" + Ui.Esc(s.Street) + "<br>" + Ui.Esc(s.City + ", " + s.Region + " " + s.Zip) + "</span></li><li>" + Icon("phone") + "<a href=\"tel:" + Ui.Esc(s.Phone) + "\">" + Ui.Esc(s.Phone) + "</a></li>");
            if (s.Fax.Length > 0)
            {
                sb.Append("<li>" + Icon("doc") + "<span>Fax " + Ui.Esc(s.Fax) + "</span></li>");
            }
            sb.Append("<li>" + Icon("mail") + "<a href=\"mailto:" + Ui.Esc(s.Email) + "\">" + Ui.Esc(s.Email) + "</a></li><li>" + Icon("clock") + "<span>" + Lines(s.Hours) + "</span></li></ul><p class=\"ct-notice\">Please do not send confidential or time-sensitive information through this website until the firm has confirmed in writing that it represents you.</p></div>");
            sb.Append("<form class=\"ct-form\" id=\"ct-form\" onsubmit=\"return ContactJs.send()\" novalidate><h2>Request a free consultation</h2><p class=\"ct-formnote\">" + Ui.Esc(s.FormNote) + "</p>");
            sb.Append("<label class=\"ct-field\" for=\"ct-name\"><span>Your name</span><input type=\"text\" id=\"ct-name\" maxlength=\"60\" autocomplete=\"name\"><em class=\"ct-err\" id=\"ct-e-name\"></em></label>");
            sb.Append("<div class=\"ct-row2\"><label class=\"ct-field\" for=\"ct-email\"><span>Email</span><input type=\"email\" id=\"ct-email\" maxlength=\"80\" autocomplete=\"email\"><em class=\"ct-err\" id=\"ct-e-email\"></em></label>");
            sb.Append("<label class=\"ct-field\" for=\"ct-phone\"><span>Phone (optional)</span><input type=\"tel\" id=\"ct-phone\" maxlength=\"30\" autocomplete=\"tel\"><em class=\"ct-err\" id=\"ct-e-phone\"></em></label></div>");
            sb.Append("<label class=\"ct-field\" for=\"ct-area\"><span>What is it about?</span><select id=\"ct-area\"><option value=\"\">Not sure yet</option>" + string.Concat(d.Areas.Select(a => "<option value=\"" + Ui.Esc(a.Key) + "\"" + (a.Key == pre ? " selected" : string.Empty) + ">" + Ui.Esc(a.Name) + "</option>")) + "</select></label>");
            sb.Append("<label class=\"ct-field\" for=\"ct-msg\"><span>Briefly, how can we help?</span><textarea id=\"ct-msg\" rows=\"6\" maxlength=\"2000\"></textarea><em class=\"ct-err\" id=\"ct-e-msg\"></em></label>");
            sb.Append("<div class=\"ct-hp\" aria-hidden=\"true\"><label for=\"ct-web\">Leave this field empty</label><input type=\"text\" id=\"ct-web\" tabindex=\"-1\" autocomplete=\"off\"></div>");
            sb.Append("<div class=\"ct-ackbox\"><label class=\"ct-ack\" for=\"ct-ack\"><input type=\"checkbox\" id=\"ct-ack\"><span>I understand that sending this form does not create an attorney-client relationship, and that I should not include confidential details.</span></label><em class=\"ct-err\" id=\"ct-e-ack\"></em></div>");
            sb.Append("<button type=\"submit\" class=\"ct-btn ct-btn-wide\" id=\"ct-send\">Send my request</button><div class=\"ct-ok\" id=\"ct-sent\" role=\"status\" aria-live=\"polite\"></div></form></section>");
            return sb.ToString();
        }

        private static bool Allowed(string visitor)
        {
            lock (Recent)
            {
                if (!Recent.TryGetValue(visitor, out List<DateTime>? list))
                {
                    list = new List<DateTime>();
                    Recent[visitor] = list;
                }
                list.RemoveAll(t => t < DateTime.UtcNow.AddMinutes(-10));
                return list.Count < 3;
            }
        }

        private static void Noted(string visitor)
        {
            lock (Recent)
            {
                if (!Recent.TryGetValue(visitor, out List<DateTime>? list))
                {
                    list = new List<DateTime>();
                    Recent[visitor] = list;
                }
                list.Add(DateTime.UtcNow);
            }
        }

        public async Task<ApiResponse> Submit()
        {
            ApiResponse response = new ApiResponse();
            PublicData d = await Store.LoadPublic();
            string name = Ui.Clip(GetDataValue("name"), 60);
            string email = Ui.Clip(GetDataValue("email"), 80);
            string phone = Ui.Clip(GetDataValue("phone"), 30);
            string area = Pick(GetDataValue("area"), d.Areas.Select(a => a.Key));
            string msg = (GetDataValue("msg") ?? string.Empty).Replace("\r", string.Empty).Trim();
            bool ack = GetDataValue("ack") == "1";
            string trap = (GetDataValue("web") ?? string.Empty).Trim();
            string eName = name.Length < 2 ? "Please enter your name." : string.Empty;
            string eEmail = !Ui.IsEmail(email) ? "Please enter a valid email address." : string.Empty;
            string ePhone = phone.Length > 0 && !Ui.IsPhone(phone) ? "Please enter a valid phone number, or leave it empty." : string.Empty;
            string eMsg = msg.Length < 20 ? "Please write at least 20 characters so we understand your question." : msg.Length > 2000 ? "Please keep it under 2,000 characters." : string.Empty;
            string eAck = ack ? string.Empty : "Please confirm that you understand this before sending.";
            response.SetElementContents("ct-e-name", eName);
            response.SetElementContents("ct-e-email", eEmail);
            response.SetElementContents("ct-e-phone", ePhone);
            response.SetElementContents("ct-e-msg", eMsg);
            response.SetElementContents("ct-e-ack", eAck);
            if (eName + eEmail + ePhone + eMsg + eAck != string.Empty)
            {
                response.SetElementContents("ct-sent", string.Empty);
                response.ExecuteScript("ContactJs.failed();");
                return response;
            }
            string who = Local.Visitor();
            if (trap.Length > 0)
            {
                response.SetElementContents("ct-sent", "<b>Thank you.</b> Your request was received.");
                response.ExecuteScript("ContactJs.sent();");
                return response;
            }
            if (!Allowed(who))
            {
                response.SetElementContents("ct-sent", "<span class=\"ct-bad\">You have sent several requests in a short time. Please wait a few minutes, or call us on " + Ui.Esc(d.Site.Phone) + ".</span>");
                response.ExecuteScript("ContactJs.failed();");
                return response;
            }
            Noted(who);
            InquiryRec rec = new InquiryRec { Id = Guid.NewGuid().ToString("N").Substring(0, 12), Name = name, Email = email, Phone = phone, Area = area, Text = msg, When = DateTime.UtcNow, Status = "New", Ack = true };
            using (await Store.Hold())
            {
                List<InquiryRec> rows = await Store.Rows<InquiryRec>("messages");
                rows.Add(rec);
                await Store.SaveRows("messages", rows);
            }
            await Store.Log("visitor", "consultation request", name, true);
            if (Mailer.Ready && Ui.IsEmail(d.Site.NotifyEmail))
            {
                string result = await Mailer.Send(d.Site.NotifyEmail, "New consultation request from " + name, "From: " + name + " <" + email + ">\nPhone: " + (phone.Length > 0 ? phone : "not given") + "\nInterested in: " + (area.Length > 0 ? AreaName(d, area) : "not sure yet") + "\n\n" + msg + "\n");
                if (result.Length > 0)
                {
                    await Store.Log("system", "notification email", result, false);
                }
            }
            response.SetElementContents("ct-sent", "<b>Thank you, " + Ui.Esc(name) + ".</b> We have your request, and a member of the firm will contact you by email or phone. Sending this form did not create an attorney-client relationship.");
            response.ExecuteScript("ContactJs.sent();");
            return response;
        }

        private static string Tok(SiteData site, string v)
        {
            return v.Replace("{name}", site.Brand);
        }

        private static string TokH(SiteData site, string v)
        {
            return v.Replace("{name}", Ui.Esc(site.Brand));
        }

        private static string Lines(string v)
        {
            return Ui.Esc(v).Replace("\n", "<br>");
        }

        private static string Icon(string k)
        {
            string a = "<svg viewBox=\"0 0 24 24\" width=\"20\" height=\"20\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\">";
            return k switch
            {
                "scale" => a + "<path d=\"M12 4v16M7 20h10M5 7h14\"/><path d=\"M5 7l-3 7a3 3 0 0 0 6 0zM19 7l-3 7a3 3 0 0 0 6 0z\"/></svg>",
                "building" => a + "<path d=\"M4 21h16M6 21V8l6-4 6 4v13M9 11h1M14 11h1M9 15h1M14 15h1M11 21v-3h2v3\"/></svg>",
                "briefcase" => a + "<rect x=\"3\" y=\"7\" width=\"18\" height=\"13\" rx=\"2\"/><path d=\"M9 7V5a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2M3 13h18\"/></svg>",
                "home" => a + "<path d=\"M4 11l8-7 8 7M6 10v10h12V10M10 20v-5h4v5\"/></svg>",
                "people" => a + "<circle cx=\"9\" cy=\"8\" r=\"3.2\"/><path d=\"M3 19c0-3.3 2.7-5.5 6-5.5s6 2.2 6 5.5\"/><circle cx=\"17\" cy=\"9\" r=\"2.4\"/><path d=\"M16 13.6c2.8 0 5 1.8 5 4.9\"/></svg>",
                "doc" => a + "<path d=\"M7 3h7l4 4v14H7zM14 3v4h4M9.5 12h5M9.5 16h5\"/></svg>",
                "clock" => a + "<circle cx=\"12\" cy=\"12\" r=\"8.5\"/><path d=\"M12 7.5V12l3 2\"/></svg>",
                "pin" => a + "<path d=\"M12 21s-6.5-6-6.5-11a6.5 6.5 0 0 1 13 0c0 5-6.5 11-6.5 11z\"/><circle cx=\"12\" cy=\"10\" r=\"2.4\"/></svg>",
                "cal" => a + "<rect x=\"3.5\" y=\"5\" width=\"17\" height=\"15\" rx=\"2\"/><path d=\"M3.5 10h17M8 3v4M16 3v4\"/></svg>",
                "star" => a + "<path d=\"M12 3.5l2.6 5.4 5.9.8-4.3 4.1 1 5.8L12 16.9l-5.2 2.7 1-5.8-4.3-4.1 5.9-.8z\"/></svg>",
                "check" => "<svg viewBox=\"0 0 24 24\" width=\"16\" height=\"16\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2.4\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M5 12.5l4.5 4.5L19 7.5\"/></svg>",
                "phone" => a + "<path d=\"M6.5 3.5h3l1.5 4.5-2 1.5a11 11 0 0 0 5.5 5.5l1.5-2 4.5 1.5v3a2 2 0 0 1-2 2A16 16 0 0 1 4.5 5.5a2 2 0 0 1 2-2z\"/></svg>",
                "mail" => a + "<rect x=\"3.5\" y=\"5.5\" width=\"17\" height=\"13\" rx=\"2\"/><path d=\"M4 7l8 6 8-6\"/></svg>",
                "shield" => a + "<path d=\"M12 3l7 3v5c0 5-3 8.5-7 10-4-1.5-7-5-7-10V6z\"/><path d=\"M9 12l2 2 4-4\"/></svg>",
                _ => string.Empty
            };
        }

        private static string Logo(int size)
        {
            return "<svg viewBox=\"0 0 40 40\" width=\"" + size + "\" height=\"" + size + "\" aria-hidden=\"true\"><rect width=\"40\" height=\"40\" rx=\"4\" fill=\"#14233F\"/><rect x=\"1.5\" y=\"1.5\" width=\"37\" height=\"37\" rx=\"3\" fill=\"none\" stroke=\"#B08D4A\" stroke-width=\"0.8\"/><path d=\"M9 11v18M9 20h8M17 11v18M22 11l5 18 5-18\" fill=\"none\" stroke=\"#B08D4A\" stroke-width=\"2.4\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/></svg>";
        }

        private static string Pic(List<MediaItem> media, string src, string alt, bool eager, bool thumb)
        {
            if (src == string.Empty)
            {
                return string.Empty;
            }
            MediaItem? m = media.FirstOrDefault(x => x.Path == src);
            string use = thumb && m != null && m.Thumb.Length > 0 ? m.Thumb : src;
            return "<img src=\"" + Ui.Esc(use) + "\" alt=\"" + Ui.Esc(m != null && m.Alt != string.Empty ? m.Alt : alt) + "\"" + (eager ? string.Empty : " loading=\"lazy\"") + " decoding=\"async\">";
        }

        private static string Mv(List<MediaItem> media, string src, string alt, bool bg, bool eager)
        {
            if (src == string.Empty)
            {
                return string.Empty;
            }
            if (Blocks.IsVideo(src))
            {
                return "<video src=\"" + Ui.Esc(src) + "\" playsinline " + (bg ? "autoplay muted loop aria-hidden=\"true\"" : "controls preload=\"metadata\" aria-label=\"" + Ui.Esc(alt) + "\"") + "></video>";
            }
            return Pic(media, src, alt, eager, false);
        }

        private static string Btn(SiteData site, string label, string link, bool main)
        {
            return label == string.Empty || link == string.Empty ? string.Empty : "<a class=\"ct-btn" + (main ? string.Empty : " ct-btn-ghost") + "\" href=\"" + Ui.Esc(link) + "\"" + (link.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? " target=\"_blank\" rel=\"noopener\"" : string.Empty) + ">" + Ui.Esc(Tok(site, label)) + "</a>";
        }

        private static string Btns(SiteData site, Block b)
        {
            string x = Btn(site, b.Label, b.Link, true) + Btn(site, b.Label2, b.Link2, false);
            return x == string.Empty ? string.Empty : "<div class=\"ct-bbtns\">" + x + "</div>";
        }

        private static string Kick(SiteData site, Block b)
        {
            return b.Kicker == string.Empty ? string.Empty : "<div class=\"ct-eyebrow\">" + Ui.Esc(Tok(site, b.Kicker)) + "</div>";
        }

        private static string H2(SiteData site, Block b, string tag = "h2")
        {
            return b.Title == string.Empty ? string.Empty : "<" + tag + " class=\"ct-bh2\">" + Ui.Esc(Tok(site, b.Title)) + "</" + tag + ">";
        }

        private static string Rich(SiteData site, string html, string cls)
        {
            return html == string.Empty ? string.Empty : "<div class=\"ct-rich" + (cls == string.Empty ? string.Empty : " " + cls) + "\">" + TokH(site, html) + "</div>";
        }

        private static string Head(SiteData site, Block b, bool lead, string tag = "h2")
        {
            string x = Kick(site, b) + H2(site, b, tag) + (lead ? Rich(site, b.Text, "ct-blead") : string.Empty);
            return x == string.Empty ? string.Empty : "<div class=\"ct-bhead\">" + x + "</div>";
        }

        private static string PageHead(SiteData site, Block b, bool h1)
        {
            return "<div class=\"ct-wrap ct-ph\" id=\"" + Ui.Esc(b.Id) + "\">" + Kick(site, b) + "<" + (h1 ? "h1" : "h2") + ">" + Ui.Esc(Tok(site, b.Title)) + "</" + (h1 ? "h1" : "h2") + ">" + Rich(site, b.Text, "ct-blead") + "</div>";
        }

        private static string Body(string inner)
        {
            return "<section class=\"ct-wrap ct-sec\">" + inner + "</section>";
        }

        private static string Sec(Block b, string inner)
        {
            return "<section class=\"ct-wrap ct-sec\" id=\"" + Ui.Esc(b.Id) + "\">" + inner + "</section>";
        }

        private static string Hero(SiteData site, List<MediaItem> media, Block b, bool h1)
        {
            string tag = h1 ? "h1" : "h2";
            bool vid = Blocks.IsVideo(b.Media);
            string mv = Mv(media, b.Media, Tok(site, b.Title), vid, h1);
            return "<section class=\"ct-bhero" + (mv == string.Empty ? " ct-bplain" : string.Empty) + "\" id=\"" + Ui.Esc(b.Id) + "\">" + (mv != string.Empty ? "<div class=\"ct-bhero-p\">" + mv + "</div>" : string.Empty) + "<div class=\"ct-wrap ct-bhero-in\"><div class=\"ct-bhero-t\">" + Kick(site, b) + "<" + tag + " class=\"ct-btitle\">" + Ui.Esc(Tok(site, b.Title)) + "</" + tag + ">" + Rich(site, b.Text, "ct-blead") + Btns(site, b) + "</div></div></section>";
        }

        private static string Split(SiteData site, List<MediaItem> media, Block b)
        {
            string btn = Btn(site, b.Label, b.Link, true);
            return Sec(b, "<div class=\"ct-bsplit" + (b.Flip ? " ct-bflip" : string.Empty) + (b.Media == string.Empty ? " ct-bnomedia" : string.Empty) + "\">" + (b.Media != string.Empty ? "<div class=\"ct-bsm\">" + Mv(media, b.Media, Tok(site, b.Title), false, false) + "</div>" : string.Empty) + "<div class=\"ct-bst\">" + Kick(site, b) + H2(site, b) + Rich(site, b.Text, string.Empty) + (btn != string.Empty ? "<div class=\"ct-bbtns\">" + btn + "</div>" : string.Empty) + "</div></div>");
        }

        private static string Cards(SiteData site, Block b)
        {
            int n = Math.Clamp(b.Items.Count, 1, 4);
            StringBuilder sb = new StringBuilder(Head(site, b, true) + "<div class=\"ct-bcards ct-n" + n + "\">");
            foreach (BlockItem it in b.Items)
            {
                string ico = Icon(it.Icon);
                string inner = (ico != string.Empty ? "<span class=\"ct-wico\">" + ico + "</span>" : string.Empty) + "<b>" + Ui.Esc(Tok(site, it.Title)) + "</b>" + (it.Text != string.Empty ? "<p>" + Lines(Tok(site, it.Text)) + "</p>" : string.Empty);
                if (it.Link != string.Empty)
                {
                    sb.Append("<a class=\"ct-bcard\" href=\"" + Ui.Esc(it.Link) + "\">" + inner + "<span class=\"ct-bmore\">" + Ui.Esc(it.Label != string.Empty ? it.Label : "Learn more") + " &rarr;</span></a>");
                }
                else
                {
                    sb.Append("<div class=\"ct-bcard\">" + inner + "</div>");
                }
            }
            sb.Append("</div>");
            return Sec(b, sb.ToString());
        }

        private static string Steps(SiteData site, Block b)
        {
            StringBuilder sb = new StringBuilder(Head(site, b, true) + "<ol class=\"ct-bsteps ct-n" + Math.Clamp(b.Items.Count, 1, 6) + "\">");
            int i = 1;
            foreach (BlockItem it in b.Items)
            {
                sb.Append("<li><i>" + i++ + "</i><b>" + Ui.Esc(Tok(site, it.Title)) + "</b><p>" + Lines(Tok(site, it.Text)) + "</p></li>");
            }
            sb.Append("</ol>");
            return Sec(b, sb.ToString());
        }

        private static string StatRow(SiteData site, Block b)
        {
            StringBuilder sb = new StringBuilder(Head(site, b, false) + "<div class=\"ct-bstats ct-n" + Math.Clamp(b.Items.Count, 1, 6) + "\">");
            foreach (BlockItem it in b.Items)
            {
                sb.Append("<div><b>" + Ui.Esc(Tok(site, it.Title)) + "</b><span>" + Lines(Tok(site, it.Text)) + "</span></div>");
            }
            sb.Append("</div>");
            return Sec(b, sb.ToString());
        }

        private static string Initials(string name)
        {
            string[] parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 0 ? "?" : string.Concat(parts.Take(2).Select(p => char.ToUpperInvariant(p[0])));
        }

        private static string People(SiteData site, List<MediaItem> media, Block b)
        {
            StringBuilder sb = new StringBuilder(Head(site, b, true) + "<div class=\"ct-bpeople ct-n" + Math.Clamp(b.Items.Count, 1, 4) + "\">");
            foreach (BlockItem it in b.Items)
            {
                sb.Append("<div class=\"ct-bperson\">" + (it.Media != string.Empty && !Blocks.IsVideo(it.Media) ? Mv(media, it.Media, it.Title, false, false) : "<span class=\"ct-bnoface\">" + Ui.Esc(Initials(it.Title)) + "</span>") + "<b>" + Ui.Esc(Tok(site, it.Title)) + "</b><span>" + Lines(Tok(site, it.Text)) + "</span></div>");
            }
            sb.Append("</div>");
            return Sec(b, sb.ToString());
        }

        private static string Faq(SiteData site, Block b)
        {
            StringBuilder sb = new StringBuilder("<div class=\"ct-bfaq\">" + Head(site, b, false) + "<div class=\"ct-bfaqs\">");
            foreach (BlockItem it in b.Items)
            {
                sb.Append("<details class=\"ct-bq\"><summary>" + Ui.Esc(Tok(site, it.Title)) + "</summary><p>" + Lines(Tok(site, it.Text)) + "</p></details>");
            }
            sb.Append("</div></div>");
            return Sec(b, sb.ToString());
        }

        private static string Wide(SiteData site, List<MediaItem> media, Block b)
        {
            return b.Media == string.Empty ? string.Empty : Sec(b, "<figure class=\"ct-bmedia\">" + Mv(media, b.Media, b.Text, false, false) + (b.Text != string.Empty ? "<figcaption>" + Ui.Esc(Tok(site, b.Text)) + "</figcaption>" : string.Empty) + "</figure>");
        }

        private static string Cta(SiteData site, Block b)
        {
            return Sec(b, "<div class=\"ct-bcta\"><div>" + Kick(site, b) + H2(site, b) + Rich(site, b.Text, "ct-blead") + "</div>" + Btns(site, b) + "</div>");
        }

        private string PageHtml(PublicData d, string key)
        {
            StringBuilder sb = new StringBuilder();
            bool first = true;
            if (!d.Site.Pages.TryGetValue(key, out PageDoc? doc))
            {
                return string.Empty;
            }
            foreach (Block b in doc.Blocks.Where(x => !x.Hidden))
            {
                string html = b.Type switch
                {
                    "hero" => Hero(d.Site, d.Media, b, first),
                    "text" => Sec(b, "<div class=\"ct-btext\">" + Kick(d.Site, b) + H2(d.Site, b) + Rich(d.Site, b.Text, string.Empty) + "</div>"),
                    "split" => Split(d.Site, d.Media, b),
                    "cards" => Cards(d.Site, b),
                    "steps" => Steps(d.Site, b),
                    "stats" => StatRow(d.Site, b),
                    "people" => People(d.Site, d.Media, b),
                    "faq" => Faq(d.Site, b),
                    "media" => Wide(d.Site, d.Media, b),
                    "cta" => Cta(d.Site, b),
                    "data" => DataPart(d, b, first),
                    _ => string.Empty
                };
                if (html.Length > 0 && (b.Type == "hero" || b.Type == "data"))
                {
                    first = false;
                }
                sb.Append(html);
            }
            return sb.ToString();
        }

        private static string AreaName(PublicData d, string key)
        {
            return d.Areas.FirstOrDefault(a => a.Key == key)?.Name ?? string.Empty;
        }

        private static string AreaCard(PublicData d, AreaRec a)
        {
            return "<a class=\"ct-card ct-acard\" href=\"PracticeArea?k=" + Ui.Esc(a.Key) + "\"><span class=\"ct-card-img\">" + Pic(d.Media, a.Media, a.Name, false, true) + "</span><span class=\"ct-card-body\"><h3>" + Ui.Esc(a.Name) + "</h3><p>" + Ui.Esc(a.Summary) + "</p><span class=\"ct-card-more\">Learn more &rarr;</span></span></a>";
        }

        private static string LawyerCard(PublicData d, LawyerRec l)
        {
            string areas = string.Join(", ", l.Areas.Select(k => AreaName(d, k)).Where(n => n.Length > 0));
            return "<a class=\"ct-card ct-lcard\" href=\"Attorney?k=" + Ui.Esc(l.Key) + "\"><span class=\"ct-card-img\">" + Pic(d.Media, l.Photo, l.Name, false, true) + "</span><span class=\"ct-card-body\"><span class=\"ct-card-kick\">" + Ui.Esc(l.Role) + "</span><h3>" + Ui.Esc(l.Name) + "</h3><p>" + Ui.Esc(areas) + "</p></span></a>";
        }

        private static string ArticleCard(PublicData d, ArticleRec a)
        {
            string topic = d.Site.Topics.FirstOrDefault(t => t.Key == a.Topic)?.Name ?? a.Topic;
            return "<a class=\"ct-card ct-tcard\" href=\"Insights?a=" + Ui.Esc(a.Key) + "\"><span class=\"ct-card-img\">" + Pic(d.Media, a.Media, a.Title, false, true) + "</span><span class=\"ct-card-body\"><span class=\"ct-card-kick\">" + Ui.Esc(topic) + "</span><h3>" + Ui.Esc(a.Title) + "</h3><p>" + Ui.Esc(a.Summary) + "</p><span class=\"ct-card-meta\">" + Ui.Esc(Ui.Day(a.Date)) + " &middot; " + ReadMinutes(a) + " min read</span></span></a>";
        }

        private static int ReadMinutes(ArticleRec a)
        {
            return Math.Max(1, (int)Math.Round(a.Body.Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length / 200.0));
        }

        private static readonly (string Page, string Name)[] NavItems =
        {
            ("Home", "Home"),
            ("PracticeAreas", "Practice Areas"),
            ("Attorneys", "Attorneys"),
            ("Insights", "Insights"),
            ("About", "About"),
            ("Contact", "Contact")
        };

        private static string HeadHtml(PublicData d, string active)
        {
            StringBuilder sb = new StringBuilder("<a class=\"ct-skip\" href=\"#ct-main\">Skip to content</a><header class=\"ct-head\" id=\"ct-head\"><div class=\"ct-wrap ct-head-row\"><a class=\"ct-logo\" href=\"Home\" aria-label=\"" + Ui.Esc(d.Site.Legal) + " home\">" + Logo(42) + "<span><b>" + Ui.Esc(d.Site.Brand) + "</b><small>Attorneys at Law</small></span></a>");
            sb.Append("<nav class=\"ct-nav\" id=\"ct-nav\" aria-label=\"Main\"><div class=\"ct-nav-top\"><b>Menu</b><button type=\"button\" class=\"ct-ibtn\" aria-label=\"Close menu\" onclick=\"ContactJs.closeNav()\"><svg viewBox=\"0 0 24 24\" width=\"22\" height=\"22\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" aria-hidden=\"true\"><path d=\"M6 6l12 12M18 6L6 18\"/></svg></button></div><ul class=\"ct-menu\">");
            foreach ((string page, string name) in NavItems)
            {
                sb.Append("<li><a class=\"ct-ml" + (page == active ? " ct-on" : string.Empty) + "\" href=\"" + page + "\"" + (page == active ? " aria-current=\"page\"" : string.Empty) + ">" + name + "</a></li>");
            }
            sb.Append("</ul></nav><a class=\"ct-btn ct-btn-sm ct-head-cta\" href=\"Contact\">Free consultation</a><button type=\"button\" class=\"ct-burger\" aria-label=\"Open menu\" onclick=\"ContactJs.openNav()\"><svg viewBox=\"0 0 24 24\" width=\"24\" height=\"24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" aria-hidden=\"true\"><path d=\"M4 7h16M4 12h16M4 17h16\"/></svg></button></div><div class=\"ct-scrim\" id=\"ct-scrim\" onclick=\"ContactJs.closeNav()\"></div></header>");
            return sb.ToString();
        }

        private static string FootHtml(PublicData d)
        {
            SiteData s = d.Site;
            StringBuilder sb = new StringBuilder("<footer class=\"ct-foot\"><div class=\"ct-wrap ct-foot-grid\"><div class=\"ct-fbrand\"><a class=\"ct-logo\" href=\"Home\">" + Logo(42) + "<span><b>" + Ui.Esc(s.Brand) + "</b><small>Attorneys at Law</small></span></a><p>" + Ui.Esc(s.Tagline) + "</p></div>");
            sb.Append("<div><h3>Practice areas</h3><ul>" + string.Concat(d.Areas.Select(a => "<li><a href=\"PracticeArea?k=" + Ui.Esc(a.Key) + "\">" + Ui.Esc(a.Name) + "</a></li>")) + "</ul></div>");
            sb.Append("<div><h3>The firm</h3><ul><li><a href=\"About\">About</a></li><li><a href=\"Attorneys\">Attorneys</a></li><li><a href=\"Insights\">Insights</a></li><li><a href=\"Contact\">Contact</a></li></ul></div>");
            sb.Append("<div><h3>Visit</h3><address><span>" + Ui.Esc(s.Street) + "</span><span>" + Ui.Esc(s.City + ", " + s.Region + " " + s.Zip) + "</span><span><a href=\"tel:" + Ui.Esc(s.Phone) + "\">" + Ui.Esc(s.Phone) + "</a></span><span><a href=\"mailto:" + Ui.Esc(s.Email) + "\">" + Ui.Esc(s.Email) + "</a></span></address></div></div>");
            sb.Append("<div class=\"ct-foot-b\"><div class=\"ct-wrap\"><span>&copy; " + DateTime.Now.Year + " " + Ui.Esc(s.Legal) + ". " + Ui.Esc(s.Disclaimer) + "</span><a class=\"ct-staff\" href=\"AdminLogin\">Staff sign in</a></div></div></footer>");
            return sb.ToString();
        }
    }
}
