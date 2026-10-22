using System.Globalization;
using System.Text;
using LawFirm.Core;
using LawFirm.Models;
using SkyNet;

namespace LawFirm.codes
{
    public class Insights : WebPage
    {
        private const int PageSize = 6;
        private string topicSel = string.Empty;
        private int pageSel = 1;
        private string artSel = string.Empty;

        public override async Task OnInitialized()
        {
            PublicData d = await Store.LoadPublic();
            topicSel = Pick(QueryValue("t"), d.Site.Topics.Select(t => t.Key));
            pageSel = PageNo(QueryValue("p"));
            artSel = Pick(QueryValue("a"), d.Articles.Select(a => a.Key));
            ArticleRec? open = d.Articles.FirstOrDefault(a => a.Key == artSel);
            HtmlDoc.SetTitle((open == null ? "Insights" : open.Title) + " | " + d.Site.Legal);
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("description", open == null ? "Articles from the attorneys of " + d.Site.Legal + " on business, employment, property and family matters." : open.Summary);
            HtmlDoc.HtmlBodyText = HtmlDoc.HtmlBodyText
                .Replace("{plhd_head}", HeadHtml(d, "Insights"))
                .Replace("{plhd_main}", PageHtml(d, "insights"))
                .Replace("{plhd_foot}", FootHtml(d));
        }

        private static string Pick(string? value, IEnumerable<string> allowed)
        {
            string v = (value ?? string.Empty).Trim().ToLowerInvariant();
            return allowed.Contains(v) ? v : string.Empty;
        }

        private static int PageNo(string? v)
        {
            return int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) && n > 0 ? n : 1;
        }

        public async Task<ApiResponse> Filter()
        {
            ApiResponse response = new ApiResponse();
            PublicData d = await Store.LoadPublic();
            string topic = Pick(GetDataValue("topic"), d.Site.Topics.Select(t => t.Key));
            int pg = PageNo(GetDataValue("pg"));
            Block? head = d.Site.Pages["insights"].Blocks.FirstOrDefault(x => x.Type == "data" && x.Source == "articlelist");
            if (head == null)
            {
                return response;
            }
            response.SetElementContents("in-stage", ListStage(d, head, topic, pg, true, out int shown));
            response.ExecuteScript("InsightsJs.filtered('" + Ui.Js(topic) + "'," + shown + ");");
            return response;
        }

        public async Task<ApiResponse> Open()
        {
            ApiResponse response = new ApiResponse();
            PublicData d = await Store.LoadPublic();
            ArticleRec? a = d.Articles.FirstOrDefault(x => x.Key == Pick(GetDataValue("art"), d.Articles.Select(y => y.Key)));
            if (a == null)
            {
                response.ExecuteScript("InsightsJs.toast('That article could not be found.');");
                return response;
            }
            response.SetElementContents("in-stage", ArticleView(d, a));
            response.ExecuteScript("InsightsJs.opened('" + Ui.Js(a.Key) + "','" + Ui.Js(a.Title + " | " + d.Site.Legal) + "');");
            return response;
        }

        private static string ListStage(PublicData d, Block b, string topic, int pg, bool h1, out int shown)
        {
            List<ArticleRec> rows = d.Articles.Where(a => topic == string.Empty || a.Topic == topic).ToList();
            int pages = Math.Max(1, (rows.Count + PageSize - 1) / PageSize);
            pg = Math.Min(pg, pages);
            shown = pg;
            StringBuilder sb = new StringBuilder(PageHead(d.Site, b, h1));
            StringBuilder chips = new StringBuilder("<button type=\"button\" class=\"in-chip" + (topic == string.Empty ? " in-act" : string.Empty) + "\" aria-pressed=\"" + (topic == string.Empty ? "true" : "false") + "\" onclick=\"InsightsJs.filter('',1)\">All topics</button>");
            foreach (TopicRec t in d.Site.Topics)
            {
                chips.Append("<button type=\"button\" class=\"in-chip" + (t.Key == topic ? " in-act" : string.Empty) + "\" aria-pressed=\"" + (t.Key == topic ? "true" : "false") + "\" onclick=\"InsightsJs.filter('" + Ui.Js(t.Key) + "',1)\">" + Ui.Esc(t.Name) + "</button>");
            }
            sb.Append("<section class=\"in-wrap in-sec\"><div class=\"in-chips\" role=\"group\" aria-label=\"Filter by topic\">" + chips + "</div>");
            if (rows.Count == 0)
            {
                sb.Append("<p class=\"in-none\">No articles in this topic yet.</p>");
            }
            else
            {
                sb.Append("<div class=\"in-cardgrid in-cg3\">" + string.Concat(rows.Skip((pg - 1) * PageSize).Take(PageSize).Select(a => ArticleCard(d, a))) + "</div>");
                if (pages > 1)
                {
                    sb.Append("<div class=\"in-pager\"><span>Page " + pg + " of " + pages + " &middot; " + Ui.Count(rows.Count, "article", "articles") + "</span><div><button type=\"button\" class=\"in-btn in-btn-ghost in-btn-sm\"" + (pg <= 1 ? " disabled" : string.Empty) + " onclick=\"InsightsJs.filter('" + Ui.Js(topic) + "'," + (pg - 1) + ")\">Previous</button><button type=\"button\" class=\"in-btn in-btn-ghost in-btn-sm\"" + (pg >= pages ? " disabled" : string.Empty) + " onclick=\"InsightsJs.filter('" + Ui.Js(topic) + "'," + (pg + 1) + ")\">Next</button></div></div>");
                }
            }
            sb.Append("</section>");
            return sb.ToString();
        }

