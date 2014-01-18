using System;
using System.Collections.Generic;
using UnityEngine;
public class QuestManager : MonoBehaviour
{
	public Quest[] quests;
	private Dictionary<string, Quest> questByName = new Dictionary<string, Quest>();
	private Dictionary<string, List<string>> exits = new Dictionary<string, List<string>>();
	private void Awake()
	{
		Quest[] array = this.quests;
		for (int i = 0; i < array.Length; i++)
		{
			Quest quest = array[i];
			this.questByName[quest.internalName] = quest;
		}
		this.exits.Add("StartingCave", new List<string>());
		this.exits["StartingCave"].Add("RatQuest");
		this.exits.Add("RatQuest", new List<string>());
		this.exits["RatQuest"].Add("StartingCave");
		this.exits["RatQuest"].Add("Town");
		this.exits.Add("Town", new List<string>());
		this.exits["Town"].Add("RatQuest");
		this.exits["Town"].Add("BearQuest");
		this.exits["Town"].Add("CaveQuest");
		this.exits["Town"].Add("Park");
		this.exits["Town"].Add("WildWest");
		this.exits.Add("BearQuest", new List<string>());
		this.exits["BearQuest"].Add("Town");
		this.exits.Add("CaveQuest", new List<string>());
		this.exits["CaveQuest"].Add("Town");
		this.exits.Add("Park", new List<string>());
		this.exits["Park"].Add("Town");
		this.exits["Park"].Add("Field");
		this.exits["Park"].Add("Cemetery");
		this.exits["Park"].Add("Snow");
		this.exits.Add("Field", new List<string>());
		this.exits["Field"].Add("Park");
		this.exits.Add("Cemetery", new List<string>());
		this.exits["Cemetery"].Add("Park");
		this.exits.Add("Snow", new List<string>());
		this.exits["Snow"].Add("Park");
		this.exits.Add("WildWest", new List<string>());
		this.exits["WildWest"].Add("Town");
		this.exits["WildWest"].Add("Ruins");
		this.exits.Add("Ruins", new List<string>());
		this.exits["Ruins"].Add("WildWest");
		this.exits["Ruins"].Add("Tomb");
		this.exits.Add("Tomb", new List<string>());
		this.exits["Tomb"].Add("Ruins");
	}
	public Quest GetQuestByName(string name)
	{
		Quest[] array = this.quests;
		for (int i = 0; i < array.Length; i++)
		{
			Quest quest = array[i];
			if (quest.internalName == name)
			{
				return quest;
			}
		}
		return null;
	}
	public string FindExitForLocation(string name)
	{
		if (name == Application.loadedLevelName)
		{
			return "";
		}
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		HashSet<string> hashSet = new HashSet<string>();
		hashSet.Add(Application.loadedLevelName);
		foreach (string current in this.exits[Application.loadedLevelName])
		{
			if (current == name)
			{
				string result = current;
				return result;
			}
			dictionary.Add(current, current);
			hashSet.Add(current);
		}
		while (dictionary.Count > 0)
		{
			Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
			foreach (string current2 in dictionary.Keys)
			{
				foreach (string current3 in this.exits[current2])
				{
					if (!hashSet.Contains(current3))
					{
						if (current3 == name)
						{
							string result = dictionary[current2];
							return result;
						}
						dictionary2.Add(current3, dictionary[current2]);
						hashSet.Add(current3);
					}
				}
			}
			dictionary = dictionary2;
		}
		return "";
	}
	public Quest GetCurrentQuest(Player player)
	{
		return this.GetQuestByName(player.questState.currentQuest);
	}
	public Objective GetCurrentObjective(Player player)
	{
		Quest currentQuest = this.GetCurrentQuest(player);
		if (currentQuest == null)
		{
			return null;
		}
		return currentQuest.GetObjectiveByName(player.questState.currentQuestState);
	}
	public void CheckQuestObjective(Player player)
	{
		Objective currentObjective = this.GetCurrentObjective(player);
		if (currentObjective == null)
		{
			return;
		}
		if (currentObjective.GetType() == typeof(ItemObjective))
		{
			ItemObjective itemObjective = (ItemObjective)currentObjective;
			if (player.inventory.GetCountForItem(itemObjective.itemName) > 0)
			{
				this.CompleteObjective(player);
			}
		}
	}
	public void StartQuest(Player player, string name)
	{
		Quest quest = this.GetQuestByName(name);
		if (quest == null)
		{
			Console.Out.Write("Tried to start quest '" + name + "' which did not exist");
			return;
		}
		if (quest.objectives.Length == 0)
		{
			Console.Out.Write("Quest '" + name + "' has no objectives");
			return;
		}
		player.questState.StartQuest(name, quest.objectives[0].internalName, quest.objectives[0].textState);
		player.SendMessage("OnQuestUpdated", SendMessageOptions.DontRequireReceiver);
		this.CheckQuestObjective(player);
	}
	public void ContinueQuest(Player player, string name)
	{
		if (!player.questState.HasQuest(name))
		{
			Console.Out.Write("Trying to continue quest '" + name + "' which has not been started");
			return;
		}
		player.questState.SetActiveQuest(name);
	}
	public void CompleteObjective(Player player)
	{
		Quest currentQuest = this.GetCurrentQuest(player);
		if (currentQuest == null)
		{
			return;
		}
		int num = -1;
		for (int i = 0; i < currentQuest.objectives.Length; i++)
		{
			if (currentQuest.objectives[i].internalName == player.questState.currentQuestState)
			{
				num = i;
				break;
			}
		}
		if (num == -1)
		{
			return;
		}
		if (currentQuest.objectives[num].questOnCompletion != null && currentQuest.objectives[num].questOnCompletion != "")
		{
			string questOnCompletion = currentQuest.objectives[num].questOnCompletion;
			this.CompleteQuest(player);
			this.StartQuest(player, questOnCompletion);
			return;
		}
		num++;
		if (num >= currentQuest.objectives.Length)
		{
			this.CompleteQuest(player);
		}
		else
		{
			player.questState.UpdateQuest(player.questState.currentQuest, currentQuest.objectives[num].internalName, currentQuest.objectives[num].textState);
			player.SendMessage("OnQuestUpdated", SendMessageOptions.DontRequireReceiver);
		}
		this.CheckQuestObjective(player);
	}
	public void CompleteQuest(Player player)
	{
		player.questState.CompleteQuest();
		player.SendMessage("OnQuestUpdated", SendMessageOptions.DontRequireReceiver);
	}
	public void OnKillEnemy(Player player, string name)
	{
		foreach (string current in player.questState.availableQuests)
		{
			Quest quest = this.GetQuestByName(current);
			if (!(quest == null))
			{
				Objective objectiveByName = quest.GetObjectiveByName(player.questState.GetQuestState(current));
				if (!(objectiveByName == null))
				{
					if (objectiveByName is KillObjective)
					{
						KillObjective killObjective = (KillObjective)objectiveByName;
						if (!(killObjective == null))
						{
							if (killObjective.targetCount != 0 || !(Application.loadedLevelName != killObjective.targetMapName))
							{
								if (!(killObjective.enemyName != name))
								{
									if (killObjective.targetCount == 0)
									{
										GameObject[] array = GameObject.FindGameObjectsWithTag("Enemy");
										bool flag = false;
										GameObject[] array2 = array;
										for (int i = 0; i < array2.Length; i++)
										{
											GameObject gameObject = array2[i];
											if (!(gameObject.GetComponent<Enemy>() == null))
											{
												if (gameObject.GetComponent<Enemy>().enemyName == name && gameObject.GetComponent<Enemy>().health > 0)
												{
													flag = true;
													break;
												}
											}
										}
										if (!flag)
										{
											this.ContinueQuest(player, current);
											this.CompleteObjective(player);
										}
										else
										{
											player.SendMessage("OnQuestUpdated", SendMessageOptions.DontRequireReceiver);
										}
									}
									else
									{
										player.questState.IncrementKillCount(current);
										if (player.questState.GetQuestKillCount(current) >= killObjective.targetCount)
										{
											this.ContinueQuest(player, current);
											this.CompleteObjective(player);
										}
										else
										{
											player.SendMessage("OnQuestUpdated", SendMessageOptions.DontRequireReceiver);
										}
									}
									break;
								}
							}
						}
					}
				}
			}
		}
	}
	public void OnKillBoss(Player player, string name)
	{
		foreach (string current in player.questState.availableQuests)
		{
			Quest quest = this.GetQuestByName(current);
			if (!(quest == null))
			{
				Objective objectiveByName = quest.GetObjectiveByName(player.questState.GetQuestState(current));
				if (!(objectiveByName == null))
				{
					if (objectiveByName is BossObjective)
					{
						BossObjective bossObjective = (BossObjective)objectiveByName;
						if (!(bossObjective == null))
						{
							if (!(bossObjective.bossName != name))
							{
								this.ContinueQuest(player, current);
								this.CompleteObjective(player);
								player.SendMessage("OnQuestUpdated", SendMessageOptions.DontRequireReceiver);
								break;
							}
						}
					}
				}
			}
		}
	}
	public void OnGetItem(Player player, string name)
	{
		foreach (string current in player.questState.availableQuests)
		{
			Quest quest = this.GetQuestByName(current);
			if (!(quest == null))
			{
				Objective objectiveByName = quest.GetObjectiveByName(player.questState.GetQuestState(current));
				if (!(objectiveByName == null))
				{
					if (objectiveByName is ItemObjective)
					{
						ItemObjective itemObjective = (ItemObjective)objectiveByName;
						if (!(itemObjective.itemName != name))
						{
							this.ContinueQuest(player, current);
							this.CompleteObjective(player);
							break;
						}
					}
				}
			}
		}
	}
	public void OnEnterArea(Player player, string name)
	{
		foreach (string current in player.questState.availableQuests)
		{
			Quest quest = this.GetQuestByName(current);
			if (!(quest == null))
			{
				Objective objectiveByName = quest.GetObjectiveByName(player.questState.GetQuestState(current));
				if (!(objectiveByName == null))
				{
					if (objectiveByName is LocationObjective)
					{
						LocationObjective locationObjective = (LocationObjective)objectiveByName;
						if (!(locationObjective.targetMapName != name))
						{
							this.ContinueQuest(player, current);
							this.CompleteObjective(player);
							break;
						}
					}
				}
			}
		}
	}
}
