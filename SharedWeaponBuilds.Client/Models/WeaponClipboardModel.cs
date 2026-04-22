using EFT;

namespace SharedWeaponBuilds.Client.Models;

/// <summary>
/// This model is technically the same as WeaponBuild on the server, so weapon builds can be copied and pasted easily
/// </summary>
public sealed class WeaponClipboardModel
{
    public MongoID Id { get; set; }
    public string Name { get; set; }
    public string ExporterName { get; set; }
    public MongoID Root { get; set; }
    public FlatItemsDataClass[] Items { get; set; }
}
