using Comfort.Common;
using EFT;
using EFT.UI;
using Newtonsoft.Json;
using SharedWeaponBuilds.Client.Models;
using SharedWeaponBuilds.Client.Models.Notifications;
using UnityEngine;

namespace SharedWeaponBuilds.Client.Utils;

public static class BuildImportExportUtils
{
    public static WeaponBuildClass Import()
    {
        var clipboard = GUIUtility.systemCopyBuffer;
        return ValidateAndCreateWeaponBuild(JsonConvert.DeserializeObject<WeaponClipboardModel>(clipboard));
    }

    private static WeaponBuildClass ValidateAndCreateWeaponBuild(WeaponClipboardModel model)
    {
        // Todo: Validate each item and check if tpl is in items array

        if (model.Items == null || model.Items.Length == 0)
        {
            throw new InvalidOperationException("No items were provided.");
        }

        var tree = SharedWeaponBuildsWSManager.ItemFactoryClass.FlatItemsToTree(model.Items);

        if (!tree.Items.TryGetValue(model.Root, out var item))
        {
            throw new InvalidOperationException("Root item was not found in the built tree.");
        }

        Singleton<PreloaderUI>.Instance.NotifierView.method_5(
            new ImportedWeaponBuildNotification { BuildName = model.Name, Username = model.ExporterName }
        );

        return new WeaponBuildClass(model.Id, "", model.Name, item, false);
    }

    public static void Export(string buildName, MongoID rootItem, FlatItemsDataClass[] items)
    {
        if (items.Length == 0)
        {
            throw new InvalidOperationException("No items were provided.");
        }

        if (string.IsNullOrEmpty(buildName))
        {
            buildName = "";
        }

        var exportModel = new WeaponClipboardModel
        {
            Id = MongoID.Generate(),
            ExporterName = SharedWeaponBuildsWSManager.Session.Profile.Nickname,
            Name = buildName,
            Root = rootItem,
            Items = items,
        };

        GUIUtility.systemCopyBuffer = JsonConvert.SerializeObject(exportModel);
        Singleton<PreloaderUI>.Instance.NotifierView.method_5(new ExportedWeaponBuildNotification { BuildName = buildName });
    }
}
