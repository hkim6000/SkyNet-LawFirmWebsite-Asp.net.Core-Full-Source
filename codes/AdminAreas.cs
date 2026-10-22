using System.Globalization;
using System.Text;
using LawFirm.Core;
using LawFirm.Models;
using SkyNet;

namespace LawFirm.codes
{
    public class AdminAreas : WebPage
    {
        private const int Size = 10;

        public override async Task OnInitialized()
        {
            HtmlDoc.SetTitle("Practice areas | Staff");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
        }

        private async Task<Ctx?> Guard(ApiResponse response)
        {
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminAreasJs.expired();");
            }
            return c;
        }

        private async Task Shell(ApiResponse response, Ctx c, SiteData site)
        {
            response.SetElementContents("xp-brand", Ui.Esc(site.Brand));
            response.SetElementContents("xp-menu", await Auth.Menu("xp", "AdminAreas", c));
            response.SetElementContents("xp-who", Auth.Who("xp", c));
        }

        private static string PickItem(MediaItem m)
        {
            string thumb = m.Thumb.Length > 0 ? m.Thumb : m.Path;
            return "<button type=\"button\" class=\"xp-pki\" data-src=\"" + Ui.Esc(m.Path) + "\" data-thumb=\"" + Ui.Esc(thumb) + "\" onclick=\"AdminAreasJs.pkPick(this)\" title=\"" + Ui.Esc(m.Name) + "\"><img src=\"" + Ui.Esc(thumb) + "\" alt=\"\" loading=\"lazy\"><span>" + Ui.Esc(m.Name) + "</span></button>";
        }

        private static string PickGrid(List<MediaItem> media, string term)
        {
            List<MediaItem> rows = media.Where(m => m.Kind == "photo" && (term == string.Empty || Ui.Has(m.Name, term) || Ui.Has(m.Alt, term) || Ui.Has(m.Caption, term))).OrderByDescending(m => m.Added).Take(400).ToList();
            if (rows.Count == 0)
            {
                return "<p class=\"xp-none\">Nothing matches. Upload a photo above, or add photos in the Media library.</p>";
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
            response.SetElementContents("xp-pkgrid", PickGrid(media, Ui.Clip(GetDataValue("pterm"), 40)));
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
                response.ExecuteScript("AdminAreasJs.pkUploaded(false,'Choose a photo first.');");
                return response;
            }
            (int added, List<string> errors) = await Media.AddAll(files.Take(1).ToList(), site.MaxVideoMb, c.U.Name);
            if (added > 0)
            {
                await Store.Log(c.U.Id, "upload media", Ui.Count(added, "file", "files"), true);
            }
            List<MediaItem> media = await Media.Load();
            response.SetElementContents("xp-pkgrid", PickGrid(media, string.Empty));
            response.ExecuteScript("AdminAreasJs.pkUploaded(" + (added > 0 ? "true" : "false") + ",'" + Ui.Js(((added > 0 ? "Photo added to the library. " : string.Empty) + string.Join(" ", errors)).Trim()) + "');");
            return response;
        }

        private static async Task<ApiResponse> Refuse(ApiResponse response, Ctx c, string action, string target, string message)
        {
            await Store.Log(c.U.Id, action, target, false);
            response.ExecuteScript("AdminAreasJs.toast('" + Ui.Js(message) + "');");
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
            List<AreaRec> areas = await Store.Rows<AreaRec>("areas");
            List<LawyerRec> lawyers = await Store.Rows<LawyerRec>("attorneys");
            List<MediaItem> media = await Media.Load();
            response.SetElementContents("xp-main", "<div class=\"xp-ph\"><div><h1>Practice areas</h1><p>The areas of law the firm handles. Each one has its own page on the website. Order here is the order on the website.</p></div><button type=\"button\" class=\"xp-btn\" onclick=\"AdminAreasJs.add()\">Add a practice area</button></div><div class=\"xp-tools\"><input type=\"search\" id=\"xp-q\" placeholder=\"Search practice areas\" maxlength=\"40\" oninput=\"AdminAreasJs.type()\" aria-label=\"Search practice areas\"></div><div id=\"xp-list\">" + ListHtml(areas, lawyers, media, string.Empty, 1) + "</div>");
            return response;
        }

