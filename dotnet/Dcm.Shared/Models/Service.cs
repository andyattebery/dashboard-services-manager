namespace Dcm.Shared.Models;
public class Service
{
    public string Name { get; set; }
    public string Url { get; set; }
    public string Category { get; set; }
    public string Icon { get; set; }
    public string ImageUrl { get; set; }
    public string Hostname { get; set; }
    public bool Ignore { get; set; }

    public Service()
    {
    }

    public Service(string name, string url, string category, string icon, string imageUrl, 
                   string hostname, bool ignore)
    {
      Name = name;
      Url = url;
      Category = category;
      Icon = icon;
      ImageUrl = imageUrl;
      Hostname = hostname;
      Ignore = ignore;
    }

    public override string ToString()
    {
        return $"{nameof(Name)}: {Name}, {nameof(Url)}: {Url}, {nameof(Category)}: {Category}, {nameof(Icon)}: {Icon}, {nameof(ImageUrl)}: {ImageUrl}, {nameof(Hostname)}: {Hostname}, {nameof(Ignore)}: {Ignore}";
    }
}
