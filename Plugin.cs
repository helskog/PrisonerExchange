using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;

using HarmonyLib;

using PrisonerExchange.Extensions;

using ProjectM;

using VampireCommandFramework;

namespace PrisonerExchange;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("gg.deca.VampireCommandFramework")]
[BepInDependency("gg.deca.Bloodstone")]
[Bloodstone.API.Reloadable]
public class Plugin : BasePlugin
{
	public static ManualLogSource Logger;
	private static Harmony _harmony;
	public static Harmony Harmony => _harmony;

	internal static Plugin Instance { get; private set; }

	public override void Load()
	{
		Logger = Log;
		Instance = this;

		Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} version {MyPluginInfo.PLUGIN_VERSION} is loaded!");

		_harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
		_harmony.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());

		Configuration.Initialize(Config);

		CommandRegistry.RegisterAll();
	}

	public void OnGameInitialized()
	{
		if (!Core.Server.IsServerWorld())
		{
			Logger.Error("EntryPoint", "Plugin is not running on a server world!");
			return;
		}

		Core.Initialize();
	}

	public override bool Unload()
	{
		CommandRegistry.UnregisterAssembly();
		_harmony?.UnpatchSelf();
		return true;
	}
}