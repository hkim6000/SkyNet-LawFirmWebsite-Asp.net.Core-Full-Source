using System.Globalization;
using System.Text;
using LawFirm.Core;
using LawFirm.Models;
using SkyNet;

namespace LawFirm.codes
{
    public class AdminInsights : WebPage
    {
        private const int Size = 10;

        public override async Task OnInitialized()
        {
            HtmlDoc.SetTitle("Insights | Staff");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
        }

        private async Task<Ctx?> Guard(ApiResponse response)
        {
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminInsightsJs.expired();");
            }
            return c;
        }

        private async Task Shell(ApiResponse response, Ctx c, SiteData site)
        {
            response.SetElementContents("xi-brand", Ui.Esc(site.Brand));
            response.SetElementContents("xi-menu", await Auth.Menu("xi", "AdminInsights", c));
            response.SetElementContents("xi-who", Auth.Who("xi", c));
        }

        private static string PickItem(MediaItem m)
        {
            string thumb = m.Thumb.Length > 0 ? m.Thumb : m.Path;
            return "<button type=\"button\" class=\"xi-pki\" data-src=\"" + Ui.Esc(m.Path) + "\" data-thumb=\"" + Ui.Esc(thumb) + "\" onclick=\"AdminInsightsJs.pkPick(this)\" title=\"" + Ui.Esc(m.Name) + "\"><img src=\"" + Ui.Esc(thumb) + "\" alt=\"\" loading=\"lazy\"><span>" + Ui.Esc(m.Name) + "</span></button>";
        }

        private static string PickGrid(List<MediaItem> media, string term)
        {
            List<MediaItem> rows = media.Where(m => m.Kind == "photo" && (term == string.Empty || Ui.Has(m.Name, term) || Ui.Has(m.Alt, term) || Ui.Has(m.Caption, term))).OrderByDescending(m => m.Added).Take(400).ToList();
            if (rows.Count == 0)
            {
                return "<p class=\"xi-none\">Nothing matches. Upload a photo above, or add photos in the Media library.</p>";
            }
            return string.Concat(rows.Select(PickItem));
        }

