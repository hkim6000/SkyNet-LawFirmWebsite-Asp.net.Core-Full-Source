namespace LawFirm.Models
{
    public class SiteData
    {
        public string Brand { get; set; } = "Harrow & Vance";
        public string Legal { get; set; } = "Harrow & Vance, Attorneys at Law";
        public string Tagline { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string Zip { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Fax { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Hours { get; set; } = string.Empty;
        public string Disclaimer { get; set; } = "Attorney advertising. Not legal advice.";
        public string FormNote { get; set; } = string.Empty;
        public string NotifyEmail { get; set; } = string.Empty;
        public int MaxVideoMb { get; set; } = 30;
        public List<TopicRec> Topics { get; set; } = new List<TopicRec>();
        public Dictionary<string, PageDoc> Pages { get; set; } = new Dictionary<string, PageDoc>();
    }

    public class TopicRec
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class AreaRec
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Intro { get; set; } = string.Empty;
        public string Media { get; set; } = string.Empty;
        public List<string> Handles { get; set; } = new List<string>();
        public bool Show { get; set; } = true;
    }

    public class BarRec
    {
        public string State { get; set; } = string.Empty;
        public int Year { get; set; }
        public string Number { get; set; } = string.Empty;
    }

    public class EduRec
    {
        public string School { get; set; } = string.Empty;
        public string Degree { get; set; } = string.Empty;
        public int Year { get; set; }
    }

    public class LawyerRec
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Photo { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public List<string> Areas { get; set; } = new List<string>();
        public List<BarRec> Bars { get; set; } = new List<BarRec>();
        public List<EduRec> Education { get; set; } = new List<EduRec>();
        public bool Show { get; set; } = true;
    }

    public class ArticleRec
    {
        public string Key { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Media { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public bool Published { get; set; } = true;
    }

    public class InquiryRec
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public DateTime When { get; set; }
        public string Status { get; set; } = "New";
        public bool Ack { get; set; }
    }

    public class MediaItem
    {
        public string Id { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Thumb { get; set; } = string.Empty;
        public string Kind { get; set; } = "photo";
        public string Name { get; set; } = string.Empty;
        public string Alt { get; set; } = string.Empty;
        public string Caption { get; set; } = string.Empty;
        public long Bytes { get; set; }
        public DateTime Added { get; set; }
        public string By { get; set; } = string.Empty;
    }

    public class BlockItem
    {
        public string Icon { get; set; } = string.Empty;
        public string Media { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }

    public class Block
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public bool Hidden { get; set; }
        public string Kicker { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Media { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
        public string Label2 { get; set; } = string.Empty;
        public string Link2 { get; set; } = string.Empty;
        public bool Flip { get; set; }
        public List<BlockItem> Items { get; set; } = new List<BlockItem>();
    }

    public class PageVersion
    {
        public DateTime When { get; set; }
        public string By { get; set; } = string.Empty;
        public List<Block> Blocks { get; set; } = new List<Block>();
    }

    public class PageDoc
    {
        public List<Block> Blocks { get; set; } = new List<Block>();
        public List<PageVersion> Versions { get; set; } = new List<PageVersion>();
        public DateTime Saved { get; set; }
        public string By { get; set; } = string.Empty;
    }

    public class AdminUser
    {
        public string Id { get; set; } = string.Empty;
        public string Login { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;
        public bool MustChange { get; set; }
    }

    public class SessionRec
    {
        public string Token { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime Expires { get; set; }
    }

    public class ActivityRec
    {
        public DateTime When { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public bool Ok { get; set; } = true;
    }

    public class PublicData
    {
        public SiteData Site { get; set; } = new SiteData();
        public List<MediaItem> Media { get; set; } = new List<MediaItem>();
        public List<AreaRec> Areas { get; set; } = new List<AreaRec>();
        public List<LawyerRec> Lawyers { get; set; } = new List<LawyerRec>();
        public List<ArticleRec> Articles { get; set; } = new List<ArticleRec>();
    }
}
