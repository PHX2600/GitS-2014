using System;
using System.Collections.Generic;
public class QuestState
{
	private string questName = "Default";
	private Dictionary<string, string> questStates = new Dictionary<string, string>();
	private Dictionary<string, string> questTextStates = new Dictionary<string, string>();
	private Dictionary<string, int> questKillCounts = new Dictionary<string, int>();
	public Player owner;
	public string currentQuest
	{
		get
		{
			return this.questName;
		}
	}
	public int currentQuestKillCount
	{
		get
		{
			if (!this.questKillCounts.ContainsKey(this.questName))
			{
				return 0;
			}
			return this.questKillCounts[this.questName];
		}
	}
	public string currentQuestState
	{
		get
		{
			if (!this.questStates.ContainsKey(this.questName))
			{
				return "";
			}
			return this.questStates[this.questName];
		}
	}
	public List<string> availableQuests
	{
		get
		{
			return new List<string>(this.questStates.Keys);
		}
	}
	public bool HasQuest(string name)
	{
		return this.questStates.ContainsKey(name);
	}
	public void StartQuest(string name, string state, string text)
	{
		this.questName = name;
		this.questStates[name] = state;
		this.questTextStates[name] = text;
		this.questKillCounts[name] = 0;
		if (GameState.isServer && GameState.IsMasterServerConnected())
		{
			GameState.masterServer.StartQuest(this.owner.id, name, state, text);
			this.owner.SendUpdate(GameServerUpdate.CreateStartQuestUpdate(name, state));
		}
	}
	public void UpdateQuest(string name, string state, string text)
	{
		this.questStates[name] = state;
		this.questTextStates[name] = text;
		this.questKillCounts[name] = 0;
		if (GameState.isServer && GameState.IsMasterServerConnected())
		{
			GameState.masterServer.UpdateQuest(this.owner.id, name, state, text);
			this.owner.SendUpdate(GameServerUpdate.CreateUpdateQuestUpdate(name, state));
		}
	}
	public void UpdateQuestTextState(string name, string state)
	{
		this.questTextStates[name] = state;
		if (GameState.isServer && GameState.IsMasterServerConnected())
		{
			GameState.masterServer.UpdateQuestTextState(this.owner.id, name, state);
		}
	}
	public void CompleteQuest()
	{
		string name = this.questName;
		this.questStates[this.questName] = "_End_";
		this.questName = null;
		if (GameState.isServer && GameState.IsMasterServerConnected())
		{
			GameState.masterServer.CompleteQuest(this.owner.id, name);
			this.owner.SendUpdate(GameServerUpdate.CreateCompleteQuestUpdate(name));
		}
	}
	public void SetActiveQuest(string name)
	{
		this.questName = name;
		if (GameState.isServer && GameState.IsMasterServerConnected())
		{
			GameState.masterServer.SetActiveQuest(this.owner.id, name);
			this.owner.SendUpdate(GameServerUpdate.CreateActiveQuestUpdate(name));
		}
	}
	public void IncrementKillCount(string name)
	{
		Dictionary<string, int> dictionary = this.questKillCounts;
		dictionary[name] = dictionary[name] + 1;
		if (GameState.isServer && GameState.IsMasterServerConnected())
		{
			GameState.masterServer.SetKillCount(this.owner.id, name, this.questKillCounts[name]);
			this.owner.SendUpdate(GameServerUpdate.CreateQuestKillCountUpdate(name, this.questKillCounts[name]));
		}
	}
	public void SetKillCount(string name, int count)
	{
		this.questKillCounts[name] = count;
		if (GameState.isServer && GameState.IsMasterServerConnected())
		{
			GameState.masterServer.SetKillCount(this.owner.id, name, this.questKillCounts[name]);
			this.owner.SendUpdate(GameServerUpdate.CreateQuestKillCountUpdate(name, this.questKillCounts[name]));
		}
	}
	public string GetQuestState(string name)
	{
		if (!this.questStates.ContainsKey(name))
		{
			return "";
		}
		return this.questStates[name];
	}
	public bool IsQuestComplete(string name)
	{
		return this.GetQuestState(name) == "_End_";
	}
	public string GetQuestTextState(string name)
	{
		if (!this.questTextStates.ContainsKey(name))
		{
			return "";
		}
		return this.questTextStates[name];
	}
	public int GetQuestKillCount(string name)
	{
		if (!this.questKillCounts.ContainsKey(name))
		{
			return 0;
		}
		return this.questKillCounts[name];
	}
	public void UpdateFromServerList(List<MasterServerConnection.QuestData> quests, string active)
	{
		this.questName = active;
		this.questStates = new Dictionary<string, string>();
		this.questTextStates = new Dictionary<string, string>();
		this.questKillCounts = new Dictionary<string, int>();
		foreach (MasterServerConnection.QuestData current in quests)
		{
			this.questStates.Add(current.name, current.state);
			this.questTextStates.Add(current.name, current.textState);
			this.questKillCounts.Add(current.name, current.killCount);
		}
	}
	public List<string> GetQuestList()
	{
		List<string> list = new List<string>();
		foreach (string current in this.questStates.Keys)
		{
			if (!(this.GetQuestState(current) == "_End_"))
			{
				list.Add(current);
			}
		}
		return list;
	}
}
