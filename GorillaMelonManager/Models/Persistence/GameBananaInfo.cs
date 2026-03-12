namespace GorillaMelonManager.Models.Persistence
{
    /// <summary> Cached mod info from gamebanana for loading gamebanana specific info from the mod install view. </summary>
    public class GameBananaInfo
    {
        public string? iconUrl;
        public string? author;
        public string? description;
        public string? name;

        public GameBananaInfo(string? iconUrl, string? author, string? description, string? name)
        {
            this.iconUrl = iconUrl;
            this.author = author;
            this.description = description;
            this.name = name;
        }
    }
}
