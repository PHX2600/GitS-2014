using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
public class ClientHandler
{
	private Thread thread;
	private Stream stream;
	private Player player = null;
	private Spectator spectator = null;
	private DateTime lastUpdate = DateTime.Now;
	public ClientHandler(Stream s)
	{
		this.stream = s;
		this.thread = new Thread(new ThreadStart(this.ClientThread));
		Console.Out.WriteLine("ClientHandler public init (Stream s)");
	}
	private void Login(BinaryReader reader)
	{
		Console.Out.WriteLine("Trying to login...");
		int id = reader.ReadInt32();
		string token = reader.ReadString();
		string previousExit = reader.ReadString();
		AutoResetEvent doneEvent = new AutoResetEvent(false);
		bool result = false;
		string charName = null;
		string team = null;
		int avatar = 0;
		GameState.masterServer.ValidateLoginToken(id, token, delegate(bool ok, string name, string t, int a)
		{
			result = ok;
			charName = name;
			team = t;
			avatar = a;
			doneEvent.Set();
		});
		if (!doneEvent.WaitOne(10000))
		{
			throw new IOException("Token validation timed out");
		}
		if (!result)
		{
			throw new ArgumentException("Invalid login information");
		}
		doneEvent = new AutoResetEvent(false);
		Inventory inventory = new Inventory();
		GameState.masterServer.GetCharacterItems(id, delegate(List<MasterServerConnection.InventoryItemData> items, string slot1, string slot2, string slot3, string slot4, int activeSlot)
		{
			inventory.UpdateFromServerList(items);
			inventory.primaryWeaponName = slot1;
			inventory.secondaryWeaponName = slot2;
			inventory.swordGrenadeName = slot3;
			inventory.accessoryName = slot4;
			inventory.activeSlot = activeSlot;
			doneEvent.Set();
		});
		if (!doneEvent.WaitOne(10000))
		{
			throw new IOException("Inventory query timed out");
		}
		doneEvent = new AutoResetEvent(false);
		QuestState state = new QuestState();
		GameState.masterServer.GetCharacterQuests(id, delegate(List<MasterServerConnection.QuestData> quests, string activeQuest)
		{
			state.UpdateFromServerList(quests, activeQuest);
			doneEvent.Set();
		});
		if (!doneEvent.WaitOne(10000))
		{
			throw new IOException("Quest list query timed out");
		}
		doneEvent = new AutoResetEvent(false);
		int playerId = -1;
		GameState.QueueRemoteEvent(delegate
		{
			this.player = Player.SpawnPlayerOnServer(id, charName, team, avatar, inventory, state, previousExit);
			this.player.clientHandler = this;
			playerId = this.player.GetComponent<ReplicatedObject>().id;
			GameState.GetQuestManager().OnEnterArea(this.player, Application.loadedLevelName);
			doneEvent.Set();
		});
		doneEvent.WaitOne();
		GameState.ServerLog("Character " + charName + " has connected");
		GameServerMessage gameServerMessage = new GameServerMessage(GameServerMessage.Command.ResponseCommand);
		BinaryWriter writer = gameServerMessage.GetWriter();
		writer.Write(result);
		if (result)
		{
			writer.Write(playerId);
		}
		gameServerMessage.Serialize(this.stream);
	}
	private void Spectate()
	{
		if (!GameState.allowSpectator)
		{
			throw new ArgumentException("Not enabled");
		}
		AutoResetEvent doneEvent = new AutoResetEvent(false);
		GameState.QueueRemoteEvent(delegate
		{
			this.spectator = Spectator.SpawnOnServer();
			this.spectator.clientHandler = this;
			doneEvent.Set();
		});
		doneEvent.WaitOne();
		GameState.ServerLog("Spectator has connected");
		GameServerMessage gameServerMessage = new GameServerMessage(GameServerMessage.Command.ResponseCommand);
		BinaryWriter writer = gameServerMessage.GetWriter();
		writer.Write(true);
		gameServerMessage.Serialize(this.stream);
	}
	private void Update(BinaryReader reader)
	{
		Vector3 remotePosition;
		remotePosition.x = reader.ReadSingle();
		remotePosition.y = reader.ReadSingle();
		remotePosition.z = reader.ReadSingle();
		Vector3 euler;
		euler.x = (float)reader.ReadByte() * 360f / 256f;
		euler.y = (float)reader.ReadByte() * 360f / 256f;
		euler.z = (float)reader.ReadByte() * 360f / 256f;
		object obj = this.player;
		Monitor.Enter(obj);
		try
		{
			this.player.remotePosition = remotePosition;
			this.player.remoteLookDirection = Quaternion.Euler(euler);
		}
		finally
		{
			Monitor.Exit(obj);
		}
		GameState.instance.SendPositionUpdates(this.player);
		GameServerMessage gameServerMessage = new GameServerMessage(GameServerMessage.Command.ResponseCommand);
		this.player.AddUpdatesToMessage(gameServerMessage);
		gameServerMessage.Serialize(this.stream);
	}
	private void SpectatorUpdate()
	{
		GameState.instance.SendPositionUpdates(this.spectator);
		GameServerMessage gameServerMessage = new GameServerMessage(GameServerMessage.Command.ResponseCommand);
		this.spectator.AddUpdatesToMessage(gameServerMessage);
		gameServerMessage.Serialize(this.stream);
	}
	private void PrepareAttack()
	{
		GameState.QueueRemoteEvent(delegate
		{
			Player.SendUpdateToAllPlayersExcept(this.player, GameServerUpdate.CreateObjectAnimationUpdate(this.player.GetComponent<ReplicatedObject>(), "attack", true, (byte)UnityEngine.Random.Range(0, 256)));
		});
	}
	private void Attack(BinaryReader reader)
	{
		Vector3 origin;
		origin.x = reader.ReadSingle();
		origin.y = reader.ReadSingle();
		origin.z = reader.ReadSingle();
		int count = reader.ReadInt32();
		if (count < 0 || count > 32)
		{
			throw new ArgumentException("Bad attack count");
		}
		Ray[] rays = new Ray[count];
		int[] hitIds = new int[count];
		GameObject[] hits = new GameObject[count];
		for (int i = 0; i < count; i++)
		{
			Vector3 direction;
			direction.x = reader.ReadSingle();
			direction.y = reader.ReadSingle();
			direction.z = reader.ReadSingle();
			rays[i] = new Ray(origin, direction);
			hitIds[i] = reader.ReadInt32();
		}
		GameState.QueueRemoteEvent(delegate
		{
			Item objectForItemName = this.player.inventory.GetObjectForItemName(this.player.currentWeapon);
			if (objectForItemName == null)
			{
				return;
			}
			Weapon component = objectForItemName.GetComponent<Weapon>();
			if (!(component == null))
			{
				for (int j = 0; j < count; j++)
				{
					ReplicatedObject replicatedObjectById = GameState.instance.GetReplicatedObjectById(hitIds[j]);
					if (replicatedObjectById != null)
					{
						hits[j] = replicatedObjectById.gameObject;
					}
					else
					{
						hits[j] = null;
					}
				}
				component.PerformAttack(this.player, rays, hits);
				return;
			}
			ThrownWeapon component2 = objectForItemName.GetComponent<ThrownWeapon>();
			if (component2 == null)
			{
				return;
			}
			if (rays.Length != 1)
			{
				return;
			}
			component2.PerformAttack(this.player, rays[0]);
		});
	}
	private void Jump(BinaryReader reader)
	{
		bool jumping = reader.ReadBoolean();
		GameState.QueueRemoteEvent(delegate
		{
			if (this.player.remoteJumpState != jumping)
			{
				this.player.remoteJumpState = jumping;
				Player.SendUpdateToAllPlayersExcept(this.player, GameServerUpdate.CreateObjectAnimationUpdate(this.player.GetComponent<ReplicatedObject>(), "jump", jumping, 0));
			}
		});
	}
	private void SwitchWeapon(BinaryReader reader)
	{
		int slot = reader.ReadInt32();
		if (slot < 0 || slot > 4)
		{
			return;
		}
		GameState.QueueRemoteEvent(delegate
		{
			this.player.inventory.activeSlot = slot;
			this.player.inventory.SetDirty();
		});
	}
	private void SetWeaponSlots(BinaryReader reader)
	{
		string slot1 = reader.ReadString();
		string slot2 = reader.ReadString();
		string slot3 = reader.ReadString();
		string slot4 = reader.ReadString();
		if (slot1 == slot2)
		{
			slot2 = "";
		}
		if (slot1 == slot3)
		{
			slot3 = "";
		}
		if (slot1 == slot4)
		{
			slot4 = "";
		}
		if (slot2 == slot3)
		{
			slot3 = "";
		}
		if (slot2 == slot4)
		{
			slot4 = "";
		}
		if (slot3 == slot4)
		{
			slot4 = "";
		}
		GameState.QueueRemoteEvent(delegate
		{
			if (this.player.inventory.GetCountForItem(slot1) <= 0)
			{
				slot1 = "";
			}
			if (this.player.inventory.GetCountForItem(slot2) <= 0)
			{
				slot2 = "";
			}
			if (this.player.inventory.GetCountForItem(slot3) <= 0)
			{
				slot3 = "";
			}
			if (this.player.inventory.GetCountForItem(slot4) <= 0)
			{
				slot4 = "";
			}
			this.player.inventory.primaryWeaponName = slot1;
			this.player.inventory.secondaryWeaponName = slot2;
			this.player.inventory.swordGrenadeName = slot3;
			this.player.inventory.accessoryName = slot4;
			this.player.inventory.SetDirty();
		});
	}
	private void GetObjectLoot(BinaryReader reader)
	{
		int id = reader.ReadInt32();
		LootCollection loot = null;
		AutoResetEvent doneEvent = new AutoResetEvent(false);
		GameState.QueueRemoteEvent(delegate
		{
			try
			{
				ReplicatedObject replicatedObjectById = GameState.instance.GetReplicatedObjectById(id);
				if (replicatedObjectById != null)
				{
					LootableObject component = replicatedObjectById.GetComponent<LootableObject>();
					if (component != null)
					{
						loot = component.collection;
						if (loot != null && loot.owner != null && loot.owner != this.player)
						{
							loot = null;
						}
					}
				}
				doneEvent.Set();
			}
			catch (Exception ex)
			{
				GameState.ServerLog(ex.ToString());
			}
		});
		doneEvent.WaitOne();
		GameServerMessage gameServerMessage = new GameServerMessage(GameServerMessage.Command.ResponseCommand);
		BinaryWriter writer = gameServerMessage.GetWriter();
		if (loot == null)
		{
			writer.Write(0);
		}
		else
		{
			writer.Write(loot.items.Count);
			foreach (string current in loot.items.Keys)
			{
				writer.Write(current);
				writer.Write(loot.items[current]);
			}
		}
		gameServerMessage.Serialize(this.stream);
	}
	private void TakeObjectLoot(BinaryReader reader)
	{
		int id = reader.ReadInt32();
		GameState.QueueRemoteEvent(delegate
		{
			ReplicatedObject replicatedObjectById = GameState.instance.GetReplicatedObjectById(id);
			if (replicatedObjectById != null)
			{
				replicatedObjectById.gameObject.SendMessage("OnLooted", this.player, SendMessageOptions.DontRequireReceiver);
			}
		});
	}
	private void IsNamedObjectLootAvailable(BinaryReader reader)
	{
		string name = reader.ReadString();
		AutoResetEvent doneEvent = new AutoResetEvent(false);
		bool result = false;
		GameState.masterServer.IsNamedObjectLootAvailable(this.player.id, name, delegate(bool ok)
		{
			result = ok;
			doneEvent.Set();
		});
		if (!doneEvent.WaitOne(10000))
		{
			throw new IOException("Request timed out");
		}
		GameServerMessage gameServerMessage = new GameServerMessage(GameServerMessage.Command.ResponseCommand);
		BinaryWriter writer = gameServerMessage.GetWriter();
		writer.Write(result);
		gameServerMessage.Serialize(this.stream);
	}
	private void TakeNamedObjectLoot(BinaryReader reader)
	{
		string name = reader.ReadString();
		GameState.QueueRemoteEvent(delegate
		{
			try
			{
				ChestLoot[] array = (ChestLoot[])UnityEngine.Object.FindObjectsOfType(typeof(ChestLoot));
				for (int i = 0; i < array.Length; i++)
				{
					ChestLoot chestLoot = array[i];
					if (chestLoot.lootName == name)
					{
						chestLoot.OnLooted(this.player);
						break;
					}
				}
			}
			catch (Exception ex)
			{
				GameState.ServerLog(ex.ToString());
			}
		});
	}
	private void Buy(BinaryReader reader)
	{
		string npcName = reader.ReadString();
		string itemName = reader.ReadString();
		int count = reader.ReadInt32();
		GameState.QueueRemoteEvent(delegate
		{
			try
			{
				bool flag = false;
				NPC[] array = (NPC[])UnityEngine.Object.FindObjectsOfType(typeof(NPC));
				int i = 0;
				while (i < array.Length)
				{
					NPC nPC = array[i];
					if (nPC.npcName == npcName)
					{
						NPCQuestText currentQuestText = nPC.GetCurrentQuestText(this.player);
						if (currentQuestText == null)
						{
							break;
						}
						StaticLootCollection component = currentQuestText.GetComponent<StaticLootCollection>();
						if (component == null)
						{
							break;
						}
						foreach (string current in component.items.Keys)
						{
							if (current == itemName)
							{
								flag = true;
							}
						}
						break;
					}
					else
					{
						i++;
					}
				}
				if (flag)
				{
					Item objectForItemName = this.player.inventory.GetObjectForItemName(itemName);
					for (int j = 0; j < count; j++)
					{
						if (objectForItemName.maximumCount > 0 && this.player.inventory.GetCountForItem(itemName) >= objectForItemName.maximumCount)
						{
							break;
						}
						if (!this.player.inventory.RemoveItem("Gold", objectForItemName.sellValue))
						{
							break;
						}
						this.player.inventory.AddItem(itemName, 1);
					}
				}
			}
			catch (Exception ex)
			{
				GameState.ServerLog(ex.ToString());
			}
		});
	}
	private void Sell(BinaryReader reader)
	{
		string itemName = reader.ReadString();
		int count = reader.ReadInt32();
		GameState.QueueRemoteEvent(delegate
		{
			try
			{
				bool flag = true;
				Item objectForItemName = this.player.inventory.GetObjectForItemName(itemName);
				if (objectForItemName == null)
				{
					flag = false;
				}
				else
				{
					if (objectForItemName.bound || !objectForItemName.showInInventoryScreen)
					{
						flag = false;
					}
					else
					{
						if (objectForItemName.itemType == Item.ItemType.Flag)
						{
							flag = false;
						}
						else
						{
							if (objectForItemName.itemType == Item.ItemType.QuestItem)
							{
								flag = false;
							}
						}
					}
				}
				if (flag)
				{
					for (int i = 0; i < count; i++)
					{
						if (!this.player.inventory.RemoveItem(itemName, 1))
						{
							break;
						}
						this.player.inventory.AddItem("Gold", objectForItemName.sellValue);
					}
				}
			}
			catch (Exception ex)
			{
				GameState.ServerLog(ex.ToString());
			}
		});
	}
	private void QuestKill(BinaryReader reader)
	{
		string enemyName = reader.ReadString();
		GameState.QueueRemoteEvent(delegate
		{
			GameState.GetQuestManager().OnKillEnemy(this.player, enemyName);
		});
	}
	private void NPCTalk(BinaryReader reader)
	{
		string npcName = reader.ReadString();
		NPCQuestText text = null;
		AutoResetEvent doneEvent = new AutoResetEvent(false);
		GameState.QueueRemoteEvent(delegate
		{
			try
			{
				NPC[] array = (NPC[])UnityEngine.Object.FindObjectsOfType(typeof(NPC));
				for (int i = 0; i < array.Length; i++)
				{
					NPC nPC = array[i];
					if (nPC.npcName == npcName)
					{
						text = nPC.GetCurrentQuestText(this.player);
						break;
					}
				}
				doneEvent.Set();
			}
			catch (Exception ex)
			{
				GameState.ServerLog(ex.ToString());
			}
		});
		doneEvent.WaitOne();
		GameServerMessage gameServerMessage = new GameServerMessage(GameServerMessage.Command.ResponseCommand);
		BinaryWriter writer = gameServerMessage.GetWriter();
		if (text == null)
		{
			writer.Write(false);
		}
		else
		{
			NPCQuestText.NPCQuestTextType finalType = text.textType;
			string finalText = text.text;
			string link = "";
			int points = 0;
			if (text.textType == NPCQuestText.NPCQuestTextType.ChallengeText)
			{
				doneEvent = new AutoResetEvent(false);
				GameState.masterServer.IsChallengeAvailable(text.targetName, delegate(bool ok)
				{
					if (ok)
					{
						GameState.masterServer.GetChallengeText(text.targetName, delegate(string challengeText, string url, int pts)
						{
							if (challengeText == "")
							{
								finalType = NPCQuestText.NPCQuestTextType.NormalText;
							}
							else
							{
								finalText = challengeText;
							}
							link = url;
							points = pts;
							doneEvent.Set();
						});
					}
					else
					{
						finalType = NPCQuestText.NPCQuestTextType.NormalText;
						doneEvent.Set();
					}
				});
				doneEvent.WaitOne();
			}
			writer.Write(true);
			writer.Write((int)finalType);
			writer.Write(finalText);
			writer.Write(text.continued);
			writer.Write(text.completesQuest);
			writer.Write(text.targetName);
			writer.Write(link);
			writer.Write(points);
		}
		gameServerMessage.Serialize(this.stream);
	}
	private void NPCFinishText(BinaryReader reader)
	{
		string npcName = reader.ReadString();
		NPC nextNpc = null;
		AutoResetEvent doneEvent = new AutoResetEvent(false);
		GameState.QueueRemoteEvent(delegate
		{
			try
			{
				NPC[] array = (NPC[])UnityEngine.Object.FindObjectsOfType(typeof(NPC));
				for (int i = 0; i < array.Length; i++)
				{
					NPC nPC = array[i];
					if (nPC.npcName == npcName)
					{
						nextNpc = nPC.CompleteQuestText(this.player, null);
						break;
					}
				}
				doneEvent.Set();
			}
			catch (Exception ex)
			{
				GameState.ServerLog(ex.ToString());
			}
		});
		doneEvent.WaitOne();
		GameServerMessage gameServerMessage = new GameServerMessage(GameServerMessage.Command.ResponseCommand);
		BinaryWriter writer = gameServerMessage.GetWriter();
		if (nextNpc == null)
		{
			writer.Write(false);
		}
		else
		{
			writer.Write(true);
			writer.Write(nextNpc.npcName);
		}
		gameServerMessage.Serialize(this.stream);
	}
	private void NPCBuyList(BinaryReader reader)
	{
		string npcName = reader.ReadString();
		Dictionary<string, int> items = new Dictionary<string, int>();
		AutoResetEvent doneEvent = new AutoResetEvent(false);
		GameState.QueueRemoteEvent(delegate
		{
			try
			{
				NPC[] array = (NPC[])UnityEngine.Object.FindObjectsOfType(typeof(NPC));
				for (int i = 0; i < array.Length; i++)
				{
					NPC nPC = array[i];
					if (nPC.npcName == npcName)
					{
						NPCQuestText currentQuestText = nPC.GetCurrentQuestText(this.player);
						if (currentQuestText != null)
						{
							StaticLootCollection component = currentQuestText.GetComponent<StaticLootCollection>();
							if (component != null)
							{
								items = component.items;
							}
						}
						break;
					}
				}
				doneEvent.Set();
			}
			catch (Exception ex)
			{
				GameState.ServerLog(ex.ToString());
			}
		});
		doneEvent.WaitOne();
		GameServerMessage gameServerMessage = new GameServerMessage(GameServerMessage.Command.ResponseCommand);
		BinaryWriter writer = gameServerMessage.GetWriter();
		writer.Write(items.Count);
		foreach (string current in items.Keys)
		{
			writer.Write(current);
			writer.Write(items[current]);
		}
		gameServerMessage.Serialize(this.stream);
	}
	private bool MoveToNewMap(BinaryReader reader)
	{
		string mapName = reader.ReadString();
		bool result = false;
		AutoResetEvent doneEvent = new AutoResetEvent(false);
		GameState.QueueRemoteEvent(delegate
		{
			try
			{
				LevelTransition[] array = (LevelTransition[])UnityEngine.Object.FindObjectsOfType(typeof(LevelTransition));
				for (int i = 0; i < array.Length; i++)
				{
					LevelTransition levelTransition = array[i];
					if (levelTransition.destination == mapName)
					{
						result = levelTransition.IsValidTransition(this.player);
						break;
					}
				}
				if (mapName == "WildWest" && GameState.isArena)
				{
					result = true;
				}
				if (mapName == "Space" && Application.loadedLevelName != "Space")
				{
					result = true;
				}
				if (mapName == "Town" && Application.loadedLevelName == "Space")
				{
					result = true;
				}
				if (result)
				{
					int id = this.player.id;
					this.player.RemoveFromServer();
					this.player = null;
					GameState.masterServer.UpdateCharacterLocation(id, mapName);
				}
				doneEvent.Set();
			}
			catch (Exception ex)
			{
				GameState.ServerLog(ex.ToString());
			}
		});
		doneEvent.WaitOne();
		GameServerMessage gameServerMessage = new GameServerMessage(GameServerMessage.Command.ResponseCommand);
		BinaryWriter writer = gameServerMessage.GetWriter();
		writer.Write(result);
		gameServerMessage.Serialize(this.stream);
		return result;
	}
	private void Reload()
	{
		GameState.QueueRemoteEvent(delegate
		{
			int num = this.player.clipSize - this.player.clip;
			if (this.player.ammo < num)
			{
				num = this.player.ammo;
			}
			if (this.player.currentWeapon == "Boomstick" && num > 1)
			{
				num = 1;
			}
			if (num == 0)
			{
				return;
			}
			if (this.player.currentWeapon == "")
			{
				return;
			}
			if (this.player.currentAmmoType == "")
			{
				return;
			}
			if (!this.player.inventory.RemoveItem(this.player.currentAmmoType, num))
			{
				return;
			}
			this.player.inventory.AddAmmoToWeapon(this.player.currentWeapon, num);
			Player.SendUpdateToAllPlayersExcept(this.player, GameServerUpdate.CreateObjectAnimationUpdate(this.player.GetComponent<ReplicatedObject>(), "reload", true, (byte)UnityEngine.Random.Range(0, 256)));
		});
	}
	private void Respawn()
	{
		bool result = false;
		Vector3 pos = new Vector3(0f, 0f, 0f);
		Vector3 rot = new Vector3(0f, 0f, 0f);
		int health = 0;
		AutoResetEvent doneEvent = new AutoResetEvent(false);
		GameState.QueueRemoteEvent(delegate
		{
			if (this.player.health > 0)
			{
				result = false;
				doneEvent.Set();
				return;
			}
			this.player.Respawn();
			result = true;
			pos = this.player.transform.position;
			rot = this.player.transform.rotation.eulerAngles;
			health = this.player.health;
			Player.SendUpdateToAllPlayersExcept(this.player, GameServerUpdate.CreateObjectRespawnUpdate(this.player.GetComponent<ReplicatedObject>(), pos, Quaternion.Euler(rot), health));
			doneEvent.Set();
		});
		doneEvent.WaitOne();
		GameServerMessage gameServerMessage = new GameServerMessage(GameServerMessage.Command.ResponseCommand);
		BinaryWriter writer = gameServerMessage.GetWriter();
		writer.Write(result);
		if (result)
		{
			writer.Write(pos.x);
			writer.Write(pos.y);
			writer.Write(pos.z);
			writer.Write((byte)((int)(rot.x * 256f / 360f) & 255));
			writer.Write((byte)((int)(rot.y * 256f / 360f) & 255));
			writer.Write((byte)((int)(rot.z * 256f / 360f) & 255));
			writer.Write(health);
		}
		gameServerMessage.Serialize(this.stream);
	}
	private void Interact(BinaryReader reader)
	{
		string objName = reader.ReadString();
		GameState.QueueRemoteEvent(delegate
		{
			ServerInteractInfo[] array = (ServerInteractInfo[])UnityEngine.Object.FindObjectsOfType(typeof(ServerInteractInfo));
			for (int i = 0; i < array.Length; i++)
			{
				ServerInteractInfo serverInteractInfo = array[i];
				if (serverInteractInfo.objectName == objName)
				{
					serverInteractInfo.SendMessage("Interact", this.player, SendMessageOptions.DontRequireReceiver);
					break;
				}
			}
		});
	}
	private void DrinkWine(BinaryReader reader)
	{
		int reduction = reader.ReadInt32();
		GameState.QueueRemoteEvent(delegate
		{
			this.player.DrinkWine(reduction);
		});
	}
	private void ValidateProductRegistration(BinaryReader reader)
	{
		int product = reader.ReadInt32();
		bool result = false;
		AutoResetEvent doneEvent = new AutoResetEvent(false);
		GameState.masterServer.ValidateProductRegistration(this.player.id, (ProductKeyVerifier.ProductType)product, delegate(bool ok)
		{
			result = ok;
			doneEvent.Set();
		});
		if (!doneEvent.WaitOne(10000))
		{
			throw new IOException("Product validation timed out");
		}
		if (result)
		{
			GameState.QueueRemoteEvent(delegate
			{
				if (product == 1 && this.player.inventory.GetCountForItem("SeasonPass") == 0)
				{
					this.player.inventory.AddItem("SeasonPass", 1);
				}
			});
		}
	}
	private void IAPPurchase(BinaryReader reader)
	{
		int quantity = reader.ReadInt32();
		string a = reader.ReadString();
		int itemPrice;
		string itemName;
		int itemQuantity;
		GameServerMessage gameServerMessage;
		BinaryWriter writer;
		if (a == "Bag of Gold")
		{
			itemName = "Gold";
			itemQuantity = 200;
			itemPrice = 99;
		}
		else
		{
			if (a == "Bucket of Gold")
			{
				itemName = "Gold";
				itemQuantity = 500;
				itemPrice = 239;
			}
			else
			{
				if (a == "Crate of Gold")
				{
					itemName = "Gold";
					itemQuantity = 1000;
					itemPrice = 469;
				}
				else
				{
					if (a == "Ammo Emergency")
					{
						if (quantity != 1)
						{
							gameServerMessage = new GameServerMessage(GameServerMessage.Command.ResponseCommand);
							writer = gameServerMessage.GetWriter();
							writer.Write(false);
							writer.Write("Only one 'Ammo Emergency' pack may be purchased at a time.");
							gameServerMessage.Serialize(this.stream);
							return;
						}
						itemName = "Ammo";
						itemQuantity = 1;
						itemPrice = 2199;
					}
					else
					{
						if (a == "Corporate Buyout")
						{
							itemName = "Gold";
							itemQuantity = 20000;
							itemPrice = 7999;
						}
						else
						{
							if (!(a == "Holy Hand Grenade"))
							{
								gameServerMessage = new GameServerMessage(GameServerMessage.Command.ResponseCommand);
								writer = gameServerMessage.GetWriter();
								writer.Write(false);
								writer.Write("Invalid item.");
								gameServerMessage.Serialize(this.stream);
								return;
							}
							itemName = "HolyHandGrenade";
							itemQuantity = 1;
							itemPrice = 89;
						}
					}
				}
			}
		}
		bool result = false;
		string error = "";
		AutoResetEvent doneEvent = new AutoResetEvent(false);
		//GameState.QueueRemoteEvent();
		//doneEvent.WaitOne();
		gameServerMessage = new GameServerMessage(GameServerMessage.Command.ResponseCommand);
		writer = gameServerMessage.GetWriter();
		writer.Write(result);
		if (!result)
		{
			writer.Write(error);
		}
		gameServerMessage.Serialize(this.stream);
	}
	private void ActivateQuest(BinaryReader reader)
	{
		string name = reader.ReadString();
		GameState.QueueRemoteEvent(delegate
		{
			if (this.player.questState.HasQuest(name) && this.player.questState.GetQuestState(name) != "_End_")
			{
				GameState.GetQuestManager().ContinueQuest(this.player, name);
			}
		});
	}
	private void Chat(BinaryReader reader)
	{
		string text = reader.ReadString();
		GameState.QueueRemoteEvent(delegate
		{
			this.player.SendChatMessage(text);
		});
	}
	private void TogglePVPFlag()
	{
		GameState.QueueRemoteEvent(delegate
		{
			this.player.TogglePVPRequest();
		});
	}
	private void ClientThread()
	{
		try
		{
			bool flag = true;
			while (flag)
			{
				this.lastUpdate = DateTime.Now;
				GameServerMessage gameServerMessage = GameServerMessage.Deserialize(this.stream);
				BinaryReader reader = gameServerMessage.GetReader();
				GameServerMessage.Command command = gameServerMessage.GetCommand();
				if (command != GameServerMessage.Command.IAPPurchaseCommand)
				{
					if (command != GameServerMessage.Command.ReloadCommand)
					{
						if (command != GameServerMessage.Command.DrinkWineCommand)
						{
							if (command != GameServerMessage.Command.AttackCommand)
							{
								if (command != GameServerMessage.Command.RespawnCommand)
								{
									if (command != GameServerMessage.Command.InteractCommand)
									{
										if (command != GameServerMessage.Command.UpdateCommand)
										{
											if (command != GameServerMessage.Command.TogglePVPFlagCommand)
											{
												if (command != GameServerMessage.Command.NPCTalkCommand)
												{
													if (command != GameServerMessage.Command.SpectatorUpdateCommand)
													{
														if (command != GameServerMessage.Command.SellCommand)
														{
															if (command != GameServerMessage.Command.QuestKillCommand)
															{
																if (command != GameServerMessage.Command.LoginCommand)
																{
																	if (command != GameServerMessage.Command.TakeNamedObjectLootCommand)
																	{
																		if (command != GameServerMessage.Command.MoveToNewMapCommand)
																		{
																			if (command != GameServerMessage.Command.SwitchWeaponCommand)
																			{
																				if (command != GameServerMessage.Command.JumpCommand)
																				{
																					if (command != GameServerMessage.Command.ActivateQuestCommand)
																					{
																						if (command != GameServerMessage.Command.NPCBuyListCommand)
																						{
																							if (command != GameServerMessage.Command.ChatCommand)
																							{
																								if (command != GameServerMessage.Command.SetWeaponSlotsCommand)
																								{
																									if (command != GameServerMessage.Command.TakeObjectLootCommand)
																									{
																										if (command != GameServerMessage.Command.NPCFinishTextCommand)
																										{
																											if (command != GameServerMessage.Command.ValidateProductRegistrationCommand)
																											{
																												if (command != GameServerMessage.Command.SpectatorCommand)
																												{
																													if (command != GameServerMessage.Command.PrepareAttackCommand)
																													{
																														if (command != GameServerMessage.Command.BuyCommand)
																														{
																															if (command != GameServerMessage.Command.IsNamedObjectLootAvailableCommand)
																															{
																																if (command == GameServerMessage.Command.GetObjectLootCommand)
																																{
																																	this.GetObjectLoot(reader);
																																}
																															}
																															else
																															{
																																this.IsNamedObjectLootAvailable(reader);
																															}
																														}
																														else
																														{
																															this.Buy(reader);
																														}
																													}
																													else
																													{
																														this.PrepareAttack();
																													}
																												}
																												else
																												{
																													this.Spectate();
																												}
																											}
																											else
																											{
																												this.ValidateProductRegistration(reader);
																											}
																										}
																										else
																										{
																											this.NPCFinishText(reader);
																										}
																									}
																									else
																									{
																										this.TakeObjectLoot(reader);
																									}
																								}
																								else
																								{
																									this.SetWeaponSlots(reader);
																								}
																							}
																							else
																							{
																								this.Chat(reader);
																							}
																						}
																						else
																						{
																							this.NPCBuyList(reader);
																						}
																					}
																					else
																					{
																						this.ActivateQuest(reader);
																					}
																				}
																				else
																				{
																					this.Jump(reader);
																				}
																			}
																			else
																			{
																				this.SwitchWeapon(reader);
																			}
																		}
																		else
																		{
																			if (this.MoveToNewMap(reader))
																			{
																				flag = false;
																			}
																		}
																	}
																	else
																	{
																		this.TakeNamedObjectLoot(reader);
																	}
																}
																else
																{
																	this.Login(reader);
																}
															}
															else
															{
																this.QuestKill(reader);
															}
														}
														else
														{
															this.Sell(reader);
														}
													}
													else
													{
														this.SpectatorUpdate();
													}
												}
												else
												{
													this.NPCTalk(reader);
												}
											}
											else
											{
												this.TogglePVPFlag();
											}
										}
										else
										{
											this.Update(reader);
										}
									}
									else
									{
										this.Interact(reader);
									}
								}
								else
								{
									this.Respawn();
								}
							}
							else
							{
								this.Attack(reader);
							}
						}
						else
						{
							this.DrinkWine(reader);
						}
					}
					else
					{
						this.Reload();
					}
				}
				else
				{
					this.IAPPurchase(reader);
				}
			}
		}
		catch (IOException)
		{
			this.stream.Close();
		}
		catch (Exception message)
		{
			Console.Out.Write(message);
			this.stream.Close();
		}
	}
	public void Start()
	{
		this.thread.Start();
	}
	public void CheckConnectionStatus()
	{
		if ((DateTime.Now - this.lastUpdate).TotalSeconds > 30.0)
		{
			this.ForceDisconnect();
		}
		if (!this.thread.IsAlive)
		{
			if (this.player != null)
			{
				GameState.QueueRemoteEvent(delegate
				{
					this.player.RemoveFromServer();
				});
			}
			if (this.spectator != null)
			{
				GameState.QueueRemoteEvent(delegate
				{
					this.spectator.RemoveFromServer();
				});
			}
		}
	}
	public void ForceDisconnect()
	{
		this.thread.Abort();
	}
}