        private static string ArticleView(PublicData d, ArticleRec a)
        {
            string topic = d.Site.Topics.FirstOrDefault(t => t.Key == a.Topic)?.Name ?? a.Topic;
            LawyerRec? by = d.Lawyers.FirstOrDefault(l => l.Key == a.Author);
            List<ArticleRec> more = d.Articles.Where(x => x.Key != a.Key && x.Topic == a.Topic).Take(2).ToList();
            StringBuilder sb = new StringBuilder();
            sb.Append("<article class=\"in-wrap in-article\"><nav class=\"in-crumbs\" aria-label=\"Breadcrumb\"><a href=\"Home\">Home</a> / <a href=\"Insights\" onclick=\"return InsightsJs.back()\">Insights</a> / " + Ui.Esc(topic) + "</nav><div class=\"in-eyebrow\">" + Ui.Esc(topic) + "</div><h1>" + Ui.Esc(a.Title) + "</h1><p class=\"in-byline\">" + Ui.Esc(Ui.Day(a.Date)) + " &middot; " + ReadMinutes(a) + " min read" + (by != null ? " &middot; By <a href=\"Attorney?k=" + Ui.Esc(by.Key) + "\">" + Ui.Esc(by.Name) + "</a>" : string.Empty) + "</p>");
            if (a.Media.Length > 0)
            {
                sb.Append("<figure class=\"in-figure\">" + Pic(d.Media, a.Media, a.Title, true, false) + "</figure>");
            }
            sb.Append("<div class=\"in-prose in-body\">" + Ui.Paras(a.Body) + "</div><p class=\"in-notice\">" + Ui.Esc(d.Site.Disclaimer) + " This article is general information. It is not a substitute for advice about your own situation.</p><div class=\"in-bbtns\"><a class=\"in-btn in-btn-ghost\" href=\"Insights\" onclick=\"return InsightsJs.back()\">&larr; All insights</a><a class=\"in-btn\" href=\"Contact\">Request a consultation</a></div></article>");
            if (more.Count > 0)
            {
                sb.Append("<section class=\"in-wrap in-sec\"><div class=\"in-bhead\"><div class=\"in-eyebrow\">Keep reading</div><h2 class=\"in-bh2\">More on " + Ui.Esc(topic.ToLowerInvariant()) + "</h2></div><div class=\"in-cardgrid in-cg3\">" + string.Concat(more.Select(x => ArticleCard(d, x))) + "</div></section>");
            }
            return sb.ToString();
        }

        private string DataPart(PublicData d, Block b, bool first)
        {
            if (b.Source != "articlelist")
            {
                return string.Empty;
            }
            ArticleRec? open = d.Articles.FirstOrDefault(a => a.Key == artSel);
            return "<div id=\"in-stage\" aria-live=\"polite\">" + (open != null ? ArticleView(d, open) : ListStage(d, b, topicSel, pageSel, first, out int shown)) + "</div>";
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
            return label == string.Empty || link == string.Empty ? string.Empty : "<a class=\"in-btn" + (main ? string.Empty : " in-btn-ghost") + "\" href=\"" + Ui.Esc(link) + "\"" + (link.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? " target=\"_blank\" rel=\"noopener\"" : string.Empty) + ">" + Ui.Esc(Tok(site, label)) + "</a>";
        }

