using System;
using System.Collections.Generic;
using UnityEngine;
public class InventoryItemCollection : MonoBehaviour
{
	public Item[] availableItems;
	private Dictionary<string, Item> itemsByName = new Dictionary<string, Item>();
	private void Awake()
	{
		Item[] array = this.availableItems;
		for (int i = 0; i < array.Length; i++)
		{
			Item item = array[i];
			this.itemsByName[item.itemName] = item;
		}
	}
	public Item GetItemByName(string name)
	{
		if (!this.itemsByName.ContainsKey(name))
		{
			return null;
		}
		return this.itemsByName[name];
	}
}
