namespace SharedWeaponBuilds.Client.Models;

public sealed class UpdatedWeaponMessage
{
    public string BuildId { get; set; }
    public WeaponBuildClass UpdatedWeaponBuild { get; set; }
    public bool IsDeleted { get; set; }
}
