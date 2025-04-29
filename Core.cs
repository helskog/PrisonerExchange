using ProjectM;
using ProjectM.Scripting;

using Unity.Entities;

internal static class Core
{
	public static World Server { get; } = GetWorld() ?? throw new System.Exception("There is no Server world (yet). Did you install a server mod on the client?");

	public static EntityManager EntityManager { get; } = Server.EntityManager;
	public static GameDataSystem GameDataSystem { get; } = Server.GetExistingSystemManaged<GameDataSystem>();
	public static DebugEventsSystem DebugEventsSystem { get; internal set; }
	public static ServerScriptMapper ServerScriptMapper { get; internal set; }

	private static bool hasInitialized;

	public static void Initialize()
	{
		if (hasInitialized) return;

		DebugEventsSystem = Server.GetExistingSystemManaged<DebugEventsSystem>();
		ServerScriptMapper = Server.GetExistingSystemManaged<ServerScriptMapper>();

		hasInitialized = true;
	}

	private static World GetWorld()
	{
		foreach (var world in World.s_AllWorlds)
		{
			if (world.Name == "Server")
			{
				return world;
			}
		}

		return null;
	}
}