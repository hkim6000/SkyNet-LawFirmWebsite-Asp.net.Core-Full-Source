using System.Globalization;
using System.Text;
using LawFirm.Core;
using LawFirm.Models;
using SkyNet;

namespace LawFirm.codes
{
    public class AdminSettings : WebPage
    {
        public override async Task OnInitialized()
        {
            HtmlDoc.SetTitle("Settings | Staff");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
        }

        private async Task<Ctx?> Guard(ApiResponse response)
        {
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminSettingsJs.expired();");
            }
            return c;
        }

        private async Task Shell(ApiResponse response, Ctx c, SiteData site)
        {
            response.SetElementContents("se-brand", Ui.Esc(site.Brand));
            response.SetElementContents("se-menu", await Auth.Menu("se", "AdminSettings", c));
            response.SetElementContents("se-who", Auth.Who("se", c));
        }

        private static async Task<ApiResponse> Refuse(ApiResponse response, Ctx c, string action, string target, string message)
        {
            await Store.Log(c.U.Id, action, target, false);
            response.ExecuteScript("AdminSettingsJs.toast('" + Ui.Js(message) + "');");
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
            response.SetElementContents("se-main", FormHtml(site));
            return response;
        }

        private static string Field(string id, string label, string value, int max, string hint = "", string type = "text")
        {
            return "<label class=\"se-field\" for=\"se-f-" + id + "\"><span>" + label + "</span><input type=\"" + type + "\" id=\"se-f-" + id + "\" data-k=\"" + id + "\" maxlength=\"" + max + "\" value=\"" + Ui.Esc(value) + "\">" + (hint.Length > 0 ? "<small>" + hint + "</small>" : string.Empty) + "<em class=\"se-err\" id=\"se-e-" + id + "\"></em></label>";
        }

        private static string Area(string id, string label, string value, int max, int rows, string hint = "")
        {
            return "<label class=\"se-field\" for=\"se-f-" + id + "\"><span>" + label + "</span><textarea id=\"se-f-" + id + "\" data-k=\"" + id + "\" rows=\"" + rows + "\" maxlength=\"" + max + "\">" + Ui.Esc(value) + "</textarea>" + (hint.Length > 0 ? "<small>" + hint + "</small>" : string.Empty) + "<em class=\"se-err\" id=\"se-e-" + id + "\"></em></label>";
        }