        public async Task<ApiResponse> MediaPick()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Guard(response);
            if (c == null)
            {
                return response;
            }
            List<MediaItem> media = await Media.Load();
            response.SetElementContents("xi-pkgrid", PickGrid(media, Ui.Clip(GetDataValue("pterm"), 40)));
            return response;
        }

        public async Task<ApiResponse> MediaUp()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Guard(response);
            if (c == null)
            {
                return response;
            }
            SiteData site = await Store.LoadSite();
            List<Incoming> files = await Media.Received();
            if (files.Count == 0)
            {
                response.ExecuteScript("AdminInsightsJs.pkUploaded(false,'Choose a photo first.');");
                return response;
            }
            (int added, List<string> errors) = await Media.AddAll(files.Take(1).ToList(), site.MaxVideoMb, c.U.Name);
            if (added > 0)
            {
                await Store.Log(c.U.Id, "upload media", Ui.Count(added, "file", "files"), true);
            }
            List<MediaItem> media = await Media.Load();
            response.SetElementContents("xi-pkgrid", PickGrid(media, string.Empty));
            response.ExecuteScript("AdminInsightsJs.pkUploaded(" + (added > 0 ? "true" : "false") + ",'" + Ui.Js(((added > 0 ? "Photo added to the library. " : string.Empty) + string.Join(" ", errors)).Trim()) + "');");
            return response;
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
            List<ArticleRec> articles = await Store.Rows<ArticleRec>("insights");
            List<LawyerRec> lawyers = await Store.Rows<LawyerRec>("attorneys");
            List<MediaItem> media = await Media.Load();
            string topics = "<option value=\"\">All topics</option>" + string.Concat(site.Topics.Select(t => "<option value=\"" + Ui.Esc(t.Key) + "\">" + Ui.Esc(t.Name) + "</option>"));
            response.SetElementContents("xi-main", "<div class=\"xi-ph\"><div><h1>Insights</h1><p>Articles for the Insights page. Drafts are only visible here until you publish them.</p></div><button type=\"button\" class=\"xi-btn\" onclick=\"AdminInsightsJs.add()\">Write an article</button></div><div class=\"xi-tools\"><input type=\"search\" id=\"xi-q\" placeholder=\"Search articles\" maxlength=\"40\" oninput=\"AdminInsightsJs.type()\" aria-label=\"Search articles\"><select id=\"xi-ft\" aria-label=\"Topic\" onchange=\"AdminInsightsJs.list(true)\">" + topics + "</select><select id=\"xi-fs\" aria-label=\"Status\" onchange=\"AdminInsightsJs.list(true)\"><option value=\"\">Published and drafts</option><option value=\"pub\">Published</option><option value=\"draft\">Drafts</option></select></div><div id=\"xi-list\">" + ListHtml(site, articles, lawyers, media, string.Empty, string.Empty, string.Empty, 1) + "</div>");
            return response;
        }

        private static int PageNo(string? v)
        {
            return int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) && n > 0 ? n : 1;
        }

        private static string ListHtml(SiteData site, List<ArticleRec> articles, List<LawyerRec> lawyers, List<MediaItem> media, string term, string topic, string status, int page)
        {
            List<ArticleRec> rows = articles.Where(a => (term == string.Empty || Ui.Has(a.Title, term) || Ui.Has(a.Summary, term)) && (topic == string.Empty || a.Topic == topic) && (status == string.Empty || (status == "pub" ? a.Published : !a.Published))).OrderByDescending(a => a.Date).ToList();
            int pages = Math.Max(1, (rows.Count + Size - 1) / Size);
            page = Math.Min(page, pages);
            if (rows.Count == 0)
            {
                return "<div class=\"xi-panel xi-empty\">" + (articles.Count == 0 ? "No articles yet. Write the first one." : "No articles match.") + "</div>";
            }
            StringBuilder sb = new StringBuilder("<div class=\"xi-wrapt\"><table class=\"xi-table\"><thead><tr><th>Article</th><th>Topic</th><th>Author</th><th>Date</th><th>Status</th></tr></thead><tbody>");
            foreach (ArticleRec a in rows.Skip((page - 1) * Size).Take(Size))
            {
                MediaItem? m = media.FirstOrDefault(x => x.Path == a.Media);
                string thumb = m == null ? string.Empty : m.Thumb.Length > 0 ? m.Thumb : m.Path;
                string author = lawyers.FirstOrDefault(l => l.Key == a.Author)?.Name ?? string.Empty;
                sb.Append("<tr class=\"xi-row\" onclick=\"AdminInsightsJs.edit('" + Ui.Js(a.Key) + "')\"><td class=\"xi-first\"><div class=\"xi-cell-main\">" + (thumb.Length > 0 ? "<img class=\"xi-thumb\" src=\"" + Ui.Esc(thumb) + "\" alt=\"\" loading=\"lazy\">" : "<span class=\"xi-thumb\"></span>") + "<div><b>" + Ui.Esc(a.Title) + "</b><small>" + Ui.Esc(a.Key) + "</small></div></div></td>");
                sb.Append("<td data-label=\"Topic\">" + Ui.Esc(site.Topics.FirstOrDefault(t => t.Key == a.Topic)?.Name ?? a.Topic) + "</td><td data-label=\"Author\">" + Ui.Esc(author) + "</td><td data-label=\"Date\">" + Ui.Esc(a.Date.ToString("MMM d, yyyy", CultureInfo.InvariantCulture)) + "</td><td data-label=\"Status\"><span class=\"xi-pill " + (a.Published ? "xi-pill-go" : "xi-pill-wait") + "\">" + (a.Published ? "Published" : "Draft") + "</span></td></tr>");
            }
            sb.Append("</tbody></table></div><div class=\"xi-pager\"><span>Page " + page + " of " + pages + " &middot; " + Ui.Count(rows.Count, "article", "articles") + "</span><div>");
            sb.Append("<button type=\"button\" class=\"xi-btn xi-btn-ghost xi-btn-sm\"" + (page <= 1 ? " disabled" : string.Empty) + " onclick=\"AdminInsightsJs.go(" + (page - 1) + ")\">Previous</button><button type=\"button\" class=\"xi-btn xi-btn-ghost xi-btn-sm\"" + (page >= pages ? " disabled" : string.Empty) + " onclick=\"AdminInsightsJs.go(" + (page + 1) + ")\">Next</button></div></div>");
            return sb.ToString();
        }

        private async Task<string> ListNow()
        {
            SiteData site = await Store.LoadSite();
            List<ArticleRec> articles = await Store.Rows<ArticleRec>("insights");
            List<LawyerRec> lawyers = await Store.Rows<LawyerRec>("attorneys");
            List<MediaItem> media = await Media.Load();
            string topic = (GetDataValue("tp") ?? string.Empty).Trim();
            string status = (GetDataValue("st") ?? string.Empty).Trim();
            return ListHtml(site, articles, lawyers, media, Ui.Clip(GetDataValue("term"), 40), site.Topics.Any(t => t.Key == topic) ? topic : string.Empty, status is "pub" or "draft" ? status : string.Empty, PageNo(GetDataValue("pg")));
        }

        public async Task<ApiResponse> List()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Guard(response);
            if (c == null)
            {
                return response;
            }
            response.SetElementContents("xi-list", await ListNow());
            return response;
        }

        public async Task<ApiResponse> Edit()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Guard(response);
            if (c == null)
            {
                return response;
            }
            string id = Ui.Clip(GetDataValue("id"), 60);
            SiteData site = await Store.LoadSite();
            List<ArticleRec> articles = await Store.Rows<ArticleRec>("insights");
            List<LawyerRec> lawyers = await Store.Rows<LawyerRec>("attorneys");
            List<MediaItem> media = await Media.Load();
            ArticleRec? a = articles.FirstOrDefault(x => x.Key == id);
            if (id.Length > 0 && a == null)
            {
                response.ExecuteScript("AdminInsightsJs.toast('That article was not found.');");
                return response;
            }
            response.SetElementContents("xi-mbody", FormHtml(site, a, lawyers, media));
            response.ExecuteScript("AdminInsightsJs.opened('" + Ui.Js(a == null ? "New article" : a.Title) + "');");
            return response;
        }

        private static string FormHtml(SiteData site, ArticleRec? a, List<LawyerRec> lawyers, List<MediaItem> media)
        {
            bool isNew = a == null;
            ArticleRec r = a ?? new ArticleRec { Topic = site.Topics.FirstOrDefault()?.Key ?? string.Empty, Date = DateTime.Today, Published = false };
            MediaItem? m = media.FirstOrDefault(x => x.Path == r.Media);
            string thumb = m == null ? string.Empty : m.Thumb.Length > 0 ? m.Thumb : m.Path;
            StringBuilder sb = new StringBuilder();
            sb.Append("<form class=\"xi-stack\" id=\"xi-form\" onsubmit=\"return AdminInsightsJs.save()\" novalidate><input type=\"hidden\" id=\"xi-f-id\" value=\"" + Ui.Esc(r.Key) + "\">");
            sb.Append("<label class=\"xi-field\" for=\"xi-f-title\"><span>Title</span><input type=\"text\" id=\"xi-f-title\" data-k=\"title\" maxlength=\"120\" value=\"" + Ui.Esc(r.Title) + "\"><em class=\"xi-err\" id=\"xi-e-title\"></em></label>");
            sb.Append("<div class=\"xi-grid2\"><label class=\"xi-field\" for=\"xi-f-topic\"><span>Topic</span><select id=\"xi-f-topic\" data-k=\"topic\">" + string.Concat(site.Topics.Select(t => "<option value=\"" + Ui.Esc(t.Key) + "\"" + (t.Key == r.Topic ? " selected" : string.Empty) + ">" + Ui.Esc(t.Name) + "</option>")) + "</select><em class=\"xi-err\" id=\"xi-e-topic\"></em></label>");
            sb.Append("<label class=\"xi-field\" for=\"xi-f-key\"><span>Web address name</span><input type=\"text\" id=\"xi-f-key\" data-k=\"key\" maxlength=\"60\" value=\"" + Ui.Esc(r.Key) + "\"" + (isNew ? string.Empty : " readonly") + "><small>" + (isNew ? "Leave empty to make it from the title." : "Cannot be changed, so links keep working.") + "</small><em class=\"xi-err\" id=\"xi-e-key\"></em></label></div>");
            sb.Append("<div class=\"xi-grid2\"><label class=\"xi-field\" for=\"xi-f-author\"><span>Author</span><select id=\"xi-f-author\" data-k=\"author\"><option value=\"\">No author shown</option>" + string.Concat(lawyers.Select(l => "<option value=\"" + Ui.Esc(l.Key) + "\"" + (l.Key == r.Author ? " selected" : string.Empty) + ">" + Ui.Esc(l.Name) + "</option>")) + "</select></label>");
            sb.Append("<label class=\"xi-field\" for=\"xi-f-date\"><span>Date</span><input type=\"date\" id=\"xi-f-date\" data-k=\"date\" value=\"" + r.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "\"><em class=\"xi-err\" id=\"xi-e-date\"></em></label></div>");
            sb.Append("<label class=\"xi-field\" for=\"xi-f-summary\"><span>Summary (shown in the list)</span><textarea id=\"xi-f-summary\" data-k=\"summary\" rows=\"2\" maxlength=\"240\">" + Ui.Esc(r.Summary) + "</textarea><em class=\"xi-err\" id=\"xi-e-summary\"></em></label>");
            sb.Append("<label class=\"xi-field\" for=\"xi-f-body\"><span>Article (separate paragraphs with a blank line; start a line with ## for a subheading)</span><textarea id=\"xi-f-body\" data-k=\"body\" rows=\"14\" maxlength=\"12000\">" + Ui.Esc(r.Body) + "</textarea><em class=\"xi-err\" id=\"xi-e-body\"></em></label>");
            sb.Append("<div class=\"xi-field\"><span>Picture</span><div class=\"xi-pic\" id=\"xi-f-media\" data-k=\"media\" data-src=\"" + Ui.Esc(r.Media) + "\" data-ph=\"Choose a picture\"><button type=\"button\" class=\"xi-picb\" onclick=\"AdminInsightsJs.pic(this)\" aria-label=\"Choose a picture\">" + (thumb.Length > 0 ? "<img src=\"" + Ui.Esc(thumb) + "\" alt=\"\">" : "<span>Choose a picture</span>") + "</button><button type=\"button\" class=\"xi-picx\"" + (thumb.Length > 0 ? string.Empty : " hidden") + " onclick=\"AdminInsightsJs.picClear(this)\">Remove</button></div><em class=\"xi-err\" id=\"xi-e-media\"></em></div>");
            sb.Append("<label class=\"xi-check\"><input type=\"checkbox\" id=\"xi-f-pub\" data-k=\"pub\"" + (r.Published ? " checked" : string.Empty) + "> Published (visible on the website)</label>");
            sb.Append("<div class=\"xi-acts\"><button type=\"submit\" class=\"xi-btn\">Save</button><button type=\"button\" class=\"xi-btn xi-btn-ghost\" onclick=\"AdminInsightsJs.closeModal()\">Cancel</button>" + (isNew ? string.Empty : "<button type=\"button\" class=\"xi-btn xi-btn-danger xi-push\" onclick=\"AdminInsightsJs.del('" + Ui.Js(r.Key) + "','" + Ui.Js(r.Title) + "')\">Delete</button>") + "</div></form>");
            return sb.ToString();
        }

        public async Task<ApiResponse> Save()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Guard(response);
            if (c == null)
            {
                return response;
            }
            string id = Ui.Clip(GetDataValue("id"), 60);
            string title = Ui.Clip(GetDataValue("title"), 120);
            string key = Ui.Slug(Ui.Clip(GetDataValue("key"), 60).Length > 0 ? GetDataValue("key") : title);
            string topic = (GetDataValue("topic") ?? string.Empty).Trim();
            string author = (GetDataValue("author") ?? string.Empty).Trim();
            string summary = Ui.Clip(GetDataValue("summary"), 240);
            string body = (GetDataValue("body") ?? string.Empty).Replace("\r", string.Empty).Trim();
            string media = Ui.Clip(GetDataValue("media"), 200);
            bool pub = GetDataValue("pub") == "1";
            bool dateOk = DateTime.TryParseExact((GetDataValue("date") ?? string.Empty).Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date);
            SiteData site = await Store.LoadSite();
            List<ArticleRec> articles = await Store.Rows<ArticleRec>("insights");
            List<LawyerRec> lawyers = await Store.Rows<LawyerRec>("attorneys");
            List<MediaItem> library = await Media.Load();
            bool isNew = id.Length == 0;
            if (!isNew && !articles.Any(x => x.Key == id))
            {
                response.ExecuteScript("AdminInsightsJs.toast('That article was not found.');");
                return response;
            }
            if (!isNew)
            {
                key = id;
            }
            string eTitle = title.Length < 5 ? "Please enter a title (at least 5 characters)." : string.Empty;
            string eKey = key.Length < 2 ? "Use at least 2 letters or numbers." : isNew && articles.Any(x => x.Key == key) ? "Another article already uses this web address name." : string.Empty;
            string eTopic = site.Topics.Any(t => t.Key == topic) ? string.Empty : "Please choose a topic.";
            string eDate = !dateOk ? "Please choose a date." : date.Date > DateTime.Today ? "Choose today or an earlier date." : date.Year < 2000 ? "Please choose a recent date." : string.Empty;
            string eSum = summary.Length < 20 ? "Please write a summary (at least 20 characters)." : string.Empty;
            string eBody = body.Length < 100 ? "Please write the article (at least 100 characters)." : body.Length > 12000 ? "Keep the article under 12,000 characters." : string.Empty;
            string eMedia = media.Length > 0 && !library.Any(m => m.Path == media && m.Kind == "photo") ? "That picture is not in the media library. Choose it again." : string.Empty;
            if (author.Length > 0 && !lawyers.Any(l => l.Key == author))
            {
                author = string.Empty;
            }
            response.SetElementContents("xi-e-title", eTitle);
            response.SetElementContents("xi-e-key", eKey);
            response.SetElementContents("xi-e-topic", eTopic);
            response.SetElementContents("xi-e-date", eDate);
            response.SetElementContents("xi-e-summary", eSum);
            response.SetElementContents("xi-e-body", eBody);
            response.SetElementContents("xi-e-media", eMedia);
            if (eTitle + eKey + eTopic + eDate + eSum + eBody + eMedia != string.Empty)
            {
                return response;
            }
            using (await Store.Hold())
            {
                articles = await Store.Rows<ArticleRec>("insights");
                ArticleRec? a = articles.FirstOrDefault(x => x.Key == key);
                if (isNew)
                {
                    if (a != null)
                    {
                        response.SetElementContents("xi-e-key", "Another article already uses this web address name.");
                        return response;
                    }
                    a = new ArticleRec { Key = key };
                    articles.Add(a);
                }
                if (a == null)
                {
                    response.ExecuteScript("AdminInsightsJs.toast('That article was not found.');");
                    return response;
                }
                a.Title = title;
                a.Topic = topic;
                a.Author = author;
                a.Summary = summary;
                a.Body = body;
                a.Media = media;
                a.Date = date.Date;
                a.Published = pub;
                await Store.SaveRows("insights", articles);
            }
            await Store.Log(c.U.Id, isNew ? "add article" : "edit article", title, true);
            response.SetElementContents("xi-list", await ListNow());
            response.ExecuteScript("AdminInsightsJs.saved();");
            return response;
        }

        public async Task<ApiResponse> Delete()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Guard(response);
            if (c == null)
            {
                return response;
            }
            string id = Ui.Clip(GetDataValue("id"), 60);
            string title = string.Empty;
            using (await Store.Hold())
            {
                List<ArticleRec> articles = await Store.Rows<ArticleRec>("insights");
                ArticleRec? a = articles.FirstOrDefault(x => x.Key == id);
                if (a != null)
                {
                    title = a.Title;
                    articles.Remove(a);
                    await Store.SaveRows("insights", articles);
                }
            }
            if (title.Length == 0)
            {
                response.ExecuteScript("AdminInsightsJs.toast('That article was not found.');");
                return response;
            }
            await Store.Log(c.U.Id, "delete article", title, true);
            response.SetElementContents("xi-list", await ListNow());
            response.ExecuteScript("AdminInsightsJs.deleted();");
            return response;
        }
    }
}
