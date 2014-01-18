using System;
using UnityEngine;
public class Weapon : MonoBehaviour
{
	public float attackTime;
	public int minimumDamage;
	public int maximumDamage;
	public int numberOfHitsPerAttack;
	private DateTime lastFire;
	private void Awake()
	{
		this.lastFire = DateTime.Now.Subtract(new TimeSpan(0, 0, 5));
	}
	public virtual void PerformAttack(Player player, Ray[] rays, GameObject[] hits)
	{
		if (rays.Length > this.numberOfHitsPerAttack || rays.Length != hits.Length)
		{
			return;
		}
		if ((DateTime.Now - this.lastFire).TotalSeconds < (double)(this.attackTime * 0.75f))
		{
			return;
		}
		Item component = base.GetComponent<Item>();
		if (player.health <= 0)
		{
			return;
		}
		if (component.clipSize != 0 && !player.inventory.RemoveAmmoFromWeapon(component.itemName, 1))
		{
			return;
		}
		this.lastFire = DateTime.Now;
		for (int i = 0; i < hits.Length; i++)
		{
			GameObject gameObject = hits[i];
			if (gameObject != null)
			{
				if (!(gameObject.GetComponent<WalkingDead>() != null) || !(component.itemName != "Boomstick"))
				{
					if (!(gameObject.GetComponent<Rabbit>() != null) || !(component.itemName != "HolyHandGrenade"))
					{
						this.ApplyDamage(player.gameObject, gameObject);
					}
				}
			}
		}
	}
	public virtual void ApplyDamage(GameObject source, GameObject target)
	{
		int num = UnityEngine.Random.Range(this.minimumDamage, this.maximumDamage + 1);
		target.SendMessage("Damage", new Damage(source, base.GetComponent<Item>(), (float)num), SendMessageOptions.DontRequireReceiver);
	}
}
