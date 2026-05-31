using SpinCore.Translation;
using SpinCore.UI;
using System.Reflection;
using BepInEx.Configuration;
using UnityEngine;

namespace SpinStatus
{
    internal static class Menu
    {
        private static CustomPage _modPage;

        private static void CreateConfigToggle(Transform parent, ConfigEntry<bool> config, string translationKey)
        {
            UIHelper.CreateLargeToggle(
                parent,
                translationKey,
                translationKey,
                config.Value,
                (value) =>
                {
                    config.Value = value;
                    config.ConfigFile.Save();
                }
            );
        }

        private static void CreateConfigRange(Transform parent, ConfigEntry<int> config, string translationKey, int start, int end)
        {
            CustomGroup groupPort = UIHelper.CreateGroup(parent, "SpinStatus_Menu_General", Axis.Horizontal);

            UIHelper.CreateLabel(groupPort, translationKey, translationKey);

            UIHelper.CreateInputField(
                groupPort,
                translationKey,
                (_, value) =>
                {
                    if (int.TryParse(value, out int port))
                    {
                        config.Value = Mathf.Clamp(port, start, end);
                        config.ConfigFile.Save();
                    }
                    else
                    {
                        Plugin.Logger.LogInfo("Not a valid number");
                    }
                }
            ).GameObject.gameObject.GetComponent<XDNavigableInputField>().SetText(config.Value.ToString(), true);
        }

        internal static void Create()
        {
            var localeStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SpinStatus.locale.json");
            TranslationHelper.LoadTranslationsFromStream(localeStream);

            _modPage = UIHelper.CreateCustomPage("SpinStatus_Menu");
            UIHelper.RegisterMenuInModSettingsRoot("SpinStatus_Menu_MenuName", _modPage);
            _modPage.OnPageLoad += modPageTransform => Reload();
        }

        internal static void Reload()
        {
            if (_modPage == null) { return; }

            foreach (Transform item in _modPage.PageContentTransform)
            {
                Object.Destroy(item.gameObject);
            }

            CustomGroup groupGeneral = UIHelper.CreateGroup(_modPage.PageContentTransform, "SpinStatus_Menu_General");
            UIHelper.CreateSectionHeader(
                groupGeneral,
                "SpinStatus_Menu_General_Header",
                "SpinStatus_Menu_General_Header",
                false
            );

            CreateConfigToggle(groupGeneral, Config.ServerEnabled, "SpinStatus_Menu_General_Enabled");
            CreateConfigRange(groupGeneral, Config.ServerPort, "SpinStatus_Menu_General_Port", 0, 65535);
            CreateConfigToggle(groupGeneral, Config.SendImageData, "SpinStatus_Menu_General_SendImage");

            CustomGroup groupEvents = UIHelper.CreateGroup(_modPage.PageContentTransform, "SpinStatus_Menu_Events");
            UIHelper.CreateSectionHeader(
                groupEvents,
                "SpinStatus_Menu_Events_Header",
                "SpinStatus_Menu_Events_Header",
                false
            );

            CreateConfigToggle(groupEvents, Config.EventNote, "SpinStatus_Menu_Events_Note");
            CreateConfigToggle(groupEvents, Config.EventScore, "SpinStatus_Menu_Events_Score");
            CreateConfigToggle(groupEvents, Config.EventTrackStart, "SpinStatus_Menu_Events_TrackStart");
            CreateConfigToggle(groupEvents, Config.EventTrackEnd, "SpinStatus_Menu_Events_TrackEnd");
            CreateConfigToggle(groupEvents, Config.EventTrackComplete, "SpinStatus_Menu_Events_TrackComplete");
            CreateConfigToggle(groupEvents, Config.EventTrackFail, "SpinStatus_Menu_Events_TrackFail");
            CreateConfigToggle(groupEvents, Config.EventTrackPause, "SpinStatus_Menu_Events_TrackPause");
            CreateConfigToggle(groupEvents, Config.EventTrackResume, "SpinStatus_Menu_Events_TrackResume");

            UIHelper.CreateButton(
                groupEvents,
                "SpinStatus_Menu_Button_Reset",
                "SpinStatus_Menu_Button_Reset",
                () =>
                {
                    Config.Reset();
                    Reload();
                }
            );
        }

    }
}
