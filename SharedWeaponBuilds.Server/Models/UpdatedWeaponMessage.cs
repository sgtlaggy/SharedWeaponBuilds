using SPTarkov.Server.Core.Models.Eft.Profile;

namespace SharedWeaponBuilds.Server.Models;

public sealed record UpdatedWeaponMessage
{
    public required string BuildId { get; set; }
    public WeaponBuild? UpdatedWeaponBuild { get; set; }
    public required bool IsDeleted { get; set; }
}
