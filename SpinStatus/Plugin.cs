using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SpinCore;

namespace SpinStatus
{
    [BepInProcess("SpinRhythm.exe")]
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(SpinCorePlugin.Guid, SpinCorePlugin.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "xyz.bacur.plugins.spinstatus";
        public const string Name = "SpinStatus";
        public const string Version = "0.4.1";

        internal static new ManualLogSource Logger;
        internal static Harmony Patcher;

        protected void Awake()
        {
            Logger = base.Logger;
            Patcher = new Harmony(Guid);
            SpinStatus.Config.Init(Config);
            Menu.Create();

            Start();
        }

        protected void OnDestroy()
        {
            Stop();
        }

        internal static void Start()
        {
            Server.Start(SpinStatus.Config.ServerPort.Value);

            Patcher.PatchAll(typeof(Patches.NoteEventHandler));
            Patcher.PatchAll(typeof(Patches.SceneEventHandler));
        }

        internal static void Stop()
        {
            Server.Stop();
            Patcher?.UnpatchSelf();
        }
    }
}