        private static string Btns(SiteData site, Block b)
        {
            string x = Btn(site, b.Label, b.Link, true) + Btn(site, b.Label2, b.Link2, false);
            return x == string.Empty ? string.Empty : "<div class=\"in-bbtns\">" + x + "</div>";
        }

        private static string Kick(SiteData site, Block b)
        {
            return b.Kicker == string.Empty ? string.Empty : "<div class=\"in-eyebrow\">" + Ui.Esc(Tok(site, b.Kicker)) + "</div>";
        }

        private static string H2(SiteData site, Block b, string tag = "h2")
        {
            return b.Title == string.Empty ? string.Empty : "<" + tag + " class=\"in-bh2\">" + Ui.Esc(Tok(site, b.Title)) + "</" + tag + ">";
        }

        private static string Rich(SiteData site, string html, string cls)
        {
            return html == string.Empty ? string.Empty : "<div class=\"in-rich" + (cls == string.Empty ? string.Empty : " " + cls) + "\">" + TokH(site, html) + "</div>";
        }

        private static string Head(SiteData site, Block b, bool lead, string tag = "h2")
        {
            string x = Kick(site, b) + H2(site, b, tag) + (lead ? Rich(site, b.Text, "in-blead") : string.Empty);
            return x == string.Empty ? string.Empty : "<div class=\"in-bhead\">" + x + "</div>";
        }

        private static string PageHead(SiteData site, Block b, bool h1)
        {
            return "<div class=\"in-wrap in-ph\" id=\"" + Ui.Esc(b.Id) + "\">" + Kick(site, b) + "<" + (h1 ? "h1" : "h2") + ">" + Ui.Esc(Tok(site, b.Title)) + "</" + (h1 ? "h1" : "h2") + ">" + Rich(site, b.Text, "in-blead") + "</div>";
        }

        private static string Body(string inner)
        {
            return "<section class=\"in-wrap in-sec\">" + inner + "</section>";
        }

        private static string Sec(Block b, string inner)
        {
            return "<section class=\"in-wrap in-sec\" id=\"" + Ui.Esc(b.Id) + "\">" + inner + "</section>";
        }

        private static string Hero(SiteData site, List<MediaItem> media, Block b, bool h1)
        {
            string tag = h1 ? "h1" : "h2";
            bool vid = Blocks.IsVideo(b.Media);
            string mv = Mv(media, b.Media, Tok(site, b.Title), vid, h1);
            return "<section class=\"in-bhero" + (mv == string.Empty ? " in-bplain" : string.Empty) + "\" id=\"" + Ui.Esc(b.Id) + "\">" + (mv != string.Empty ? "<div class=\"in-bhero-p\">" + mv + "</div>" : string.Empty) + "<div class=\"in-wrap in-bhero-in\"><div class=\"in-bhero-t\">" + Kick(site, b) + "<" + tag + " class=\"in-btitle\">" + Ui.Esc(Tok(site, b.Title)) + "</" + tag + ">" + Rich(site, b.Text, "in-blead") + Btns(site, b) + "</div></div></section>";
        }

        private static string Split(SiteData site, List<MediaItem> media, Block b)
        {
            string btn = Btn(site, b.Label, b.Link, true);
            return Sec(b, "<div class=\"in-bsplit" + (b.Flip ? " in-bflip" : string.Empty) + (b.Media == string.Empty ? " in-bnomedia" : string.Empty) + "\">" + (b.Media != string.Empty ? "<div class=\"in-bsm\">" + Mv(media, b.Media, Tok(site, b.Title), false, false) + "</div>" : string.Empty) + "<div class=\"in-bst\">" + Kick(site, b) + H2(site, b) + Rich(site, b.Text, string.Empty) + (btn != string.Empty ? "<div class=\"in-bbtns\">" + btn + "</div>" : string.Empty) + "</div></div>");
        }

        private static string Cards(SiteData site, Block b)
        {
            int n = Math.Clamp(b.Items.Count, 1, 4);
            StringBuilder sb = new StringBuilder(Head(site, b, true) + "<div class=\"in-bcards in-n" + n + "\">");
            foreach (BlockItem it in b.Items)
            {
                string ico = Icon(it.Icon);
                string inner = (ico != string.Empty ? "<span class=\"in-wico\">" + ico + "</span>" : string.Empty) + "<b>" + Ui.Esc(Tok(site, it.Title)) + "</b>" + (it.Text != string.Empty ? "<p>" + Lines(Tok(site, it.Text)) + "</p>" : string.Empty);
                if (it.Link != string.Empty)
                {
                    sb.Append("<a class=\"in-bcard\" href=\"" + Ui.Esc(it.Link) + "\">" + inner + "<span class=\"in-bmore\">" + Ui.Esc(it.Label != string.Empty ? it.Label : "Learn more") + " &rarr;</span></a>");
                }
                else
                {
                    sb.Append("<div class=\"in-bcard\">" + inner + "</div>");
                }
            }
            sb.Append("</div>");
            return Sec(b, sb.ToString());
        }

