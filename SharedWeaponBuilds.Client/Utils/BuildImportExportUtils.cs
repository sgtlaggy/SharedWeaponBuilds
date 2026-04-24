using EFT;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharedWeaponBuilds.Client.Models;
using SharedWeaponBuilds.Client.Models.Notifications;
using UnityEngine;

namespace SharedWeaponBuilds.Client.Utils;

public static class BuildImportExportUtils
{
    public static WeaponBuildClass Import()
    {
        var clipboard = GUIUtility.systemCopyBuffer;

        if (string.IsNullOrWhiteSpace(clipboard))
        {
            return null;
        }

        if (!TryDeserializeClipboard(clipboard, out WeaponClipboardModel clipboardModel))
        {
            return null;
        }

        return ValidateAndCreateWeaponBuild(clipboardModel);
    }

    private static bool TryDeserializeClipboard(string json, out WeaponClipboardModel model)
    {
        model = null;

        try
        {
            var parsedToken = JToken.Parse(json);

            if (parsedToken.Type != JTokenType.Object)
            {
                return false;
            }

            model = parsedToken.ToObject<WeaponClipboardModel>();

            if (model == null)
            {
                SharedWeaponBuildsWSManager.notifierView.method_5(
                    new FailedNotification("Could not deserialize JSON in clipboard to a weapon build")
                );
                return false;
            }

            return true;
        }
        catch (JsonReaderException)
        {
            return false;
        }
        catch (JsonSerializationException)
        {
            SharedWeaponBuildsWSManager.notifierView.method_5(
                new FailedNotification("JSON in clipboard does not match a weapon build model")
            );
            return false;
        }
    }

    private static WeaponBuildClass ValidateAndCreateWeaponBuild(WeaponClipboardModel model)
    {
        if (model.Items == null || model.Items.Length == 0)
        {
            SharedWeaponBuildsWSManager.notifierView.method_5(new FailedNotification("No items for the weapon build were provided"));
            return null;
        }

        var missingTemplatesCount = 0;

        foreach (var modelItems in model.Items)
        {
            if (!SharedWeaponBuildsWSManager.ItemFactoryClass.ItemTemplates.ContainsKey(modelItems._tpl))
            {
                missingTemplatesCount++;
            }
        }

        if (missingTemplatesCount > 0)
        {
            SharedWeaponBuildsWSManager.notifierView.method_5(
                new FailedNotification($"Failed to import weapon build due to {missingTemplatesCount} missing item templates")
            );
            return null;
        }

        var itemTree = SharedWeaponBuildsWSManager.ItemFactoryClass.FlatItemsToTree(model.Items);

        if (!itemTree.Items.TryGetValue(model.Root, out var item))
        {
            SharedWeaponBuildsWSManager.notifierView.method_5(
                new FailedNotification("Root item was not found in the imported weapon build")
            );
            return null;
        }

        SharedWeaponBuildsWSManager.notifierView.method_5(
            new ImportedWeaponBuildNotification { BuildName = model.Name, Username = model.ExporterName }
        );

        return new WeaponBuildClass(model.Id, "", model.Name, item, false);
    }

    public static void Export(string buildName, MongoID rootItem, FlatItemsDataClass[] items)
    {
        if (items.Length == 0)
        {
            SharedWeaponBuildsWSManager.notifierView.method_5(new FailedNotification("No items for the weapon build were provided", true));
            return;
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
        SharedWeaponBuildsWSManager.notifierView.method_5(new ExportedWeaponBuildNotification { BuildName = buildName });
    }
}
