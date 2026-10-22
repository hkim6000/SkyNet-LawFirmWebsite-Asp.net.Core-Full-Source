using System.Globalization;
using System.Text;
using LawFirm.Core;
using LawFirm.Models;
using SkyNet;

namespace LawFirm.codes
{
    public class AdminAttorneys : WebPage
    {
        private const int Size = 10;
        private static readonly string[] Roles = { "Founding Partner", "Managing Partner", "Partner", "Senior Associate", "Associate", "Of Counsel" };

        public override async Task OnInitialized()
        {
            HtmlDoc.SetTitle("Attorneys | Staff");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
        }

        private async Task<Ctx?> Guard(ApiResponse response)
        {
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminAttorneysJs.expired();");
            }
            return c;
        }

        private async Task Shell(ApiResponse response, Ctx c, SiteData site)
        {
            response.SetElementContents("xa-brand", Ui.Esc(site.Brand));
            response.SetElementContents("xa-menu", await Auth.Menu("xa", "AdminAttorneys", c));
            response.SetElementContents("xa-who", Auth.Who("xa", c));
        }

        private static string PickItem(MediaItem m)
        {
            string thumb = m.Thumb.Length > 0 ? m.Thumb : m.Path;
            return "<button type=\"button\" class=\"xa-pki\" data-src=\"" + Ui.Esc(m.Path) + "\" data-thumb=\"" + Ui.Esc(thumb) + "\" onclick=\"AdminAttorneysJs.pkPick(this)\" title=\"" + Ui.Esc(m.Name) + "\"><img src=\"" + Ui.Esc(thumb) + "\" alt=\"\" loading=\"lazy\"><span>" + Ui.Esc(m.Name) + "</span></button>";
        }

        private static string PickGrid(List<MediaItem> media, string term)
        {
            List<MediaItem> rows = media.Where(m => m.Kind == "photo" && (term == string.Empty || Ui.Has(m.Name, term) || Ui.Has(m.Alt, term) || Ui.Has(m.Caption, term))).OrderByDescending(m => m.Added).Take(400).ToList();
            if (rows.Count == 0)
            {
                return "<p class=\"xa-none\">Nothing matches. Upload a photo above, or add photos in the Media library.</p>";
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
            response.SetElementContents("xa-pkgrid", PickGrid(media, Ui.Clip(GetDataValue("pterm"), 40)));
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
                response.ExecuteScript("AdminAttorneysJs.pkUploaded(false,'Choose a photo first.');");
                return response;
            }
            (int added, List<string> errors) = await Media.AddAll(files.Take(1).ToList(), site.MaxVideoMb, c.U.Name);
            if (added > 0)
            {
                await Store.Log(c.U.Id, "upload media", Ui.Count(added, "file", "files"), true);
            }
            List<MediaItem> media = await Media.Load();
            response.SetElementContents("xa-pkgrid", PickGrid(media, string.Empty));
            response.ExecuteScript("AdminAttorneysJs.pkUploaded(" + (added > 0 ? "true" : "false") + ",'" + Ui.Js(((added > 0 ? "Photo added to the library. " : string.Empty) + string.Join(" ", errors)).Trim()) + "');");
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
            List<LawyerRec> lawyers = await Store.Rows<LawyerRec>("attorneys");
            List<AreaRec> areas = await Store.Rows<AreaRec>("areas");
            List<MediaItem> media = await Media.Load();
            response.SetElementContents("xa-main", "<div class=\"xa-ph\"><div><h1>Attorneys</h1><p>The people shown on the Attorneys page, each with a profile page. Order here is the order on the website.</p></div><button type=\"button\" class=\"xa-btn\" onclick=\"AdminAttorneysJs.add()\">Add an attorney</button></div><div class=\"xa-tools\"><input type=\"search\" id=\"xa-q\" placeholder=\"Search by name, role or practice area\" maxlength=\"40\" oninput=\"AdminAttorneysJs.type()\" aria-label=\"Search attorneys\"></div><div id=\"xa-list\">" + ListHtml(lawyers, areas, media, string.Empty, 1) + "</div>");
            return response;
        }

