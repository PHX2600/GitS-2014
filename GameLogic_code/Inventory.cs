using System;
using System.Collections.Generic;
public class Inventory
{
	private List<string> items = new List<string>();
	private Dictionary<string, int> itemCounts = new Dictionary<string, int>();
	private Dictionary<string, int> itemLoadedAmmoCounts = new Dictionary<string, int>();
	public string primaryWeaponName = "";
	public string secondaryWeaponName = "";
	public string swordGrenadeName = "";
	public string accessoryName = "";
	public int activeSlot = 0;
	public Player owner;
	private bool dirty = false;
	private void Start()
	{
	}
	public void AddItem(string itemName, int count = 1)
	{
		Item itemByName = GameState.GetInventoryItemCollection().GetItemByName(itemName);
		if (itemByName == null)
		{
			return;
		}
		if (!this.itemCounts.ContainsKey(itemName))
		{
			this.items.Add(itemName);
			this.itemCounts[itemName] = count;
			this.itemLoadedAmmoCounts[itemName] = itemByName.clipSize;
			if (itemByName.itemType == Item.ItemType.Gun)
			{
				if (this.primaryWeaponName == "")
				{
					this.primaryWeaponName = itemName;
					if ((!GameState.isServer && this.activeSlot == 0) || this.activeSlot == 1)
					{
						LocalPlayerEvents.SetWeaponBySlot(1);
					}
				}
				else
				{
					if (this.secondaryWeaponName == "")
					{
						this.secondaryWeaponName = itemName;
						if ((!GameState.isServer && this.activeSlot == 0) || this.activeSlot == 2)
						{
							LocalPlayerEvents.SetWeaponBySlot(2);
						}
					}
				}
			}
			else
			{
				if (itemByName.itemType == Item.ItemType.SwordOrGrenade)
				{
					if (this.swordGrenadeName == "")
					{
						this.swordGrenadeName = itemName;
						if ((!GameState.isServer && this.activeSlot == 0) || this.activeSlot == 3)
						{
							LocalPlayerEvents.SetWeaponBySlot(3);
						}
					}
				}
				else
				{
					if (itemByName.itemType == Item.ItemType.Accessory && this.accessoryName == "")
					{
						this.accessoryName = itemName;
						if ((!GameState.isServer && this.activeSlot == 0) || this.activeSlot == 4)
						{
							LocalPlayerEvents.SetWeaponBySlot(4);
						}
					}
				}
			}
		}
		else
		{
			Dictionary<string, int> dictionary;
			(dictionary = this.itemCounts)[itemName] = dictionary[itemName] + count;
		}
		this.dirty = true;
		if (GameState.isServer)
		{
			this.owner.SendUpdate(GameServerUpdate.CreateAddItemUpdate(itemName, count));
		}
		GameState.GetQuestManager().OnGetItem(this.owner, itemName);
	}
	public bool RemoveItem(string itemName, int count = 1)
	{
		if (count == 0)
		{
			return true;
		}
		if (!this.itemCounts.ContainsKey(itemName))
		{
			return false;
		}
		if (this.itemCounts[itemName] < count)
		{
			return false;
		}
		Dictionary<string, int> dictionary;
		(dictionary = this.itemCounts)[itemName] = dictionary[itemName] - count;
		if (this.itemCounts[itemName] <= 0)
		{
			this.items.Remove(itemName);
			this.itemCounts.Remove(itemName);
			if (this.primaryWeaponName == itemName)
			{
				this.primaryWeaponName = "";
			}
			if (this.secondaryWeaponName == itemName)
			{
				this.secondaryWeaponName = "";
			}
			if (this.swordGrenadeName == itemName)
			{
				this.swordGrenadeName = "";
			}
			if (this.accessoryName == itemName)
			{
				this.accessoryName = "";
			}
		}
		if (GameState.isServer)
		{
			this.owner.SendUpdate(GameServerUpdate.CreateRemoveItemUpdate(itemName, count));
		}
		this.dirty = true;
		return true;
	}
	public bool AdjustQuantityForItem(string itemName, int adjustment)
	{
		if (adjustment == 0)
		{
			return true;
		}
		if (adjustment > 0)
		{
			this.AddItem(itemName, adjustment);
			return true;
		}
		return this.RemoveItem(itemName, -adjustment);
	}
	public int GetLoadedAmmoForWeapon(string itemName)
	{
		Item itemByName = GameState.GetInventoryItemCollection().GetItemByName(itemName);
		if (itemByName == null)
		{
			return 0;
		}
		if (!this.itemCounts.ContainsKey(itemName))
		{
			return 0;
		}
		if (this.itemCounts[itemName] <= 0)
		{
			return 0;
		}
		return this.itemLoadedAmmoCounts[itemName];
	}
	public void AddAmmoToWeapon(string itemName, int count)
	{
		Item itemByName = GameState.GetInventoryItemCollection().GetItemByName(itemName);
		if (itemByName == null)
		{
			return;
		}
		if (!this.itemCounts.ContainsKey(itemName))
		{
			return;
		}
		if (this.itemCounts[itemName] <= 0)
		{
			return;
		}
		Dictionary<string, int> dictionary;
		(dictionary = this.itemLoadedAmmoCounts)[itemName] = dictionary[itemName] + count;
		if (GameState.isServer)
		{
			this.owner.SendUpdate(GameServerUpdate.CreateLoadedAmmoUpdate(itemName, this.itemLoadedAmmoCounts[itemName]));
		}
		this.dirty = true;
	}
	public bool RemoveAmmoFromWeapon(string itemName, int count)
	{
		Item itemByName = GameState.GetInventoryItemCollection().GetItemByName(itemName);
		if (itemByName == null)
		{
			return false;
		}
		if (!this.itemCounts.ContainsKey(itemName))
		{
			return false;
		}
		if (this.itemCounts[itemName] <= 0)
		{
			return false;
		}
		if (this.itemLoadedAmmoCounts[itemName] < count)
		{
			return false;
		}
		Dictionary<string, int> dictionary;
		(dictionary = this.itemLoadedAmmoCounts)[itemName] = dictionary[itemName] - count;
		if (GameState.isServer)
		{
			this.owner.SendUpdate(GameServerUpdate.CreateLoadedAmmoUpdate(itemName, this.itemLoadedAmmoCounts[itemName]));
		}
		this.dirty = true;
		return true;
	}
	public bool SetLoadedAmmoForWeapon(string itemName, int count)
	{
		Item itemByName = GameState.GetInventoryItemCollection().GetItemByName(itemName);
		if (itemByName == null)
		{
			return false;
		}
		if (!this.itemCounts.ContainsKey(itemName))
		{
			return false;
		}
		if (this.itemCounts[itemName] <= 0)
		{
			return false;
		}
		this.itemLoadedAmmoCounts[itemName] = count;
		if (GameState.isServer)
		{
			this.owner.SendUpdate(GameServerUpdate.CreateLoadedAmmoUpdate(itemName, this.itemLoadedAmmoCounts[itemName]));
		}
		this.dirty = true;
		return true;
	}
	public List<string> GetItemList()
	{
		return this.items;
	}
	public int GetCountForItem(string itemName)
	{
		if (!this.itemCounts.ContainsKey(itemName))
		{
			return 0;
		}
		return this.itemCounts[itemName];
	}
	public Item GetObjectForItemName(string itemName)
	{
		return GameState.GetInventoryItemCollection().GetItemByName(itemName);
	}
	public void UpdateFromServerList(List<MasterServerConnection.InventoryItemData> serverItems)
	{
		this.items = new List<string>();
		this.itemCounts = new Dictionary<string, int>();
		this.itemLoadedAmmoCounts = new Dictionary<string, int>();
		foreach (MasterServerConnection.InventoryItemData current in serverItems)
		{
			this.items.Add(current.name);
			this.itemCounts.Add(current.name, current.count);
			this.itemLoadedAmmoCounts.Add(current.name, current.loadedAmmo);
		}
		this.dirty = false;
	}
	public bool IsDirty()
	{
		return this.dirty;
	}
	public void SetDirty()
	{
		this.dirty = true;
	}
	public void MarkAsUpdated()
	{
		this.dirty = false;
	}
}
