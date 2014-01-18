using System;
using System.Collections.Generic;
using UnityEngine;
public class NPC : MonoBehaviour
{
	public string npcName;
	public NPCQuestText[] questText;
	public GameObject npcIcon = null;
	public bool Interact()
	{
		NPCQuestText qt;
		if (!GameState.isServer)
		{
			GameState.gameServer.NPCTalk(this.npcName, delegate(bool ok, NPCQuestText.NPCQuestTextType type, string text, bool continued, bool complete, string target, string link, int points)
			{
				if (ok)
				{
					qt = NPCQuestText.InstantiateRemote();
					qt.textType = type;
					qt.text = text;
					qt.continued = continued;
					qt.completesQuest = complete;
					qt.targetName = target;
					qt.link = link;
					qt.points = points;
					LocalPlayerEvents.UpdateQuestText(this, qt);
				}
			});
			return true;
		}
		qt = this.GetCurrentQuestText(LocalPlayerEvents.localPlayer);
		if (qt == null)
		{
			return false;
		}
		LocalPlayerEvents.UpdateQuestText(this, qt);
		return true;
	}
	public NPCQuestText GetCurrentQuestText(Player player)
	{
		NPCQuestText result = null;
		NPCQuestText[] array = this.questText;
		int i = 0;
		while (i < array.Length)
		{
			NPCQuestText nPCQuestText = array[i];
			if (nPCQuestText.requiredQuest == null || nPCQuestText.requiredQuest == "")
			{
				if (!(player.GetNoQuestStateName(this.npcName) != nPCQuestText.textStateName))
				{
					goto IL_9B;
				}
			}
			else
			{
				if (player.questState.HasQuest(nPCQuestText.requiredQuest))
				{
					if (!(player.questState.GetQuestTextState(nPCQuestText.requiredQuest) != nPCQuestText.textStateName))
					{
						goto IL_9B;
					}
				}
			}
			IL_9D:
			i++;
			continue;
			IL_9B:
			result = nPCQuestText;
			goto IL_9D;
		}
		return result;
	}
	public NPC CompleteQuestText(Player player, NPCQuestText text = null)
	{
		if (text == null)
		{
			text = this.GetCurrentQuestText(player);
		}
		if (text == null)
		{
			return null;
		}
		NPC nPC = this;
		if (text.newNPC)
		{
			nPC = text.newNPC;
		}
		if (text.newStateName != null && text.newStateName != "")
		{
			if (text.requiredQuest == null || text.requiredQuest == "")
			{
				player.SetNoQuestStateName(nPC.npcName, text.newStateName);
			}
			else
			{
				player.questState.UpdateQuestTextState(text.requiredQuest, text.newStateName);
			}
		}
		if (text.textType == NPCQuestText.NPCQuestTextType.ItemText)
		{
			player.inventory.AddItem(text.targetName, 1);
			if (!GameState.isServer)
			{
				if (player.inventory.primaryWeaponName == text.targetName)
				{
					LocalPlayerEvents.SetWeaponBySlot(1);
				}
				else
				{
					if (player.inventory.secondaryWeaponName == text.targetName)
					{
						LocalPlayerEvents.SetWeaponBySlot(2);
					}
					else
					{
						if (player.inventory.swordGrenadeName == text.targetName)
						{
							LocalPlayerEvents.SetWeaponBySlot(3);
						}
					}
				}
			}
		}
		if (text.completesQuest)
		{
			GameState.GetQuestManager().CompleteObjective(player);
		}
		if (text.textType == NPCQuestText.NPCQuestTextType.NewQuestText)
		{
			GameState.GetQuestManager().StartQuest(player, text.targetName);
		}
		if (text.continued)
		{
			return nPC;
		}
		return null;
	}
	public void OnQuestTextComplete(Player player, NPCQuestText text)
	{
		if (!GameState.isServer)
		{
			GameState.gameServer.NPCFinishText(this.npcName, delegate(bool more, string nextNpcName)
			{
				if (more)
				{
					NPC[] array = (NPC[])UnityEngine.Object.FindObjectsOfType(typeof(NPC));
					for (int i = 0; i < array.Length; i++)
					{
						NPC nPC2 = array[i];
						if (nPC2.npcName == nextNpcName)
						{
							nPC2.Interact();
							break;
						}
					}
				}
			});
			return;
		}
		NPC nPC = this.CompleteQuestText(player, text);
		if (nPC != null)
		{
			nPC.Interact();
		}
	}
	public void GetBuyList(GameServerConnection.GetLootCallback callback)
	{
		if (GameState.isServer)
		{
			callback(new Dictionary<string, int>());
			return;
		}
		GameState.gameServer.NPCBuyList(this.npcName, callback);
	}
	public void OnBuyItem(string item, int count)
	{
		if (!GameState.isServer)
		{
			GameState.gameServer.Buy(this.npcName, item, count);
		}
	}
	public void OnSellItem(string item, int count)
	{
		if (!GameState.isServer)
		{
			GameState.gameServer.Sell(item, count);
		}
	}
}
