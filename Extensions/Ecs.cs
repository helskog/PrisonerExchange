﻿using Il2CppInterop.Runtime;
using System;
using System.Runtime.InteropServices;
using Unity.Entities;
using ProjectM;

namespace PrisonerExchange.Extensions
{
	internal static class Ecs
	{
		private static EntityManager Em => Core.EntityManager;

		public static bool Exists(this Entity entity)
		{
			return entity != Entity.Null && Em.Exists(entity);
		}

		public static bool HasValue(this Entity entity)
		{
			return entity != Entity.Null;
		}

		public static bool Has<T>(this Entity entity) where T : struct
		{
			return Em.HasComponent<T>(entity);
		}

		public static bool TryGetComponent<T>(this Entity entity, out T component) where T : struct
		{
			if (Em.HasComponent<T>(entity))
			{
				component = entity.Read<T>();
				return true;
			}
			component = default;
			return false;
		}

		public static unsafe T Read<T>(this Entity entity) where T : struct
		{
			return Em.GetComponentData<T>(entity);
		}

		public static void Write<T>(this Entity entity, T data) where T : struct
		{
			Em.SetComponentData(entity, data);
		}

		public static unsafe void WriteRaw<T>(this Entity entity, T data) where T : struct
		{
			// Example if you want to do SetComponentDataRaw
			var ct = new ComponentType(Il2CppType.Of<T>());
			byte[] bytes = StructureToByteArray(data);
			int size = Marshal.SizeOf<T>();

			fixed (byte* p = bytes)
			{
				Em.SetComponentDataRaw(entity, ct.TypeIndex, p, size);
			}
		}

		private static byte[] StructureToByteArray<T>(T structure) where T : struct
		{
			int size = Marshal.SizeOf(structure);
			byte[] byteArray = new byte[size];
			IntPtr ptr = Marshal.AllocHGlobal(size);

			try
			{
				Marshal.StructureToPtr(structure, ptr, true);
				Marshal.Copy(ptr, byteArray, 0, size);
			}
			finally
			{
				Marshal.FreeHGlobal(ptr);
			}
			return byteArray;
		}

		public static void Add<T>(this Entity entity) where T : struct
		{
			if (!entity.Has<T>())
			{
				Em.AddComponent<T>(entity);
			}
		}

		public static void Remove<T>(this Entity entity)
		{
			var ct = new ComponentType(Il2CppType.Of<T>());
			Core.EntityManager.RemoveComponent(entity, ct);
		}

		public static bool TryRemoveComponent<T>(this Entity entity) where T : struct
		{
			if (entity.Has<T>())
			{
				Em.RemoveComponent<T>(entity);
				return true;
			}
			return false;
		}

		public delegate void WithRefHandler<T>(ref T item);

		static void With<T>(this Entity entity, WithRefHandler<T> action) where T : struct
		{
			T item = entity.Read<T>();
			action(ref item);

			Em.SetComponentData(entity, item);
		}
		public static void AddWith<T>(this Entity entity, WithRefHandler<T> action) where T : struct
		{
			if (!entity.Has<T>())
			{
				entity.Add<T>();
			}

			entity.With(action);
		}
		public static void HasWith<T>(this Entity entity, WithRefHandler<T> action) where T : struct
		{
			if (entity.Has<T>())
			{
				entity.With(action);
			}
		}

		public static bool SameTeam(this Entity entityA, Entity entityB)
		{
			if (!entityA.Exists() || !entityB.Exists())
				return false;

			if (!entityA.Has<Team>() || !entityB.Has<Team>())
				return false;

			var teamA = entityA.Read<Team>();
			var teamB = entityB.Read<Team>();

			return teamA.Value == teamB.Value;
		}
	}
}