using System;
using UnityEngine;
public class BossChest : MonoBehaviour
{
	private bool open = false;
	private void Start()
	{
		if (GameState.isServer)
		{
			return;
		}
		ChestLoot component = base.GetComponent<ChestLoot>();
		if (LocalPlayerEvents.localPlayer.questState.HasQuest(component.requiredQuest) && (LocalPlayerEvents.localPlayer.questState.IsQuestComplete(component.requiredQuest) || LocalPlayerEvents.localPlayer.questState.GetQuestState(component.requiredQuest) == component.requiredQuestState))
		{
			this.open = true;
			base.GetComponent<Animation>().Play("open");
		}
		else
		{
			this.open = false;
			base.GetComponent<LootableObject>().enabled = false;
		}
	}
	private void Update()
	{
		if (GameState.isServer)
		{
			return;
		}
		if (GameState.isSpectator)
		{
			return;
		}
		if (this.open)
		{
			return;
		}
		ChestLoot component = base.GetComponent<ChestLoot>();
		if (LocalPlayerEvents.localPlayer.questState.HasQuest(component.requiredQuest) && (LocalPlayerEvents.localPlayer.questState.IsQuestComplete(component.requiredQuest) || LocalPlayerEvents.localPlayer.questState.GetQuestState(component.requiredQuest) == component.requiredQuestState))
		{
			base.GetComponent<Animation>().Play("open");
			this.open = true;
			GameState.gameServer.IsNamedObjectLootAvailable(base.GetComponent<ChestLoot>().lootName, delegate(bool ok)
			{
				if (ok)
				{
					base.GetComponent<LootableObject>().enabled = true;
				}
			});
		}
	}
}
