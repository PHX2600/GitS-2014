using System;
using System.IO;
public class GameServerMessage
{
	public enum Command
	{
		ResponseCommand,
		LoginCommand = 1313296204,
		SpectatorCommand = 1464158550,
		SpectatorUpdateCommand = 1263488844,
		UpdateCommand = 1163284301,
		PrepareAttackCommand = 1497646418,
		AttackCommand = 1163020614,
		JumpCommand = 1347245386,
		SwitchWeaponCommand = 1346459475,
		SetWeaponSlotsCommand = 1414483027,
		GetObjectLootCommand = 1514227797,
		TakeObjectLootCommand = 1414483788,
		IsNamedObjectLootAvailableCommand = 1514227790,
		TakeNamedObjectLootCommand = 1313554772,
		BuyCommand = 1498759753,
		SellCommand = 1280066899,
		QuestKillCommand = 1280067915,
		NPCTalkCommand = 1263288660,
		NPCFinishTextCommand = 1415071054,
		NPCBuyListCommand = 1380337231,
		MoveToNewMapCommand = 1346456910,
		ReloadCommand = 1145130828,
		RespawnCommand = 1163086162,
		InteractCommand = 1163089225,
		DrinkWineCommand = 1162758487,
		ValidateProductRegistrationCommand = 1447251012,
		IAPPurchaseCommand = 609239369,
		ActivateQuestCommand = 1364350275,
		ChatCommand = 1413564483,
		TogglePVPFlagCommand = 1179678288
	}
	private GameServerMessage.Command command;
	private MemoryStream stream;
	private BinaryReader reader;
	private BinaryWriter writer;
	public int length
	{
		get
		{
			return (int)this.stream.Length + 2;
		}
	}
	public GameServerMessage(GameServerMessage.Command cmd)
	{
		this.command = cmd;
		this.stream = new MemoryStream();
		this.writer = new BinaryWriter(this.stream);
		if (this.command != GameServerMessage.Command.ResponseCommand)
		{
			this.writer.Write((int)cmd);
		}
	}
	private GameServerMessage(byte[] cmd, bool response)
	{
		this.stream = new MemoryStream(cmd);
		this.reader = new BinaryReader(this.stream);
		if (response)
		{
			this.command = GameServerMessage.Command.ResponseCommand;
		}
		else
		{
			this.command = (GameServerMessage.Command)this.reader.ReadInt32();
		}
	}
	public BinaryReader GetReader()
	{
		return this.reader;
	}
	public BinaryWriter GetWriter()
	{
		return this.writer;
	}
	public GameServerMessage.Command GetCommand()
	{
		return this.command;
	}
	public byte[] GetData()
	{
		return this.stream.ToArray();
	}
	public void WriteUpdate(GameServerUpdate update)
	{
		update.Serialize(this.writer);
	}
	public GameServerUpdate ReadUpdate()
	{
		return GameServerUpdate.Deserialize(this.reader);
	}
	public bool HasMoreUpdates()
	{
		return this.stream.Position < this.stream.Length;
	}
	public void Serialize(Stream output)
	{
		MemoryStream memoryStream = new MemoryStream();
		BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		byte[] data = this.GetData();
		binaryWriter.Write((ushort)data.Length);
		binaryWriter.Write(data);
		binaryWriter.Flush();
		binaryWriter = new BinaryWriter(output);
		binaryWriter.Write(memoryStream.ToArray());
		binaryWriter.Flush();
	}
	public static GameServerMessage Deserialize(Stream input)
	{
		BinaryReader binaryReader = new BinaryReader(input);
		int num = (int)binaryReader.ReadUInt16();
		byte[] array = binaryReader.ReadBytes(num);
		if (array.Length < num)
		{
			throw new IOException("Incomplete message");
		}
		return new GameServerMessage(array, false);
	}
	public static GameServerMessage DeserializeResponse(Stream input)
	{
		BinaryReader binaryReader = new BinaryReader(input);
		int num = (int)binaryReader.ReadUInt16();
		byte[] array = binaryReader.ReadBytes(num);
		if (array.Length < num)
		{
			throw new IOException("Incomplete message");
		}
		return new GameServerMessage(array, true);
	}
}
