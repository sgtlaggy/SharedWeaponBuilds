using EFT.Communications;

namespace SharedWeaponBuilds.Client.Models.Notifications;

public sealed class FailedNotification(string failedReason, bool export = false) : NotificationAbstractClass
{
    public override string Description
    {
        get
        {
            if (export)
            {
                return $"Could not export weapon build: {failedReason}";
            }
            else
            {
                return $"Could not import weapon build: {failedReason}";
            }
        }
    }

    public override ENotificationIconType Icon
    {
        get { return ENotificationIconType.Alert; }
    }
}
