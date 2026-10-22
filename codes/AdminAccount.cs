using System.Text;
using LawFirm.Core;
using LawFirm.Models;
using SkyNet;

namespace LawFirm.codes
{
    public class AdminAccount : WebPage
    {
        public override async Task OnInitialized()
        {
            HtmlDoc.SetTitle("My account | Staff");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
        }

        private async Task<Ctx?> Guard(ApiResponse response)
        {
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminAccountJs.expired();");
            }
            return c;
        }

        private async Task Shell(ApiResponse response, Ctx c, SiteData site)
        {
            response.SetElementContents("my-brand", Ui.Esc(site.Brand));
            response.SetElementContents("my-menu", await Auth.Menu("my", "AdminAccount", c));
            response.SetElementContents("my-who", Auth.Who("my", c));
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
            StringBuilder sb = new StringBuilder();
            sb.Append("<div class=\"my-ph\"><div><h1>My account</h1><p>The one administrator account for this website.</p></div></div>");
            sb.Append("<section class=\"my-panel my-stack\"><h2>Signed in as</h2><dl class=\"my-kv\"><dt>Name</dt><dd>" + Ui.Esc(c.U.Name) + "</dd><dt>User name</dt><dd>" + Ui.Esc(c.U.Login) + "</dd><dt>Role</dt><dd>Administrator, with access to every page of the staff area</dd></dl></section>");
            if (c.U.Id == "u-demo")
            {
                sb.Append("<section class=\"my-panel my-stack\"><h2>Password</h2><p class=\"my-hint\">You are signed in as the demo manager, which has no password of its own. Set the run mode to Production in appConfig/application.cfg and restart the site to create the real administrator account.</p></section>");
            }
            else
            {
                sb.Append("<form class=\"my-panel my-stack\" id=\"my-form\" onsubmit=\"return AdminAccountJs.change()\" novalidate><h2>Change password</h2>");
                sb.Append("<label class=\"my-field\" for=\"my-f-cur\"><span>Current password</span><input type=\"password\" id=\"my-f-cur\" data-k=\"cur\" autocomplete=\"current-password\" maxlength=\"100\"><em class=\"my-err\" id=\"my-e-cur\"></em></label>");
                sb.Append("<label class=\"my-field\" for=\"my-f-new\"><span>New password</span><input type=\"password\" id=\"my-f-new\" data-k=\"next\" autocomplete=\"new-password\" maxlength=\"100\"><small>At least 8 characters.</small><em class=\"my-err\" id=\"my-e-new\"></em></label>");
                sb.Append("<label class=\"my-field\" for=\"my-f-again\"><span>New password again</span><input type=\"password\" id=\"my-f-again\" data-k=\"again\" autocomplete=\"new-password\" maxlength=\"100\"><em class=\"my-err\" id=\"my-e-again\"></em></label>");
                sb.Append("<div class=\"my-acts\"><button type=\"submit\" class=\"my-btn\">Change password</button></div><p class=\"my-hint\">Changing the password signs you out of other browsers.</p></form>");
            }
            response.SetElementContents("my-main", sb.ToString());
            return response;
        }

        public async Task<ApiResponse> Change()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Guard(response);
            if (c == null)
            {
                return response;
            }
            string cur = GetDataValue("cur") ?? string.Empty;
            string next = GetDataValue("next") ?? string.Empty;
            string again = GetDataValue("again") ?? string.Empty;
            string eCur = cur.Length == 0 ? "Please enter your current password." : string.Empty;
            string eNew = next.Length == 0 ? "Please enter a new password." : string.Empty;
            string eAgain = again.Length == 0 ? "Please repeat the new password." : string.Empty;
            response.SetElementContents("my-e-cur", eCur);
            response.SetElementContents("my-e-new", eNew);
            response.SetElementContents("my-e-again", eAgain);
            if (eCur + eNew + eAgain != string.Empty)
            {
                return response;
            }
            string error = await Auth.ChangeOwn(c, cur, next, again);
            if (error.Length > 0)
            {
                await Store.Log(c.U.Id, "change password", "refused", false);
                bool wrongCurrent = error.StartsWith("The current", StringComparison.Ordinal);
                response.SetElementContents(wrongCurrent ? "my-e-cur" : "my-e-new", error);
                return response;
            }
            await Store.Log(c.U.Id, "change password", c.U.Login, true);
            response.ExecuteScript("AdminAccountJs.changed();");
            return response;
        }
    }
}
