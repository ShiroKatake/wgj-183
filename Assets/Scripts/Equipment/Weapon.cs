﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A base class for the logic for weapons.
/// </summary>
public class Weapon : MonoBehaviour
{
    //Private Fields---------------------------------------------------------------------------------------------------------------------------------

    //Serialized Fields----------------------------------------------------------------------------

    [SerializeField] private bool isHand;
    [SerializeField] private bool isPlayerWeapon;
    [SerializeField] private Transform barrelTip;
    [SerializeField] private List<Collider> ownerColliders;

    //Non-Serialized Fields--------------------------------------------------------------------

    private AudioSource audioSource;
    private WeaponStats stats;
	private EHandSide handSide;
	private Animator handAnimator;

	//Public Properties------------------------------------------------------------------------------------------------------------------------------

	//Basic Public Properties----------------------------------------------------------------------
	public UnityAction<EHandSide> OnWeaponChange;

	//Complex Public Properties--------------------------------------------------------------------

	/// <summary>
	/// The stats of the currently equipped weapon.
	/// </summary>
	public WeaponStats CurrentStats
    {
        get
        {
            return stats;
        }

        set
        {
            //Debug.Log($"Weapon.currentStats will be set to {value}.CurrentStats");
            stats = value;

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (stats == null)
            {
                audioSource.clip = null;
            }
            else if (audioSource != null)
            {
                audioSource.clip = stats.AudioClip;
            }
            else
            {
                Debug.LogError($"{this}.Weapon has no audioSource that can have an audio clip applioed to it by Weapon.CurrentStats.set");
            }
        }
    }

    //Initialization Methods-------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Awake() is run when the script instance is being loaded, regardless of whether or not the script is enabled. 
    /// Awake() runs before Start().
    /// </summary>
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        stats = GetComponent<WeaponStats>();
        if (isHand) handSide = GetComponent<Hand>().HandSide;
    }

	/// <summary>
	/// Start() is run on the frame when a script is enabled just before any of the Update methods are called for the first time. 
	/// Start() runs after Awake().
	/// </summary>
	private void Start()
	{
		if (isHand) handAnimator = handSide == EHandSide.Left ? Player.Instance.LeftHandAnimator : Player.Instance.RightHandAnimator;
	}

	//Core Recurring Methods-------------------------------------------------------------------------------------------------------------------------

	/// <summary>
	/// Update() is run every frame.
	/// </summary>
	private void Update()
    {
        if (stats != null) CheckOverheating();
    }

    //Recurring Methods (Update())-------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Checks if it's overheated and needs to cool down.
    /// </summary>
    private void CheckOverheating()
    {
        if (stats.Overheated)
        {
            if (Time.time - stats.TimeOfLastOverheat > stats.OverheatingCooldown)
            {
                stats.Overheated = false;
                stats.BarrelHeat = 0;
            }
        }
        else
        {
            stats.BarrelHeat -= Mathf.Min(stats.BarrelHeat, stats.CoolingPerSecond * Time.fixedDeltaTime);
        }
    }

    //Triggered Methods------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Checks if the weapon is ready to shoot.
    /// </summary>
    /// <returns>Whether or not the weapon can shoot.</returns>
    public bool ReadyToShoot(bool triggerDown)
    {
        //Debug.Log($"{this}.Weapon.ReadyToShoot(), triggerDown: {triggerDown}, stats.TriggerDown: {stats.TriggerDown}");
        //if (isHand) Debug.Log($"{this}.Weapon.ReadyToShoot(), is hand, animation.state: {handAnimator.GetCurrentAnimatorStateInfo(0).IsName("Pistol")}");
		if (isHand && !handAnimator.GetCurrentAnimatorStateInfo(0).IsName("Finger Pistol")) return false;
		if (stats.CurrentAmmo <= 0 || Time.time - stats.TimeOfLastShot < stats.ShotCooldown) return false;

        switch (stats.WeaponClass)
        { 
            case EWeaponClass.Manual:
                return !stats.TriggerDown; 
            case EWeaponClass.BurstManual:
                return !stats.Overheated && !stats.TriggerDown;
            case EWeaponClass.BurstAutomatic:
                return !stats.Overheated;
            case EWeaponClass.FullyAutomatic:
                return true;
            default:
                return false;            
        }
    }

    /// <summary>
    /// Fires a projectile from this weapon's barrel tip.
    /// </summary>
    public void Shoot()
    {
        audioSource.Play();
        if (isPlayerWeapon) stats.CurrentAmmo--;

        for (int i = 0; i < stats.PelletsPerShot; i++)
        {
            //Quaternion randomRotation = Random.rotation;
            //Quaternion projectileRotation = Quaternion.RotateTowards(barrelTip.transform.rotation, randomRotation, spreadAngle);
            Quaternion projectileRotation = Quaternion.RotateTowards(barrelTip.transform.rotation, Random.rotation, stats.SpreadAngle);
            //Debug.Log($"randomRotation is {randomRotation} (Quaternion) / {randomRotation.eulerAngles} (EulerAngles)");
            //Debug.Log($"projectileRotation is {projectileRotation} (Quaternion) / {projectileRotation.eulerAngles} (EulerAngles)");
            Projectile projectile = ProjectileFactory.Instance.Get(transform, ownerColliders, barrelTip.position, projectileRotation, stats.ProjectileType);
            projectile.Shoot(stats.ShotForce);
            //Debug.Log($"{this}.Weapon.Shoot(), projectile is {projectile}");
            stats.TriggerDown = true;
            stats.TimeOfLastShot = Time.time;
            stats.BarrelHeat += stats.HeatPerShot;

            if (stats.BarrelHeat > stats.OverheatingThreshold)
            {
                stats.Overheated = true;
                stats.TimeOfLastOverheat = Time.time;
            }
        }

		if (isHand) OnWeaponChange.Invoke(handSide);
	}
}
