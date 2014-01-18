using System;
using System.Collections.Generic;
using UnityEngine;
public class StaticLootCollection : LootCollection
{
	public Loot[] loot;
	private void Start()
	{
		this.items = new Dictionary<string, int>();
		Loot[] array = this.loot;
		for (int i = 0; i < array.Length; i++)
		{
			Loot loot = array[i];
			int num = UnityEngine.Random.Range(loot.minimumCount, loot.maximumCount + 1);
			if (num > 0)
			{
				this.items.Add(loot.itemName, num);
			}
		}
	}
}
