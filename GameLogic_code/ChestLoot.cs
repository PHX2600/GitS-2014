using System;
using UnityEngine;
public class ChestLoot : MonoBehaviour
{
	public string lootName;
	public string requiredQuest;
	public string requiredQuestState;
	private void Start()
	{
		if (!GameState.isServer && !GameState.isSpectator)
		{
			GameState.gameServer.IsNamedObjectLootAvailable(this.lootName, delegate(bool ok)
			{
				if (!ok)
				{
					this.RemoveLoot();
				}
			});
		}
	}
	public void OnLooted(Player player)
	{
		LootableObject lootObj = base.GetComponent<LootableObject>();
		if (lootObj != null)
		{
			if (!lootObj.enabled)
			{
				return;
			}
			if (this.requiredQuest != null && this.requiredQuest != "")
			{
				if (!player.questState.HasQuest(this.requiredQuest))
				{
					return;
				}
				if (this.requiredQuestState != null && this.requiredQuestState != "" && player.questState.GetQuestState(this.requiredQuest) != this.requiredQuestState)
				{
					return;
				}
			}
			if (GameState.isServer)
			{
				if (lootObj.collection.owner != null && lootObj.collection.owner != player)
				{
					return;
				}
				GameState.masterServer.IsNamedObjectLootAvailable(player.id, this.lootName, delegate(bool ok)
				{
					if (ok)
					{
						foreach (string current in lootObj.collection.items.Keys)
						{
							player.inventory.AddItem(current, lootObj.collection.items[current]);
						}
						GameState.UpdateCharacterItems(player.id, player.inventory);
						GameState.masterServer.MarkNamedObjectAsLooted(player.id, this.lootName);
					}
				});
			}
			else
			{
				GameState.gameServer.TakeNamedObjectLoot(this.lootName);
				this.RemoveLoot();
			}
		}
	}
	public void CheckAvailability(GameServerConnection.ResultCallback callback)
	{
		if (GameState.isServer)
		{
			callback(true);
			return;
		}
		GameState.gameServer.IsNamedObjectLootAvailable(this.lootName, callback);
	}
	protected virtual void RemoveLoot()
	{
		LootableObject component = base.GetComponent<LootableObject>();
		UnityEngine.Object.Destroy(component.collection);
		component.collection = null;
		component.enabled = false;
	}
}
