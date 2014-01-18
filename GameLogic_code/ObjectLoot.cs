using System;
public class ObjectLoot : ChestLoot
{
	protected override void RemoveLoot()
	{
		base.RemoveLoot();
		base.gameObject.SetActive(false);
	}
}