        private static int PageNo(string? v)
        {
            return int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) && n > 0 ? n : 1;
        }

        private static string AreaNames(LawyerRec l, List<AreaRec> areas)
        {
            return string.Join(", ", l.Areas.Select(k => areas.FirstOrDefault(a => a.Key == k)?.Name ?? k));
        }

        private static string ListHtml(List<LawyerRec> lawyers, List<AreaRec> areas, List<MediaItem> media, string term, int page)
        {
            List<LawyerRec> rows = lawyers.Where(l => term == string.Empty || Ui.Has(l.Name, term) || Ui.Has(l.Role, term) || Ui.Has(AreaNames(l, areas), term)).ToList();
            int pages = Math.Max(1, (rows.Count + Size - 1) / Size);
            page = Math.Min(page, pages);
            if (rows.Count == 0)
            {
                return "<div class=\"xa-panel xa-empty\">" + (lawyers.Count == 0 ? "No attorneys yet. Add the first one." : "No attorneys match.") + "</div>";
            }
            StringBuilder sb = new StringBuilder("<div class=\"xa-wrapt\"><table class=\"xa-table\"><thead><tr><th>Attorney</th><th>Practice areas</th><th>Bar admissions</th><th>Website</th><th>Order</th></tr></thead><tbody>");
            foreach (LawyerRec l in rows.Skip((page - 1) * Size).Take(Size))
            {
                MediaItem? m = media.FirstOrDefault(x => x.Path == l.Photo);
                string thumb = m == null ? string.Empty : m.Thumb.Length > 0 ? m.Thumb : m.Path;
                int at = lawyers.IndexOf(l);
                string key = Ui.Js(l.Key);
                sb.Append("<tr class=\"xa-row\" onclick=\"AdminAttorneysJs.edit('" + key + "')\"><td class=\"xa-first\"><div class=\"xa-cell-main\">" + (thumb.Length > 0 ? "<img class=\"xa-thumb xa-thumb-round\" src=\"" + Ui.Esc(thumb) + "\" alt=\"\" loading=\"lazy\">" : "<span class=\"xa-thumb xa-thumb-round\"></span>") + "<div><b>" + Ui.Esc(l.Name) + "</b><small>" + Ui.Esc(l.Role) + "</small></div></div></td>");
                sb.Append("<td data-label=\"Practice areas\">" + Ui.Esc(AreaNames(l, areas)) + "</td><td data-label=\"Bar admissions\">" + string.Join(", ", l.Bars.Select(b => Ui.Esc(b.State))) + "</td><td data-label=\"Website\"><span class=\"xa-pill " + (l.Show ? "xa-pill-go" : "xa-pill-off") + "\">" + (l.Show ? "Shown" : "Hidden") + "</span></td>");
                sb.Append("<td data-label=\"Order\" onclick=\"event.stopPropagation()\">" + (term == string.Empty ? "<span class=\"xa-ctl\"><button type=\"button\" aria-label=\"Move up\"" + (at == 0 ? " disabled" : string.Empty) + " onclick=\"AdminAttorneysJs.move('" + key + "',-1)\"><svg viewBox=\"0 0 24 24\" width=\"16\" height=\"16\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M6 15l6-6 6 6\"/></svg></button><button type=\"button\" aria-label=\"Move down\"" + (at == lawyers.Count - 1 ? " disabled" : string.Empty) + " onclick=\"AdminAttorneysJs.move('" + key + "',1)\"><svg viewBox=\"0 0 24 24\" width=\"16\" height=\"16\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M6 9l6 6 6-6\"/></svg></button></span>" : "<small>Clear the search to reorder</small>") + "</td></tr>");
            }
            sb.Append("</tbody></table></div><div class=\"xa-pager\"><span>Page " + page + " of " + pages + " &middot; " + Ui.Count(rows.Count, "attorney", "attorneys") + "</span><div>");
            sb.Append("<button type=\"button\" class=\"xa-btn xa-btn-ghost xa-btn-sm\"" + (page <= 1 ? " disabled" : string.Empty) + " onclick=\"AdminAttorneysJs.go(" + (page - 1) + ")\">Previous</button><button type=\"button\" class=\"xa-btn xa-btn-ghost xa-btn-sm\"" + (page >= pages ? " disabled" : string.Empty) + " onclick=\"AdminAttorneysJs.go(" + (page + 1) + ")\">Next</button></div></div>");
            return sb.ToString();
        }

        private async Task<string> ListNow()
        {
            List<LawyerRec> lawyers = await Store.Rows<LawyerRec>("attorneys");
            List<AreaRec> areas = await Store.Rows<AreaRec>("areas");
            List<MediaItem> media = await Media.Load();
            return ListHtml(lawyers, areas, media, Ui.Clip(GetDataValue("term"), 40), PageNo(GetDataValue("pg")));
        }

        public async Task<ApiResponse> List()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Guard(response);
            if (c == null)
            {
                return response;
            }
            response.SetElementContents("xa-list", await ListNow());
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
            List<LawyerRec> lawyers = await Store.Rows<LawyerRec>("attorneys");
            List<AreaRec> areas = await Store.Rows<AreaRec>("areas");
            List<MediaItem> media = await Media.Load();
            LawyerRec? l = lawyers.FirstOrDefault(x => x.Key == id);
            if (id.Length > 0 && l == null)
            {
                response.ExecuteScript("AdminAttorneysJs.toast('That attorney was not found.');");
                return response;
            }
            response.SetElementContents("xa-mbody", FormHtml(l, areas, media));
            response.ExecuteScript("AdminAttorneysJs.opened('" + Ui.Js(l == null ? "New attorney" : l.Name) + "');");
            return response;
        }

        private static string FormHtml(LawyerRec? l, List<AreaRec> areas, List<MediaItem> media)
        {
            bool isNew = l == null;
            LawyerRec r = l ?? new LawyerRec { Role = "Associate" };
            MediaItem? m = media.FirstOrDefault(x => x.Path == r.Photo);
            string thumb = m == null ? string.Empty : m.Thumb.Length > 0 ? m.Thumb : m.Path;
            StringBuilder sb = new StringBuilder();
            sb.Append("<form class=\"xa-stack\" id=\"xa-form\" onsubmit=\"return AdminAttorneysJs.save()\" novalidate><input type=\"hidden\" id=\"xa-f-id\" value=\"" + Ui.Esc(r.Key) + "\">");
            sb.Append("<div class=\"xa-grid2\"><label class=\"xa-field\" for=\"xa-f-name\"><span>Full name</span><input type=\"text\" id=\"xa-f-name\" data-k=\"name\" maxlength=\"60\" value=\"" + Ui.Esc(r.Name) + "\"><em class=\"xa-err\" id=\"xa-e-name\"></em></label>");
            sb.Append("<label class=\"xa-field\" for=\"xa-f-key\"><span>Web address name</span><input type=\"text\" id=\"xa-f-key\" data-k=\"key\" maxlength=\"60\" value=\"" + Ui.Esc(r.Key) + "\"" + (isNew ? string.Empty : " readonly") + "><small>" + (isNew ? "Leave empty to make it from the name." : "Cannot be changed, so links keep working.") + "</small><em class=\"xa-err\" id=\"xa-e-key\"></em></label></div>");
            sb.Append("<div class=\"xa-grid3\"><label class=\"xa-field\" for=\"xa-f-role\"><span>Role</span><select id=\"xa-f-role\" data-k=\"role\">" + string.Concat(Roles.Select(x => "<option" + (x == r.Role ? " selected" : string.Empty) + ">" + x + "</option>")) + "</select></label>");
            sb.Append("<label class=\"xa-field\" for=\"xa-f-email\"><span>Email</span><input type=\"email\" id=\"xa-f-email\" data-k=\"email\" maxlength=\"80\" value=\"" + Ui.Esc(r.Email) + "\"><em class=\"xa-err\" id=\"xa-e-email\"></em></label>");
            sb.Append("<label class=\"xa-field\" for=\"xa-f-phone\"><span>Phone</span><input type=\"text\" id=\"xa-f-phone\" data-k=\"phone\" maxlength=\"30\" value=\"" + Ui.Esc(r.Phone) + "\"><em class=\"xa-err\" id=\"xa-e-phone\"></em></label></div>");
            sb.Append("<div class=\"xa-field\"><span>Portrait</span><div class=\"xa-pic xa-pic-round\" id=\"xa-f-media\" data-k=\"media\" data-src=\"" + Ui.Esc(r.Photo) + "\" data-ph=\"Choose a portrait\"><button type=\"button\" class=\"xa-picb\" onclick=\"AdminAttorneysJs.pic(this)\" aria-label=\"Choose a portrait\">" + (thumb.Length > 0 ? "<img src=\"" + Ui.Esc(thumb) + "\" alt=\"\">" : "<span>Choose a portrait</span>") + "</button><button type=\"button\" class=\"xa-picx\"" + (thumb.Length > 0 ? string.Empty : " hidden") + " onclick=\"AdminAttorneysJs.picClear(this)\">Remove</button></div><em class=\"xa-err\" id=\"xa-e-media\"></em></div>");
            sb.Append("<label class=\"xa-field\" for=\"xa-f-bio\"><span>Biography (separate paragraphs with a blank line)</span><textarea id=\"xa-f-bio\" data-k=\"bio\" rows=\"7\" maxlength=\"3000\">" + Ui.Esc(r.Bio) + "</textarea><em class=\"xa-err\" id=\"xa-e-bio\"></em></label>");
            sb.Append("<div class=\"xa-field\"><span>Practice areas</span>");
            if (areas.Count == 0)
            {
                sb.Append("<small>Add practice areas first, then link them here.</small>");
            }
            else
            {
                sb.Append("<div class=\"xa-checks\">" + string.Concat(areas.Select(a => "<label class=\"xa-check\"><input type=\"checkbox\" class=\"xa-ar\" data-v=\"" + Ui.Esc(a.Key) + "\"" + (r.Areas.Contains(a.Key) ? " checked" : string.Empty) + "> " + Ui.Esc(a.Name) + "</label>")) + "</div>");
            }
            sb.Append("</div>");
            sb.Append("<label class=\"xa-field\" for=\"xa-f-bars\"><span>Bar admissions (one per line: state | year | bar number)</span><textarea id=\"xa-f-bars\" data-k=\"bars\" rows=\"4\" maxlength=\"800\" placeholder=\"Ohio | 2011 | 0081234\">" + Ui.Esc(string.Join("\n", r.Bars.Select(b => b.State + " | " + b.Year + " | " + b.Number))) + "</textarea><em class=\"xa-err\" id=\"xa-e-bars\"></em></label>");
            sb.Append("<label class=\"xa-field\" for=\"xa-f-edu\"><span>Education (one per line: school | degree | year)</span><textarea id=\"xa-f-edu\" data-k=\"edu\" rows=\"3\" maxlength=\"800\" placeholder=\"Cardinal Hill University School of Law | J.D. | 2009\">" + Ui.Esc(string.Join("\n", r.Education.Select(e => e.School + " | " + e.Degree + " | " + e.Year))) + "</textarea><em class=\"xa-err\" id=\"xa-e-edu\"></em></label>");
            sb.Append("<label class=\"xa-check\"><input type=\"checkbox\" id=\"xa-f-show\" data-k=\"show\"" + (r.Show ? " checked" : string.Empty) + "> Show on the website</label>");
            sb.Append("<div class=\"xa-acts\"><button type=\"submit\" class=\"xa-btn\">Save</button><button type=\"button\" class=\"xa-btn xa-btn-ghost\" onclick=\"AdminAttorneysJs.closeModal()\">Cancel</button>" + (isNew ? string.Empty : "<button type=\"button\" class=\"xa-btn xa-btn-danger xa-push\" onclick=\"AdminAttorneysJs.del('" + Ui.Js(r.Key) + "','" + Ui.Js(r.Name) + "')\">Delete</button>") + "</div></form>");
            return sb.ToString();
        }

        private static (List<BarRec> Rows, string Error) ParseBars(string text)
        {
            List<BarRec> rows = new List<BarRec>();
            foreach (string line in text.Replace("\r", string.Empty).Split('\n').Select(x => x.Trim()).Where(x => x.Length > 0))
            {
                string[] p = line.Split('|').Select(x => x.Trim()).ToArray();
                if (p.Length != 3 || p[0].Length < 2 || p[0].Length > 40 || p[2].Length < 1 || p[2].Length > 20 || !int.TryParse(p[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int year) || year < 1950 || year > DateTime.Today.Year)
                {
                    return (rows, "Check the line \"" + Ui.Clip(line, 40) + "\". Use: state | year | bar number.");
                }
                rows.Add(new BarRec { State = p[0], Year = year, Number = p[2] });
            }
            return (rows, rows.Count > 8 ? "Up to 8 admissions, please." : string.Empty);
        }

        private static (List<EduRec> Rows, string Error) ParseEdu(string text)
        {
            List<EduRec> rows = new List<EduRec>();
            foreach (string line in text.Replace("\r", string.Empty).Split('\n').Select(x => x.Trim()).Where(x => x.Length > 0))
            {
                string[] p = line.Split('|').Select(x => x.Trim()).ToArray();
                if (p.Length != 3 || p[0].Length < 2 || p[0].Length > 100 || p[1].Length < 1 || p[1].Length > 40 || !int.TryParse(p[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int year) || year < 1950 || year > DateTime.Today.Year)
                {
                    return (rows, "Check the line \"" + Ui.Clip(line, 40) + "\". Use: school | degree | year.");
                }
                rows.Add(new EduRec { School = p[0], Degree = p[1], Year = year });
            }
            return (rows, rows.Count > 6 ? "Up to 6 lines, please." : string.Empty);
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
            string role = (GetDataValue("role") ?? string.Empty).Trim();
            string email = Ui.Clip(GetDataValue("email"), 80);
            string phone = Ui.Clip(GetDataValue("phone"), 30);
            string photo = Ui.Clip(GetDataValue("media"), 200);
            string bio = (GetDataValue("bio") ?? string.Empty).Replace("\r", string.Empty).Trim();
            bool show = GetDataValue("show") == "1";
            List<string> picked = (GetDataValue("areas") ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Distinct().ToList();
            (List<BarRec> bars, string eBars) = ParseBars(GetDataValue("bars") ?? string.Empty);
            (List<EduRec> edu, string eEdu) = ParseEdu(GetDataValue("edu") ?? string.Empty);
            List<LawyerRec> lawyers = await Store.Rows<LawyerRec>("attorneys");
            List<AreaRec> areas = await Store.Rows<AreaRec>("areas");
            List<MediaItem> library = await Media.Load();
            bool isNew = id.Length == 0;
            if (!isNew && !lawyers.Any(x => x.Key == id))
            {
                response.ExecuteScript("AdminAttorneysJs.toast('That attorney was not found.');");
                return response;
            }
            if (!isNew)
            {
                key = id;
            }
            string eName = name.Length < 2 ? "Please enter the full name." : string.Empty;
            string eKey = key.Length < 2 ? "Use at least 2 letters or numbers." : isNew && lawyers.Any(x => x.Key == key) ? "Another attorney already uses this web address name." : string.Empty;
            string eEmail = email.Length > 0 && !Ui.IsEmail(email) ? "Please enter a valid email address." : string.Empty;
            string ePhone = phone.Length > 0 && !Ui.IsPhone(phone) ? "Please enter a valid phone number." : string.Empty;
            string eBio = bio.Length < 20 ? "Please write a short biography (at least 20 characters)." : bio.Length > 3000 ? "Keep the biography under 3,000 characters." : string.Empty;
            string eMedia = photo.Length > 0 && !library.Any(m => m.Path == photo && m.Kind == "photo") ? "That picture is not in the media library. Choose it again." : string.Empty;
            if (!Roles.Contains(role))
            {
                role = "Associate";
            }
            response.SetElementContents("xa-e-name", eName);
            response.SetElementContents("xa-e-key", eKey);
            response.SetElementContents("xa-e-email", eEmail);
            response.SetElementContents("xa-e-phone", ePhone);
            response.SetElementContents("xa-e-bio", eBio);
            response.SetElementContents("xa-e-media", eMedia);
            response.SetElementContents("xa-e-bars", eBars);
            response.SetElementContents("xa-e-edu", eEdu);
            if (eName + eKey + eEmail + ePhone + eBio + eMedia + eBars + eEdu != string.Empty)
            {
                return response;
            }
            using (await Store.Hold())
            {
                lawyers = await Store.Rows<LawyerRec>("attorneys");
                LawyerRec? l = lawyers.FirstOrDefault(x => x.Key == key);
                if (isNew)
                {
                    if (l != null)
                    {
                        response.SetElementContents("xa-e-key", "Another attorney already uses this web address name.");
                        return response;
                    }
                    l = new LawyerRec { Key = key };
                    lawyers.Add(l);
                }
                if (l == null)
                {
                    response.ExecuteScript("AdminAttorneysJs.toast('That attorney was not found.');");
                    return response;
                }
                l.Name = name;
                l.Role = role;
                l.Email = email;
                l.Phone = phone;
                l.Photo = photo;
                l.Bio = bio;
                l.Areas = areas.Where(a => picked.Contains(a.Key)).Select(a => a.Key).ToList();
                l.Bars = bars;
                l.Education = edu;
                l.Show = show;
                await Store.SaveRows("attorneys", lawyers);
            }
            await Store.Log(c.U.Id, isNew ? "add attorney" : "edit attorney", name, true);
            response.SetElementContents("xa-list", await ListNow());
            response.ExecuteScript("AdminAttorneysJs.saved();");
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
                List<LawyerRec> lawyers = await Store.Rows<LawyerRec>("attorneys");
                LawyerRec? l = lawyers.FirstOrDefault(x => x.Key == id);
                if (l != null)
                {
                    name = l.Name;
                    lawyers.Remove(l);
                    await Store.SaveRows("attorneys", lawyers);
                    List<ArticleRec> articles = await Store.Rows<ArticleRec>("insights");
                    foreach (ArticleRec a in articles.Where(x => x.Author == id))
                    {
                        a.Author = string.Empty;
                    }
                    await Store.SaveRows("insights", articles);
                }
            }
            if (name.Length == 0)
            {
                response.ExecuteScript("AdminAttorneysJs.toast('That attorney was not found.');");
                return response;
            }
            await Store.Log(c.U.Id, "delete attorney", name, true);
            response.SetElementContents("xa-list", await ListNow());
            response.ExecuteScript("AdminAttorneysJs.deleted();");
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
                List<LawyerRec> lawyers = await Store.Rows<LawyerRec>("attorneys");
                int at = lawyers.FindIndex(x => x.Key == id);
                int to = at + dir;
                if (at >= 0 && to >= 0 && to < lawyers.Count)
                {
                    name = lawyers[at].Name;
                    (lawyers[at], lawyers[to]) = (lawyers[to], lawyers[at]);
                    await Store.SaveRows("attorneys", lawyers);
                }
            }
            if (name.Length > 0)
            {
                await Store.Log(c.U.Id, "reorder attorneys", name, true);
            }
            response.SetElementContents("xa-list", await ListNow());
            return response;
        }
    }
}
