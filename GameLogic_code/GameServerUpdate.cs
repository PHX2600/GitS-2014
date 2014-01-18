using System;
using System.IO;
using System.Threading;
using UnityEngine;
public class GameServerUpdate
{
	public enum UpdateType
	{
		ObjectPositionUpdate = 1397706831,
		ObjectSpawnUpdate = 1314345043,
		ObjectRespawnUpdate = 1163086162,
		ObjectDestroyUpdate = 1162170950,
		ObjectStateUpdate = 1413567571,
		ObjectAnimationUpdate = 1296649793,
		ObjectDeathUpdate = 1145128260,
		ObjectHealthUpdate = 1196246095,
		LocalPlayerHealthUpdate = 1162234188,
		TeleportLocalPlayerUpdate = 1163284301,
		PlayerJoinUpdate = 1313427274,
		ChatUpdate = 1413564483,
		AddItemUpdate = 1296389193,
		RemoveItemUpdate = 1162626372,
		LoadedAmmoUpdate = 1330466113,
		StartQuestUpdate = 1414681683,
		UpdateQuestUpdate = 1146115409,
		CompleteQuestUpdate = 1162760004,
		ActiveQuestUpdate = 1364350275,
		QuestKillCountUpdate = 1414415179,
		LoadCompleteUpdate = 1145130828,
		CountdownUpdate = 1162692948,
		ArenaTeamScoreUpdate = 1380143956,
		ArenaPlayerScoreUpdate = 1380143952,
		BossUpdate = 1397968706
	}
	private GameServerUpdate.UpdateType type;
	private MemoryStream stream;
	private BinaryReader reader;
	private BinaryWriter writer;
	public GameServerUpdate(GameServerUpdate.UpdateType t)
	{
		this.type = t;
		this.stream = new MemoryStream();
		this.writer = new BinaryWriter(this.stream);
		this.writer.Write((int)this.type);
	}
	private GameServerUpdate(byte[] update)
	{
		this.stream = new MemoryStream(update);
		this.reader = new BinaryReader(this.stream);
		this.type = (GameServerUpdate.UpdateType)this.reader.ReadInt32();
	}
	public BinaryReader GetReader()
	{
		return this.reader;
	}
	public BinaryWriter GetWriter()
	{
		return this.writer;
	}
	public GameServerUpdate.UpdateType GetUpdateType()
	{
		return this.type;
	}
	public byte[] GetData()
	{
		return this.stream.ToArray();
	}
	public void Serialize(BinaryWriter writer)
	{
		byte[] data = this.GetData();
		writer.Write((ushort)data.Length);
		writer.Write(data);
	}
	public static GameServerUpdate Deserialize(BinaryReader reader)
	{
		int num = (int)reader.ReadUInt16();
		byte[] array = reader.ReadBytes(num);
		if (array.Length < num)
		{
			throw new IOException("Incomplete message");
		}
		return new GameServerUpdate(array);
	}
	public static GameServerUpdate CreateObjectPositionUpdate(ReplicatedObject obj)
	{
		GameServerUpdate gameServerUpdate = new GameServerUpdate(GameServerUpdate.UpdateType.ObjectPositionUpdate);
		BinaryWriter binaryWriter = gameServerUpdate.GetWriter();
		Monitor.Enter(obj);
		Vector3 remotePosition;
		Vector3 eulerAngles;
		try
		{
			remotePosition = obj.remotePosition;
			eulerAngles = obj.remoteOrientation.eulerAngles;
		}
		finally
		{
			Monitor.Exit(obj);
		}
		binaryWriter.Write(obj.id);
		binaryWriter.Write(remotePosition.x);
		binaryWriter.Write(remotePosition.y);
		binaryWriter.Write(remotePosition.z);
		binaryWriter.Write((byte)((int)(eulerAngles.x * 256f / 360f) & 255));
		binaryWriter.Write((byte)((int)(eulerAngles.y * 256f / 360f) & 255));
		binaryWriter.Write((byte)((int)(eulerAngles.z * 256f / 360f) & 255));
		return gameServerUpdate;
	}
	public static GameServerUpdate CreateObjectSpawnUpdate(ReplicatedObject obj)
	{
		GameServerUpdate gameServerUpdate = new GameServerUpdate(GameServerUpdate.UpdateType.ObjectSpawnUpdate);
		BinaryWriter binaryWriter = gameServerUpdate.GetWriter();
		Monitor.Enter(obj);
		Vector3 remotePosition;
		Vector3 eulerAngles;
		try
		{
			remotePosition = obj.remotePosition;
			eulerAngles = obj.remoteOrientation.eulerAngles;
		}
		finally
		{
			Monitor.Exit(obj);
		}
		binaryWriter.Write(obj.id);
		binaryWriter.Write(obj.replicationName);
		binaryWriter.Write(remotePosition.x);
		binaryWriter.Write(remotePosition.y);
		binaryWriter.Write(remotePosition.z);
		binaryWriter.Write((byte)((int)(eulerAngles.x * 256f / 360f) & 255));
		binaryWriter.Write((byte)((int)(eulerAngles.y * 256f / 360f) & 255));
		binaryWriter.Write((byte)((int)(eulerAngles.z * 256f / 360f) & 255));
		return gameServerUpdate;
	}
	public static GameServerUpdate CreateObjectRespawnUpdate(ReplicatedObject obj, Vector3 pos, Quaternion rot, int health)
	{
		GameServerUpdate gameServerUpdate = new GameServerUpdate(GameServerUpdate.UpdateType.ObjectRespawnUpdate);
		BinaryWriter binaryWriter = gameServerUpdate.GetWriter();
		Vector3 eulerAngles = rot.eulerAngles;
		binaryWriter.Write(obj.id);
		binaryWriter.Write(pos.x);
		binaryWriter.Write(pos.y);
		binaryWriter.Write(pos.z);
		binaryWriter.Write((byte)((int)(eulerAngles.x * 256f / 360f) & 255));
		binaryWriter.Write((byte)((int)(eulerAngles.y * 256f / 360f) & 255));
		binaryWriter.Write((byte)((int)(eulerAngles.z * 256f / 360f) & 255));
		binaryWriter.Write(health);
		return gameServerUpdate;
	}
	public static GameServerUpdate CreateObjectDestroyUpdate(ReplicatedObject obj)
	{
		GameServerUpdate gameServerUpdate = new GameServerUpdate(GameServerUpdate.UpdateType.ObjectDestroyUpdate);
		BinaryWriter binaryWriter = gameServerUpdate.GetWriter();
		binaryWriter.Write(obj.id);
		return gameServerUpdate;
	}
	public static GameServerUpdate CreateObjectStateUpdate(ReplicatedObject obj, string name)
	{
		GameServerUpdate gameServerUpdate = new GameServerUpdate(GameServerUpdate.UpdateType.ObjectStateUpdate);
		BinaryWriter binaryWriter = gameServerUpdate.GetWriter();
		binaryWriter.Write(obj.id);
		binaryWriter.Write(name);
		return gameServerUpdate;
	}
	public static GameServerUpdate CreateObjectAnimationUpdate(ReplicatedObject obj, string name, bool val, byte random)
	{
		GameServerUpdate gameServerUpdate = new GameServerUpdate(GameServerUpdate.UpdateType.ObjectAnimationUpdate);
		BinaryWriter binaryWriter = gameServerUpdate.GetWriter();
		binaryWriter.Write(obj.id);
		binaryWriter.Write(name);
		binaryWriter.Write(val);
		binaryWriter.Write(random);
		return gameServerUpdate;
	}
	public static GameServerUpdate CreateObjectDeathUpdate(ReplicatedObject obj, ReplicatedObject killer)
	{
		GameServerUpdate gameServerUpdate = new GameServerUpdate(GameServerUpdate.UpdateType.ObjectDeathUpdate);
		BinaryWriter binaryWriter = gameServerUpdate.GetWriter();
		binaryWriter.Write(obj.id);
		binaryWriter.Write((!(killer != null)) ? -1 : killer.id);
		return gameServerUpdate;
	}
	public static GameServerUpdate CreateObjectHealthUpdate(ReplicatedObject obj, int health)
	{
		GameServerUpdate gameServerUpdate = new GameServerUpdate(GameServerUpdate.UpdateType.ObjectHealthUpdate);
		BinaryWriter binaryWriter = gameServerUpdate.GetWriter();
		binaryWriter.Write(obj.id);
		binaryWriter.Write(health);
		return gameServerUpdate;
	}
	public static GameServerUpdate CreateLocalPlayerHealthUpdate(int health)
	{
		GameServerUpdate gameServerUpdate = new GameServerUpdate(GameServerUpdate.UpdateType.LocalPlayerHealthUpdate);
		BinaryWriter binaryWriter = gameServerUpdate.GetWriter();
		binaryWriter.Write(health);
		return gameServerUpdate;
	}
	public static GameServerUpdate CreateTeleportLocalPlayerUpdate(Vector3 pos, Quaternion rot)
	{
		GameServerUpdate gameServerUpdate = new GameServerUpdate(GameServerUpdate.UpdateType.TeleportLocalPlayerUpdate);
		BinaryWriter binaryWriter = gameServerUpdate.GetWriter();
		Vector3 eulerAngles = rot.eulerAngles;
		binaryWriter.Write(pos.x);
		binaryWriter.Write(pos.y);
		binaryWriter.Write(pos.z);
		binaryWriter.Write((byte)((int)(eulerAngles.x * 256f / 360f) & 255));
		binaryWriter.Write((byte)((int)(eulerAngles.y * 256f / 360f) & 255));
		binaryWriter.Write((byte)((int)(eulerAngles.z * 256f / 360f) & 255));
		return gameServerUpdate;
	}
	public static GameServerUpdate CreatePlayerJoinUpdate(ReplicatedObject obj)
	{
		GameServerUpdate gameServerUpdate = new GameServerUpdate(GameServerUpdate.UpdateType.PlayerJoinUpdate);
		BinaryWriter binaryWriter = gameServerUpdate.GetWriter();
		Monitor.Enter(obj);
		Vector3 remotePosition;
		Vector3 eulerAngles;
		try
		{
			remotePosition = obj.remotePosition;
			eulerAngles = obj.remoteOrientation.eulerAngles;
		}
		finally
		{
			Monitor.Exit(obj);
		}
		binaryWriter.Write(obj.id);
		binaryWriter.Write(obj.replicationName);
		binaryWriter.Write(obj.GetComponent<Player>().team);
		binaryWriter.Write(obj.GetComponent<Player>().avatar);
		binaryWriter.Write(remotePosition.x);
		binaryWriter.Write(remotePosition.y);
		binaryWriter.Write(remotePosition.z);
		binaryWriter.Write((byte)((int)(eulerAngles.x * 256f / 360f) & 255));
		binaryWriter.Write((byte)((int)(eulerAngles.y * 256f / 360f) & 255));
		binaryWriter.Write((byte)((int)(eulerAngles.z * 256f / 360f) & 255));
		return gameServerUpdate;
	}
	public static GameServerUpdate CreateChatUpdate(string text)
	{
		GameServerUpdate gameServerUpdate = new GameServerUpdate(GameServerUpdate.UpdateType.ChatUpdate);
		BinaryWriter binaryWriter = gameServerUpdate.GetWriter();
		binaryWriter.Write(text);
		return gameServerUpdate;
	}
	public static GameServerUpdate CreateAddItemUpdate(string name, int count)
	{
		GameServerUpdate gameServerUpdate = new GameServerUpdate(GameServerUpdate.UpdateType.AddItemUpdate);
		BinaryWriter binaryWriter = gameServerUpdate.GetWriter();
		binaryWriter.Write(name);
		binaryWriter.Write(count);
		return gameServerUpdate;
	}
	public static GameServerUpdate CreateRemoveItemUpdate(string name, int count)
	{
		GameServerUpdate gameServerUpdate = new GameServerUpdate(GameServerUpdate.UpdateType.RemoveItemUpdate);
		BinaryWriter binaryWriter = gameServerUpdate.GetWriter();
		binaryWriter.Write(name);
		binaryWriter.Write(count);
		return gameServerUpdate;
	}
	public static GameServerUpdate CreateLoadedAmmoUpdate(string name, int count)
	{
		GameServerUpdate gameServerUpdate = new GameServerUpdate(GameServerUpdate.UpdateType.LoadedAmmoUpdate);
		BinaryWriter binaryWriter = gameServerUpdate.GetWriter();
		binaryWriter.Write(name);
		binaryWriter.Write(count);
		return gameServerUpdate;
	}
	public static GameServerUpdate CreateStartQuestUpdate(string name, string state)
	{
		GameServerUpdate gameServerUpdate = new GameServerUpdate(GameServerUpdate.UpdateType.StartQuestUpdate);
		BinaryWriter binaryWriter = gameServerUpdate.GetWriter();
		binaryWriter.Write(name);
		binaryWriter.Write(state);
		return gameServerUpdate;
	}
	public static GameServerUpdate CreateUpdateQuestUpdate(string name, string state)
	{
		GameServerUpdate gameServerUpdate = new GameServerUpdate(GameServerUpdate.UpdateType.UpdateQuestUpdate);
		BinaryWriter binaryWriter = gameServerUpdate.GetWriter();
		binaryWriter.Write(name);
		binaryWriter.Write(state);
		return gameServerUpdate;
	}
	public static GameServerUpdate CreateCompleteQuestUpdate(string name)
	{
		GameServerUpdate gameServerUpdate = new GameServerUpdate(GameServerUpdate.UpdateType.CompleteQuestUpdate);
		BinaryWriter binaryWriter = gameServerUpdate.GetWriter();
		binaryWriter.Write(name);
		return gameServerUpdate;
	}
	public static GameServerUpdate CreateActiveQuestUpdate(string name)
	{
		GameServerUpdate gameServerUpdate = new GameServerUpdate(GameServerUpdate.UpdateType.ActiveQuestUpdate);
		BinaryWriter binaryWriter = gameServerUpdate.GetWriter();
		binaryWriter.Write(name);
		return gameServerUpdate;
	}
	public static GameServerUpdate CreateQuestKillCountUpdate(string name, int count)
	{
		GameServerUpdate gameServerUpdate = new GameServerUpdate(GameServerUpdate.UpdateType.QuestKillCountUpdate);
		BinaryWriter binaryWriter = gameServerUpdate.GetWriter();
		binaryWriter.Write(name);
		binaryWriter.Write(count);
		return gameServerUpdate;
	}
	public static GameServerUpdate CreateLoadCompleteUpdate()
	{
		return new GameServerUpdate(GameServerUpdate.UpdateType.LoadCompleteUpdate);
	}
	public static GameServerUpdate CreateCountdownUpdate(int seconds, string desc)
	{
		GameServerUpdate gameServerUpdate = new GameServerUpdate(GameServerUpdate.UpdateType.CountdownUpdate);
		BinaryWriter binaryWriter = gameServerUpdate.GetWriter();
		binaryWriter.Write(seconds);
		binaryWriter.Write(desc);
		return gameServerUpdate;
	}
	public static GameServerUpdate CreateArenaTeamScoreUpdate(int team, int score)
	{
		GameServerUpdate gameServerUpdate = new GameServerUpdate(GameServerUpdate.UpdateType.ArenaTeamScoreUpdate);
		BinaryWriter binaryWriter = gameServerUpdate.GetWriter();
		binaryWriter.Write(team);
		binaryWriter.Write(score);
		return gameServerUpdate;
	}
	public static GameServerUpdate CreateArenaPlayerScoreUpdate(Player player)
	{
		GameServerUpdate gameServerUpdate = new GameServerUpdate(GameServerUpdate.UpdateType.ArenaPlayerScoreUpdate);
		BinaryWriter binaryWriter = gameServerUpdate.GetWriter();
		binaryWriter.Write(player.GetComponent<ReplicatedObject>().id);
		binaryWriter.Write(player.arenaTeamIndex);
		binaryWriter.Write(player.arenaKills);
		binaryWriter.Write(player.arenaAssists);
		binaryWriter.Write(player.arenaDeaths);
		return gameServerUpdate;
	}
	public static GameServerUpdate CreateStartBossUpdate()
	{
		GameServerUpdate gameServerUpdate = new GameServerUpdate(GameServerUpdate.UpdateType.BossUpdate);
		BinaryWriter binaryWriter = gameServerUpdate.GetWriter();
		binaryWriter.Write(true);
		return gameServerUpdate;
	}
	public static GameServerUpdate CreateEndBossUpdate()
	{
		GameServerUpdate gameServerUpdate = new GameServerUpdate(GameServerUpdate.UpdateType.BossUpdate);
		BinaryWriter binaryWriter = gameServerUpdate.GetWriter();
		binaryWriter.Write(false);
		return gameServerUpdate;
	}
}
