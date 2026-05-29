using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace SpinStatus;

[BepInProcess("SpinRhythm.exe")]
[BepInPlugin(Guid, Name, Version)]
public class Plugin : BaseUnityPlugin
{
    public const string Guid = "xyz.bacur.plugins.spinstatus";
    public const string Name = "SpinStatus";
    public const string Version = "0.4.1";

    internal static new ManualLogSource Logger;
    private static Harmony _harmony;

    internal static Server.Socket server;
    public static int port = 38304;

    protected void Awake()
    {
        Logger = base.Logger;

        server = new Server.Socket(port);
        server.Start();

        Logger.LogInfo($"Server started on port {port}");

        _harmony = new Harmony(Guid);
        _harmony.PatchAll(typeof(Patches.NoteEventHandler));
        _harmony.PatchAll(typeof(Patches.SceneEventHandler));
    }

    protected void OnDestroy()
    {
        server?.Stop();
        _harmony.UnpatchSelf();
    }
}
