using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine;
public class GameState : MonoBehaviour
{
	public delegate void RemoteEventCallback();
	private static bool initialized = false;
	public static bool invertLook = false;
	public static float sensitivity = 0.5f;
	public static PlayerInfo localPlayerInfo = new PlayerInfo();
	public static PlayerModelData[] models;
	public static Inventory localPlayerInventory = new Inventory();
	public static QuestState localQuestState = new QuestState();
	public static string currentMusic;
	public static string previousExit = "";
	public static string newMapName;
	public static string newSpectatorHost;
	public static int newSpectatorPort;
	public PlayerModelData[] defaultModels;
	public WeaponModelData[] weaponModels;
	private static GameState currentObject;
	public static MasterServerConnection masterServer = null;
	public static GameServerConnection gameServer = null;
	public static bool isServer = false;
	public static bool isArena = false;
	public static bool isSpectator = false;
	public static bool allowSpectator = true;
	private float commitTimer = 60f;
	private float assignmentCheckTimer = 10f;
	private float keepAliveTimer = 10f;
	private static Queue<GameState.RemoteEventCallback> remoteEvents = new Queue<GameState.RemoteEventCallback>();
	private Dictionary<int, ReplicatedObject> replicatedObjects = new Dictionary<int, ReplicatedObject>();
	private int nextReplicatedObjectId = 1;
	public GameObject[] possibleReplicatedObjects;
	private Dictionary<string, GameObject> possibleReplicatedObjectsByName = new Dictionary<string, GameObject>();
	public static GameState instance
	{
		get
		{
			return GameState.currentObject;
		}
	}
	private void Awake()
	{
		GameState.currentObject = this;
		GameObject[] array = this.possibleReplicatedObjects;
		for (int i = 0; i < array.Length; i++)
		{
			GameObject gameObject = array[i];
			Enemy component = gameObject.GetComponent<Enemy>();
			if (component != null)
			{
				this.possibleReplicatedObjectsByName.Add(component.enemyName, gameObject);
			}
			Explosive component2 = gameObject.GetComponent<Explosive>();
			if (component2 != null)
			{
				this.possibleReplicatedObjectsByName.Add(component2.itemName, gameObject);
			}
		}
	}
	private void Start()
	{
		if (!GameState.initialized)
		{
			GameState.invertLook = (PlayerPrefs.GetInt("InvertLook", 0) != 0);
			GameState.sensitivity = PlayerPrefs.GetFloat("Sensitivity", 0.5f);
			GameState.models = this.defaultModels;
			PlayerModelData[] array = GameState.models;
			for (int i = 0; i < array.Length; i++)
			{
				PlayerModelData playerModelData = array[i];
				UnityEngine.Object.DontDestroyOnLoad(playerModelData.gameObject);
			}
			GameState.initialized = true;
		}
		if (!GameState.isServer && GameState.IsGameServerConnected())
		{
			Player localPlayer = LocalPlayerEvents.localPlayer;
			if (localPlayer != null)
			{
				ReplicatedObject replicatedObject = localPlayer.gameObject.AddComponent<ReplicatedObject>();
				replicatedObject.replicationName = GameState.localPlayerInfo.name;
				replicatedObject.remotePosition = localPlayer.transform.position;
				replicatedObject.remoteOrientation = localPlayer.transform.rotation;
				replicatedObject.id = GameState.localPlayerInfo.replicatedPlayerId;
				object obj = this.replicatedObjects;
				Monitor.Enter(obj);
				try
				{
					this.replicatedObjects.Add(replicatedObject.id, replicatedObject);
				}
				finally
				{
					Monitor.Exit(obj);
				}
			}
		}
	}
	private void Update()
	{
		if (GameState.masterServer != null)
		{
			this.keepAliveTimer -= Time.deltaTime;
			if (this.keepAliveTimer <= 0f)
			{
				this.keepAliveTimer = 10f;
				GameState.masterServer.SendKeepAlive();
			}
			GameState.masterServer.Update();
		}
		if (GameState.gameServer != null)
		{
			GameState.gameServer.Update();
		}
		if (GameState.isServer)
		{
			if (!GameState.IsMasterServerConnected())
			{
				UnityEngine.Debug.LogError("Server is disconnected from master server, aborting");
				Process.GetCurrentProcess().Kill();
			}
			this.commitTimer -= Time.deltaTime;
			if (this.commitTimer <= 0f)
			{
				this.commitTimer = 60f;
				GameObject[] array = GameObject.FindGameObjectsWithTag("Player");
				for (int i = 0; i < array.Length; i++)
				{
					GameObject gameObject = array[i];
					GameState.UpdateCharacterItems(gameObject.GetComponent<Player>().id, gameObject.GetComponent<Player>().inventory);
				}
			}
			if (Application.loadedLevelName != "Server")
			{
				this.assignmentCheckTimer -= Time.deltaTime;
				if (this.assignmentCheckTimer <= 0f)
				{
					this.assignmentCheckTimer = 10f;
					GameState.masterServer.IsServerAssignmentValid(Application.loadedLevelName, delegate(bool assigned)
					{
						if (!assigned)
						{
							Spectator.DisconnectAllSpectators();
							GameState.masterServer.FreeServerAssignment();
							Application.LoadLevel("Server");
						}
					});
				}
			}
			Player.CheckConnectionStatus();
			Spectator.CheckConnectionStatus();
		}
		object obj = GameState.remoteEvents;
		Monitor.Enter(obj);
		try
		{
			while (GameState.remoteEvents.Count > 0)
			{
				try
				{
					GameState.remoteEvents.Dequeue()();
				}
				catch (Exception ex)
				{
					if (GameState.isServer)
					{
						GameState.ServerLog(ex.ToString());
					}
					else
					{
						UnityEngine.Debug.Log(ex.ToString());
					}
				}
			}
		}
		finally
		{
			Monitor.Exit(obj);
		}
	}
	public static void UpdateCharacterItems(int id, Inventory inventory)
	{
		if (GameState.masterServer == null)
		{
			return;
		}
		if (!inventory.IsDirty())
		{
			return;
		}
		inventory.MarkAsUpdated();
		List<MasterServerConnection.InventoryItemData> list = new List<MasterServerConnection.InventoryItemData>();
		foreach (string current in inventory.GetItemList())
		{
			MasterServerConnection.InventoryItemData item;
			item.name = current;
			item.count = inventory.GetCountForItem(current);
			item.loadedAmmo = inventory.GetLoadedAmmoForWeapon(current);
			list.Add(item);
		}
		string primaryWeaponName = inventory.primaryWeaponName;
		string secondaryWeaponName = inventory.secondaryWeaponName;
		string swordGrenadeName = inventory.swordGrenadeName;
		string accessoryName = inventory.accessoryName;
		int activeSlot = inventory.activeSlot;
		GameState.masterServer.UpdateCharacterItems(id, list, primaryWeaponName, secondaryWeaponName, swordGrenadeName, accessoryName, activeSlot);
	}
	public static QuestManager GetQuestManager()
	{
		return GameState.currentObject.GetComponent<QuestManager>();
	}
	public static InventoryItemCollection GetInventoryItemCollection()
	{
		return GameState.currentObject.GetComponent<InventoryItemCollection>();
	}
	public static void ConnectToMasterServer(string host, int port, MasterServerConnection.ConnectCallback callback)
	{
		GameState.masterServer = new MasterServerConnection(host, port, delegate(bool ok, string errorMsg)
		{
			callback(ok, errorMsg);
			if (!ok)
			{
				GameState.DisconnectFromMasterServer();
			}
		});
		GameState.masterServer.Start();
	}
	public static void DisconnectFromMasterServer()
	{
		if (GameState.masterServer == null)
		{
			return;
		}
		UnityEngine.Debug.Log("Disconnected from master server");
		GameState.masterServer.Stop();
		GameState.masterServer = null;
	}
	public static bool IsMasterServerConnected()
	{
		return GameState.masterServer != null && GameState.masterServer.connected;
	}
	public static void ConnectToGameServer(string host, int port, GameServerConnection.ResultCallback callback)
	{
		GameState.gameServer = new GameServerConnection(host, port, delegate(bool ok)
		{
			callback(ok);
			if (!ok)
			{
				GameState.DisconnectFromGameServer();
			}
		});
		GameState.gameServer.Start();
	}
	public static void DisconnectFromGameServer()
	{
		if (GameState.gameServer == null)
		{
			return;
		}
		UnityEngine.Debug.Log("Disconnected from game server");
		GameState.gameServer.Stop();
		GameState.gameServer = null;
	}
	public static bool IsGameServerConnected()
	{
		return GameState.gameServer != null && GameState.gameServer.connected;
	}
	public static void OnPlayerDisconnect(Player player)
	{
		if (GameState.isServer)
		{
			GameState.UpdateCharacterItems(player.id, player.inventory);
		}
	}
	public static void ServerLog(string msg)
	{
		if (GameState.isServer && GameState.IsMasterServerConnected())
		{
			GameState.masterServer.ServerLog(msg);
		}
	}
	public static void QueueRemoteEvent(GameState.RemoteEventCallback callback)
	{
		object obj = GameState.remoteEvents;
		Monitor.Enter(obj);
		try
		{
			GameState.remoteEvents.Enqueue(callback);
		}
		finally
		{
			Monitor.Exit(obj);
		}
	}
	public ReplicatedObject AddServerReplicatedObject(string name, GameObject obj)
	{
		if (!GameState.isServer)
		{
			return null;
		}
		ReplicatedObject replicatedObject = obj.GetComponent<ReplicatedObject>();
		if (replicatedObject != null)
		{
			return replicatedObject;
		}
		replicatedObject = obj.AddComponent<ReplicatedObject>();
		replicatedObject.replicationName = name;
		replicatedObject.remotePosition = obj.transform.position;
		replicatedObject.remoteOrientation = obj.transform.rotation;
		do
		{
			replicatedObject.id = this.nextReplicatedObjectId++;
		}
		while (this.replicatedObjects.ContainsKey(replicatedObject.id));
		object obj2 = this.replicatedObjects;
		Monitor.Enter(obj2);
		try
		{
			this.replicatedObjects.Add(replicatedObject.id, replicatedObject);
		}
		finally
		{
			Monitor.Exit(obj2);
		}
		Player.SendUpdateToAllPlayers(GameServerUpdate.CreateObjectSpawnUpdate(replicatedObject));
		return replicatedObject;
	}
	public ReplicatedObject AddServerReplicatedPlayer(Player player, string name)
	{
		if (!GameState.isServer)
		{
			return null;
		}
		ReplicatedObject replicatedObject = player.GetComponent<ReplicatedObject>();
		if (replicatedObject != null)
		{
			return replicatedObject;
		}
		replicatedObject = player.gameObject.AddComponent<ReplicatedObject>();
		replicatedObject.replicationName = name;
		replicatedObject.remotePosition = player.transform.position;
		replicatedObject.remoteOrientation = player.transform.rotation;
		do
		{
			replicatedObject.id = this.nextReplicatedObjectId++;
		}
		while (this.replicatedObjects.ContainsKey(replicatedObject.id));
		object obj = this.replicatedObjects;
		Monitor.Enter(obj);
		try
		{
			this.replicatedObjects.Add(replicatedObject.id, replicatedObject);
		}
		finally
		{
			Monitor.Exit(obj);
		}
		Player.SendUpdateToAllPlayersExcept(player, GameServerUpdate.CreatePlayerJoinUpdate(replicatedObject));
		return replicatedObject;
	}
	public ReplicatedObject AddClientReplicatedObject(int id, string name, GameObject obj)
	{
		if (GameState.isServer)
		{
			return null;
		}
		ReplicatedObject replicatedObject = obj.GetComponent<ReplicatedObject>();
		if (replicatedObject != null)
		{
			replicatedObject.id = id;
			return replicatedObject;
		}
		replicatedObject = obj.AddComponent<ReplicatedObject>();
		replicatedObject.replicationName = name;
		replicatedObject.remotePosition = obj.transform.position;
		replicatedObject.remoteOrientation = obj.transform.rotation;
		replicatedObject.id = id;
		object obj2 = this.replicatedObjects;
		Monitor.Enter(obj2);
		try
		{
			this.replicatedObjects.Add(id, replicatedObject);
		}
		finally
		{
			Monitor.Exit(obj2);
		}
		return replicatedObject;
	}
	public void RemoveReplicatedObject(GameObject obj)
	{
		this.RemoveReplicatedObject(obj.GetComponent<ReplicatedObject>());
	}
	public void RemoveReplicatedObject(ReplicatedObject obj)
	{
		if (obj != null)
		{
			object obj2 = this.replicatedObjects;
			Monitor.Enter(obj2);
			try
			{
				this.replicatedObjects.Remove(obj.id);
			}
			finally
			{
				Monitor.Exit(obj2);
			}
			if (GameState.isServer)
			{
				Player.SendUpdateToAllPlayers(GameServerUpdate.CreateObjectDestroyUpdate(obj));
			}
		}
	}
	public void SendSpawnUpdatesForAllReplicatedObjects(ClientObject target)
	{
		foreach (ReplicatedObject current in this.replicatedObjects.Values)
		{
			if (current.gameObject != target.gameObject)
			{
				if (current.GetComponent<Player>() != null)
				{
					target.SendUpdate(GameServerUpdate.CreatePlayerJoinUpdate(current));
					target.SendUpdate(GameServerUpdate.CreateObjectHealthUpdate(current, current.GetComponent<Player>().health));
					target.SendUpdate(GameServerUpdate.CreateObjectStateUpdate(current, current.GetComponent<Player>().remoteWeapon));
					if (GameState.isArena)
					{
						target.SendUpdate(GameServerUpdate.CreateArenaPlayerScoreUpdate(current.GetComponent<Player>()));
					}
				}
				else
				{
					target.SendUpdate(GameServerUpdate.CreateObjectSpawnUpdate(current));
					if (current.GetComponent<Enemy>() != null)
					{
						target.SendUpdate(GameServerUpdate.CreateObjectHealthUpdate(current, current.GetComponent<Enemy>().health));
						current.GetComponent<Enemy>().SendStatesToPlayer(target);
					}
				}
			}
		}
	}
	public void SendPositionUpdates(ClientObject player)
	{
		object obj = this.replicatedObjects;
		Monitor.Enter(obj);
		try
		{
			foreach (ReplicatedObject current in this.replicatedObjects.Values)
			{
				if (!(current.gameObject == player.gameObject))
				{
					if (!(current.GetComponent<Enemy>() != null) || current.GetComponent<Enemy>().health > 0)
					{
						if (!(current.GetComponent<Player>() != null) || current.GetComponent<Player>().health > 0)
						{
							player.SendUpdate(GameServerUpdate.CreateObjectPositionUpdate(current));
						}
					}
				}
			}
		}
		finally
		{
			Monitor.Exit(obj);
		}
	}
	public ReplicatedObject GetReplicatedObjectById(int id)
	{
		object obj = this.replicatedObjects;
		Monitor.Enter(obj);
		ReplicatedObject result;
		try
		{
			if (!this.replicatedObjects.ContainsKey(id))
			{
				result = null;
			}
			else
			{
				result = this.replicatedObjects[id];
			}
		}
		finally
		{
			Monitor.Exit(obj);
		}
		return result;
	}
	public void SpawnClientReplicatedObject(int id, string name, Vector3 pos, Quaternion rot)
	{
		if (!this.possibleReplicatedObjectsByName.ContainsKey(name))
		{
			return;
		}
		GameObject original = this.possibleReplicatedObjectsByName[name];
		GameObject obj = (GameObject)UnityEngine.Object.Instantiate(original, pos, rot);
		this.AddClientReplicatedObject(id, name, obj);
		LocalPlayerEvents.localPlayer.SendMessage("OnQuestUpdated", SendMessageOptions.DontRequireReceiver);
	}
	public void SpawnClientReplicatedPlayer(int id, string name, string team, int avatar, Vector3 pos, Quaternion rot)
	{
		int num = (avatar >> 24 & 255) % this.defaultModels.Length;
		PlayerModelData playerModelData = this.defaultModels[num];
		int num2 = (avatar >> 8 & 15) % playerModelData.firstColorChoices.Length;
		Color firstColor = playerModelData.firstColorChoices[num2];
		int num3 = (avatar >> 4 & 15) % playerModelData.secondColorChoices.Length;
		Color secondColor = playerModelData.secondColorChoices[num3];
		int num4 = (avatar & 15) % playerModelData.thirdColorChoices.Length;
		Color thirdColor = playerModelData.thirdColorChoices[num4];
		Player player = Player.SpawnPlayerOnClient(id, name, team, playerModelData.model, firstColor, secondColor, thirdColor, pos, rot);
		this.AddClientReplicatedObject(id, name, player.gameObject);
	}
	public static WeaponModelData GetWeaponModelData(string name)
	{
		WeaponModelData[] array = GameState.instance.weaponModels;
		for (int i = 0; i < array.Length; i++)
		{
			WeaponModelData weaponModelData = array[i];
			if (weaponModelData.weaponName == name)
			{
				return weaponModelData;
			}
		}
		return null;
	}
}
