using System.Globalization;
using System.Text;
using LawFirm.Core;
using LawFirm.Models;
using SkyNet;

namespace LawFirm.codes
{
    public class Admin : WebPage
    {
        public override async Task OnInitialized()
        {
            HtmlDoc.SetTitle("Dashboard | Staff");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
        }

        private async Task<Ctx?> Guard(ApiResponse response)
        {
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminJs.expired();");
            }
            return c;
        }

        private async Task Shell(ApiResponse response, Ctx c, SiteData site)
        {
            response.SetElementContents("db-brand", Ui.Esc(site.Brand));
            response.SetElementContents("db-menu", await Auth.Menu("db", "Admin", c));
            response.SetElementContents("db-who", Auth.Who("db", c));
        }

        private static string Tile(string href, string number, string label, string note)
        {
            return "<a class=\"db-tile\" href=\"" + href + "\"><b>" + Ui.Esc(number) + "</b><span>" + Ui.Esc(label) + "</span><small>" + Ui.Esc(note) + "</small></a>";
        }

        public async Task<ApiResponse> Load()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Guard(response);
            if (c == null)
            {
                return response;
            }
            SiteData site = await Store.LoadSite();
            await Shell(response, c, site);
            List<InquiryRec> msgs = await Store.Rows<InquiryRec>("messages");
            List<AreaRec> areas = await Store.Rows<AreaRec>("areas");
            List<LawyerRec> lawyers = await Store.Rows<LawyerRec>("attorneys");
            List<ArticleRec> articles = await Store.Rows<ArticleRec>("insights");
            List<MediaItem> media = await Media.Load();
            List<ActivityRec> log = await Store.Rows<ActivityRec>("activity");
            int fresh = msgs.Count(m => m.Status == "New");
            int drafts = articles.Count(a => !a.Published);
            StringBuilder sb = new StringBuilder();
            sb.Append("<div class=\"db-ph\"><div><h1>Dashboard</h1><p>Welcome, " + Ui.Esc(c.U.Name) + ". Here is how " + Ui.Esc(site.Brand) + " looks today.</p></div><a class=\"db-btn db-btn-ghost\" href=\"Home\" target=\"_blank\" rel=\"noopener\">View the website</a></div>");
            sb.Append("<div class=\"db-tiles\">" + Tile("AdminMessages", fresh.ToString(CultureInfo.InvariantCulture), fresh == 1 ? "new request" : "new requests", msgs.Count + " in total") + Tile("AdminAreas", areas.Count(a => a.Show).ToString(CultureInfo.InvariantCulture), "practice areas shown", areas.Count(a => !a.Show) + " hidden") + Tile("AdminAttorneys", lawyers.Count(l => l.Show).ToString(CultureInfo.InvariantCulture), "attorneys shown", lawyers.Count(l => !l.Show) + " hidden") + Tile("AdminInsights", articles.Count(a => a.Published).ToString(CultureInfo.InvariantCulture), "articles published", Ui.Count(drafts, "draft", "drafts")) + Tile("AdminMedia", media.Count.ToString(CultureInfo.InvariantCulture), "files in the library", media.Count(m => m.Kind == "video") + " videos") + "</div>");
            sb.Append("<div class=\"db-cols\"><section class=\"db-panel\"><div class=\"db-sh\"><h2>Latest requests</h2><a class=\"db-link\" href=\"AdminMessages\">All messages</a></div>");
            List<InquiryRec> last = msgs.OrderByDescending(m => m.When).Take(5).ToList();
            if (last.Count == 0)
            {
                sb.Append("<p class=\"db-hint\">No consultation requests yet.</p>");
            }
            else
            {
                sb.Append("<ul class=\"db-feed\">" + string.Concat(last.Select(m => "<li><a href=\"AdminMessages\"><b>" + Ui.Esc(m.Name) + "</b><span class=\"db-pill " + (m.Status == "New" ? "db-pill-new" : m.Status == "Replied" ? "db-pill-go" : m.Status == "Archived" ? "db-pill-off" : string.Empty) + "\">" + Ui.Esc(m.Status) + "</span><small>" + Ui.Esc(Ui.Clip(m.Text, 80)) + (m.Text.Trim().Length > 80 ? "&hellip;" : string.Empty) + " &middot; " + Ui.Esc(Ui.When(m.When)) + "</small></a></li>")) + "</ul>");
            }
            sb.Append("</section><section class=\"db-panel\"><div class=\"db-sh\"><h2>Recent activity</h2><a class=\"db-link\" href=\"AdminActivity\">Activity log</a></div>");
            List<ActivityRec> recent = log.Take(5).ToList();
            if (recent.Count == 0)
            {
                sb.Append("<p class=\"db-hint\">Nothing has been logged yet.</p>");
            }
            else
            {
                StringBuilder feed = new StringBuilder("<ul class=\"db-feed\">");
                foreach (ActivityRec a in recent)
                {
                    feed.Append("<li><div><b>" + Ui.Esc(a.Action) + "</b><span class=\"db-pill " + (a.Ok ? "db-pill-go" : "db-pill-bad") + "\">" + (a.Ok ? "Done" : "Refused") + "</span><small>" + Ui.Esc(a.Target) + " &middot; " + Ui.Esc(await Store.Who(a.UserId)) + " &middot; " + Ui.Esc(Ui.When(a.When)) + "</small></div></li>");
                }
                sb.Append(feed + "</ul>");
            }
            sb.Append("</section></div>");
            sb.Append("<section class=\"db-panel\"><h2>Quick actions</h2><div class=\"db-quick\"><a class=\"db-btn db-btn-ghost\" href=\"AdminPages?pg=home\">Edit the Home page</a><a class=\"db-btn db-btn-ghost\" href=\"AdminInsights\">Write an article</a><a class=\"db-btn db-btn-ghost\" href=\"AdminAttorneys\">Edit attorneys</a><a class=\"db-btn db-btn-ghost\" href=\"AdminMedia\">Upload pictures</a><a class=\"db-btn db-btn-ghost\" href=\"AdminSettings\">Firm details</a></div></section>");
            response.SetElementContents("db-main", sb.ToString());
            return response;
        }
    }
}
