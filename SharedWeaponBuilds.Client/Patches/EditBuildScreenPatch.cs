using System.Reflection;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SharedWeaponBuilds.Client.Utils;
using SPT.Reflection.Patching;
using UnityEngine;
using UnityEngine.UI;

namespace SharedWeaponBuilds.Client.Patches;

public sealed class EditBuildScreenPatch : ModulePatch
{
    public static readonly FieldInfo WeaponBuildClassField = typeof(EditBuildScreen).GetField(
        "weaponBuildClass",
        BindingFlags.Instance | BindingFlags.NonPublic
    );
    private static readonly FieldInfo _openBuildButtonField = typeof(EditBuildScreen).GetField(
        "_openBuildButton",
        BindingFlags.Instance | BindingFlags.NonPublic
    );
    private static readonly FieldInfo _uIField = typeof(EditBuildScreen).GetField("UI", BindingFlags.Instance | BindingFlags.NonPublic);

    private static Button _importFromClìpboardButton = null;
    private static Button _exportToClipboardButton = null;
    private static CanvasGroup _exportToClipboardButtonCanvas = null;
    public static readonly BindableStateClass<bool> ExportToClipboardBindableState = new(false, null);

    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(
            typeof(EditBuildScreen),
            nameof(EditBuildScreen.Show),
            [typeof(Item), typeof(Item), typeof(InventoryController), typeof(ISession)]
        );
    }

    [PatchPrefix]
    public static void Prefix(EditBuildScreen __instance)
    {
        var eftopenButton = (Button)_openBuildButtonField.GetValue(__instance);
        var UI = (AddViewListClass)_uIField.GetValue(__instance);

        if (eftopenButton == null || UI == null)
        {
            return;
        }

        if (_importFromClìpboardButton == null)
        {
            CreateImportButton(__instance, eftopenButton);
        }

        if (_exportToClipboardButton == null || _exportToClipboardButtonCanvas == null)
        {
            CreateExportButton(__instance, eftopenButton);
        }

        UI.AddDisposable(
            ExportToClipboardBindableState.Bind(value =>
            {
                _exportToClipboardButton.enabled = value;
                _exportToClipboardButtonCanvas.SetUnlockStatus(value, false);
            })
        );
    }

    private static void CreateImportButton(EditBuildScreen __instance, Button buttonToCopy)
    {
        _importFromClìpboardButton = UnityEngine.Object.Instantiate(buttonToCopy, buttonToCopy.transform.parent, false);
        _importFromClìpboardButton.name = "ImportBuildFromClipboard";
        _importFromClìpboardButton.transform.SetSiblingIndex(2);
        _importFromClìpboardButton.GetComponentInChildren<LocalizedText>().LocalizationKey = "PASTE BUILD ...";

        var handbookButtonObject = GameObject.Find("Preloader UI/BottomPanel/Content/TaskBar/Tabs/Handbook/HandbookButton/Icon");

        if (handbookButtonObject != null)
        {
            Image handbookImage = handbookButtonObject.GetComponent<Image>();
            Image buttonImageComponent = _importFromClìpboardButton.transform.GetChild(1).GetComponentInChildren<Image>();

            if (handbookImage != null && buttonImageComponent != null)
            {
                buttonImageComponent.sprite = handbookImage.sprite;
                buttonImageComponent.preserveAspect = true;

                RectTransform iconRect = buttonImageComponent.rectTransform;
                iconRect.sizeDelta = new Vector2(18f, 18f);
            }
        }

        _importFromClìpboardButton.onClick.AddListener(() =>
        {
            var import = BuildImportExportUtils.Import();

            if (import == null)
            {
                return;
            }

            __instance.method_31(import);
        });
    }

    private static void CreateExportButton(EditBuildScreen __instance, Button buttonToCopy)
    {
        _exportToClipboardButton = UnityEngine.Object.Instantiate(buttonToCopy, buttonToCopy.transform.parent, false);
        _exportToClipboardButtonCanvas = _exportToClipboardButton.GetComponent<CanvasGroup>();
        _exportToClipboardButton.name = "SaveBuildToClipboard";
        _exportToClipboardButton.transform.SetSiblingIndex(4);
        _exportToClipboardButton.GetComponentInChildren<LocalizedText>().LocalizationKey = "COPY BUILD ...";

        var handbookButtonObject = GameObject.Find("Preloader UI/BottomPanel/Content/TaskBar/Tabs/Handbook/HandbookButton/Icon");

        if (handbookButtonObject != null)
        {
            Image handbookImage = handbookButtonObject.GetComponent<Image>();
            Image buttonImageComponent = _importFromClìpboardButton.transform.GetChild(1).GetComponentInChildren<Image>();

            if (handbookImage != null && buttonImageComponent != null)
            {
                buttonImageComponent.sprite = handbookImage.sprite;
                buttonImageComponent.preserveAspect = true;

                RectTransform iconRect = buttonImageComponent.rectTransform;
                iconRect.sizeDelta = new Vector2(18f, 18f);
            }
        }

        _exportToClipboardButton.onClick.AddListener(() =>
        {
            var currentBuild = (WeaponBuildClass)WeaponBuildClassField.GetValue(__instance);

            if (currentBuild == null)
            {
                return;
            }

            var flatBuild = SharedWeaponBuildsWSManager.ItemFactoryClass.TreeToFlatItems(currentBuild.Item);

            BuildImportExportUtils.Export(currentBuild.HandbookName, currentBuild.Item.Id, flatBuild);
        });
    }
}

public sealed class EnableWeaponSavePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(EditBuildScreen), nameof(EditBuildScreen.method_45));
    }

    [PatchPrefix]
    public static void Prefix(bool anyMissing)
    {
        EditBuildScreenPatch.ExportToClipboardBindableState.Value = !anyMissing;
    }
}

public sealed class EnableKeyboardBindingsPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(EditBuildScreen), nameof(EditBuildScreen.Update));
    }

    [PatchPrefix]
    public static void Prefix(EditBuildScreen __instance)
    {
        if (IsCopyShortcutPressed())
        {
            var currentBuild = (WeaponBuildClass)EditBuildScreenPatch.WeaponBuildClassField.GetValue(__instance);

            if (currentBuild == null)
            {
                return;
            }

            var flatBuild = SharedWeaponBuildsWSManager.ItemFactoryClass.TreeToFlatItems(currentBuild.Item);

            BuildImportExportUtils.Export(currentBuild.HandbookName, currentBuild.Item.Id, flatBuild);
        }

        if (IsPasteShortcutPressed())
        {
            var import = BuildImportExportUtils.Import();

            if (import == null)
            {
                return;
            }

            __instance.method_31(import);
        }
    }

    private static bool IsCopyShortcutPressed()
    {
        bool ctrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        bool cPressed = Input.GetKeyDown(KeyCode.C);

        return ctrlHeld && cPressed;
    }

    private static bool IsPasteShortcutPressed()
    {
        bool ctrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        bool vPressed = Input.GetKeyDown(KeyCode.V);

        return ctrlHeld && vPressed;
    }
}
