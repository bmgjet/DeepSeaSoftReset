/* ▄▄▄▄    ███▄ ▄███▓  ▄████  ▄▄▄██▀▀▀▓█████▄▄▄█████▓
  ▓█████▄ ▓██▒▀█▀ ██▒ ██▒ ▀█▒   ▒██   ▓█   ▀▓  ██▒ ▓▒
  ▒██▒ ▄██▓██    ▓██░▒██░▄▄▄░   ░██   ▒███  ▒ ▓██░ ▒░
  ▒██░█▀  ▒██    ▒██ ░▓█  ██▓▓██▄██▓  ▒▓█  ▄░ ▓██▓ ░ 
  ░▓█  ▀█▓▒██▒   ░██▒░▒▓███▀▒ ▓███▒   ░▒████▒ ▒██▒ ░ 
  ░▒▓███▀▒░ ▒░   ░  ░ ░▒   ▒  ▒▓▒▒░   ░░ ▒░ ░ ▒ ░░   
  ▒░▒   ░ ░  ░      ░  ░   ░  ▒ ░▒░    ░ ░  ░   ░    
   ░    ░ ░      ░   ░ ░   ░  ░ ░ ░      ░    ░      
   ░             ░         ░  ░   ░      ░  ░ */
using Newtonsoft.Json;
using Rust.Ai.Gen2;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
	[Info("DeepSeaSoftReset", "bmgjet", "1.0.0")]
	public class DeepSeaSoftReset : RustPlugin
	{
		private Coroutine _coroutine;
		private Dictionary<Vector3, RespawnInfo> Respawn = new Dictionary<Vector3, RespawnInfo>();
		private PluginConfig config;

		protected override void LoadDefaultConfig() => config = new PluginConfig();

		protected override void LoadConfig()
		{
			base.LoadConfig();
			try
			{
				config = Config.ReadObject<PluginConfig>();
				if (config == null) throw new JsonException();

				if (!config.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys))
				{
					PrintWarning("Configuration appears to be outdated; updating and saving");
					SaveConfig();
				}
			}
			catch
			{
				PrintWarning($"Configuration file {Name}.json is invalid; using defaults");
				LoadDefaultConfig();
			}
		}

		protected override void SaveConfig()
		{
			PrintWarning($"Configuration changes saved to {Name}.json");
			Config.WriteObject(config, true);
		}

		public class PluginConfig
		{
			public string ToJson() => JsonConvert.SerializeObject(this);
			public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());

			[JsonProperty("Check For Loot/Npc Respawn Ever (Mins)")]
			public int RespawnTimer { get; set; } = 15;

			[JsonProperty("Min Distance From Player To Allow Respawn")]
			public float MinDistance { get; set; } = 200;

			[JsonProperty("Respawn Hackable Create On Random Ghostship")]
			public bool RespawnHackable { get; set; } = true;

			[JsonProperty("Number Of Hackable Crates To Spawn")]
			public int hackablecrate_count { get; set; } = 1;

			[JsonProperty("Block Building In Deep Sea")]
			public bool block_building { get; set; } = true;

			[JsonProperty("Deep Sea Portal Edge (0 Map-based, 1 North, 2 East, 3 South, 4 West)")]
			public int forceentranceportaldirection { get; set; } = 0;

			[JsonProperty("Floating City Count")]
			public int floatingcity_count { get; set; } = 1;

			[JsonProperty("Deep Sea Island Count")]
			public int island_count { get; set; } = 6;

			[JsonProperty("Deep Sea Ghostship Count")]
			public int ghostship_count { get; set; } = 4;

			[JsonProperty("Deep Sea Rhib Count")]
			public int rhib_count { get; set; } = 4;

			[JsonProperty("Deep Sea Wipe Duration (Duration in seconds of the deep sea even)")]
			public int wipeduration { get; set; } = 10800;

			[JsonProperty("Deep Sea Wipe Cooldown (Seconds before a deep sea re-opens after closing)")]
			public int wipecooldown { get; set; } = 5400;

			[JsonProperty("Duration In Seconds Of The Final Wipe Phase")]
			public int wipeEndPhaseDuration { get; set; } = 5400;

			[JsonProperty("Seconds Before Radiation Starts To Ramp Up Before Deep Sea Wipe")]
			public int wipeRadiationPhaseDuration { get; set; } = 5400;

			[JsonProperty("Should The Deep Sea Map Be Covered By Fog Of War")]
			public bool deepseafogofwar { get; set; } = true;
		}

		private class RespawnInfo
		{
			public string prefabpath;
			public Quaternion rot;
			public BaseEntity parent;
			public bool hackable;
		}

		[ChatCommand("deepseasoftreset")]
		private void softreset(BasePlayer player) //Help chat command
		{
			if (player.IsAdmin)
			{
				player.ChatMessage("Running Soft Reset");
				ServerMgr.Instance.StartCoroutine(_reset());
			}
		}

		void Init() { SetVars(); }

		private void OnServerInitialized() { SetVars(); timer.Once(10f, () => { _coroutine = ServerMgr.Instance.StartCoroutine(_checker()); }); } //Startup

		private void OnEntityKill(StorageContainer bn)
		{
			if (bn == null || bn?.OwnerID != 0 || !DeepSeaManager.IsInsideDeepSea(bn))
				return;

			if (bn?.GetParentEntity() is BaseBoat) { return; }

			var pos = bn.transform.position;
			if (Respawn.ContainsKey(pos))
				return;

			Respawn[pos] = new RespawnInfo
			{
				prefabpath = bn.PrefabName,
				rot = bn.transform.rotation,
				parent = bn.GetParentEntity(),
				hackable = bn is HackableLockedCrate,
			};
		}

		private void OnEntityKill(ScientistNPC2 npc)
		{
			if (npc == null || !npc.IsOnMovingObject() || !DeepSeaManager.IsInsideDeepSea(npc))
				return;

			var pos = npc.transform.position;
			if (Respawn.ContainsKey(pos))
				return;

			Respawn[pos] = new RespawnInfo
			{
				prefabpath = npc.PrefabName,
				rot = npc.transform.rotation,
				parent = npc.GetParentEntity(),
				hackable = false,
			};
		}

		private void OnDeepSeaClosed() { Respawn.Clear(); }

		private void OnDeepSeaOpened() { Respawn.Clear(); }

		private void Unload() { if (_coroutine != null) { ServerMgr.Instance.StopCoroutine(_coroutine); }; Respawn.Clear(); }

		private void SetVars()
		{
			Debug.LogWarning("Setting Deepsea Convars");
			ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), $"deepsea.hackablecrate_count {config.hackablecrate_count}", System.Array.Empty<object>());
			ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), $"deepsea.block_building {config.block_building}", System.Array.Empty<object>());
			ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), $"deepsea.forceentranceportaldirection {config.block_building}", System.Array.Empty<object>());
			ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), $"deepsea.floatingcity_count {config.floatingcity_count}", System.Array.Empty<object>());
			ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), $"deepsea.island_count {config.island_count}", System.Array.Empty<object>());
			ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), $"deepsea.ghostship_count {config.ghostship_count}", System.Array.Empty<object>());
			ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), $"deepsea.rhib_count {config.rhib_count}", System.Array.Empty<object>());
			ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), $"deepsea.wipeduration {config.wipeduration}", System.Array.Empty<object>());
			ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), $"deepsea.wipecooldown {config.wipecooldown}", System.Array.Empty<object>());
			ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), $"deepsea.deepseafogofwar {config.deepseafogofwar}", System.Array.Empty<object>());
			ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), $"deepsea.wipeEndPhaseDuration {config.wipeEndPhaseDuration}", System.Array.Empty<object>());
			ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), $"deepsea.wipeRadiationPhaseDuration {config.wipeRadiationPhaseDuration}", System.Array.Empty<object>());
		}

		private IEnumerator _reset()
		{
			int checks = 0;
			List<Vector3> removed = Facepunch.Pool.Get<List<Vector3>>();
			foreach (var ent in Respawn)
			{
				if (++checks >= 100)
				{
					yield return CoroutineEx.waitForSeconds(0.0035f); //Wait a tick
					checks = 0;
				}
				if (ent.Value.hackable) { if (config.RespawnHackable) { if (SpawnGhostShipHackableCrate()) { removed.Add(ent.Key); } } else { removed.Add(ent.Key); } }
				else
				{
					if (!BaseNetworkable.HasCloseConnections(ent.Key, config.MinDistance))
					{
						var entity = GameManager.server.CreateEntity(ent.Value.prefabpath, ent.Key, ent.Value.rot);
						if (entity != null)
						{
							entity.Spawn();
							if (ent.Value.parent != null) { entity.SetParent(ent.Value.parent, true, true); }
							removed.Add(ent.Key);
						}
					}
				}
			}
			Puts("Respawned " + removed.Count + " entites.");
			if (removed.Count > 0) { foreach (var r in removed) { Respawn.Remove(r); } }
			Facepunch.Pool.FreeUnmanaged(ref removed);
		}

		private IEnumerator _checker()
		{
			while (true)
			{
				Puts("Checking for respawn! " + Respawn.Count + " entites.");
				if (Respawn.Count > 0) { yield return _reset(); }
				yield return CoroutineEx.waitForSeconds((int)(config.RespawnTimer * 60));
			}
		}

		private bool SpawnGhostShipHackableCrate()
		{
			int num = Mathf.Min(ConVar.DeepSea.hackablecrate_count, DeepSeaManager.ServerGhostShips.Count);
			if (num <= 0) { return false; }
			List<Prefabs.Misc.GhostShip> list = Facepunch.Pool.Get<List<Prefabs.Misc.GhostShip>>();
			list.AddRange(DeepSeaManager.ServerGhostShips.Values);
			ListEx.Shuffle(list, (uint)Random.Range(0, 100));
			var ghostShip = list[0];
			bool spawned = false;
			if (!BaseNetworkable.HasCloseConnections(ghostShip.transform.position, config.MinDistance)) //Distance check
			{
				ghostShip.SpawnHackableLockedCrate();
				spawned = true;
			}
			Facepunch.Pool.FreeUnmanaged(ref list);
			return spawned;
		}
	}
}