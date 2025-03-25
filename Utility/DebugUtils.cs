using Unity.Entities;

namespace PrisonerExchange.Utility;

public static class DebugUtils
{
	public static void ExploreEntity(Entity entity)
	{
		var sb = new Il2CppSystem.Text.StringBuilder();
		ProjectM.EntityDebuggingUtility.DumpEntity(Core.Server, entity, true, sb);
		Plugin.Logger.LogInfo(sb.ToString());
	}
}