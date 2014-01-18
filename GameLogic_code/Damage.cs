using System;
using UnityEngine;
public class Damage
{
	public GameObject source;
	public Item weapon;
	public float amount;
	public Damage(GameObject s, Item w, float a)
	{
		this.source = s;
		this.weapon = w;
		this.amount = a;
	}
}
