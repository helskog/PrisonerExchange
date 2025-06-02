using ProjectM;
using ProjectM.Scripting;

using Unity.Entities;

internal static class Core
{
	public static World Server { get; } = GetWorld() ?? throw new System.Exception("There is no Server world (yet). Did you install a server mod on the client?");

	public static EntityManager EntityManager { get; } = Server.EntityManager;
	public static GameDataSystem GameDataSystem { get; } = Server.GetExistingSystemManaged<GameDataSystem>();
	public static DebugEventsSystem DebugEventsSystem { get; internal set; }
	public static ServerGameManager ServerGameManager => ServerScriptMapper.GetServerGameManager();
	public static PrefabCollectionSystem PrefabCollection { get; } = Server.GetExistingSystemManaged<PrefabCollectionSystem>();

	static ServerScriptMapper serverScriptMapper;
	public static ServerScriptMapper ServerScriptMapper
	{
		get
		{
			serverScriptMapper ??= Server.GetExistingSystemManaged<ServerScriptMapper>();
			return serverScriptMapper;
		}
	}

	private static bool hasInitialized;

	public static void Initialize()
	{
		if (hasInitialized) return;

		DebugEventsSystem = Server.GetExistingSystemManaged<DebugEventsSystem>();

		hasInitialized = true;
	}

	private static World? GetWorld()
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