        private static string Steps(SiteData site, Block b)
        {
            StringBuilder sb = new StringBuilder(Head(site, b, true) + "<ol class=\"in-bsteps in-n" + Math.Clamp(b.Items.Count, 1, 6) + "\">");
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
            StringBuilder sb = new StringBuilder(Head(site, b, false) + "<div class=\"in-bstats in-n" + Math.Clamp(b.Items.Count, 1, 6) + "\">");
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
            StringBuilder sb = new StringBuilder(Head(site, b, true) + "<div class=\"in-bpeople in-n" + Math.Clamp(b.Items.Count, 1, 4) + "\">");
            foreach (BlockItem it in b.Items)
            {
                sb.Append("<div class=\"in-bperson\">" + (it.Media != string.Empty && !Blocks.IsVideo(it.Media) ? Mv(media, it.Media, it.Title, false, false) : "<span class=\"in-bnoface\">" + Ui.Esc(Initials(it.Title)) + "</span>") + "<b>" + Ui.Esc(Tok(site, it.Title)) + "</b><span>" + Lines(Tok(site, it.Text)) + "</span></div>");
            }
            sb.Append("</div>");
            return Sec(b, sb.ToString());
        }

        private static string Faq(SiteData site, Block b)
        {
            StringBuilder sb = new StringBuilder("<div class=\"in-bfaq\">" + Head(site, b, false) + "<div class=\"in-bfaqs\">");
            foreach (BlockItem it in b.Items)
            {
                sb.Append("<details class=\"in-bq\"><summary>" + Ui.Esc(Tok(site, it.Title)) + "</summary><p>" + Lines(Tok(site, it.Text)) + "</p></details>");
            }
            sb.Append("</div></div>");
            return Sec(b, sb.ToString());
        }

        private static string Wide(SiteData site, List<MediaItem> media, Block b)
        {
            return b.Media == string.Empty ? string.Empty : Sec(b, "<figure class=\"in-bmedia\">" + Mv(media, b.Media, b.Text, false, false) + (b.Text != string.Empty ? "<figcaption>" + Ui.Esc(Tok(site, b.Text)) + "</figcaption>" : string.Empty) + "</figure>");
        }

        private static string Cta(SiteData site, Block b)
        {
            return Sec(b, "<div class=\"in-bcta\"><div>" + Kick(site, b) + H2(site, b) + Rich(site, b.Text, "in-blead") + "</div>" + Btns(site, b) + "</div>");
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
                    "text" => Sec(b, "<div class=\"in-btext\">" + Kick(d.Site, b) + H2(d.Site, b) + Rich(d.Site, b.Text, string.Empty) + "</div>"),
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
            return "<a class=\"in-card in-acard\" href=\"PracticeArea?k=" + Ui.Esc(a.Key) + "\"><span class=\"in-card-img\">" + Pic(d.Media, a.Media, a.Name, false, true) + "</span><span class=\"in-card-body\"><h3>" + Ui.Esc(a.Name) + "</h3><p>" + Ui.Esc(a.Summary) + "</p><span class=\"in-card-more\">Learn more &rarr;</span></span></a>";
        }

        private static string LawyerCard(PublicData d, LawyerRec l)
        {
            string areas = string.Join(", ", l.Areas.Select(k => AreaName(d, k)).Where(n => n.Length > 0));
            return "<a class=\"in-card in-lcard\" href=\"Attorney?k=" + Ui.Esc(l.Key) + "\"><span class=\"in-card-img\">" + Pic(d.Media, l.Photo, l.Name, false, true) + "</span><span class=\"in-card-body\"><span class=\"in-card-kick\">" + Ui.Esc(l.Role) + "</span><h3>" + Ui.Esc(l.Name) + "</h3><p>" + Ui.Esc(areas) + "</p></span></a>";
        }

