using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
public class Player : ClientObject
{
	private class AssistData
	{
		public Player player;
		public float timeLeft;
	}
	public bool isRemote = false;
	public int startingHealth = 100;
	public float healthRegenPerSecond = 1f;
	public int health;
	private float healthRegenTimer = 0f;
	public int id = -1;
	public Inventory inventory;
	public QuestState questState;
	public int avatar;
	public string team;
	private List<GameServerUpdate> updates = new List<GameServerUpdate>();
	public Vector3 remotePosition;
	public Quaternion remoteLookDirection = Quaternion.identity;
	public string remoteWeapon = "";
	public bool remoteJumpState = false;
	public int remoteCountdown = -1;
	public bool priorityCountdown = false;
	public int arenaTeamIndex = 0;
	public int arenaKills = 0;
	public int arenaAssists = 0;
	public int arenaDeaths = 0;
	public GameObject mapIcon = null;
	public ClientHandler clientHandler;
	private bool reloading = false;
	private int reloadClipCount;
	private float stopAttackTime = 0f;
	private float stopReloadTime = 0f;
	private float activeDamageReduction = 0f;
	private float activeDamageReductionTime = 0f;
	private Vector3 throttle = new Vector3(0f, 0f, 0f);
	private float jumpForce = 0f;
	public bool pvpEnabled = false;
	private bool pvpRequest = false;
	private int pvpZonesEntered = 0;
	private float pvpZoneCountdown;
	private float chatRateLimiter = 0f;
	private Dictionary<string, string> noQuestStateName = new Dictionary<string, string>();
	private List<Player.AssistData> assists = new List<Player.AssistData>();
	public string currentWeapon
	{
		get
		{
			if (this.inventory.activeSlot == 1)
			{
				return this.inventory.primaryWeaponName;
			}
			if (this.inventory.activeSlot == 2)
			{
				return this.inventory.secondaryWeaponName;
			}
			if (this.inventory.activeSlot == 3)
			{
				return this.inventory.swordGrenadeName;
			}
			if (this.inventory.activeSlot == 4)
			{
				return this.inventory.accessoryName;
			}
			return "";
		}
	}
	public string currentAmmoType
	{
		get
		{
			Item objectForItemName = this.inventory.GetObjectForItemName(this.currentWeapon);
			string result = "";
			if (objectForItemName != null)
			{
				result = objectForItemName.ammoType;
			}
			return result;
		}
	}
	public int ammo
	{
		get
		{
			return this.inventory.GetCountForItem(this.currentAmmoType);
		}
	}
	public int clip
	{
		get
		{
			if (this.reloading)
			{
				return this.reloadClipCount;
			}
			return this.inventory.GetLoadedAmmoForWeapon(this.currentWeapon);
		}
	}
	public int clipSize
	{
		get
		{
			Item objectForItemName = this.inventory.GetObjectForItemName(this.currentWeapon);
			if (objectForItemName != null)
			{
				return objectForItemName.clipSize;
			}
			return 0;
		}
	}
	private void InitialSpawn(string previousExit)
	{
		UnityEngine.Object[] array = UnityEngine.Object.FindObjectsOfType(typeof(Entrance));
		for (int i = 0; i < array.Length; i++)
		{
			UnityEngine.Object @object = array[i];
			Entrance entrance = (Entrance)@object;
			if (entrance.previousExit == previousExit)
			{
				this.RespawnAt(entrance.transform.position, entrance.transform.rotation);
				return;
			}
		}
		this.Respawn();
	}
	private void Start()
	{
		if (!this.isRemote)
		{
			this.InitialSpawn(GameState.previousExit);
		}
	}
	private void Update()
	{
		if (!GameState.isServer)
		{
			if (this.stopAttackTime > 0f)
			{
				this.stopAttackTime -= Time.deltaTime;
				if (this.stopAttackTime <= 0f)
				{
					Animator component = base.GetComponent<Animator>();
					if (component != null)
					{
						component.SetBool("attack", false);
					}
				}
			}
			if (this.stopReloadTime > 0f)
			{
				this.stopReloadTime -= Time.deltaTime;
				if (this.stopReloadTime <= 0f)
				{
					Animator component2 = base.GetComponent<Animator>();
					if (component2 != null)
					{
						component2.SetBool("reload", false);
					}
				}
			}
			if (this.mapIcon != null)
			{
				LocalPlayerEvents.UpdatePlayerMapIcon(this, base.GetComponent<ReplicatedObject>().replicationName);
			}
			if (this.isRemote)
			{
				bool flag = true;
				if (!GameState.isArena && !LocalPlayerEvents.localPlayer.pvpEnabled)
				{
					flag = false;
				}
				base.GetComponent<Collider>().isTrigger = !flag;
			}
			return;
		}
		if (this.health > 0 && this.health < this.startingHealth && this.healthRegenPerSecond > 0f)
		{
			this.healthRegenTimer -= Time.deltaTime;
			if (this.healthRegenTimer <= 0f)
			{
				this.healthRegenTimer = 1f / this.healthRegenPerSecond;
				this.health++;
				this.SendUpdate(GameServerUpdate.CreateLocalPlayerHealthUpdate(this.health));
				Player.SendUpdateToAllPlayersExcept(this, GameServerUpdate.CreateObjectHealthUpdate(base.GetComponent<ReplicatedObject>(), this.health));
			}
		}
		if (this.remoteWeapon != this.currentWeapon)
		{
			this.remoteWeapon = this.currentWeapon;
			Player.SendUpdateToAllPlayersExcept(this, GameServerUpdate.CreateObjectStateUpdate(base.GetComponent<ReplicatedObject>(), this.remoteWeapon));
		}
		if (GameState.isServer && this.isRemote)
		{
			Monitor.Enter(this);
			try
			{
				base.transform.position = this.remotePosition;
			}
			finally
			{
				Monitor.Exit(this);
			}
		}
		if (this.activeDamageReductionTime > 0f)
		{
			this.activeDamageReductionTime -= Time.deltaTime;
			if (this.activeDamageReductionTime <= 0f)
			{
				this.activeDamageReduction = 0f;
			}
		}
		for (int i = 0; i < this.assists.Count; i++)
		{
			this.assists[i].timeLeft -= Time.deltaTime;
			if (this.assists[i].timeLeft <= 0f)
			{
				this.assists.RemoveAt(i);
				i--;
			}
		}
		if (this.pvpEnabled)
		{
			if (this.pvpZonesEntered == 0 && this.pvpZoneCountdown > 0f)
			{
				this.pvpZoneCountdown -= Time.deltaTime;
				if (this.pvpZoneCountdown <= 0f)
				{
					this.pvpEnabled = false;
					if (!this.priorityCountdown)
					{
						this.remoteCountdown = -1;
						this.SendUpdate(GameServerUpdate.CreateCountdownUpdate(-1, ""));
					}
					Player.SendUpdateToAllPlayers(GameServerUpdate.CreateObjectAnimationUpdate(base.GetComponent<ReplicatedObject>(), "pvp", false, 0));
				}
				else
				{
					int num = (int)this.pvpZoneCountdown;
					if (!this.priorityCountdown && num != this.remoteCountdown)
					{
						this.remoteCountdown = num;
						this.SendUpdate(GameServerUpdate.CreateCountdownUpdate(num, "Leaving PvP Mode"));
					}
				}
			}
		}
		else
		{
			if (this.pvpZonesEntered > 0 && this.pvpZoneCountdown > 0f)
			{
				this.pvpZoneCountdown -= Time.deltaTime;
				if (this.pvpZoneCountdown <= 0f)
				{
					this.pvpEnabled = true;
					if (!this.priorityCountdown)
					{
						this.remoteCountdown = -1;
						this.SendUpdate(GameServerUpdate.CreateCountdownUpdate(-1, ""));
					}
					Player.SendUpdateToAllPlayers(GameServerUpdate.CreateObjectAnimationUpdate(base.GetComponent<ReplicatedObject>(), "pvp", true, 0));
				}
				else
				{
					int num2 = (int)this.pvpZoneCountdown;
					if (!this.priorityCountdown && num2 != this.remoteCountdown)
					{
						this.remoteCountdown = num2;
						this.SendUpdate(GameServerUpdate.CreateCountdownUpdate(num2, "Entering PvP Mode"));
					}
				}
			}
		}
		if (this.chatRateLimiter > 0f)
		{
			this.chatRateLimiter -= Time.deltaTime;
		}
	}
	public void Damage(Damage dmg)
	{
		if (this.health <= 0)
		{
			return;
		}
		bool flag = false;
		if (GameState.isArena)
		{
			Player player = null;
			if (dmg.source != null)
			{
				player = dmg.source.GetComponent<Player>();
			}
			if (player != null && player != this && player.arenaTeamIndex == this.arenaTeamIndex)
			{
				return;
			}
			flag = true;
		}
		else
		{
			if (dmg.source != null && dmg.source.GetComponent<Player>() != null)
			{
				flag = true;
				if (dmg.source.GetComponent<Player>() != this && (!this.pvpEnabled || !dmg.source.GetComponent<Player>().pvpEnabled))
				{
					return;
				}
			}
		}
		int num;
		if (flag && this.activeDamageReduction > 0f)
		{
			num = (int)(dmg.amount * 0.825f);
		}
		else
		{
			num = (int)(dmg.amount * (1f - this.activeDamageReduction));
		}
		if (num < 0)
		{
			num = 0;
		}
		this.health -= num;
		if (base.GetComponent<ReplicatedObject>() != null)
		{
			Player.SendUpdateToAllPlayersExcept(this, GameServerUpdate.CreateObjectHealthUpdate(base.GetComponent<ReplicatedObject>(), this.health));
		}
		if (GameState.isArena && dmg.source != null && dmg.source.GetComponent<Player>() != null)
		{
			Player component = dmg.source.GetComponent<Player>();
			bool flag2 = false;
			foreach (Player.AssistData current in this.assists)
			{
				if (current.player == component)
				{
					current.timeLeft = 5f;
					flag2 = true;
					break;
				}
			}
			if (!flag2)
			{
				Player.AssistData assistData = new Player.AssistData();
				assistData.player = component;
				assistData.timeLeft = 5f;
				this.assists.Add(assistData);
			}
		}
		if (this.health <= 0)
		{
			base.SendMessage("Die", null, SendMessageOptions.DontRequireReceiver);
			if (GameState.isArena)
			{
				if (!ArenaState.instance.IsGameComplete())
				{
					this.arenaDeaths++;
					Player.SendUpdateToAllPlayers(GameServerUpdate.CreateArenaPlayerScoreUpdate(this));
					Player player2 = null;
					if (dmg.source != null)
					{
						player2 = dmg.source.GetComponent<Player>();
					}
					if (player2 == null && this.assists.Count == 1)
					{
						player2 = this.assists[0].player;
					}
					if (player2 == null || player2 == this || (player2 != null && player2.arenaTeamIndex == this.arenaTeamIndex))
					{
						ArenaState.instance.Suicide(this);
					}
					else
					{
						player2.arenaKills++;
						ArenaState.instance.ScoreKill(player2);
						Player.SendUpdateToAllPlayers(GameServerUpdate.CreateArenaPlayerScoreUpdate(player2));
					}
					foreach (Player.AssistData current2 in this.assists)
					{
						if (current2.player != player2 && current2.player.arenaTeamIndex != this.arenaTeamIndex)
						{
							current2.player.arenaAssists++;
							Player.SendUpdateToAllPlayers(GameServerUpdate.CreateArenaPlayerScoreUpdate(current2.player));
						}
					}
				}
				this.assists.Clear();
			}
			if (dmg.source != null && dmg.source.GetComponent<Player>() != null)
			{
				Player component2 = dmg.source.GetComponent<Player>();
				if (component2 == this)
				{
					string text = "[8080ff]" + base.GetComponent<ReplicatedObject>().replicationName + "[-] can't throw a grenade.";
					Player.SendUpdateToAllPlayers(GameServerUpdate.CreateChatUpdate(text));
				}
				else
				{
					string text2;
					if (dmg.weapon == null)
					{
						text2 = "Large Trout";
					}
					else
					{
						text2 = dmg.weapon.itemDescription;
					}
					string text3 = string.Concat(new string[]
					{
						"[8080ff]",
						component2.GetComponent<ReplicatedObject>().replicationName,
						"[-] [[",
						text2,
						"] [ff4040]",
						base.GetComponent<ReplicatedObject>().replicationName,
						"[-]"
					});
					Player.SendUpdateToAllPlayers(GameServerUpdate.CreateChatUpdate(text3));
				}
			}
			else
			{
				if (dmg.source != null && dmg.source.GetComponent<Enemy>() != null)
				{
					Enemy component3 = dmg.source.GetComponent<Enemy>();
					string text4 = component3.killText.Replace("%", "[8080ff]" + base.GetComponent<ReplicatedObject>().replicationName + "[-]");
					Player.SendUpdateToAllPlayers(GameServerUpdate.CreateChatUpdate(text4));
				}
				else
				{
					if (Application.loadedLevelName == "Tomb")
					{
						string text5 = "[8080ff]" + base.GetComponent<ReplicatedObject>().replicationName + "[-] was not worthy.";
						Player.SendUpdateToAllPlayers(GameServerUpdate.CreateChatUpdate(text5));
					}
					else
					{
						string text6 = "[8080ff]" + base.GetComponent<ReplicatedObject>().replicationName + "[-] jumped off a cliff.";
						Player.SendUpdateToAllPlayers(GameServerUpdate.CreateChatUpdate(text6));
					}
				}
			}
			int num2 = this.inventory.GetCountForItem("Gold") / 10;
			this.inventory.RemoveItem("Gold", num2);
			if (dmg.source != null && dmg.source.GetComponent<Player>() != null && dmg.source.GetComponent<Player>() != this && num2 != 0)
			{
				dmg.source.GetComponent<Player>().inventory.AddItem("Gold", num2);
				dmg.source.GetComponent<Player>().SendUpdate(GameServerUpdate.CreateChatUpdate(string.Concat(new string[]
				{
					"You stole ",
					num2.ToString(),
					" gold from [8080ff]",
					base.GetComponent<ReplicatedObject>().replicationName,
					"[-]."
				})));
			}
			if (num2 != 0)
			{
				this.SendUpdate(GameServerUpdate.CreateChatUpdate("Death takes its toll of " + num2.ToString() + " gold."));
			}
		}
		this.SendUpdate(GameServerUpdate.CreateLocalPlayerHealthUpdate(this.health));
	}
	public void SetHealth(int val)
	{
		int num = this.health;
		this.health = val;
		if (num > 0 && this.health <= 0)
		{
			base.SendMessage("Die", null, SendMessageOptions.DontRequireReceiver);
		}
	}
	public void Respawn()
	{
		GameObject[] array;
		if (GameState.isArena)
		{
			if (this.arenaTeamIndex == 0)
			{
				array = GameObject.FindGameObjectsWithTag("BlackRespawn");
			}
			else
			{
				array = GameObject.FindGameObjectsWithTag("WhiteRespawn");
			}
		}
		else
		{
			array = GameObject.FindGameObjectsWithTag("Respawn");
		}
		GameObject gameObject = array[UnityEngine.Random.Range(0, array.Length)];
		this.RespawnAt(gameObject.transform.position, gameObject.transform.rotation);
	}
	public void RespawnAt(Vector3 pos, Quaternion rotation)
	{
		base.transform.position = pos;
		base.transform.rotation = rotation;
		this.health = this.startingHealth;
		base.SendMessage("ResetPosition", SendMessageOptions.DontRequireReceiver);
		this.SendUpdate(GameServerUpdate.CreateTeleportLocalPlayerUpdate(base.transform.position, base.transform.rotation));
		if (base.GetComponent<Animator>() != null)
		{
			base.GetComponent<Animator>().SetBool("dead", false);
		}
		if (GameState.isArena)
		{
			Item objectForItemName = this.inventory.GetObjectForItemName(this.inventory.primaryWeaponName);
			Item objectForItemName2 = this.inventory.GetObjectForItemName(this.inventory.secondaryWeaponName);
			if (objectForItemName != null)
			{
				Item objectForItemName3 = this.inventory.GetObjectForItemName(objectForItemName.ammoType);
				if (objectForItemName3 != null && this.inventory.GetCountForItem(objectForItemName.ammoType) < objectForItemName3.arenaMinimumCount)
				{
					this.inventory.AddItem(objectForItemName.ammoType, objectForItemName3.arenaMinimumCount - this.inventory.GetCountForItem(objectForItemName.ammoType));
				}
			}
			if (objectForItemName2 != null)
			{
				Item objectForItemName4 = this.inventory.GetObjectForItemName(objectForItemName2.ammoType);
				if (objectForItemName4 != null && this.inventory.GetCountForItem(objectForItemName2.ammoType) < objectForItemName4.arenaMinimumCount)
				{
					this.inventory.AddItem(objectForItemName2.ammoType, objectForItemName4.arenaMinimumCount - this.inventory.GetCountForItem(objectForItemName2.ammoType));
				}
			}
		}
	}
	public void TeleportTo(Vector3 pos, Quaternion rotation)
	{
		base.transform.position = pos;
		base.transform.rotation = rotation;
		base.SendMessage("ResetPosition", SendMessageOptions.DontRequireReceiver);
	}
	public override void SendUpdate(GameServerUpdate update)
	{
		if (GameState.isServer)
		{
			object obj = this.updates;
			Monitor.Enter(obj);
			try
			{
				this.updates.Add(update);
			}
			finally
			{
				Monitor.Exit(obj);
			}
		}
	}
	public void SendUpdateToTeammates(GameServerUpdate update)
	{
		if (GameState.isServer)
		{
			GameObject[] array = GameObject.FindGameObjectsWithTag("Player");
			for (int i = 0; i < array.Length; i++)
			{
				GameObject gameObject = array[i];
				Player component = gameObject.GetComponent<Player>();
				if (component != this && component.team == this.team)
				{
					component.SendUpdate(update);
				}
			}
		}
	}
	public static void SendUpdateToAllPlayers(GameServerUpdate update)
	{
		if (GameState.isServer)
		{
			GameObject[] array = GameObject.FindGameObjectsWithTag("Player");
			for (int i = 0; i < array.Length; i++)
			{
				GameObject gameObject = array[i];
				gameObject.GetComponent<Player>().SendUpdate(update);
			}
		}
		Spectator.SendUpdateToAllSpectators(update);
	}
	public static void SendUpdateToAllPlayersExcept(Player excluded, GameServerUpdate update)
	{
		if (GameState.isServer)
		{
			GameObject[] array = GameObject.FindGameObjectsWithTag("Player");
			for (int i = 0; i < array.Length; i++)
			{
				GameObject gameObject = array[i];
				if (gameObject != excluded.gameObject)
				{
					gameObject.GetComponent<Player>().SendUpdate(update);
				}
			}
		}
		Spectator.SendUpdateToAllSpectators(update);
	}
	public void AddUpdatesToMessage(GameServerMessage msg)
	{
		object obj = this.updates;
		Monitor.Enter(obj);
		try
		{
			foreach (GameServerUpdate current in this.updates)
			{
				msg.WriteUpdate(current);
			}
			this.updates.Clear();
		}
		finally
		{
			Monitor.Exit(obj);
		}
	}
	public static Player SpawnPlayerOnClient(int id, string name, string team, GameObject avatar, Color firstColor, Color secondColor, Color thirdColor, Vector3 pos, Quaternion rot)
	{
		GameObject gameObject = (GameObject)UnityEngine.Object.Instantiate(avatar);
		gameObject.AddComponent<Player>();
		gameObject.AddComponent<ThirdPersonAvatar>();
		gameObject.AddComponent<CapsuleCollider>();
		gameObject.GetComponent<CapsuleCollider>().isTrigger = false;
		gameObject.GetComponent<CapsuleCollider>().center = new Vector3(0f, 0.9f, 0f);
		gameObject.GetComponent<CapsuleCollider>().radius = 0.5f;
		gameObject.GetComponent<CapsuleCollider>().height = 1.8f;
		gameObject.GetComponent<CapsuleCollider>().direction = 1;
		GameObject gameObject2 = (GameObject)UnityEngine.Object.Instantiate(Resources.Load("PlayerIcon", typeof(GameObject)));
		gameObject2.transform.parent = gameObject.transform;
		gameObject2.transform.localPosition = Vector3.zero;
		IEnumerator enumerator = gameObject.transform.GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				Transform transform = (Transform)enumerator.Current;
				SkinnedMeshRenderer component = transform.GetComponent<SkinnedMeshRenderer>();
				if (!(component == null))
				{
					component.material.SetColor("_Color1", firstColor);
					component.material.SetColor("_Color2", secondColor);
					component.material.SetColor("_Color3", thirdColor);
				}
			}
		}
		finally
		{
			IDisposable disposable;
			if ((disposable = (enumerator as IDisposable)) != null)
			{
				disposable.Dispose();
			}
		}
		Player component2 = gameObject.GetComponent<Player>();
		component2.isRemote = true;
		component2.id = id;
		component2.team = team;
		component2.inventory = new Inventory();
		component2.inventory.owner = component2;
		component2.questState = new QuestState();
		component2.questState.owner = component2;
		component2.tag = "Player";
		component2.RespawnAt(pos, rot);
		component2.mapIcon = gameObject2;
		LocalPlayerEvents.UpdatePlayerMapIcon(component2, name);
		return component2;
	}
	public static Player SpawnPlayerOnServer(int id, string name, string team, int avatar, Inventory inventory, QuestState questState, string previousExit)
	{
		GameObject[] array = GameObject.FindGameObjectsWithTag("Player");
		for (int i = 0; i < array.Length; i++)
		{
			GameObject gameObject = array[i];
			Player component = gameObject.GetComponent<Player>();
			if (!(component == null))
			{
				ReplicatedObject component2 = gameObject.GetComponent<ReplicatedObject>();
				if (!(component2 == null))
				{
					if (!(component2.replicationName != name))
					{
						component.ForceDisconnectFromServer();
					}
				}
			}
		}
		GameObject gameObject2 = (GameObject)UnityEngine.Object.Instantiate(Resources.Load("RemotePlayer", typeof(GameObject)));
		Player component3 = gameObject2.GetComponent<Player>();
		component3.id = id;
		component3.team = team;
		component3.avatar = avatar;
		component3.inventory = inventory;
		component3.questState = questState;
		component3.tag = "Player";
		inventory.owner = component3;
		questState.owner = component3;
		component3.InitialSpawn(previousExit);
		GameState.instance.AddServerReplicatedPlayer(component3, name);
		GameState.instance.SendSpawnUpdatesForAllReplicatedObjects(component3);
		component3.SendUpdate(GameServerUpdate.CreateLoadCompleteUpdate());
		if (GameState.isArena)
		{
			component3.arenaTeamIndex = ArenaState.instance.AddPlayer(component3);
			Player.SendUpdateToAllPlayers(GameServerUpdate.CreateArenaPlayerScoreUpdate(component3));
			component3.SendUpdate(GameServerUpdate.CreateArenaTeamScoreUpdate(0, ArenaState.instance.GetTeamScore(0)));
			component3.SendUpdate(GameServerUpdate.CreateArenaTeamScoreUpdate(1, ArenaState.instance.GetTeamScore(1)));
		}
		return component3;
	}
	public void RemoveFromServer()
	{
		GameState.OnPlayerDisconnect(this);
		UnityEngine.Object.Destroy(base.gameObject);
		this.id = -1;
	}
	public static void CheckConnectionStatus()
	{
		GameObject[] array = GameObject.FindGameObjectsWithTag("Player");
		for (int i = 0; i < array.Length; i++)
		{
			GameObject gameObject = array[i];
			Player component = gameObject.GetComponent<Player>();
			if (component.clientHandler != null)
			{
				component.clientHandler.CheckConnectionStatus();
			}
		}
	}
	public void ForceDisconnectFromServer()
	{
		if (this.clientHandler != null)
		{
			this.clientHandler.ForceDisconnect();
		}
	}
	public bool StartReload()
	{
		if (this.reloading)
		{
			return false;
		}
		if (this.currentWeapon == "")
		{
			return false;
		}
		if (this.currentAmmoType == "")
		{
			return false;
		}
		int num = this.clipSize - this.clip;
		if (this.ammo < num)
		{
			num = this.ammo;
		}
		if (num == 0)
		{
			return false;
		}
		if (this.currentWeapon == "Boomstick")
		{
			num = 1;
		}
		if (this.inventory.GetCountForItem(this.currentAmmoType) < num)
		{
			return false;
		}
		this.reloadClipCount = this.clip;
		this.reloading = true;
		GameState.gameServer.Reload();
		return true;
	}
	public void StopReload()
	{
		this.reloading = false;
	}
	private void OnDisable()
	{
		if (GameState.isArena)
		{
			ArenaState.instance.RemovePlayer(this);
		}
		GameState.instance.RemoveReplicatedObject(base.gameObject);
	}
	public void SetAnimationState(string name, bool val, byte random)
	{
		if (name == "pvp")
		{
			this.pvpEnabled = val;
			return;
		}
		Animator component = base.GetComponent<Animator>();
		if (component != null)
		{
			component.SetFloat("random", (float)random / 256f);
			component.SetBool(name, val);
		}
		if (name == "attack")
		{
			this.stopAttackTime = 0.1f;
		}
		if (name == "reload")
		{
			this.stopReloadTime = 0.1f;
		}
		if (name == "attack" && val)
		{
			Item objectForItemName = this.inventory.GetObjectForItemName(this.remoteWeapon);
			if (objectForItemName != null && objectForItemName.fireSound != "")
			{
				LocalPlayerEvents.PlaySound(objectForItemName.fireSound, base.transform);
			}
			if (base.GetComponent<ThirdPersonAvatar>() != null)
			{
				base.GetComponent<ThirdPersonAvatar>().ShowMuzzleFlash();
			}
		}
	}
	public void SetJumpState(bool jumping)
	{
		if (GameState.isServer)
		{
			return;
		}
		if (this.remoteJumpState == jumping)
		{
			return;
		}
		this.remoteJumpState = jumping;
		GameState.gameServer.SetJumpState(jumping);
	}
	public string GetNoQuestStateName(string npc)
	{
		if (!this.noQuestStateName.ContainsKey(npc))
		{
			return "";
		}
		return this.noQuestStateName[npc];
	}
	public void SetNoQuestStateName(string npc, string state)
	{
		if (this.noQuestStateName.ContainsKey(npc))
		{
			this.noQuestStateName[npc] = state;
		}
		else
		{
			this.noQuestStateName.Add(npc, state);
		}
	}
	public void DrinkWine(int damageReduction)
	{
		if (GameState.isServer)
		{
			if (!this.inventory.RemoveItem("Wine", 1))
			{
				return;
			}
			this.activeDamageReduction = (float)damageReduction / 100f;
			this.activeDamageReductionTime = 60f;
		}
		else
		{
			GameState.gameServer.DrinkWine(damageReduction);
		}
	}
	public void ResetArenaGame()
	{
		this.arenaKills = 0;
		this.arenaAssists = 0;
		this.arenaDeaths = 0;
		Player.SendUpdateToAllPlayers(GameServerUpdate.CreateArenaPlayerScoreUpdate(this));
		this.Respawn();
		this.SendUpdate(GameServerUpdate.CreateLocalPlayerHealthUpdate(this.health));
		Player.SendUpdateToAllPlayersExcept(this, GameServerUpdate.CreateObjectRespawnUpdate(base.GetComponent<ReplicatedObject>(), base.transform.position, base.transform.rotation, this.health));
	}
	public void UpdateMovement(Vector2 movementControls, float slope, bool flying, bool running, bool sneaking)
	{
		float num;
		float num2;
		float d;
		if (sneaking)
		{
			num = ((!flying) ? 1f : 0f);
			num2 = 0.35f;
			d = 0.0084f;
		}
		else
		{
			if (running)
			{
				num = ((!flying) ? 1f : 0.9f);
				num2 = 0.15f;
				d = 0.0252f;
			}
			else
			{
				num = ((!flying) ? 1f : 0.35f);
				num2 = 0.17f;
				d = 0.018f;
			}
		}
		this.throttle += movementControls.y * (base.transform.TransformDirection(Vector3.forward * d * num) * slope);
		this.throttle += movementControls.x * (base.transform.TransformDirection(Vector3.right * d * num) * slope);
		this.throttle.x = this.throttle.x / (1f + num2 * num);
		this.throttle.z = this.throttle.z / (1f + num2 * num);
		this.throttle.x = ((Mathf.Abs(this.throttle.x) >= 0.0001f) ? this.throttle.x : 0f);
		this.throttle.y = ((Mathf.Abs(this.throttle.y) >= 0.0001f) ? this.throttle.y : 0f);
		this.throttle.z = ((Mathf.Abs(this.throttle.z) >= 0.0001f) ? this.throttle.z : 0f);
	}
	public void ApplyInitialJumpForce()
	{
		this.throttle.y = 0.11f;
	}
	public void UpdateJumpForce(bool jumpHeld)
	{
		if (jumpHeld)
		{
			this.jumpForce += 0.003f;
		}
		this.throttle.y = this.throttle.y + this.jumpForce;
		this.jumpForce /= 1.5f;
		this.throttle.y = this.throttle.y / 1.08f;
	}
	public void ResetJump()
	{
		this.throttle.y = 0f;
		this.jumpForce = 0f;
	}
	public void StopMovement()
	{
		this.throttle = Vector3.zero;
		this.jumpForce = 0f;
	}
	public void UpdateMovementWithForce(Vector3 force)
	{
		this.throttle += force;
	}
	public Vector3 GetMovementThrottle()
	{
		return this.throttle;
	}
	public void EnterPVPZone()
	{
		if (this.pvpZonesEntered++ == 0 && !this.pvpEnabled)
		{
			this.pvpZoneCountdown = 5f;
			if (!this.priorityCountdown)
			{
				this.remoteCountdown = 5;
				this.SendUpdate(GameServerUpdate.CreateCountdownUpdate(5, "Entering PvP Mode"));
			}
		}
		else
		{
			if (this.pvpEnabled && !this.priorityCountdown)
			{
				this.remoteCountdown = -1;
				this.SendUpdate(GameServerUpdate.CreateCountdownUpdate(-1, ""));
			}
		}
	}
	public void ExitPVPZone()
	{
		if (--this.pvpZonesEntered == 0 && this.pvpEnabled)
		{
			this.pvpZoneCountdown = 5f;
			if (!this.priorityCountdown)
			{
				this.remoteCountdown = 5;
				this.SendUpdate(GameServerUpdate.CreateCountdownUpdate(5, "Leaving PvP Mode"));
			}
		}
		else
		{
			if (!this.priorityCountdown)
			{
				this.remoteCountdown = -1;
				this.SendUpdate(GameServerUpdate.CreateCountdownUpdate(-1, ""));
			}
		}
	}
	public void TogglePVPRequest()
	{
		if (this.pvpRequest)
		{
			this.pvpRequest = false;
			this.ExitPVPZone();
		}
		else
		{
			this.pvpRequest = true;
			this.EnterPVPZone();
		}
	}
	public void SendChatMessage(string text)
	{
		if (this.chatRateLimiter > 5f)
		{
			return;
		}
		this.chatRateLimiter += 1f;
		text = text.Replace("[", "[[");
		text = text.Replace("\n", " ");
		if (text.Length > 100)
		{
			text = text.Substring(0, 100);
		}
		Player.SendUpdateToAllPlayers(GameServerUpdate.CreateChatUpdate("[8080ff]<" + base.GetComponent<ReplicatedObject>().replicationName + ">[-] " + text));
	}
}
