using System;
using UnityEngine;
public class ThirdPersonWeapon : MonoBehaviour
{
	public GameObject muzzleFlash;
	public void Fire()
	{
		if (this.muzzleFlash != null)
		{
			LocalPlayerEvents.ShowMuzzleFlash(this.muzzleFlash);
		}
	}
}
