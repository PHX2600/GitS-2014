using System;
using UnityEngine;
public class Item : MonoBehaviour
{
	public enum ItemType
	{
		Money,
		Gun,
		Ammo,
		QuestItem,
		Accessory,
		Flag,
		SwordOrGrenade
	}
	public Item.ItemType itemType;
	public string itemName;
	public string itemDescription;
	public string itemDetail;
	public int maximumCount;
	public int arenaMinimumCount = 0;
	public string iconName;
	public string modelName;
	public string ammoType;
	public string animationType;
	public string fireSound;
	public int clipSize;
	public int sellValue;
	public bool showInInventoryScreen = true;
	public bool bound = false;
	public bool teamItem = false;
	public int rarity;
	public string coloredItemDescription
	{
		get
		{
			string text = this.itemDescription;
			if (this.rarity == 1)
			{
				text = "[8080ff]" + text;
			}
			else
			{
				if (this.rarity == 2)
				{
					text = "[ffc040]" + text;
				}
			}
			return text;
		}
	}
}
