using System;
using UnityEngine;
public class NPCQuestText : MonoBehaviour
{
	public enum NPCQuestTextType
	{
		NormalText,
		ItemText,
		NewQuestText,
		ImmediatelyProceed,
		BuyAndSellText,
		ChallengeText
	}
	public string requiredQuest;
	public string textStateName;
	public NPCQuestText.NPCQuestTextType textType;
	public string text;
	public bool continued;
	public bool completesQuest;
	public string targetName;
	public string newStateName;
	public string link;
	public int points = 0;
	public NPC newNPC;
	public static NPCQuestText InstantiateRemote()
	{
		GameObject gameObject = new GameObject();
		gameObject.AddComponent(typeof(NPCQuestText));
		NPCQuestText component = gameObject.GetComponent<NPCQuestText>();
		component.textStateName = "__remote";
		return component;
	}
	public bool IsRemote()
	{
		return this.textStateName == "__remote";
	}
}
