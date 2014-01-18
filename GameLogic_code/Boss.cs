using System;
using System.Collections.Generic;
using UnityEngine;
public class Boss : MonoBehaviour
{
	private List<Player> damageSources = new List<Player>();
	private void Damage(Damage dmg)
	{
		if (dmg.source == null)
		{
			return;
		}
		Player component = dmg.source.GetComponent<Player>();
		if (component == null)
		{
			return;
		}
		if (!this.damageSources.Contains(component))
		{
			this.damageSources.Add(component);
		}
	}
	public void OnDeath()
	{
		if (!GameState.isServer)
		{
			return;
		}
		foreach (Player current in this.damageSources)
		{
			if (current.id != -1)
			{
				GameState.GetQuestManager().OnKillBoss(current, base.GetComponent<Enemy>().enemyName);
			}
		}
		this.damageSources.Clear();
	}
}
