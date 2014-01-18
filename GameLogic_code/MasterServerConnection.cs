using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using UnityEngine;
public class MasterServerConnection
{
	private delegate void ProcessingFunction();
	public delegate void ResultCallback(bool ok);
	public delegate void ConnectCallback(bool ok, string error);
	public delegate void UserLoginCallback(bool ok, int id, string team);
	public delegate void LoginCallback(bool ok, int id);
	public delegate void RegisterResultCallback(bool ok, int id, string teamHash);
	public delegate void GetTeamHashCallback(string hash);
	public delegate void NameStatusCallback(MasterServerConnection.NameStatusResult result);
	public delegate void FindTeamByHashCallback(bool valid, string name);
	public delegate void CharacterListCallback(bool valid, List<MasterServerConnection.CharacterData> chars);
	public delegate void GetLocationCallback(string name);
	public delegate void GetItemsCallback(List<MasterServerConnection.InventoryItemData> items, string slot1, string slot2, string slot3, string slot4, int activeSlot);
	public delegate void GetQuestsCallback(List<MasterServerConnection.QuestData> items, string activeQuest);
	public delegate void JoinServerCallback(bool ok, string host, int port, string token);
	public delegate void GetKeyTextCallback(string text, bool submitted);
	public delegate void CheckForServerAssignmentCallback(bool valid, string map);
	public delegate void ValidateLoginTokenCallback(bool ok, string name, string team, int avatar);
	public delegate void TeamStateCallback(bool set, int val);
	public delegate void TeamScoreCallback(int score);
	public delegate void TopScoreCallback(List<MasterServerConnection.ScoreData> scores);
	public delegate void SubmitCallback(bool ok, string message);
	public delegate void ChallengeTextCallback(string text, string url, int points);
	public delegate void CharacterCountCallback(int count);
	public delegate void ActiveServerListCallback(List<MasterServerConnection.ServerData> servers);
	public delegate void ChallengeListCallback(List<string> open, List<string> solved);
	private enum ClientCommand
	{
		LoginCommand,
		RegisterCommand,
		GetTeamHashCommand,
		UserNameStatusCommand,
		TeamNameStatusCommand,
		CharacterNameStatusCommand,
		FindTeamByHashCommand,
		CheckForProductCommand,
		RegisterProductKeyCommand,
		IsRegistrationAllowedCommand,
		CreateCharacterCommand = 16,
		CharacterLoginCommand,
		CharacterListCommand,
		RemoveCharacterCommand,
		CurrentCharacterLocationCommand = 32,
		CurrentCharacterItemsCommand,
		CurrentCharacterQuestsCommand,
		JoinServerCommand,
		GetKeyTextCommand,
		GetCharacterItemsCommand = 48,
		UpdateCharacterItemsCommand,
		UpdateCharacterLocationCommand,
		IsNamedObjectLootAvailableCommand,
		MarkNamedObjectAsLootedCommand,
		GetTeamStateCommand,
		SetTeamStateCommand,
		GetCharacterQuestsCommand = 64,
		StartQuestCommand,
		UpdateQuestCommand,
		UpdateQuestTextStateCommand,
		CompleteQuestCommand,
		SetActiveQuestCommand,
		SetKillCountCommand,
		AddServerToPoolCommand = 80,
		ServerLogCommand,
		CheckForServerAssignmentCommand,
		ValidateLoginTokenCommand,
		FreeServerAssignmentCommand,
		IsServerAssignmentValidCommand,
		ValidateProductRegistrationCommand,
		GetTeamScoreCommand = 96,
		GetTopScoresCommand,
		HasSolvedCommand,
		SubmitFlagCommand,
		SubmitFlagAsItemCommand,
		IsChallengeAvailableCommand,
		GetChallengeTextCommand,
		GetChallengesCommand,
		LoggedInCharacterCountCommand = 112,
		GetActiveServerListCommand,
		KeepAliveCommand = 127
	}
	public enum NameStatusResult
	{
		NameInvalid,
		NameAlreadyUsed,
		NameAvailable,
		NameStatusUnknown = -1
	}
	public struct CharacterData
	{
		public string name;
		public int avatar;
	}
	public struct InventoryItemData
	{
		public string name;
		public int count;
		public int loadedAmmo;
	}
	public struct QuestData
	{
		public string name;
		public string state;
		public string textState;
		public int killCount;
	}
	public struct ScoreData
	{
		public string team;
		public int score;
		public bool online;
	}
	public struct ServerData
	{
		public string map;
		public string host;
		public int port;
		public int players;
	}
	private const int version = 9;
	private const bool sslEnabled = true;
	private Thread thread;
	private Stream stream;
	private BinaryReader reader;
	private BinaryWriter writer;
	private AutoResetEvent commandEvent;
	private bool running = true;
	private string host;
	private int port;
	private Queue<MasterServerConnection.ProcessingFunction> commandQueue = new Queue<MasterServerConnection.ProcessingFunction>();
	private Queue<MasterServerConnection.ProcessingFunction> responseQueue = new Queue<MasterServerConnection.ProcessingFunction>();
	public bool connected
	{
		get
		{
			return this.stream != null;
		}
	}
	public MasterServerConnection(string _host, int _port, MasterServerConnection.ConnectCallback cb)
	{
		this.host = _host;
		this.port = _port;
		this.commandEvent = new AutoResetEvent(false);
		this.thread = new Thread(new ThreadStart(this.CommandProcessingThread));
		this.thread.IsBackground = true;
		string dataPath = Application.dataPath;
		this.commandQueue.Enqueue(delegate
		{
			try
			{
				this.stream = this.Connect();
				if (this.stream == null)
				{
					object obj = this.responseQueue;
					Monitor.Enter(obj);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							cb(false, "Unable to connect to master server");
						});
					}
					finally
					{
						Monitor.Exit(obj);
					}
				}
				else
				{
					X509Certificate cert;
					try
					{
						cert = new X509Certificate2(Path.Combine(dataPath, "master.crt"));
					}
					catch (Exception)
					{
						object obj2 = this.responseQueue;
						Monitor.Enter(obj2);
						try
						{
							this.responseQueue.Enqueue(delegate
							{
								cb(false, "Server certificate missing");
							});
						}
						finally
						{
							Monitor.Exit(obj2);
						}
						return;
					}
					SslStream sslStream = new SslStream(this.stream, false, delegate(object sender, X509Certificate serverCert, X509Chain chain, SslPolicyErrors errors)
					{
						if (errors == SslPolicyErrors.None)
						{
							return true;
						}
						byte[] certHash = cert.GetCertHash();
						byte[] certHash2 = serverCert.GetCertHash();
						if (certHash.Length != certHash2.Length)
						{
							return false;
						}
						for (int i = 0; i < certHash.Length; i++)
						{
							if (certHash[i] != certHash2[i])
							{
								return false;
							}
						}
						return true;
					}, null);
					this.stream = sslStream;
					try
					{
						sslStream.AuthenticateAsClient("master");
						sslStream.Flush();
					}
					catch (Exception)
					{
						object obj3 = this.responseQueue;
						Monitor.Enter(obj3);
						try
						{
							this.responseQueue.Enqueue(delegate
							{
								cb(false, "Secure connection failed");
							});
						}
						finally
						{
							Monitor.Exit(obj3);
						}
						return;
					}
					this.reader = new BinaryReader(this.stream);
					this.writer = new BinaryWriter(this.stream);
					this.writer.Write("PWN2");
					int num = this.reader.ReadInt32();
					if (9 < num)
					{
						object obj4 = this.responseQueue;
						Monitor.Enter(obj4);
						try
						{
							this.responseQueue.Enqueue(delegate
							{
								cb(false, "Game version is out of date");
							});
						}
						finally
						{
							Monitor.Exit(obj4);
						}
					}
					else
					{
						if (9 > num)
						{
							object obj5 = this.responseQueue;
							Monitor.Enter(obj5);
							try
							{
								this.responseQueue.Enqueue(delegate
								{
									cb(false, "Master server is out of date");
								});
							}
							finally
							{
								Monitor.Exit(obj5);
							}
						}
						else
						{
							object obj6 = this.responseQueue;
							Monitor.Enter(obj6);
							try
							{
								this.responseQueue.Enqueue(delegate
								{
									cb(true, null);
								});
							}
							finally
							{
								Monitor.Exit(obj6);
							}
						}
					}
				}
			}
			catch (Exception)
			{
				object obj7 = this.responseQueue;
				Monitor.Enter(obj7);
				try
				{
					this.responseQueue.Enqueue(delegate
					{
						cb(false, "Unable to connect to master server");
					});
				}
				finally
				{
					Monitor.Exit(obj7);
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
			result = new TcpClient(this.host, this.port).GetStream();
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
					MasterServerConnection.ProcessingFunction processingFunction;
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
	public void Login(string name, string password, MasterServerConnection.UserLoginCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(0);
					this.writer.Write(name);
					this.writer.Write(password);
					this.writer.Flush();
					bool result = this.reader.ReadBoolean();
					int id = -1;
					string team = null;
					if (result)
					{
						id = this.reader.ReadInt32();
						team = this.reader.ReadString();
					}
					object obj2 = this.responseQueue;
					Monitor.Enter(obj2);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(result, id, team);
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
							callback(false, -1, null);
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
	public void Register(string name, string team, string password, MasterServerConnection.RegisterResultCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(1);
					this.writer.Write(name);
					this.writer.Write(team);
					this.writer.Write(password);
					this.writer.Flush();
					bool result = this.reader.ReadBoolean();
					int id = -1;
					string teamHash = null;
					if (result)
					{
						id = this.reader.ReadInt32();
						teamHash = this.reader.ReadString();
					}
					object obj2 = this.responseQueue;
					Monitor.Enter(obj2);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(result, id, teamHash);
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
							callback(false, -1, null);
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
	public void GetTeamHash(MasterServerConnection.GetTeamHashCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(2);
					this.writer.Flush();
					string hash = this.reader.ReadString();
					object obj2 = this.responseQueue;
					Monitor.Enter(obj2);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(hash);
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
							callback(null);
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
	public void GetUserNameStatus(string name, MasterServerConnection.NameStatusCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(3);
					this.writer.Write(name);
					this.writer.Flush();
					MasterServerConnection.NameStatusResult result = (MasterServerConnection.NameStatusResult)this.reader.ReadByte();
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
							callback(MasterServerConnection.NameStatusResult.NameStatusUnknown);
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
	public void GetTeamNameStatus(string name, MasterServerConnection.NameStatusCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(4);
					this.writer.Write(name);
					this.writer.Flush();
					MasterServerConnection.NameStatusResult result = (MasterServerConnection.NameStatusResult)this.reader.ReadByte();
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
							callback(MasterServerConnection.NameStatusResult.NameStatusUnknown);
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
	public void GetCharacterNameStatus(string name, MasterServerConnection.NameStatusCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(5);
					this.writer.Write(name);
					this.writer.Flush();
					MasterServerConnection.NameStatusResult result = (MasterServerConnection.NameStatusResult)this.reader.ReadByte();
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
							callback(MasterServerConnection.NameStatusResult.NameStatusUnknown);
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
	public void FindTeamByHash(string hash, MasterServerConnection.FindTeamByHashCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(6);
					this.writer.Write(hash);
					this.writer.Flush();
					bool result = this.reader.ReadBoolean();
					string name = null;
					if (result)
					{
						name = this.reader.ReadString();
					}
					object obj2 = this.responseQueue;
					Monitor.Enter(obj2);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(result, name);
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
	public void CheckForProduct(ProductKeyVerifier.ProductType type, MasterServerConnection.ResultCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(7);
					this.writer.Write((int)type);
					this.writer.Flush();
					bool result = this.reader.ReadBoolean();
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
	public void RegisterProductKey(ProductKeyVerifier.ProductType type, string key, MasterServerConnection.ResultCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(8);
					this.writer.Write((int)type);
					this.writer.Write(key);
					this.writer.Flush();
					bool result = this.reader.ReadBoolean();
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
	public void IsRegistrationAllowed(MasterServerConnection.ResultCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(9);
					this.writer.Flush();
					bool result = this.reader.ReadBoolean();
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
	public void CreateCharacter(string name, int avatar, MasterServerConnection.LoginCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(16);
					this.writer.Write(name);
					this.writer.Write(avatar);
					this.writer.Flush();
					bool result = this.reader.ReadBoolean();
					int id = -1;
					if (result)
					{
						id = this.reader.ReadInt32();
					}
					object obj2 = this.responseQueue;
					Monitor.Enter(obj2);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(result, id);
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
	public void CharacterLogin(string name, MasterServerConnection.LoginCallback callback)
	{
		GameState.previousExit = "";
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(17);
					this.writer.Write(name);
					this.writer.Flush();
					bool result = this.reader.ReadBoolean();
					int id = -1;
					if (result)
					{
						id = this.reader.ReadInt32();
					}
					object obj2 = this.responseQueue;
					Monitor.Enter(obj2);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(result, id);
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
	public void GetCharacterList(MasterServerConnection.CharacterListCallback callback)
	{
		List<MasterServerConnection.CharacterData> chars = new List<MasterServerConnection.CharacterData>();
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(18);
					this.writer.Flush();
					int num = this.reader.ReadInt32();
					for (int i = 0; i < num; i++)
					{
						MasterServerConnection.CharacterData item;
						item.name = this.reader.ReadString();
						item.avatar = this.reader.ReadInt32();
						chars.Add(item);
					}
					object obj2 = this.responseQueue;
					Monitor.Enter(obj2);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(true, chars);
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
	public void RemoveCharacter(string name, MasterServerConnection.ResultCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(19);
					this.writer.Write(name);
					this.writer.Flush();
					bool result = this.reader.ReadBoolean();
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
	public void GetCurrentCharacterLocation(MasterServerConnection.GetLocationCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(32);
					this.writer.Flush();
					string name = this.reader.ReadString();
					object obj2 = this.responseQueue;
					Monitor.Enter(obj2);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(name);
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
							callback(null);
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
	public void GetCurrentCharacterItems(MasterServerConnection.GetItemsCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(33);
					this.writer.Flush();
					List<MasterServerConnection.InventoryItemData> items = new List<MasterServerConnection.InventoryItemData>();
					int num = this.reader.ReadInt32();
					for (int i = 0; i < num; i++)
					{
						MasterServerConnection.InventoryItemData item;
						item.name = this.reader.ReadString();
						item.count = this.reader.ReadInt32();
						item.loadedAmmo = this.reader.ReadInt32();
						items.Add(item);
					}
					string slot1 = this.reader.ReadString();
					string slot2 = this.reader.ReadString();
					string slot3 = this.reader.ReadString();
					string slot4 = this.reader.ReadString();
					int activeSlot = this.reader.ReadInt32();
					object obj2 = this.responseQueue;
					Monitor.Enter(obj2);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(items, slot1, slot2, slot3, slot4, activeSlot);
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
							callback(null, null, null, null, null, -1);
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
	public void GetCurrentCharacterQuests(MasterServerConnection.GetQuestsCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(34);
					this.writer.Flush();
					List<MasterServerConnection.QuestData> quests = new List<MasterServerConnection.QuestData>();
					int num = this.reader.ReadInt32();
					for (int i = 0; i < num; i++)
					{
						MasterServerConnection.QuestData item;
						item.name = this.reader.ReadString();
						item.state = this.reader.ReadString();
						item.textState = this.reader.ReadString();
						item.killCount = this.reader.ReadInt32();
						quests.Add(item);
					}
					string activeQuest = this.reader.ReadString();
					object obj2 = this.responseQueue;
					Monitor.Enter(obj2);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(quests, activeQuest);
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
							callback(null, null);
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
	public void JoinServer(MasterServerConnection.JoinServerCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(35);
					this.writer.Flush();
					string host = this.reader.ReadString();
					int port = this.reader.ReadInt32();
					string token = this.reader.ReadString();
					object obj2 = this.responseQueue;
					Monitor.Enter(obj2);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(true, host, port, token);
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
							callback(false, null, 0, null);
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
	public void GetKeyText(string name, MasterServerConnection.GetKeyTextCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(36);
					this.writer.Write(name);
					this.writer.Flush();
					string text = this.reader.ReadString();
					bool submitted = this.reader.ReadBoolean();
					object obj2 = this.responseQueue;
					Monitor.Enter(obj2);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(text, submitted);
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
							callback("Failed to fetch key!", false);
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
	public void GetCharacterItems(int id, MasterServerConnection.GetItemsCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(48);
					this.writer.Write(id);
					this.writer.Flush();
					List<MasterServerConnection.InventoryItemData> items = new List<MasterServerConnection.InventoryItemData>();
					int num = this.reader.ReadInt32();
					for (int i = 0; i < num; i++)
					{
						MasterServerConnection.InventoryItemData item;
						item.name = this.reader.ReadString();
						item.count = this.reader.ReadInt32();
						item.loadedAmmo = this.reader.ReadInt32();
						items.Add(item);
					}
					string slot1 = this.reader.ReadString();
					string slot2 = this.reader.ReadString();
					string slot3 = this.reader.ReadString();
					string slot4 = this.reader.ReadString();
					int activeSlot = this.reader.ReadInt32();
					object obj2 = this.responseQueue;
					Monitor.Enter(obj2);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(items, slot1, slot2, slot3, slot4, activeSlot);
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
							callback(null, null, null, null, null, -1);
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
	public void UpdateCharacterItems(int id, List<MasterServerConnection.InventoryItemData> items, string slot1, string slot2, string slot3, string slot4, int activeSlot)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(49);
					this.writer.Write(id);
					this.writer.Write(items.Count);
					foreach (MasterServerConnection.InventoryItemData current in items)
					{
						this.writer.Write(current.name);
						this.writer.Write(current.count);
						this.writer.Write(current.loadedAmmo);
					}
					this.writer.Write(slot1);
					this.writer.Write(slot2);
					this.writer.Write(slot3);
					this.writer.Write(slot4);
					this.writer.Write(activeSlot);
					this.writer.Flush();
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
	public void UpdateCharacterLocation(int id, string name)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(50);
					this.writer.Write(id);
					this.writer.Write(name);
					this.writer.Flush();
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
	public void IsNamedObjectLootAvailable(int id, string name, MasterServerConnection.ResultCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(51);
					this.writer.Write(id);
					this.writer.Write(name);
					this.writer.Flush();
					bool result = this.reader.ReadBoolean();
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
	public void MarkNamedObjectAsLooted(int id, string name)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(52);
					this.writer.Write(id);
					this.writer.Write(name);
					this.writer.Flush();
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
	public void GetTeamState(string name, MasterServerConnection.TeamStateCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(53);
					this.writer.Write(name);
					this.writer.Flush();
					bool set = this.reader.ReadBoolean();
					int val = -1;
					if (set)
					{
						val = this.reader.ReadInt32();
					}
					object obj2 = this.responseQueue;
					Monitor.Enter(obj2);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(set, val);
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
	public void SetTeamState(string name, int val)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(54);
					this.writer.Write(name);
					this.writer.Write(val);
					this.writer.Flush();
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
	public void GetCharacterQuests(int id, MasterServerConnection.GetQuestsCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(64);
					this.writer.Write(id);
					this.writer.Flush();
					List<MasterServerConnection.QuestData> quests = new List<MasterServerConnection.QuestData>();
					int num = this.reader.ReadInt32();
					for (int i = 0; i < num; i++)
					{
						MasterServerConnection.QuestData item;
						item.name = this.reader.ReadString();
						item.state = this.reader.ReadString();
						item.textState = this.reader.ReadString();
						item.killCount = this.reader.ReadInt32();
						quests.Add(item);
					}
					string activeQuest = this.reader.ReadString();
					object obj2 = this.responseQueue;
					Monitor.Enter(obj2);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(quests, activeQuest);
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
							callback(null, null);
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
	public void StartQuest(int id, string name, string state, string text)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(65);
					this.writer.Write(id);
					this.writer.Write(name);
					this.writer.Write(state);
					this.writer.Write(text);
					this.writer.Flush();
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
	public void UpdateQuest(int id, string name, string state, string text)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(66);
					this.writer.Write(id);
					this.writer.Write(name);
					this.writer.Write(state);
					this.writer.Write(text);
					this.writer.Flush();
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
	public void UpdateQuestTextState(int id, string name, string state)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(67);
					this.writer.Write(id);
					this.writer.Write(name);
					this.writer.Write(state);
					this.writer.Flush();
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
	public void CompleteQuest(int id, string name)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(68);
					this.writer.Write(id);
					this.writer.Write(name);
					this.writer.Flush();
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
	public void SetActiveQuest(int id, string name)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(69);
					this.writer.Write(id);
					this.writer.Write(name);
					this.writer.Flush();
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
	public void SetKillCount(int id, string name, int count)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(70);
					this.writer.Write(id);
					this.writer.Write(name);
					this.writer.Write(count);
					this.writer.Flush();
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
	public void AddServerToPool(string host, int port)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(80);
					this.writer.Write(host);
					this.writer.Write(port);
					this.writer.Flush();
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
	public void ServerLog(string msg)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(81);
					this.writer.Write(msg);
					this.writer.Flush();
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
	public void CheckForServerAssignment(MasterServerConnection.CheckForServerAssignmentCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(82);
					this.writer.Flush();
					bool valid = this.reader.ReadBoolean();
					string map = null;
					if (valid)
					{
						map = this.reader.ReadString();
					}
					object obj2 = this.responseQueue;
					Monitor.Enter(obj2);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(valid, map);
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
	public void ValidateLoginToken(int id, string token, MasterServerConnection.ValidateLoginTokenCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(83);
					this.writer.Write(id);
					this.writer.Write(token);
					this.writer.Flush();
					bool valid = this.reader.ReadBoolean();
					string name = null;
					string team = null;
					int avatar = -1;
					if (valid)
					{
						name = this.reader.ReadString();
						team = this.reader.ReadString();
						avatar = this.reader.ReadInt32();
					}
					object obj2 = this.responseQueue;
					Monitor.Enter(obj2);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(valid, name, team, avatar);
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
							callback(false, null, null, -1);
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
	public void FreeServerAssignment()
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(84);
					this.writer.Flush();
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
	public void IsServerAssignmentValid(string mapName, MasterServerConnection.ResultCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(85);
					this.writer.Write(mapName);
					this.writer.Flush();
					bool valid = this.reader.ReadBoolean();
					object obj2 = this.responseQueue;
					Monitor.Enter(obj2);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(valid);
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
	public void ValidateProductRegistration(int user, ProductKeyVerifier.ProductType type, MasterServerConnection.ResultCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(86);
					this.writer.Write(user);
					this.writer.Write((int)type);
					this.writer.Flush();
					bool valid = this.reader.ReadBoolean();
					object obj2 = this.responseQueue;
					Monitor.Enter(obj2);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(valid);
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
	public void GetTeamScore(MasterServerConnection.TeamScoreCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(96);
					this.writer.Flush();
					int score = this.reader.ReadInt32();
					object obj2 = this.responseQueue;
					Monitor.Enter(obj2);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(score);
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
							callback(0);
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
	public void GetTopScores(MasterServerConnection.TopScoreCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(97);
					this.writer.Flush();
					int num = this.reader.ReadInt32();
					List<MasterServerConnection.ScoreData> result = new List<MasterServerConnection.ScoreData>();
					for (int i = 0; i < num; i++)
					{
						MasterServerConnection.ScoreData item;
						item.team = this.reader.ReadString();
						item.score = this.reader.ReadInt32();
						item.online = this.reader.ReadBoolean();
						result.Add(item);
					}
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
							callback(new List<MasterServerConnection.ScoreData>());
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
	public void HasSolved(string problem, MasterServerConnection.ResultCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(98);
					this.writer.Write(problem);
					this.writer.Flush();
					bool result = this.reader.ReadBoolean();
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
	public void SubmitFlag(string problem, string flag, MasterServerConnection.SubmitCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(99);
					this.writer.Write(problem);
					this.writer.Write(flag);
					this.writer.Flush();
					bool result = this.reader.ReadBoolean();
					string message = this.reader.ReadString();
					object obj2 = this.responseQueue;
					Monitor.Enter(obj2);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(result, message);
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
							callback(false, "Failed to submit flag.");
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
	public void SubmitFlagAsItem(string name, MasterServerConnection.SubmitCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(100);
					this.writer.Write(name);
					this.writer.Flush();
					bool result = this.reader.ReadBoolean();
					string message = this.reader.ReadString();
					object obj2 = this.responseQueue;
					Monitor.Enter(obj2);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(result, message);
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
							callback(false, "Failed to submit flag.");
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
	public void IsChallengeAvailable(string name, MasterServerConnection.ResultCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(101);
					this.writer.Write(name);
					this.writer.Flush();
					bool result = this.reader.ReadBoolean();
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
	public void GetChallengeText(string name, MasterServerConnection.ChallengeTextCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(102);
					this.writer.Write(name);
					this.writer.Flush();
					string text = this.reader.ReadString();
					string url = this.reader.ReadString();
					int points = this.reader.ReadInt32();
					object obj2 = this.responseQueue;
					Monitor.Enter(obj2);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(text, url, points);
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
							callback("", "", 0);
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
	public void GetChallenges(MasterServerConnection.ChallengeListCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(103);
					this.writer.Flush();
					List<string> open = new List<string>();
					List<string> solved = new List<string>();
					int num = this.reader.ReadInt32();
					for (int i = 0; i < num; i++)
					{
						open.Add(this.reader.ReadString());
					}
					int num2 = this.reader.ReadInt32();
					for (int j = 0; j < num2; j++)
					{
						solved.Add(this.reader.ReadString());
					}
					object obj2 = this.responseQueue;
					Monitor.Enter(obj2);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(open, solved);
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
							callback(new List<string>(), new List<string>());
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
	public void SendKeepAlive()
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(127);
					this.writer.Flush();
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
	public void GetLoggedInCharacterCount(MasterServerConnection.CharacterCountCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(112);
					this.writer.Flush();
					int count = this.reader.ReadInt32();
					object obj2 = this.responseQueue;
					Monitor.Enter(obj2);
					try
					{
						this.responseQueue.Enqueue(delegate
						{
							callback(count);
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
							callback(0);
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
	public void GetActiveServerList(MasterServerConnection.ActiveServerListCallback callback)
	{
		object obj = this.commandQueue;
		Monitor.Enter(obj);
		try
		{
			this.commandQueue.Enqueue(delegate
			{
				try
				{
					this.writer.Write(113);
					this.writer.Flush();
					List<MasterServerConnection.ServerData> result = new List<MasterServerConnection.ServerData>();
					int num = this.reader.ReadInt32();
					for (int i = 0; i < num; i++)
					{
						string map = this.reader.ReadString();
						int num2 = this.reader.ReadInt32();
						for (int j = 0; j < num2; j++)
						{
							MasterServerConnection.ServerData item;
							item.map = map;
							item.host = this.reader.ReadString();
							item.port = this.reader.ReadInt32();
							item.players = this.reader.ReadInt32();
							result.Add(item);
						}
					}
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
							callback(new List<MasterServerConnection.ServerData>());
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
}