        private static string FormHtml(SiteData s)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<div class=\"se-ph\"><div><h1>Settings</h1><p>The firm facts that appear in the header, footer, Contact page and forms.</p></div></div>");
            sb.Append("<form class=\"se-panel se-stack\" id=\"se-form\" onsubmit=\"return AdminSettingsJs.save()\" novalidate>");
            sb.Append("<h2>The firm</h2><div class=\"se-grid2\">" + Field("brand", "Short name", s.Brand, 40, "Used in the header and in titles.") + Field("legal", "Full name", s.Legal, 80, "Used in the footer and in emails.") + "</div>");
            sb.Append(Field("tagline", "Tagline", s.Tagline, 140));
            sb.Append("<h2 class=\"se-sec\">Office</h2>" + Field("street", "Street address", s.Street, 80));
            sb.Append("<div class=\"se-grid3\">" + Field("city", "City", s.City, 40) + Field("region", "State", s.Region, 30) + Field("zip", "ZIP code", s.Zip, 10) + "</div>");
            sb.Append("<div class=\"se-grid3\">" + Field("phone", "Phone", s.Phone, 30) + Field("fax", "Fax (optional)", s.Fax, 30) + Field("email", "Email", s.Email, 80, string.Empty, "email") + "</div>");
            sb.Append(Area("hours", "Office hours (one line per day or range)", s.Hours, 200, 3));
            sb.Append("<h2 class=\"se-sec\">Website text</h2>" + Field("disclaimer", "Footer disclaimer", s.Disclaimer, 120, "Shown at the bottom of every public page."));
            sb.Append(Area("formnote", "Note above the consultation form", s.FormNote, 400, 3, "Say that sending the form does not create an attorney-client relationship."));
            sb.Append("<h2 class=\"se-sec\">Messages and files</h2>" + Field("notify", "Send a copy of each request to", s.NotifyEmail, 80, Mailer.Ready ? "Mail is set up, so a copy is emailed here." : "Mail is not set up yet (see below), so requests are only saved in Messages.", "email"));
            sb.Append(Field("video", "Largest video upload (MB)", s.MaxVideoMb.ToString(CultureInfo.InvariantCulture), 3, "Between 1 and 100.", "number"));
            sb.Append("<div class=\"se-acts\"><button type=\"submit\" class=\"se-btn\">Save settings</button></div></form>");
            sb.Append("<section class=\"se-panel se-stack\"><h2>Email</h2><dl class=\"se-kv\"><dt>Mail server</dt><dd>" + Ui.Esc(Mailer.Server.Length > 0 ? Mailer.Server : "Not set") + "</dd><dt>Sender</dt><dd>" + Ui.Esc(Mailer.From.Length > 0 ? Mailer.From : "Not set") + "</dd><dt>Status</dt><dd>" + (Mailer.Ready ? "Ready. New requests are copied to the address above." : "Not set up. The sample values in appConfig/application.cfg are placeholders.") + "</dd></dl><p class=\"se-hint\">The mail server, sender and password live in the app.mail lines of appConfig/application.cfg. Edit that file and restart the site. The password is never shown here.</p></section>");
            sb.Append("<section class=\"se-panel se-stack\"><h2>Run mode</h2><p class=\"se-hint\">Run mode is set in appConfig/application.cfg.</p></section>");
            if (Store.Demo())
            {
                sb.Append("<section class=\"se-panel se-stack se-danger\"><h2>Initialize data</h2><p class=\"se-hint\">Removes every consultation request, the activity log and the staff account made so far. Pages, practice areas, attorneys, articles, settings and the media library stay. The next sign-in page shows a new first password.</p><div><button type=\"button\" class=\"se-btn se-btn-danger\" onclick=\"AdminSettingsJs.initialize()\">Initialize data</button></div></section>");
            }
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
            string brand = Ui.Clip(GetDataValue("brand"), 40);
            string legal = Ui.Clip(GetDataValue("legal"), 80);
            string tagline = Ui.Clip(GetDataValue("tagline"), 140);
            string street = Ui.Clip(GetDataValue("street"), 80);
            string city = Ui.Clip(GetDataValue("city"), 40);
            string region = Ui.Clip(GetDataValue("region"), 30);
            string zip = Ui.Clip(GetDataValue("zip"), 10);
            string phone = Ui.Clip(GetDataValue("phone"), 30);
            string fax = Ui.Clip(GetDataValue("fax"), 30);
            string email = Ui.Clip(GetDataValue("email"), 80);
            string hours = Ui.Clip((GetDataValue("hours") ?? string.Empty).Replace("\r", string.Empty), 200);
            string disclaimer = Ui.Clip(GetDataValue("disclaimer"), 120);
            string formNote = Ui.Clip(GetDataValue("formnote"), 400);
            string notify = Ui.Clip(GetDataValue("notify"), 80);
            bool videoOk = int.TryParse((GetDataValue("video") ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int video);
            Dictionary<string, string> e = new Dictionary<string, string>
            {
                { "brand", brand.Length < 2 ? "Please enter the short name." : string.Empty },
                { "legal", legal.Length < 2 ? "Please enter the full name." : string.Empty },
                { "tagline", string.Empty },
                { "street", street.Length < 3 ? "Please enter the street address." : string.Empty },
                { "city", city.Length < 2 ? "Please enter the city." : string.Empty },
                { "region", region.Length < 2 ? "Please enter the state." : string.Empty },
                { "zip", zip.Length < 3 ? "Please enter the ZIP code." : string.Empty },
                { "phone", !Ui.IsPhone(phone) ? "Please enter a valid phone number." : string.Empty },
                { "fax", fax.Length > 0 && !Ui.IsPhone(fax) ? "Please enter a valid fax number, or leave it empty." : string.Empty },
                { "email", !Ui.IsEmail(email) ? "Please enter a valid email address." : string.Empty },
                { "hours", hours.Length < 3 ? "Please enter the office hours." : string.Empty },
                { "disclaimer", disclaimer.Length < 5 ? "Please enter the footer disclaimer." : string.Empty },
                { "formnote", formNote.Length < 10 ? "Please write the note (at least 10 characters)." : string.Empty },
                { "notify", notify.Length > 0 && !Ui.IsEmail(notify) ? "Please enter a valid email address, or leave it empty." : string.Empty },
                { "video", !videoOk || video < 1 || video > 100 ? "Enter a whole number from 1 to 100." : string.Empty }
            };
            foreach (KeyValuePair<string, string> kv in e)
            {
                response.SetElementContents("se-e-" + kv.Key, kv.Value);
            }
            if (e.Values.Any(v => v.Length > 0))
            {
                return response;
            }
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                site.Brand = brand;
                site.Legal = legal;
                site.Tagline = tagline;
                site.Street = street;
                site.City = city;
                site.Region = region;
                site.Zip = zip;
                site.Phone = phone;
                site.Fax = fax;
                site.Email = email;
                site.Hours = hours;
                site.Disclaimer = disclaimer;
                site.FormNote = formNote;
                site.NotifyEmail = notify;
                site.MaxVideoMb = video;
                await Store.SaveSite(site);
            }
            await Store.Log(c.U.Id, "save settings", brand, true);
            SiteData fresh = await Store.LoadSite();
            response.SetElementContents("se-brand", Ui.Esc(fresh.Brand));
            response.ExecuteScript("AdminSettingsJs.saved();");
            return response;
        }

        public async Task<ApiResponse> Initialize()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Guard(response);
            if (c == null)
            {
                return response;
            }
            if (!Store.Demo())
            {
                return await Refuse(response, c, "initialize data", "settings", "In Production the data is initialized once, on the first start. Edit records in their own pages instead.");
            }
            if ((GetDataValue("word") ?? string.Empty).Trim() != "INITIALIZE")
            {
                response.ExecuteScript("AdminSettingsJs.toast('Type INITIALIZE to confirm.');");
                return response;
            }
            await Store.Log(c.U.Id, "initialize data", "requested from Settings", true);
            int removed = await Init.Run("Settings, by " + c.U.Name, false);
            response.ExecuteScript("AdminSettingsJs.initialized(" + removed + ");");
            return response;
        }
    }
}
