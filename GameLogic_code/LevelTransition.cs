using System;
using UnityEngine;
public class LevelTransition : MonoBehaviour
{
	public string destination;
	public string levelDescription;
	public string exitName;
	public string requiredKey;
	public string requiredQuest;
	public NPC npc;
	public bool IsValidTransition(Player player)
	{
		return (this.requiredQuest == null || !(this.requiredQuest != "") || player.questState.HasQuest(this.requiredQuest)) && (this.requiredKey == null || !(this.requiredKey != "") || player.inventory.GetCountForItem(this.requiredKey) > 0);
	}
}
