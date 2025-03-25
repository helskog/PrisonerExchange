using BepInEx.Logging;

namespace PrisonerExchange.Extensions;

public static class Log
{
	public static void Info(this ManualLogSource logger, string context, string message)
	{
		logger.LogInfo($"[{context}] {message}");
	}

	public static void Message(this ManualLogSource logger, string context, string message)
	{
		logger.LogMessage($"[{context}] {message}");
	}

	public static void Warning(this ManualLogSource logger, string context, string message)
	{
		logger.LogWarning($"[{context}] {message}");
	}

	public static void Error(this ManualLogSource logger, string context, string message)
	{
		logger.LogError($"[{context}] {message}");
	}
}