using System;
using UnityEngine;
public class ForcedNPCInteract : MonoBehaviour
{
	public NPC npc;
	public string requiredQuest;
	private void OnTriggerEnter(Collider other)
	{
		if (GameState.isServer)
		{
			return;
		}
		if (this.requiredQuest != null && this.requiredQuest != "" && LocalPlayerEvents.localPlayer.questState.HasQuest(this.requiredQuest))
		{
			return;
		}
		this.npc.Interact();
	}
}
