using System;
using BepInEx.Configuration;

namespace SpinStatus
{
    internal static class Config
    {
        public static ConfigEntry<bool> ServerEnabled;
        public static ConfigEntry<int> ServerPort;
        public static ConfigEntry<bool> SendImageData;

        public static ConfigEntry<bool> EventNote;
        public static ConfigEntry<bool> EventScore;
        public static ConfigEntry<bool> EventTrackStart;
        public static ConfigEntry<bool> EventTrackEnd;
        public static ConfigEntry<bool> EventTrackComplete;
        public static ConfigEntry<bool> EventTrackFail;
        public static ConfigEntry<bool> EventTrackPause;
        public static ConfigEntry<bool> EventTrackResume;

        private static ConfigFile _configFile;

        private static ConfigEntry<T> Bind<T>(this ConfigFile config, string section, string key, T defaultValue, Action<T> callback)
        {
            ConfigEntry<T> entry = config.Bind(section, key, defaultValue);
            entry.SettingChanged += (sender, e) => callback(entry.Value);
            return entry;
        }

        public static void Init(ConfigFile config)
        {
            _configFile = config;

            ServerEnabled = config.Bind("General", "serverEnabled", true, OnServerEnabledChanged);
            ServerPort = config.Bind("General", "serverPort", 38304, OnServerPortChanged);
            SendImageData = config.Bind("General", "sendImageData", true);

            EventNote = config.Bind("Event Types", "noteHitMissed", true);
            EventScore = config.Bind("Event Types", "scoreChanged", true);
            EventTrackStart = config.Bind("Event Types", "trackStarted", true);
            EventTrackEnd = config.Bind("Event Types", "trackEnded", true);
            EventTrackComplete = config.Bind("Event Types", "trackCompleted", true);
            EventTrackFail = config.Bind("Event Types", "trackFailed", true);
            EventTrackPause = config.Bind("Event Types", "trackPaused", true);
            EventTrackResume = config.Bind("Event Types", "trackResumed", true);
        }

        public static void Reset()
        {
            if (_configFile == null) { return; }

            foreach (ConfigDefinition definition in _configFile.Keys)
            {
                ConfigEntryBase entry = _configFile[definition];
                entry.BoxedValue = entry.DefaultValue;
            }

            _configFile.Save();
        }

        private static void OnServerEnabledChanged(bool enabled)
        {
            if (enabled) { Plugin.Start(); }
            else { Plugin.Stop(); }
        }

        private static void OnServerPortChanged(int port)
        {
            Server.Stop();
            Server.Start(port);
        }
    }
}