        private static string ArticleCard(PublicData d, ArticleRec a)
        {
            string topic = d.Site.Topics.FirstOrDefault(t => t.Key == a.Topic)?.Name ?? a.Topic;
            return "<a class=\"in-card in-tcard\" href=\"Insights?a=" + Ui.Esc(a.Key) + "\"><span class=\"in-card-img\">" + Pic(d.Media, a.Media, a.Title, false, true) + "</span><span class=\"in-card-body\"><span class=\"in-card-kick\">" + Ui.Esc(topic) + "</span><h3>" + Ui.Esc(a.Title) + "</h3><p>" + Ui.Esc(a.Summary) + "</p><span class=\"in-card-meta\">" + Ui.Esc(Ui.Day(a.Date)) + " &middot; " + ReadMinutes(a) + " min read</span></span></a>";
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
            StringBuilder sb = new StringBuilder("<a class=\"in-skip\" href=\"#in-main\">Skip to content</a><header class=\"in-head\" id=\"in-head\"><div class=\"in-wrap in-head-row\"><a class=\"in-logo\" href=\"Home\" aria-label=\"" + Ui.Esc(d.Site.Legal) + " home\">" + Logo(42) + "<span><b>" + Ui.Esc(d.Site.Brand) + "</b><small>Attorneys at Law</small></span></a>");
            sb.Append("<nav class=\"in-nav\" id=\"in-nav\" aria-label=\"Main\"><div class=\"in-nav-top\"><b>Menu</b><button type=\"button\" class=\"in-ibtn\" aria-label=\"Close menu\" onclick=\"InsightsJs.closeNav()\"><svg viewBox=\"0 0 24 24\" width=\"22\" height=\"22\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" aria-hidden=\"true\"><path d=\"M6 6l12 12M18 6L6 18\"/></svg></button></div><ul class=\"in-menu\">");
            foreach ((string page, string name) in NavItems)
            {
                sb.Append("<li><a class=\"in-ml" + (page == active ? " in-on" : string.Empty) + "\" href=\"" + page + "\"" + (page == active ? " aria-current=\"page\"" : string.Empty) + ">" + name + "</a></li>");
            }
            sb.Append("</ul></nav><a class=\"in-btn in-btn-sm in-head-cta\" href=\"Contact\">Free consultation</a><button type=\"button\" class=\"in-burger\" aria-label=\"Open menu\" onclick=\"InsightsJs.openNav()\"><svg viewBox=\"0 0 24 24\" width=\"24\" height=\"24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" aria-hidden=\"true\"><path d=\"M4 7h16M4 12h16M4 17h16\"/></svg></button></div><div class=\"in-scrim\" id=\"in-scrim\" onclick=\"InsightsJs.closeNav()\"></div></header>");
            return sb.ToString();
        }

        private static string FootHtml(PublicData d)
        {
            SiteData s = d.Site;
            StringBuilder sb = new StringBuilder("<footer class=\"in-foot\"><div class=\"in-wrap in-foot-grid\"><div class=\"in-fbrand\"><a class=\"in-logo\" href=\"Home\">" + Logo(42) + "<span><b>" + Ui.Esc(s.Brand) + "</b><small>Attorneys at Law</small></span></a><p>" + Ui.Esc(s.Tagline) + "</p></div>");
            sb.Append("<div><h3>Practice areas</h3><ul>" + string.Concat(d.Areas.Select(a => "<li><a href=\"PracticeArea?k=" + Ui.Esc(a.Key) + "\">" + Ui.Esc(a.Name) + "</a></li>")) + "</ul></div>");
            sb.Append("<div><h3>The firm</h3><ul><li><a href=\"About\">About</a></li><li><a href=\"Attorneys\">Attorneys</a></li><li><a href=\"Insights\">Insights</a></li><li><a href=\"Contact\">Contact</a></li></ul></div>");
            sb.Append("<div><h3>Visit</h3><address><span>" + Ui.Esc(s.Street) + "</span><span>" + Ui.Esc(s.City + ", " + s.Region + " " + s.Zip) + "</span><span><a href=\"tel:" + Ui.Esc(s.Phone) + "\">" + Ui.Esc(s.Phone) + "</a></span><span><a href=\"mailto:" + Ui.Esc(s.Email) + "\">" + Ui.Esc(s.Email) + "</a></span></address></div></div>");
            sb.Append("<div class=\"in-foot-b\"><div class=\"in-wrap\"><span>&copy; " + DateTime.Now.Year + " " + Ui.Esc(s.Legal) + ". " + Ui.Esc(s.Disclaimer) + "</span><a class=\"in-staff\" href=\"AdminLogin\">Staff sign in</a></div></div></footer>");
            return sb.ToString();
        }
    }
}
