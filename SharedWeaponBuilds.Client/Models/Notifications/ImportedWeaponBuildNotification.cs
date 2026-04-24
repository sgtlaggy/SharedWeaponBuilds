using EFT.Communications;

namespace SharedWeaponBuilds.Client.Models.Notifications;

public sealed class ImportedWeaponBuildNotification : NotificationAbstractClass
{
    public override string Description
    {
        get
        {
            var hasBuildName = !string.IsNullOrWhiteSpace(BuildName);
            var hasUsername = !string.IsNullOrWhiteSpace(Username);

            if (hasBuildName && hasUsername)
            {
                return $"Successfully imported build {BuildName} from {Username}";
            }

            if (hasBuildName)
            {
                return $"Successfully imported build {BuildName}";
            }

            if (hasUsername)
            {
                return $"Successfully imported a build from {Username}";
            }

            return "Successfully imported a build";
        }
    }

    public string Username { get; set; }
    public string BuildName { get; set; }
    public override ENotificationIconType Icon
    {
        get { return ENotificationIconType.Default; }
    }
}
