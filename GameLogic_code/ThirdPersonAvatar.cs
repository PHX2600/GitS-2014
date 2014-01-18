using System;
using System.Threading;
using UnityEngine;
public class ThirdPersonAvatar : MonoBehaviour
{
	private Player player;
	private Animator anim;
	private Vector3 lastPosition;
	private Vector3 velocity;
	private Quaternion smoothedLookDirection = Quaternion.identity;
	private float smoothedRightSpeed = 0f;
	private float smoothedForwardSpeed = 0f;
	private Transform weaponAttachmentPoint = null;
	private Transform hatAttachmentPoint = null;
	private Item currentWeapon = null;
	private GameObject attachedWeapon = null;
	private GameObject attachedHat = null;
	private int attachedHatTeam = -1;
	private void Awake()
	{
		this.player = base.GetComponent<Player>();
		this.anim = base.GetComponent<Animator>();
		Transform[] componentsInChildren = base.transform.GetComponentsInChildren<Transform>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			Transform transform = componentsInChildren[i];
			if (transform.name == "WeaponAttachmentPoint")
			{
				this.weaponAttachmentPoint = transform;
			}
			if (transform.name == "HatAttachmentPoint")
			{
				this.hatAttachmentPoint = transform;
			}
		}
	}
	private void Update()
	{
		if (base.GetComponent<Player>().health <= 0)
		{
			this.anim.SetBool("dead", true);
			return;
		}
		this.anim.SetBool("dead", false);
		base.transform.rotation = Quaternion.Euler(new Vector3(0f, this.smoothedLookDirection.eulerAngles.y, 0f));
		this.anim.SetLookAtPosition(base.transform.position + this.smoothedLookDirection * new Vector3(0f, 0f, 100f));
		this.anim.SetLookAtWeight(1f, 0.1f, 1f);
		this.anim.SetFloat("right", this.smoothedRightSpeed * 0.3f);
		this.anim.SetFloat("forward", this.smoothedForwardSpeed * 0.3f);
		Item objectForItemName = this.player.inventory.GetObjectForItemName(this.player.remoteWeapon);
		if (objectForItemName == null)
		{
			this.anim.SetBool("pistol", false);
			this.anim.SetBool("rifle", false);
			this.anim.SetBool("sword", false);
			this.anim.SetBool("drink", false);
		}
		else
		{
			this.anim.SetBool("pistol", objectForItemName.animationType == "pistol");
			this.anim.SetBool("rifle", objectForItemName.animationType == "rifle");
			this.anim.SetBool("sword", objectForItemName.animationType == "sword");
			this.anim.SetBool("drink", objectForItemName.animationType == "drink");
		}
		if (objectForItemName != this.currentWeapon)
		{
			this.currentWeapon = objectForItemName;
			if (this.attachedWeapon != null)
			{
				UnityEngine.Object.Destroy(this.attachedWeapon);
			}
			this.attachedWeapon = null;
			if (objectForItemName != null && this.weaponAttachmentPoint != null)
			{
				WeaponModelData weaponModelData = GameState.GetWeaponModelData(this.player.remoteWeapon);
				if (weaponModelData != null)
				{
					this.attachedWeapon = (GameObject)UnityEngine.Object.Instantiate(weaponModelData.model);
					this.attachedWeapon.transform.parent = this.weaponAttachmentPoint;
					this.attachedWeapon.transform.localPosition = new Vector3(0f, 0f, 0f);
					this.attachedWeapon.transform.localRotation = Quaternion.identity;
					this.attachedWeapon.transform.localScale = new Vector3(1f, 1f, 1f);
				}
			}
		}
		if (GameState.isArena && this.player.arenaTeamIndex != this.attachedHatTeam)
		{
			if (this.attachedHat != null)
			{
				UnityEngine.Object.Destroy(this.attachedHat);
			}
			this.attachedHat = null;
			this.attachedHatTeam = this.player.arenaTeamIndex;
			string path = (this.player.arenaTeamIndex != 0) ? "White Hat" : "Black Hat";
			this.attachedHat = (GameObject)UnityEngine.Object.Instantiate(Resources.Load(path, typeof(GameObject)));
			this.attachedHat.transform.parent = this.hatAttachmentPoint;
			this.attachedHat.transform.localPosition = new Vector3(0f, 0f, 0f);
			this.attachedHat.transform.localRotation = Quaternion.identity;
			this.attachedHat.transform.localScale = new Vector3(1f, 1f, 1f);
		}
	}
	private void FixedUpdate()
	{
		if (base.GetComponent<Player>().health <= 0)
		{
			return;
		}
		ReplicatedObject component = base.GetComponent<ReplicatedObject>();
		object obj = component;
		Monitor.Enter(obj);
		Quaternion remoteOrientation;
		try
		{
			remoteOrientation = component.remoteOrientation;
		}
		finally
		{
			Monitor.Exit(obj);
		}
		this.smoothedLookDirection = Quaternion.Lerp(this.smoothedLookDirection, remoteOrientation, 0.2f);
		this.velocity = (base.transform.position - this.lastPosition) / Time.fixedDeltaTime;
		this.lastPosition = base.transform.position;
		this.smoothedForwardSpeed = Mathf.Lerp(this.smoothedForwardSpeed, Vector3.Dot(this.velocity, base.transform.forward), 0.2f);
		this.smoothedRightSpeed = Mathf.Lerp(this.smoothedRightSpeed, Vector3.Dot(this.velocity, base.transform.right), 0.2f);
	}
	public void ShowMuzzleFlash()
	{
		if (this.attachedWeapon == null)
		{
			return;
		}
		if (this.attachedWeapon.GetComponent<ThirdPersonWeapon>() == null)
		{
			return;
		}
		this.attachedWeapon.GetComponent<ThirdPersonWeapon>().Fire();
	}
}
