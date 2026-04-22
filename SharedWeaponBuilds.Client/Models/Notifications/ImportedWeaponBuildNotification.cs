using EFT.Communications;

namespace SharedWeaponBuilds.Client.Models.Notifications;

public sealed class ImportedWeaponBuildNotification : NotificationAbstractClass
{
    public override string Description
    {
        get
        {
            if (!string.IsNullOrEmpty(Username))
            {
                return $"Succesfully imported build {BuildName} from {Username}";
            }
            else
            {
                return $"Succesfully imported build {BuildName}";
            }
        }
    }

    public string Username { get; set; }
    public string BuildName { get; set; }
    public override ENotificationIconType Icon
    {
        get { return ENotificationIconType.Default; }
    }
}