        private static string Term(string? v)
        {
            return Ui.Clip(v, 40);
        }

        private static int PageNo(string? v)
        {
            return int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) && n > 0 ? n : 1;
        }

        private static string ListHtml(List<AreaRec> areas, List<LawyerRec> lawyers, List<MediaItem> media, string term, int page)
        {
            List<AreaRec> rows = areas.Where(a => term == string.Empty || Ui.Has(a.Name, term) || Ui.Has(a.Summary, term)).ToList();
            int pages = Math.Max(1, (rows.Count + Size - 1) / Size);
            page = Math.Min(page, pages);
            if (rows.Count == 0)
            {
                return "<div class=\"xp-panel xp-empty\">" + (areas.Count == 0 ? "No practice areas yet. Add the first one." : "No practice areas match.") + "</div>";
            }
            StringBuilder sb = new StringBuilder("<div class=\"xp-wrapt\"><table class=\"xp-table\"><thead><tr><th>Practice area</th><th>Summary</th><th>Attorneys</th><th>Website</th><th>Order</th></tr></thead><tbody>");
            foreach (AreaRec a in rows.Skip((page - 1) * Size).Take(Size))
            {
                MediaItem? m = media.FirstOrDefault(x => x.Path == a.Media);
                string thumb = m == null ? string.Empty : m.Thumb.Length > 0 ? m.Thumb : m.Path;
                int count = lawyers.Count(l => l.Areas.Contains(a.Key));
                int at = areas.IndexOf(a);
                string key = Ui.Js(a.Key);
                sb.Append("<tr class=\"xp-row\" onclick=\"AdminAreasJs.edit('" + key + "')\"><td class=\"xp-first\"><div class=\"xp-cell-main\">" + (thumb.Length > 0 ? "<img class=\"xp-thumb\" src=\"" + Ui.Esc(thumb) + "\" alt=\"\" loading=\"lazy\">" : "<span class=\"xp-thumb\"></span>") + "<div><b>" + Ui.Esc(a.Name) + "</b><small>" + Ui.Esc(a.Key) + "</small></div></div></td>");
                sb.Append("<td data-label=\"Summary\">" + Ui.Esc(a.Summary) + "</td><td data-label=\"Attorneys\">" + count + "</td><td data-label=\"Website\"><span class=\"xp-pill " + (a.Show ? "xp-pill-go" : "xp-pill-off") + "\">" + (a.Show ? "Shown" : "Hidden") + "</span></td>");
                sb.Append("<td data-label=\"Order\" onclick=\"event.stopPropagation()\">" + (term == string.Empty ? "<span class=\"xp-ctl\"><button type=\"button\" aria-label=\"Move up\"" + (at == 0 ? " disabled" : string.Empty) + " onclick=\"AdminAreasJs.move('" + key + "',-1)\"><svg viewBox=\"0 0 24 24\" width=\"16\" height=\"16\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M6 15l6-6 6 6\"/></svg></button><button type=\"button\" aria-label=\"Move down\"" + (at == areas.Count - 1 ? " disabled" : string.Empty) + " onclick=\"AdminAreasJs.move('" + key + "',1)\"><svg viewBox=\"0 0 24 24\" width=\"16\" height=\"16\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M6 9l6 6 6-6\"/></svg></button></span>" : "<small>Clear the search to reorder</small>") + "</td></tr>");
            }
            sb.Append("</tbody></table></div><div class=\"xp-pager\"><span>Page " + page + " of " + pages + " &middot; " + Ui.Count(rows.Count, "practice area", "practice areas") + "</span><div>");
            sb.Append("<button type=\"button\" class=\"xp-btn xp-btn-ghost xp-btn-sm\"" + (page <= 1 ? " disabled" : string.Empty) + " onclick=\"AdminAreasJs.go(" + (page - 1) + ")\">Previous</button><button type=\"button\" class=\"xp-btn xp-btn-ghost xp-btn-sm\"" + (page >= pages ? " disabled" : string.Empty) + " onclick=\"AdminAreasJs.go(" + (page + 1) + ")\">Next</button></div></div>");
            return sb.ToString();
        }

        private async Task<string> ListNow()
        {
            List<AreaRec> areas = await Store.Rows<AreaRec>("areas");
            List<LawyerRec> lawyers = await Store.Rows<LawyerRec>("attorneys");
            List<MediaItem> media = await Media.Load();
            return ListHtml(areas, lawyers, media, Term(GetDataValue("term")), PageNo(GetDataValue("pg")));
        }

        public async Task<ApiResponse> List()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Guard(response);
            if (c == null)
            {
                return response;
            }
            response.SetElementContents("xp-list", await ListNow());
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
            List<AreaRec> areas = await Store.Rows<AreaRec>("areas");
            List<LawyerRec> lawyers = await Store.Rows<LawyerRec>("attorneys");
            List<MediaItem> media = await Media.Load();
            AreaRec? a = areas.FirstOrDefault(x => x.Key == id);
            if (id.Length > 0 && a == null)
            {
                response.ExecuteScript("AdminAreasJs.toast('That practice area was not found.');");
                return response;
            }
            response.SetElementContents("xp-mbody", FormHtml(a, lawyers, media));
            response.ExecuteScript("AdminAreasJs.opened('" + Ui.Js(a == null ? "New practice area" : a.Name) + "');");
            return response;
        }

        private static string FormHtml(AreaRec? a, List<LawyerRec> lawyers, List<MediaItem> media)
        {
            bool isNew = a == null;
            AreaRec r = a ?? new AreaRec();
            MediaItem? m = media.FirstOrDefault(x => x.Path == r.Media);
            string thumb = m == null ? string.Empty : m.Thumb.Length > 0 ? m.Thumb : m.Path;
            StringBuilder sb = new StringBuilder();
            sb.Append("<form class=\"xp-stack\" id=\"xp-form\" onsubmit=\"return AdminAreasJs.save()\" novalidate><input type=\"hidden\" id=\"xp-f-id\" value=\"" + Ui.Esc(r.Key) + "\">");
            sb.Append("<div class=\"xp-grid2\"><label class=\"xp-field\" for=\"xp-f-name\"><span>Name</span><input type=\"text\" id=\"xp-f-name\" data-k=\"name\" maxlength=\"60\" value=\"" + Ui.Esc(r.Name) + "\"><em class=\"xp-err\" id=\"xp-e-name\"></em></label>");
            sb.Append("<label class=\"xp-field\" for=\"xp-f-key\"><span>Web address name</span><input type=\"text\" id=\"xp-f-key\" data-k=\"key\" maxlength=\"60\" value=\"" + Ui.Esc(r.Key) + "\"" + (isNew ? string.Empty : " readonly") + "><small>" + (isNew ? "Leave empty to make it from the name. Letters, numbers and dashes." : "Cannot be changed, so links keep working.") + "</small><em class=\"xp-err\" id=\"xp-e-key\"></em></label></div>");
            sb.Append("<label class=\"xp-field\" for=\"xp-f-summary\"><span>Summary (one or two sentences, shown on cards)</span><textarea id=\"xp-f-summary\" data-k=\"summary\" rows=\"2\" maxlength=\"200\">" + Ui.Esc(r.Summary) + "</textarea><em class=\"xp-err\" id=\"xp-e-summary\"></em></label>");
            sb.Append("<label class=\"xp-field\" for=\"xp-f-intro\"><span>Overview (separate paragraphs with a blank line)</span><textarea id=\"xp-f-intro\" data-k=\"intro\" rows=\"7\" maxlength=\"3000\">" + Ui.Esc(r.Intro) + "</textarea><em class=\"xp-err\" id=\"xp-e-intro\"></em></label>");
            sb.Append("<label class=\"xp-field\" for=\"xp-f-handles\"><span>What we handle (one item per line, up to 12)</span><textarea id=\"xp-f-handles\" data-k=\"handles\" rows=\"6\" maxlength=\"1600\">" + Ui.Esc(string.Join("\n", r.Handles)) + "</textarea><em class=\"xp-err\" id=\"xp-e-handles\"></em></label>");
            sb.Append("<div class=\"xp-field\"><span>Picture</span><div class=\"xp-pic\" id=\"xp-f-media\" data-k=\"media\" data-src=\"" + Ui.Esc(r.Media) + "\" data-ph=\"Choose a picture\"><button type=\"button\" class=\"xp-picb\" onclick=\"AdminAreasJs.pic(this)\" aria-label=\"Choose a picture\">" + (thumb.Length > 0 ? "<img src=\"" + Ui.Esc(thumb) + "\" alt=\"\">" : "<span>Choose a picture</span>") + "</button><button type=\"button\" class=\"xp-picx\"" + (thumb.Length > 0 ? string.Empty : " hidden") + " onclick=\"AdminAreasJs.picClear(this)\">Remove</button></div><em class=\"xp-err\" id=\"xp-e-media\"></em></div>");
            sb.Append("<div class=\"xp-field\"><span>Attorneys who practice here</span>");
            if (lawyers.Count == 0)
            {
                sb.Append("<small>Add attorneys first, then link them here.</small>");
            }
            else
            {
                sb.Append("<div class=\"xp-checks\">" + string.Concat(lawyers.Select(l => "<label class=\"xp-check\"><input type=\"checkbox\" class=\"xp-lw\" data-v=\"" + Ui.Esc(l.Key) + "\"" + (l.Areas.Contains(r.Key) && !isNew ? " checked" : string.Empty) + "> " + Ui.Esc(l.Name) + "</label>")) + "</div>");
            }
            sb.Append("</div><label class=\"xp-check\"><input type=\"checkbox\" id=\"xp-f-show\" data-k=\"show\"" + (r.Show ? " checked" : string.Empty) + "> Show on the website</label>");
            sb.Append("<div class=\"xp-acts\"><button type=\"submit\" class=\"xp-btn\">Save</button><button type=\"button\" class=\"xp-btn xp-btn-ghost\" onclick=\"AdminAreasJs.closeModal()\">Cancel</button>" + (isNew ? string.Empty : "<button type=\"button\" class=\"xp-btn xp-btn-danger xp-push\" onclick=\"AdminAreasJs.del('" + Ui.Js(r.Key) + "','" + Ui.Js(r.Name) + "')\">Delete</button>") + "</div></form>");
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
            string name = Ui.Clip(GetDataValue("name"), 60);
            string key = Ui.Slug(Ui.Clip(GetDataValue("key"), 60).Length > 0 ? GetDataValue("key") : name);
            string summary = Ui.Clip(GetDataValue("summary"), 200);
            string intro = (GetDataValue("intro") ?? string.Empty).Replace("\r", string.Empty).Trim();
            string media = Ui.Clip(GetDataValue("media"), 200);
            bool show = GetDataValue("show") == "1";
            List<string> handles = (GetDataValue("handles") ?? string.Empty).Replace("\r", string.Empty).Split('\n').Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
            List<string> picked = (GetDataValue("lawyers") ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Distinct().ToList();
            List<AreaRec> areas = await Store.Rows<AreaRec>("areas");
            List<LawyerRec> lawyers = await Store.Rows<LawyerRec>("attorneys");
            List<MediaItem> library = await Media.Load();
            AreaRec? existing = areas.FirstOrDefault(x => x.Key == id);
            bool isNew = id.Length == 0;
            if (!isNew && existing == null)
            {
                response.ExecuteScript("AdminAreasJs.toast('That practice area was not found.');");
                return response;
            }
            if (!isNew)
            {
                key = id;
            }
            string eName = name.Length < 2 ? "Please enter the name (at least 2 characters)." : string.Empty;
            string eKey = key.Length < 2 ? "Use at least 2 letters or numbers." : isNew && areas.Any(x => x.Key == key) ? "Another practice area already uses this web address name." : string.Empty;
            string eSum = summary.Length < 10 ? "Please write a short summary (at least 10 characters)." : string.Empty;
            string eIntro = intro.Length > 3000 ? "Keep the overview under 3,000 characters." : string.Empty;
            string eHandles = handles.Count > 12 ? "Up to 12 lines, please." : handles.Any(h => h.Length > 120) ? "Keep each line under 120 characters." : string.Empty;
            string eMedia = media.Length > 0 && !library.Any(m => m.Path == media && m.Kind == "photo") ? "That picture is not in the media library. Choose it again." : string.Empty;
            response.SetElementContents("xp-e-name", eName);
            response.SetElementContents("xp-e-key", eKey);
            response.SetElementContents("xp-e-summary", eSum);
            response.SetElementContents("xp-e-intro", eIntro);
            response.SetElementContents("xp-e-handles", eHandles);
            response.SetElementContents("xp-e-media", eMedia);
            if (eName + eKey + eSum + eIntro + eHandles + eMedia != string.Empty)
            {
                return response;
            }
            using (await Store.Hold())
            {
                areas = await Store.Rows<AreaRec>("areas");
                lawyers = await Store.Rows<LawyerRec>("attorneys");
                AreaRec? a = areas.FirstOrDefault(x => x.Key == key);
                if (isNew)
                {
                    if (a != null)
                    {
                        response.SetElementContents("xp-e-key", "Another practice area already uses this web address name.");
                        return response;
                    }
                    a = new AreaRec { Key = key };
                    areas.Add(a);
                }
                if (a == null)
                {
                    response.ExecuteScript("AdminAreasJs.toast('That practice area was not found.');");
                    return response;
                }
                a.Name = name;
                a.Summary = summary;
                a.Intro = intro;
                a.Handles = handles;
                a.Media = media;
                a.Show = show;
                foreach (LawyerRec l in lawyers)
                {
                    bool want = picked.Contains(l.Key);
                    bool has = l.Areas.Contains(key);
                    if (want && !has)
                    {
                        l.Areas.Add(key);
                    }
                    else if (!want && has)
                    {
                        l.Areas.Remove(key);
                    }
                }
                await Store.SaveRows("areas", areas);
                await Store.SaveRows("attorneys", lawyers);
            }
            await Store.Log(c.U.Id, isNew ? "add practice area" : "edit practice area", name, true);
            response.SetElementContents("xp-list", await ListNow());
            response.ExecuteScript("AdminAreasJs.saved();");
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
            string name = string.Empty;
            using (await Store.Hold())
            {
                List<AreaRec> areas = await Store.Rows<AreaRec>("areas");
                AreaRec? a = areas.FirstOrDefault(x => x.Key == id);
                if (a != null)
                {
                    name = a.Name;
                    areas.Remove(a);
                    List<LawyerRec> lawyers = await Store.Rows<LawyerRec>("attorneys");
                    foreach (LawyerRec l in lawyers)
                    {
                        l.Areas.Remove(id);
                    }
                    await Store.SaveRows("areas", areas);
                    await Store.SaveRows("attorneys", lawyers);
                }
            }
            if (name.Length == 0)
            {
                response.ExecuteScript("AdminAreasJs.toast('That practice area was not found.');");
                return response;
            }
            await Store.Log(c.U.Id, "delete practice area", name, true);
            response.SetElementContents("xp-list", await ListNow());
            response.ExecuteScript("AdminAreasJs.deleted();");
            return response;
        }

        public async Task<ApiResponse> Move()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Guard(response);
            if (c == null)
            {
                return response;
            }
            string id = Ui.Clip(GetDataValue("id"), 60);
            int dir = GetDataValue("dir") == "-1" ? -1 : 1;
            string name = string.Empty;
            using (await Store.Hold())
            {
                List<AreaRec> areas = await Store.Rows<AreaRec>("areas");
                int at = areas.FindIndex(x => x.Key == id);
                int to = at + dir;
                if (at >= 0 && to >= 0 && to < areas.Count)
                {
                    name = areas[at].Name;
                    (areas[at], areas[to]) = (areas[to], areas[at]);
                    await Store.SaveRows("areas", areas);
                }
            }
            if (name.Length > 0)
            {
                await Store.Log(c.U.Id, "reorder practice areas", name, true);
            }
            response.SetElementContents("xp-list", await ListNow());
            return response;
        }
    }
}
