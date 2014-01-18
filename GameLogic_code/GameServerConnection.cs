using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
public class GameServerConnection
{
	private delegate void ProcessingFunction();
	public delegate void ResultCallback(bool ok);
	public delegate void LoginCallback(bool ok, int id);
	public delegate void CompletionCallback();
	public delegate void NPCTalkCallback(bool ok, NPCQuestText.NPCQuestTextType type, string text, bool continued, bool complete, string target, string link, int points);
	public delegate void NPCFinishTextCallback(bool more, string npcName);
	public delegate void GetLootCallback(Dictionary<string, int> items);
	public delegate void RespawnCallback(bool ok, Vector3 pos, Quaternion rot, int health);
	public delegate void PurchaseCallback(bool ok, string error);
	private string host;
	private int port;
	private Thread thread;
	private Stream stream;
	private AutoResetEvent commandEvent;
	private bool running = true;
	private bool loaded = false;
	public int bytesSent = 0;
	public int bytesRecv = 0;
	private Queue<GameServerConnection.ProcessingFunction> commandQueue = new Queue<GameServerConnection.ProcessingFunction>();
	private Queue<GameServerConnection.ProcessingFunction> responseQueue = new Queue<GameServerConnection.ProcessingFunction>();
	public bool connected
	{
		get
		{
			return this.stream != null;
		}
	}
	public bool loadComplete
	{
		get
		{
			return this.loaded;
		}
	}
	public GameServerConnection(string _host, int _port, GameServerConnection.ResultCallback cb)
	{
		GameServerConnection $this = this;
		this.host = _host;
		this.port = _port;
		this.commandEvent = new AutoResetEvent(false);
		this.thread = new Thread(new ThreadStart(this.CommandProcessingThread));
		this.thread.IsBackground = true;
		this.commandQueue.Enqueue(delegate
		{
			try
			{
				$this.stream = $this.Connect();
				if ($this.stream == null)
				{
					Debug.Log("Game server rejected connection");
				}
				object obj = $this.responseQueue;
				Monitor.Enter(obj);
				try
				{
					$this.responseQueue.Enqueue(delegate
					{
						cb($this.stream != null);
					});
				}
				finally
				{
					Monitor.Exit(obj);
				}
			}
			catch (Exception)
			{
				object obj2 = $this.responseQueue;
				Monitor.Enter(obj2);
				try
				{
					$this.responseQueue.Enqueue(delegate
					{
						cb(false);
					});
				}
				finally
				{
					Monitor.Exit(obj2);
				}
			}
		});
		this.commandEvent.Set();
	}
	private Stream Connect()
	{
		Stream result;
		try
		{
			result = new TcpClient(this.host, this.port)
			{
				NoDelay = true
			}.GetStream();
		}
		catch (Exception)
		{
			result = null;
		}
		return result;
	}
	private void CommandProcessingThread()
	{
		try
		{
			while (this.running)
			{
				this.commandEvent.WaitOne();
				while (true)
				{
					object obj = this.commandQueue;
					Monitor.Enter(obj);
					GameServerConnection.ProcessingFunction processingFunction;
					try
					{
						if (this.commandQueue.Count == 0)
						{
							break;
						}
						processingFunction = this.commandQueue.Dequeue();
					}
					finally
					{
						Monitor.Exit(obj);
					}
					processingFunction();
				}
			}
		}
		catch (Exception)
		{
		}
		this.stream.Close();
		this.stream = null;
	}
	public void Start()
	{
		this.thread.Start();
	}
	public void Stop()
	{
		this.running = false;
		this.commandEvent.Set();
	}
	public void Update()
	{
		object obj = this.responseQueue;
		Monitor.Enter(obj);
		try
		{
			while (this.responseQueue.Count > 0)
			{
				this.responseQueue.Dequeue()();
			}
		}
		finally
		{
			Monitor.Exit(obj);
		}
	}
	public void Login(int id, string token, GameServerConnection.LoginCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					GameServerMessage gameServerMessage = new GameServerMessage(GameServerMessage.Command.LoginCommand);
					BinaryWriter writer = gameServerMessage.GetWriter();
					writer.Write(id);
					writer.Write(token);
					writer.Write(GameState.previousExit);
					gameServerMessage.Serialize(this.stream);
					this.bytesSent += gameServerMessage.length;
					GameServerMessage gameServerMessage2 = GameServerMessage.DeserializeResponse(this.stream);
					this.bytesRecv += gameServerMessage2.length;
					BinaryReader reader = gameServerMessage2.GetReader();
					bool result = reader.ReadBoolean();
					int playerId = -1;
					if (result)
					{
						playerId = reader.ReadInt32();
					}
					object obj2 = this.responseQueue;
					Monitor.Enter(obj2);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(result, playerId);
						});
					}
					finally
					{
						Monitor.Exit(obj2);
					}
				}
				catch (Exception)
				{
					object obj3 = this.responseQueue;
					Monitor.Enter(obj3);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(false, -1);
						});
					}
					finally
					{
						Monitor.Exit(obj3);
					}
					this.Stop();
				}
			});
			this.commandEvent.Set();
		}
		finally
		{
			Monitor.Exit(obj);
		}
	}
	public void Spectate(GameServerConnection.ResultCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					GameServerMessage gameServerMessage = new GameServerMessage(GameServerMessage.Command.SpectatorCommand);
					gameServerMessage.Serialize(this.stream);
					this.bytesSent += gameServerMessage.length;
					GameServerMessage gameServerMessage2 = GameServerMessage.DeserializeResponse(this.stream);
					this.bytesRecv += gameServerMessage2.length;
					BinaryReader reader = gameServerMessage2.GetReader();
					bool result = reader.ReadBoolean();
					object obj2 = this.responseQueue;
					Monitor.Enter(obj2);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(result);
						});
					}
					finally
					{
						Monitor.Exit(obj2);
					}
				}
				catch (Exception)
				{
					object obj3 = this.responseQueue;
					Monitor.Enter(obj3);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(false);
						});
					}
					finally
					{
						Monitor.Exit(obj3);
					}
					this.Stop();
				}
			});
			this.commandEvent.Set();
		}
		finally
		{
			Monitor.Exit(obj);
		}
	}
	private void ObjectPositionUpdate(BinaryReader reader)
	{
		int id = reader.ReadInt32();
		Vector3 pos;
		pos.x = reader.ReadSingle();
		pos.y = reader.ReadSingle();
		pos.z = reader.ReadSingle();
		Vector3 rot;
		rot.x = (float)reader.ReadByte() * 360f / 256f;
		rot.y = (float)reader.ReadByte() * 360f / 256f;
		rot.z = (float)reader.ReadByte() * 360f / 256f;
		GameState.QueueRemoteEvent(delegate
		{
			ReplicatedObject replicatedObjectById = GameState.instance.GetReplicatedObjectById(id);
			if (replicatedObjectById == null)
			{
				return;
			}
			object obj = replicatedObjectById;
			Monitor.Enter(obj);
			try
			{
				replicatedObjectById.remotePosition = pos;
				replicatedObjectById.remoteOrientation = Quaternion.Euler(rot);
			}
			finally
			{
				Monitor.Exit(obj);
			}
		});
	}
	private void ObjectSpawnUpdate(BinaryReader reader)
	{
		int id = reader.ReadInt32();
		string name = reader.ReadString();
		Vector3 pos;
		pos.x = reader.ReadSingle();
		pos.y = reader.ReadSingle();
		pos.z = reader.ReadSingle();
		Vector3 rot;
		rot.x = (float)reader.ReadByte() * 360f / 256f;
		rot.y = (float)reader.ReadByte() * 360f / 256f;
		rot.z = (float)reader.ReadByte() * 360f / 256f;
		GameState.QueueRemoteEvent(delegate
		{
			GameState.instance.SpawnClientReplicatedObject(id, name, pos, Quaternion.Euler(rot));
		});
	}
	private void ObjectRespawnUpdate(BinaryReader reader)
	{
		int id = reader.ReadInt32();
		Vector3 pos;
		pos.x = reader.ReadSingle();
		pos.y = reader.ReadSingle();
		pos.z = reader.ReadSingle();
		Vector3 rot;
		rot.x = (float)reader.ReadByte() * 360f / 256f;
		rot.y = (float)reader.ReadByte() * 360f / 256f;
		rot.z = (float)reader.ReadByte() * 360f / 256f;
		int health = reader.ReadInt32();
		GameState.QueueRemoteEvent(delegate
		{
			ReplicatedObject replicatedObjectById = GameState.instance.GetReplicatedObjectById(id);
			if (replicatedObjectById == null)
			{
				return;
			}
			if (replicatedObjectById.GetComponent<Player>() == null)
			{
				return;
			}
			replicatedObjectById.GetComponent<Player>().RespawnAt(pos, Quaternion.Euler(rot));
			replicatedObjectById.GetComponent<Player>().SetHealth(health);
		});
	}
	private void ObjectDestroyUpdate(BinaryReader reader)
	{
		int id = reader.ReadInt32();
		GameState.QueueRemoteEvent(delegate
		{
			ReplicatedObject replicatedObjectById = GameState.instance.GetReplicatedObjectById(id);
			if (replicatedObjectById != null)
			{
				GameState.instance.RemoveReplicatedObject(replicatedObjectById);
				UnityEngine.Object.Destroy(replicatedObjectById.gameObject);
			}
		});
	}
	private void ObjectStateUpdate(BinaryReader reader)
	{
		int id = reader.ReadInt32();
		string name = reader.ReadString();
		GameState.QueueRemoteEvent(delegate
		{
			ReplicatedObject replicatedObjectById = GameState.instance.GetReplicatedObjectById(id);
			if (replicatedObjectById != null)
			{
				Player component = replicatedObjectById.GetComponent<Player>();
				if (component != null)
				{
					component.remoteWeapon = name;
				}
				Enemy component2 = replicatedObjectById.GetComponent<Enemy>();
				if (component2 != null)
				{
					component2.StateUpdate(name);
				}
			}
		});
	}
	private void ObjectAnimationUpdate(BinaryReader reader)
	{
		int id = reader.ReadInt32();
		string name = reader.ReadString();
		bool val = reader.ReadBoolean();
		byte random = reader.ReadByte();
		GameState.QueueRemoteEvent(delegate
		{
			ReplicatedObject replicatedObjectById = GameState.instance.GetReplicatedObjectById(id);
			if (replicatedObjectById != null)
			{
				Enemy component = replicatedObjectById.GetComponent<Enemy>();
				if (component != null)
				{
					component.UpdateRandomState(random);
					component.SetState(name, val);
				}
				Player component2 = replicatedObjectById.GetComponent<Player>();
				if (component2 != null)
				{
					component2.SetAnimationState(name, val, random);
				}
			}
		});
	}
	private void ObjectDeathUpdate(BinaryReader reader)
	{
		int id = reader.ReadInt32();
		int killerId = reader.ReadInt32();
		GameState.QueueRemoteEvent(delegate
		{
			ReplicatedObject replicatedObjectById = GameState.instance.GetReplicatedObjectById(id);
			ReplicatedObject replicatedObject = (killerId == -1) ? null : GameState.instance.GetReplicatedObjectById(killerId);
			if (replicatedObjectById != null)
			{
				Enemy component = replicatedObjectById.GetComponent<Enemy>();
				if (component != null)
				{
					component.Die((!(replicatedObject != null)) ? null : replicatedObject.gameObject);
				}
			}
		});
	}
	private void ObjectHealthUpdate(BinaryReader reader)
	{
		int id = reader.ReadInt32();
		int health = reader.ReadInt32();
		GameState.QueueRemoteEvent(delegate
		{
			ReplicatedObject replicatedObjectById = GameState.instance.GetReplicatedObjectById(id);
			if (replicatedObjectById != null)
			{
				Enemy component = replicatedObjectById.GetComponent<Enemy>();
				if (component != null)
				{
					component.SetHealth(health);
				}
				Player component2 = replicatedObjectById.GetComponent<Player>();
				if (component2 != null)
				{
					component2.SetHealth(health);
				}
			}
		});
	}
	private void LocalPlayerHealthUpdate(BinaryReader reader)
	{
		int health = reader.ReadInt32();
		GameState.QueueRemoteEvent(delegate
		{
			LocalPlayerEvents.UpdateHealth(health);
		});
	}
	private void TeleportLocalPlayerUpdate(BinaryReader reader)
	{
		Vector3 pos;
		pos.x = reader.ReadSingle();
		pos.y = reader.ReadSingle();
		pos.z = reader.ReadSingle();
		Vector3 rot;
		rot.x = (float)reader.ReadByte() * 360f / 256f;
		rot.y = (float)reader.ReadByte() * 360f / 256f;
		rot.z = (float)reader.ReadByte() * 360f / 256f;
		GameState.QueueRemoteEvent(delegate
		{
			LocalPlayerEvents.localPlayer.TeleportTo(pos, Quaternion.Euler(rot));
		});
	}
	private void PlayerJoinUpdate(BinaryReader reader)
	{
		int id = reader.ReadInt32();
		string name = reader.ReadString();
		string team = reader.ReadString();
		int avatar = reader.ReadInt32();
		Vector3 pos;
		pos.x = reader.ReadSingle();
		pos.y = reader.ReadSingle();
		pos.z = reader.ReadSingle();
		Vector3 rot;
		rot.x = (float)reader.ReadByte() * 360f / 256f;
		rot.y = (float)reader.ReadByte() * 360f / 256f;
		rot.z = (float)reader.ReadByte() * 360f / 256f;
		GameState.QueueRemoteEvent(delegate
		{
			GameState.instance.SpawnClientReplicatedPlayer(id, name, team, avatar, pos, Quaternion.Euler(rot));
		});
	}
	private void ChatUpdate(BinaryReader reader)
	{
		string text = reader.ReadString();
		GameState.QueueRemoteEvent(delegate
		{
			LocalPlayerEvents.ChatMessage(text);
		});
	}
	private void AddItemUpdate(BinaryReader reader)
	{
		string name = reader.ReadString();
		int count = reader.ReadInt32();
		GameState.QueueRemoteEvent(delegate
		{
			GameState.localPlayerInventory.AddItem(name, count);
		});
	}
	private void RemoveItemUpdate(BinaryReader reader)
	{
		string name = reader.ReadString();
		int count = reader.ReadInt32();
		GameState.QueueRemoteEvent(delegate
		{
			GameState.localPlayerInventory.RemoveItem(name, count);
		});
	}
	private void LoadedAmmoUpdate(BinaryReader reader)
	{
		string name = reader.ReadString();
		int count = reader.ReadInt32();
		GameState.QueueRemoteEvent(delegate
		{
			GameState.localPlayerInventory.SetLoadedAmmoForWeapon(name, count);
		});
	}
	private void StartQuestUpdate(BinaryReader reader)
	{
		string name = reader.ReadString();
		string state = reader.ReadString();
		GameState.QueueRemoteEvent(delegate
		{
			GameState.localQuestState.StartQuest(name, state, "");
			LocalPlayerEvents.localPlayer.SendMessage("OnQuestUpdated", SendMessageOptions.DontRequireReceiver);
		});
	}
	private void UpdateQuestUpdate(BinaryReader reader)
	{
		string name = reader.ReadString();
		string state = reader.ReadString();
		GameState.QueueRemoteEvent(delegate
		{
			GameState.localQuestState.UpdateQuest(name, state, "");
			LocalPlayerEvents.localPlayer.SendMessage("OnQuestUpdated", SendMessageOptions.DontRequireReceiver);
		});
	}
	private void CompleteQuestUpdate(BinaryReader reader)
	{
		string name = reader.ReadString();
		GameState.QueueRemoteEvent(delegate
		{
			if (GameState.localQuestState.currentQuest != name)
			{
				GameState.localQuestState.SetActiveQuest(name);
			}
			GameState.localQuestState.CompleteQuest();
			LocalPlayerEvents.localPlayer.SendMessage("OnQuestUpdated", SendMessageOptions.DontRequireReceiver);
		});
	}
	private void ActiveQuestUpdate(BinaryReader reader)
	{
		string name = reader.ReadString();
		GameState.QueueRemoteEvent(delegate
		{
			GameState.localQuestState.SetActiveQuest(name);
			LocalPlayerEvents.localPlayer.SendMessage("OnQuestUpdated", SendMessageOptions.DontRequireReceiver);
		});
	}
	private void QuestKillCountUpdate(BinaryReader reader)
	{
		string name = reader.ReadString();
		int count = reader.ReadInt32();
		GameState.QueueRemoteEvent(delegate
		{
			GameState.localQuestState.SetKillCount(name, count);
			LocalPlayerEvents.localPlayer.SendMessage("OnQuestUpdated", SendMessageOptions.DontRequireReceiver);
		});
	}
	private void CountdownUpdate(BinaryReader reader)
	{
		int val = reader.ReadInt32();
		string desc = reader.ReadString();
		GameState.QueueRemoteEvent(delegate
		{
			LocalPlayerEvents.UpdateCountdown(val, desc);
		});
	}
	private void ArenaTeamScoreUpdate(BinaryReader reader)
	{
		int team = reader.ReadInt32();
		int score = reader.ReadInt32();
		GameState.QueueRemoteEvent(delegate
		{
			if (GameState.isArena)
			{
				ArenaState.instance.SetTeamScore(team, score);
			}
		});
	}
	private void ArenaPlayerScoreUpdate(BinaryReader reader)
	{
		int id = reader.ReadInt32();
		int team = reader.ReadInt32();
		int kills = reader.ReadInt32();
		int assists = reader.ReadInt32();
		int deaths = reader.ReadInt32();
		GameState.QueueRemoteEvent(delegate
		{
			Player component = GameState.instance.GetReplicatedObjectById(id).GetComponent<Player>();
			component.arenaTeamIndex = team;
			component.arenaKills = kills;
			component.arenaAssists = assists;
			component.arenaDeaths = deaths;
		});
	}
	private void BossUpdate(BinaryReader reader)
	{
		bool boss = reader.ReadBoolean();
		GameState.QueueRemoteEvent(delegate
		{
			if (boss)
			{
				GameObject.FindGameObjectWithTag("GameLevel").GetComponent<GameLevel>().StartBoss();
			}
			else
			{
				GameObject.FindGameObjectWithTag("GameLevel").GetComponent<GameLevel>().EndBoss();
			}
		});
	}
	private void ProcessUpdate(GameServerUpdate update)
	{
		GameServerUpdate.UpdateType updateType = update.GetUpdateType();
		if (updateType != GameServerUpdate.UpdateType.ObjectDeathUpdate)
		{
			if (updateType != GameServerUpdate.UpdateType.LoadCompleteUpdate)
			{
				if (updateType != GameServerUpdate.UpdateType.UpdateQuestUpdate)
				{
					if (updateType != GameServerUpdate.UpdateType.ObjectDestroyUpdate)
					{
						if (updateType != GameServerUpdate.UpdateType.LocalPlayerHealthUpdate)
						{
							if (updateType != GameServerUpdate.UpdateType.RemoveItemUpdate)
							{
								if (updateType != GameServerUpdate.UpdateType.CountdownUpdate)
								{
									if (updateType != GameServerUpdate.UpdateType.CompleteQuestUpdate)
									{
										if (updateType != GameServerUpdate.UpdateType.ObjectRespawnUpdate)
										{
											if (updateType != GameServerUpdate.UpdateType.TeleportLocalPlayerUpdate)
											{
												if (updateType != GameServerUpdate.UpdateType.ObjectHealthUpdate)
												{
													if (updateType != GameServerUpdate.UpdateType.AddItemUpdate)
													{
														if (updateType != GameServerUpdate.UpdateType.ObjectAnimationUpdate)
														{
															if (updateType != GameServerUpdate.UpdateType.PlayerJoinUpdate)
															{
																if (updateType != GameServerUpdate.UpdateType.ObjectSpawnUpdate)
																{
																	if (updateType != GameServerUpdate.UpdateType.LoadedAmmoUpdate)
																	{
																		if (updateType != GameServerUpdate.UpdateType.ActiveQuestUpdate)
																		{
																			if (updateType != GameServerUpdate.UpdateType.ArenaPlayerScoreUpdate)
																			{
																				if (updateType != GameServerUpdate.UpdateType.ArenaTeamScoreUpdate)
																				{
																					if (updateType != GameServerUpdate.UpdateType.ObjectPositionUpdate)
																					{
																						if (updateType != GameServerUpdate.UpdateType.BossUpdate)
																						{
																							if (updateType != GameServerUpdate.UpdateType.ChatUpdate)
																							{
																								if (updateType != GameServerUpdate.UpdateType.ObjectStateUpdate)
																								{
																									if (updateType != GameServerUpdate.UpdateType.QuestKillCountUpdate)
																									{
																										if (updateType == GameServerUpdate.UpdateType.StartQuestUpdate)
																										{
																											this.StartQuestUpdate(update.GetReader());
																										}
																									}
																									else
																									{
																										this.QuestKillCountUpdate(update.GetReader());
																									}
																								}
																								else
																								{
																									this.ObjectStateUpdate(update.GetReader());
																								}
																							}
																							else
																							{
																								this.ChatUpdate(update.GetReader());
																							}
																						}
																						else
																						{
																							this.BossUpdate(update.GetReader());
																						}
																					}
																					else
																					{
																						this.ObjectPositionUpdate(update.GetReader());
																					}
																				}
																				else
																				{
																					this.ArenaTeamScoreUpdate(update.GetReader());
																				}
																			}
																			else
																			{
																				this.ArenaPlayerScoreUpdate(update.GetReader());
																			}
																		}
																		else
																		{
																			this.ActiveQuestUpdate(update.GetReader());
																		}
																	}
																	else
																	{
																		this.LoadedAmmoUpdate(update.GetReader());
																	}
																}
																else
																{
																	this.ObjectSpawnUpdate(update.GetReader());
																}
															}
															else
															{
																this.PlayerJoinUpdate(update.GetReader());
															}
														}
														else
														{
															this.ObjectAnimationUpdate(update.GetReader());
														}
													}
													else
													{
														this.AddItemUpdate(update.GetReader());
													}
												}
												else
												{
													this.ObjectHealthUpdate(update.GetReader());
												}
											}
											else
											{
												this.TeleportLocalPlayerUpdate(update.GetReader());
											}
										}
										else
										{
											this.ObjectRespawnUpdate(update.GetReader());
										}
									}
									else
									{
										this.CompleteQuestUpdate(update.GetReader());
									}
								}
								else
								{
									this.CountdownUpdate(update.GetReader());
								}
							}
							else
							{
								this.RemoveItemUpdate(update.GetReader());
							}
						}
						else
						{
							this.LocalPlayerHealthUpdate(update.GetReader());
						}
					}
					else
					{
						this.ObjectDestroyUpdate(update.GetReader());
					}
				}
				else
				{
					this.UpdateQuestUpdate(update.GetReader());
				}
			}
			else
			{
				this.loaded = true;
			}
		}
		else
		{
			this.ObjectDeathUpdate(update.GetReader());
		}
	}
	public void Update(Player player, GameServerConnection.CompletionCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					GameServerMessage gameServerMessage = new GameServerMessage(GameServerMessage.Command.UpdateCommand);
					BinaryWriter writer = gameServerMessage.GetWriter();
					object player2 = player;
					Monitor.Enter(player2);
					Vector3 remotePosition;
					Vector3 eulerAngles;
					try
					{
						remotePosition = player.remotePosition;
						eulerAngles = player.remoteLookDirection.eulerAngles;
					}
					finally
					{
						Monitor.Exit(player2);
					}
					writer.Write(remotePosition.x);
					writer.Write(remotePosition.y);
					writer.Write(remotePosition.z);
					writer.Write((byte)((int)(eulerAngles.x * 256f / 360f) & 255));
					writer.Write((byte)((int)(eulerAngles.y * 256f / 360f) & 255));
					writer.Write((byte)((int)(eulerAngles.z * 256f / 360f) & 255));
					gameServerMessage.Serialize(this.stream);
					this.bytesSent += gameServerMessage.length;
					GameServerMessage gameServerMessage2 = GameServerMessage.DeserializeResponse(this.stream);
					this.bytesRecv += gameServerMessage2.length;
					while (gameServerMessage2.HasMoreUpdates())
					{
						GameServerUpdate update = gameServerMessage2.ReadUpdate();
						try
						{
							this.ProcessUpdate(update);
						}
						catch (Exception ex)
						{
							Debug.Log(ex.ToString());
						}
					}
					object obj2 = this.responseQueue;
					Monitor.Enter(obj2);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback();
						});
					}
					finally
					{
						Monitor.Exit(obj2);
					}
				}
				catch (Exception)
				{
					object obj3 = this.responseQueue;
					Monitor.Enter(obj3);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback();
						});
					}
					finally
					{
						Monitor.Exit(obj3);
					}
					this.Stop();
				}
			});
			this.commandEvent.Set();
		}
		finally
		{
			Monitor.Exit(obj);
		}
	}
	public void SpectatorUpdate(GameServerConnection.CompletionCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					GameServerMessage gameServerMessage = new GameServerMessage(GameServerMessage.Command.SpectatorUpdateCommand);
					gameServerMessage.Serialize(this.stream);
					this.bytesSent += gameServerMessage.length;
					GameServerMessage gameServerMessage2 = GameServerMessage.DeserializeResponse(this.stream);
					this.bytesRecv += gameServerMessage2.length;
					while (gameServerMessage2.HasMoreUpdates())
					{
						GameServerUpdate update = gameServerMessage2.ReadUpdate();
						this.ProcessUpdate(update);
					}
					object obj2 = this.responseQueue;
					Monitor.Enter(obj2);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback();
						});
					}
					finally
					{
						Monitor.Exit(obj2);
					}
				}
				catch (Exception)
				{
					object obj3 = this.responseQueue;
					Monitor.Enter(obj3);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback();
						});
					}
					finally
					{
						Monitor.Exit(obj3);
					}
					this.Stop();
				}
			});
			this.commandEvent.Set();
		}
		finally
		{
			Monitor.Exit(obj);
		}
	}
	public void PrepareAttack()
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					GameServerMessage gameServerMessage = new GameServerMessage(GameServerMessage.Command.PrepareAttackCommand);
					gameServerMessage.Serialize(this.stream);
					this.bytesSent += gameServerMessage.length;
				}
				catch (Exception)
				{
					this.Stop();
				}
			});
			this.commandEvent.Set();
		}
		finally
		{
			Monitor.Exit(obj);
		}
	}
	public void Attack(Ray[] rays, int[] hitIds)
	{
		if (rays.Length == 0)
		{
			return;
		}
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					GameServerMessage gameServerMessage = new GameServerMessage(GameServerMessage.Command.AttackCommand);
					BinaryWriter writer = gameServerMessage.GetWriter();
					writer.Write(rays[0].origin.x);
					writer.Write(rays[0].origin.y);
					writer.Write(rays[0].origin.z);
					writer.Write(rays.Length);
					for (int i = 0; i < rays.Length; i++)
					{
						writer.Write(rays[i].direction.x);
						writer.Write(rays[i].direction.y);
						writer.Write(rays[i].direction.z);
						writer.Write(hitIds[i]);
					}
					gameServerMessage.Serialize(this.stream);
					this.bytesSent += gameServerMessage.length;
				}
				catch (Exception)
				{
					this.Stop();
				}
			});
			this.commandEvent.Set();
		}
		finally
		{
			Monitor.Exit(obj);
		}
	}
	public void SetJumpState(bool jumping)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					GameServerMessage gameServerMessage = new GameServerMessage(GameServerMessage.Command.JumpCommand);
					BinaryWriter writer = gameServerMessage.GetWriter();
					writer.Write(jumping);
					gameServerMessage.Serialize(this.stream);
					this.bytesSent += gameServerMessage.length;
				}
				catch (Exception)
				{
					this.Stop();
				}
			});
			this.commandEvent.Set();
		}
		finally
		{
			Monitor.Exit(obj);
		}
	}
	public void SwitchWeapon(int slot)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					GameServerMessage gameServerMessage = new GameServerMessage(GameServerMessage.Command.SwitchWeaponCommand);
					BinaryWriter writer = gameServerMessage.GetWriter();
					writer.Write(slot);
					gameServerMessage.Serialize(this.stream);
					this.bytesSent += gameServerMessage.length;
				}
				catch (Exception)
				{
					this.Stop();
				}
			});
			this.commandEvent.Set();
		}
		finally
		{
			Monitor.Exit(obj);
		}
	}
	public void SetWeaponSlots(Inventory inventory)
	{
		string slot1 = inventory.primaryWeaponName;
		string slot2 = inventory.secondaryWeaponName;
		string slot3 = inventory.swordGrenadeName;
		string slot4 = inventory.accessoryName;
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					GameServerMessage gameServerMessage = new GameServerMessage(GameServerMessage.Command.SetWeaponSlotsCommand);
					BinaryWriter writer = gameServerMessage.GetWriter();
					writer.Write(slot1);
					writer.Write(slot2);
					writer.Write(slot3);
					writer.Write(slot4);
					gameServerMessage.Serialize(this.stream);
					this.bytesSent += gameServerMessage.length;
				}
				catch (Exception)
				{
					this.Stop();
				}
			});
			this.commandEvent.Set();
		}
		finally
		{
			Monitor.Exit(obj);
		}
	}
	public void GetObjectLoot(ReplicatedObject obj, GameServerConnection.GetLootCallback callback)
	{
		object obj2 = this.commandQueue;
		Monitor.Enter(obj2);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					GameServerMessage gameServerMessage = new GameServerMessage(GameServerMessage.Command.GetObjectLootCommand);
					BinaryWriter writer = gameServerMessage.GetWriter();
					writer.Write(obj.id);
					gameServerMessage.Serialize(this.stream);
					this.bytesSent += gameServerMessage.length;
					GameServerMessage gameServerMessage2 = GameServerMessage.DeserializeResponse(this.stream);
					this.bytesRecv += gameServerMessage2.length;
					BinaryReader reader = gameServerMessage2.GetReader();
					int num = reader.ReadInt32();
					Dictionary<string, int> items = new Dictionary<string, int>();
					for (int i = 0; i < num; i++)
					{
						string key = reader.ReadString();
						int value = reader.ReadInt32();
						items.Add(key, value);
					}
					object obj3 = this.responseQueue;
					Monitor.Enter(obj3);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(items);
						});
					}
					finally
					{
						Monitor.Exit(obj3);
					}
				}
				catch (Exception)
				{
					object obj4 = this.responseQueue;
					Monitor.Enter(obj4);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(new Dictionary<string, int>());
						});
					}
					finally
					{
						Monitor.Exit(obj4);
					}
					this.Stop();
				}
			});
			this.commandEvent.Set();
		}
		finally
		{
			Monitor.Exit(obj2);
		}
	}
	public void TakeObjectLoot(ReplicatedObject obj)
	{
		if (obj == null)
		{
			return;
		}
		object obj2 = this.commandQueue;
		Monitor.Enter(obj2);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					GameServerMessage gameServerMessage = new GameServerMessage(GameServerMessage.Command.TakeObjectLootCommand);
					BinaryWriter writer = gameServerMessage.GetWriter();
					writer.Write(obj.id);
					gameServerMessage.Serialize(this.stream);
					this.bytesSent += gameServerMessage.length;
				}
				catch (Exception)
				{
					this.Stop();
				}
			});
			this.commandEvent.Set();
		}
		finally
		{
			Monitor.Exit(obj2);
		}
	}
	public void IsNamedObjectLootAvailable(string lootName, GameServerConnection.ResultCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					GameServerMessage gameServerMessage = new GameServerMessage(GameServerMessage.Command.IsNamedObjectLootAvailableCommand);
					BinaryWriter writer = gameServerMessage.GetWriter();
					writer.Write(lootName);
					gameServerMessage.Serialize(this.stream);
					this.bytesSent += gameServerMessage.length;
					GameServerMessage gameServerMessage2 = GameServerMessage.DeserializeResponse(this.stream);
					this.bytesRecv += gameServerMessage2.length;
					bool result = gameServerMessage2.GetReader().ReadBoolean();
					object obj2 = this.responseQueue;
					Monitor.Enter(obj2);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(result);
						});
					}
					finally
					{
						Monitor.Exit(obj2);
					}
				}
				catch (Exception)
				{
					object obj3 = this.responseQueue;
					Monitor.Enter(obj3);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(false);
						});
					}
					finally
					{
						Monitor.Exit(obj3);
					}
					this.Stop();
				}
			});
			this.commandEvent.Set();
		}
		finally
		{
			Monitor.Exit(obj);
		}
	}
	public void TakeNamedObjectLoot(string name)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					GameServerMessage gameServerMessage = new GameServerMessage(GameServerMessage.Command.TakeNamedObjectLootCommand);
					BinaryWriter writer = gameServerMessage.GetWriter();
					writer.Write(name);
					gameServerMessage.Serialize(this.stream);
					this.bytesSent += gameServerMessage.length;
				}
				catch (Exception)
				{
					this.Stop();
				}
			});
			this.commandEvent.Set();
		}
		finally
		{
			Monitor.Exit(obj);
		}
	}
	public void Buy(string npcName, string item, int count)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					GameServerMessage gameServerMessage = new GameServerMessage(GameServerMessage.Command.BuyCommand);
					BinaryWriter writer = gameServerMessage.GetWriter();
					writer.Write(npcName);
					writer.Write(item);
					writer.Write(count);
					gameServerMessage.Serialize(this.stream);
					this.bytesSent += gameServerMessage.length;
				}
				catch (Exception)
				{
					this.Stop();
				}
			});
			this.commandEvent.Set();
		}
		finally
		{
			Monitor.Exit(obj);
		}
	}
	public void Sell(string item, int count)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					GameServerMessage gameServerMessage = new GameServerMessage(GameServerMessage.Command.SellCommand);
					BinaryWriter writer = gameServerMessage.GetWriter();
					writer.Write(item);
					writer.Write(count);
					gameServerMessage.Serialize(this.stream);
					this.bytesSent += gameServerMessage.length;
				}
				catch (Exception)
				{
					this.Stop();
				}
			});
			this.commandEvent.Set();
		}
		finally
		{
			Monitor.Exit(obj);
		}
	}
	public void QuestKill(string enemyName)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					GameServerMessage gameServerMessage = new GameServerMessage(GameServerMessage.Command.QuestKillCommand);
					BinaryWriter writer = gameServerMessage.GetWriter();
					writer.Write(enemyName);
					gameServerMessage.Serialize(this.stream);
					this.bytesSent += gameServerMessage.length;
				}
				catch (Exception)
				{
					this.Stop();
				}
			});
			this.commandEvent.Set();
		}
		finally
		{
			Monitor.Exit(obj);
		}
	}
	public void NPCTalk(string npc, GameServerConnection.NPCTalkCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					GameServerMessage gameServerMessage = new GameServerMessage(GameServerMessage.Command.NPCTalkCommand);
					BinaryWriter writer = gameServerMessage.GetWriter();
					writer.Write(npc);
					gameServerMessage.Serialize(this.stream);
					this.bytesSent += gameServerMessage.length;
					GameServerMessage gameServerMessage2 = GameServerMessage.DeserializeResponse(this.stream);
					this.bytesRecv += gameServerMessage2.length;
					BinaryReader reader = gameServerMessage2.GetReader();
					bool result = reader.ReadBoolean();
					NPCQuestText.NPCQuestTextType type = NPCQuestText.NPCQuestTextType.NormalText;
					string text = null;
					bool continued = false;
					bool completesQuest = false;
					string target = null;
					string link = null;
					int points = 0;
					if (result)
					{
						type = (NPCQuestText.NPCQuestTextType)reader.ReadInt32();
						text = reader.ReadString();
						continued = reader.ReadBoolean();
						completesQuest = reader.ReadBoolean();
						target = reader.ReadString();
						link = reader.ReadString();
						points = reader.ReadInt32();
					}
					object obj2 = this.responseQueue;
					Monitor.Enter(obj2);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(result, type, text, continued, completesQuest, target, link, points);
						});
					}
					finally
					{
						Monitor.Exit(obj2);
					}
				}
				catch (Exception)
				{
					object obj3 = this.responseQueue;
					Monitor.Enter(obj3);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(false, NPCQuestText.NPCQuestTextType.NormalText, null, false, false, null, null, 0);
						});
					}
					finally
					{
						Monitor.Exit(obj3);
					}
					this.Stop();
				}
			});
			this.commandEvent.Set();
		}
		finally
		{
			Monitor.Exit(obj);
		}
	}
	public void NPCFinishText(string npc, GameServerConnection.NPCFinishTextCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					GameServerMessage gameServerMessage = new GameServerMessage(GameServerMessage.Command.NPCFinishTextCommand);
					BinaryWriter writer = gameServerMessage.GetWriter();
					writer.Write(npc);
					gameServerMessage.Serialize(this.stream);
					this.bytesSent += gameServerMessage.length;
					GameServerMessage gameServerMessage2 = GameServerMessage.DeserializeResponse(this.stream);
					this.bytesRecv += gameServerMessage2.length;
					BinaryReader reader = gameServerMessage2.GetReader();
					bool more = reader.ReadBoolean();
					string name = null;
					if (more)
					{
						name = reader.ReadString();
					}
					object obj2 = this.responseQueue;
					Monitor.Enter(obj2);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(more, name);
						});
					}
					finally
					{
						Monitor.Exit(obj2);
					}
				}
				catch (Exception)
				{
					object obj3 = this.responseQueue;
					Monitor.Enter(obj3);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(false, null);
						});
					}
					finally
					{
						Monitor.Exit(obj3);
					}
					this.Stop();
				}
			});
			this.commandEvent.Set();
		}
		finally
		{
			Monitor.Exit(obj);
		}
	}
	public void NPCBuyList(string npcName, GameServerConnection.GetLootCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					GameServerMessage gameServerMessage = new GameServerMessage(GameServerMessage.Command.NPCBuyListCommand);
					BinaryWriter writer = gameServerMessage.GetWriter();
					writer.Write(npcName);
					gameServerMessage.Serialize(this.stream);
					this.bytesSent += gameServerMessage.length;
					GameServerMessage gameServerMessage2 = GameServerMessage.DeserializeResponse(this.stream);
					this.bytesRecv += gameServerMessage2.length;
					BinaryReader reader = gameServerMessage2.GetReader();
					int num = reader.ReadInt32();
					Dictionary<string, int> items = new Dictionary<string, int>();
					for (int i = 0; i < num; i++)
					{
						string key = reader.ReadString();
						int value = reader.ReadInt32();
						items.Add(key, value);
					}
					object obj2 = this.responseQueue;
					Monitor.Enter(obj2);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(items);
						});
					}
					finally
					{
						Monitor.Exit(obj2);
					}
				}
				catch (Exception)
				{
					object obj3 = this.responseQueue;
					Monitor.Enter(obj3);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(new Dictionary<string, int>());
						});
					}
					finally
					{
						Monitor.Exit(obj3);
					}
					this.Stop();
				}
			});
			this.commandEvent.Set();
		}
		finally
		{
			Monitor.Exit(obj);
		}
	}
	public void MoveToNewMap(string mapName, GameServerConnection.ResultCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					GameServerMessage gameServerMessage = new GameServerMessage(GameServerMessage.Command.MoveToNewMapCommand);
					BinaryWriter writer = gameServerMessage.GetWriter();
					writer.Write(mapName);
					gameServerMessage.Serialize(this.stream);
					this.bytesSent += gameServerMessage.length;
					GameServerMessage gameServerMessage2 = GameServerMessage.DeserializeResponse(this.stream);
					this.bytesRecv += gameServerMessage2.length;
					bool result = gameServerMessage2.GetReader().ReadBoolean();
					object obj2 = this.responseQueue;
					Monitor.Enter(obj2);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(result);
						});
					}
					finally
					{
						Monitor.Exit(obj2);
					}
				}
				catch (Exception)
				{
					object obj3 = this.responseQueue;
					Monitor.Enter(obj3);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(false);
						});
					}
					finally
					{
						Monitor.Exit(obj3);
					}
					this.Stop();
				}
			});
			this.commandEvent.Set();
		}
		finally
		{
			Monitor.Exit(obj);
		}
	}
	public void Reload()
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					GameServerMessage gameServerMessage = new GameServerMessage(GameServerMessage.Command.ReloadCommand);
					gameServerMessage.Serialize(this.stream);
					this.bytesSent += gameServerMessage.length;
				}
				catch (Exception)
				{
					this.Stop();
				}
			});
			this.commandEvent.Set();
		}
		finally
		{
			Monitor.Exit(obj);
		}
	}
	public void Respawn(GameServerConnection.RespawnCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					GameServerMessage gameServerMessage = new GameServerMessage(GameServerMessage.Command.RespawnCommand);
					gameServerMessage.Serialize(this.stream);
					this.bytesSent += gameServerMessage.length;
					GameServerMessage gameServerMessage2 = GameServerMessage.DeserializeResponse(this.stream);
					this.bytesRecv += gameServerMessage2.length;
					BinaryReader reader = gameServerMessage2.GetReader();
					bool result = reader.ReadBoolean();
					Vector3 pos = new Vector3(0f, 0f, 0f);
					Vector3 rot = new Vector3(0f, 0f, 0f);
					int health = 0;
					if (result)
					{
						pos.x = reader.ReadSingle();
						pos.y = reader.ReadSingle();
						pos.z = reader.ReadSingle();
						rot.x = (float)reader.ReadByte() * 360f / 256f;
						rot.y = (float)reader.ReadByte() * 360f / 256f;
						rot.z = (float)reader.ReadByte() * 360f / 256f;
						health = reader.ReadInt32();
					}
					object obj2 = this.responseQueue;
					Monitor.Enter(obj2);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(result, pos, Quaternion.Euler(rot), health);
						});
					}
					finally
					{
						Monitor.Exit(obj2);
					}
				}
				catch (Exception)
				{
					object obj3 = this.responseQueue;
					Monitor.Enter(obj3);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(false, new Vector3(0f, 0f, 0f), Quaternion.identity, 0);
						});
					}
					finally
					{
						Monitor.Exit(obj3);
					}
					this.Stop();
				}
			});
			this.commandEvent.Set();
		}
		finally
		{
			Monitor.Exit(obj);
		}
	}
	public void Interact(string name)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					GameServerMessage gameServerMessage = new GameServerMessage(GameServerMessage.Command.InteractCommand);
					BinaryWriter writer = gameServerMessage.GetWriter();
					writer.Write(name);
					gameServerMessage.Serialize(this.stream);
					this.bytesSent += gameServerMessage.length;
				}
				catch (Exception)
				{
					this.Stop();
				}
			});
			this.commandEvent.Set();
		}
		finally
		{
			Monitor.Exit(obj);
		}
	}
	public void DrinkWine(int reduction)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					GameServerMessage gameServerMessage = new GameServerMessage(GameServerMessage.Command.DrinkWineCommand);
					BinaryWriter writer = gameServerMessage.GetWriter();
					writer.Write(reduction);
					gameServerMessage.Serialize(this.stream);
					this.bytesSent += gameServerMessage.length;
				}
				catch (Exception)
				{
					this.Stop();
				}
			});
			this.commandEvent.Set();
		}
		finally
		{
			Monitor.Exit(obj);
		}
	}
	public void ValidateProductRegistration(ProductKeyVerifier.ProductType product)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					GameServerMessage gameServerMessage = new GameServerMessage(GameServerMessage.Command.ValidateProductRegistrationCommand);
					BinaryWriter writer = gameServerMessage.GetWriter();
					writer.Write((int)product);
					gameServerMessage.Serialize(this.stream);
					this.bytesSent += gameServerMessage.length;
				}
				catch (Exception)
				{
					this.Stop();
				}
			});
			this.commandEvent.Set();
		}
		finally
		{
			Monitor.Exit(obj);
		}
	}
	public void IAPPurchase(string name, int quantity, GameServerConnection.PurchaseCallback callback)
	{
		if (quantity < 0)
		{
			object obj = this.responseQueue;
			Monitor.Enter(obj);
			try
			{
				this.responseQueue.Enqueue(delegate
				{
					callback(false, "Invalid quantity.");
				});
			}
			finally
			{
				Monitor.Exit(obj);
			}
			return;
		}
		object obj2 = this.commandQueue;
		Monitor.Enter(obj2);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					GameServerMessage gameServerMessage = new GameServerMessage(GameServerMessage.Command.IAPPurchaseCommand);
					BinaryWriter writer = gameServerMessage.GetWriter();
					writer.Write(quantity);
					writer.Write(name);
					gameServerMessage.Serialize(this.stream);
					this.bytesSent += gameServerMessage.length;
					GameServerMessage gameServerMessage2 = GameServerMessage.DeserializeResponse(this.stream);
					this.bytesRecv += gameServerMessage2.length;
					BinaryReader reader = gameServerMessage2.GetReader();
					bool result = reader.ReadBoolean();
					string error = null;
					if (!result)
					{
						error = reader.ReadString();
					}
					object obj3 = this.responseQueue;
					Monitor.Enter(obj3);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(result, error);
						});
					}
					finally
					{
						Monitor.Exit(obj3);
					}
				}
				catch (Exception)
				{
					object obj4 = this.responseQueue;
					Monitor.Enter(obj4);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(false, "Server command failed");
						});
					}
					finally
					{
						Monitor.Exit(obj4);
					}
					this.Stop();
				}
			});
			this.commandEvent.Set();
		}
		finally
		{
			Monitor.Exit(obj2);
		}
	}
	public void ActivateQuest(string name)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					GameServerMessage gameServerMessage = new GameServerMessage(GameServerMessage.Command.ActivateQuestCommand);
					BinaryWriter writer = gameServerMessage.GetWriter();
					writer.Write(name);
					gameServerMessage.Serialize(this.stream);
					this.bytesSent += gameServerMessage.length;
				}
				catch (Exception)
				{
					this.Stop();
				}
			});
			this.commandEvent.Set();
		}
		finally
		{
			Monitor.Exit(obj);
		}
	}
	public void Chat(string text)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					GameServerMessage gameServerMessage = new GameServerMessage(GameServerMessage.Command.ChatCommand);
					BinaryWriter writer = gameServerMessage.GetWriter();
					writer.Write(text);
					gameServerMessage.Serialize(this.stream);
					this.bytesSent += gameServerMessage.length;
				}
				catch (Exception)
				{
					this.Stop();
				}
			});
			this.commandEvent.Set();
		}
		finally
		{
			Monitor.Exit(obj);
		}
	}
	public void TogglePVPFlag()
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					GameServerMessage gameServerMessage = new GameServerMessage(GameServerMessage.Command.TogglePVPFlagCommand);
					gameServerMessage.Serialize(this.stream);
					this.bytesSent += gameServerMessage.length;
				}
				catch (Exception)
				{
					this.Stop();
				}
			});
			this.commandEvent.Set();
		}
		finally
		{
			Monitor.Exit(obj);
		}
	}
}
