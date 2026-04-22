using EFT.Communications;

namespace SharedWeaponBuilds.Client.Models.Notifications;

public sealed class ExportedWeaponBuildNotification : NotificationAbstractClass
{
    public override string Description
    {
        get { return $"Exported weapon build {BuildName} to clipboard"; }
    }

    public string BuildName { get; set; }
    public override ENotificationIconType Icon
    {
        get { return ENotificationIconType.Alert; }
    }
}
