﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GolfBall : MonoBehaviour, ICanBeFollowed
{
    public const string LAYER_NAME = "Ball";
    public static int Layer => LayerMask.NameToLayer(LAYER_NAME);
    public static int Mask => LayerMask.GetMask(LAYER_NAME);

    // Constants 
    public readonly static RigidPreset Preset_Air = new RigidPreset(0f, 0f);
    public readonly static RigidPreset Preset_Grass = new RigidPreset(3f, 1f);
    public readonly static RigidPreset Preset_GrassHole = new RigidPreset(1.5f, 1f);
    public readonly static RigidPreset Preset_Sand = new RigidPreset(9f, 12f);
    public readonly static RigidPreset Preset_Water = new RigidPreset(20f, 50f);
    public readonly static RigidPreset Preset_Ice = new RigidPreset(0f, 0f);

    // States
    public PlayState State;
    public bool IsOnGround = false;
    public bool IsFrozen = false;

    [Space]
    public Biome.Type CurrentBiome;

    // Statistics
    public Stats CurrentHoleStats;
    public List<Stats> HolesCompleted = new List<Stats>();


    private Vector3 Facing
    {
        get
        {
            if (rigid != null)
            {
                switch (State)
                {
                    case PlayState.Shooting:
                        return transform.forward;
                    // Use the velocity
                    case PlayState.Flying:
                        return rigid.velocity;
                    // Use the velocity
                    case PlayState.Rolling:
                        return rigid.velocity;
                }
            }

            return Vector3.zero;
        }
    }

    public Vector3 Forward => Facing.normalized;

    public Vector3 Position => transform.position;


    // Settings
    public const float FullPower = 50;
    public const float SpeedCutoffThreshold = 0.5f;
    public const float SecondsRequiredBelowSpeedThreshold = 1f;
    private float stopRollingTimer;

    [Header("Control settings")]
    [Range(0, 1)] public float Power;
    public float Rotation;
    public float Angle;


    private float Scale => (transform.localScale.x + transform.localScale.y + transform.localScale.z) / 3;
    public float Radius => Scale / 2;

    /// <summary>
    /// Event called when the GolfBall has finished rolling and the Shooting state has been entered.
    /// </summary>
    public UnityAction OnRollingFinished;


    [Header("References")]
    public Rigidbody rigid;
    public SphereCollider sphereCollider;


    [Header("Line Previews")]
    public LinePreview ShotPowerPreview;
    public LinePreview ShotNormalPreview;
    public TextMesh ShotAnglePreview;



    private void Awake()
    {
        gameObject.layer = Layer;
        transform.localScale = new Vector3(Scale, Scale, Scale);

        OnRollingFinished += Utils.EMPTY;


        ShotPowerPreview.enabled = false;
    }



    private void FixedUpdate()
    {
        // Get the onground value
        Collider[] groundCollisions = GroundCheck.GetGroundCollisions(transform.position, sphereCollider.radius + GroundCheck.DEFAULT_RADIUS);
        IsOnGround = groundCollisions.Length > 0;

        // Get the current biome
        Collider c = null;
        if (groundCollisions.Length > 0)
        {
            c = groundCollisions[0];
        }


        CurrentBiome = Biome.GetBiomeSamplePoint(c, Position);
        if (!IsOnGround)
        {
            CurrentBiome = Biome.Type.None;
        }


        PlayState lastFrame = State;

        // Update the state
        // On the ground
        if (IsOnGround)
        {
            float speed = rigid.velocity.magnitude;

            // Speed is below the threshold
            if (speed < SpeedCutoffThreshold)
            {
                stopRollingTimer += Time.fixedDeltaTime;
                if (stopRollingTimer >= SecondsRequiredBelowSpeedThreshold)
                {
                    State = PlayState.Shooting;

                    // First frame of shooting
                    if (lastFrame == PlayState.Rolling && State == PlayState.Shooting)
                    {
                        WaitForNextShot();
                        OnRollingFinished.Invoke();
                    }
                }
            }
            // Still above it
            else
            {
                stopRollingTimer = 0;
                State = PlayState.Rolling;
            }

        }
        // In the air
        else
        {
            State = PlayState.Flying;
        }


        // Update the rigidbody properties
        RigidPreset r = Preset_Air;
        // On the ground
        if (State == PlayState.Rolling)
        {
            switch (CurrentBiome)
            {
                case Biome.Type.Grass:
                    r = Preset_Grass;
                    break;
                case Biome.Type.Sand:
                    r = Preset_Sand;
                    break;
                case Biome.Type.Hole:
                    r = Preset_GrassHole;
                    break;
                case Biome.Type.Water:
                    r = Preset_Water;
                    break;
                case Biome.Type.Ice:
                    r = Preset_Ice;
                    break;
            }

        }

        // Set the values
        rigid.angularDrag = r.AngularDrag;
        rigid.drag = r.Drag;
    }



    public void Shoot()
    {
        // Undo any freezes
        Freeze(false);

        // Apply the force in direction
        Vector3 force = transform.forward * Power * FullPower;
        rigid.AddForce(force, ForceMode.Impulse);

        CurrentHoleStats.ShotsForThisHole++;
    }



    public void Reset()
    {
        transform.rotation = Quaternion.identity;

        rigid.velocity = Vector3.zero;
        rigid.angularVelocity = Vector3.zero;

        SetDefaultShotValues();
    }





    public void WaitForNextShot()
    {
        Reset();

        if (!IsFrozen)
        {
            StartCoroutine(FreezeUntilShoot());
        }
    }


    private IEnumerator FreezeUntilShoot()
    {
        Freeze(true);


        // Freeze until a shot has been taken
        int shotsBefore = CurrentHoleStats.ShotsForThisHole;
        while (shotsBefore == CurrentHoleStats.ShotsForThisHole)
        {
            yield return null;
        }


        Freeze(false);
    }


    private void Freeze(bool freeze)
    {
        IsFrozen = freeze;

        // Set the freeze mode
        RigidbodyConstraints c = RigidbodyConstraints.None;
        if (freeze)
        {
            c = RigidbodyConstraints.FreezeAll;
        }

        rigid.constraints = c;
    }


    public void SetShotPowerPreview(bool useRotation, bool useAngle, bool usePower)
    {
        // Calculate which axis to use
        Vector3 offset = transform.forward;
        offset.x = useRotation ? offset.x : 0;
        offset.z = useRotation ? offset.z : 0;

        offset.y = useAngle ? offset.y : 0;

        // Now ensure it is the correct length
        offset.Normalize();

        // Apply power multiplier if we need to
        offset *= usePower ? Power : 1;

        // Assign the point
        ShotPowerPreview.SetPoints(transform.position, offset);
    }


    public void SetShotNormalPreview()
    {
        Vector3 forwardNoYComponent = Forward;
        forwardNoYComponent.y = 0;

        ShotNormalPreview.SetPoints(transform.position, forwardNoYComponent.normalized);
    }


    public void SetShotAnglePreview(string text)
    {
        Vector3 rotation = new Vector3(0, 90, Angle);

        ShotAnglePreview.transform.localEulerAngles = rotation;

        ShotAnglePreview.text = text;
    }


    public void SetValues(float rotation, float angle, float power)
    {
        Rotation = rotation;
        Angle = angle;
        Power = power;
        ValidateValues();
    }

    private void SetDefaultShotValues()
    {
        // Default values
        Power = 0.5f;
        Rotation = 0;
        Angle = 40;

        ValidateValues();
    }

    private void ValidateValues()
    {
        // Clamp the power
        Power = Mathf.Clamp01(Power);

        // Ensure rotation is between 0 and 360
        while (Rotation < 0)
        {
            Rotation += 360;
        }
        while (Rotation > 360)
        {
            Rotation -= 360;
        }

        // Clamp the angle
        Angle = Mathf.Clamp(Angle, 0, 80);


        transform.rotation = Quaternion.Euler(-Angle, Rotation, 0);
    }


    private void OnValidate()
    {
        ValidateValues();
    }




    private void OnDrawGizmos()
    {
        // Draw the facing
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + Forward / 2);

        /*
        if (biomeSamplePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(biomeSamplePoint.LocalVertexPosition + biomeSamplePoint.Offset, 0.1f);
        }
        */
    }



    public Stats HoleCompleted(Hole nextHole)
    {
        // Finish the current hole
        CurrentHoleStats.EndTime = DateTime.Now;
        HolesCompleted.Add(CurrentHoleStats);

        // Create a new one
        CurrentHoleStats = new Stats(nextHole);
        return CurrentHoleStats;
    }




    public void ResetAllStats(Hole firstHole)
    {
        CurrentHoleStats = new Stats(firstHole);

        HolesCompleted.Clear();
    }


    public class Stats
    {
        public Hole Hole;
        public int ShotsForThisHole;

        public DateTime StartTime;
        public DateTime EndTime;

        public double SecondsForThisHole { get { DateTime now = EndTime; if (now == null) { now = DateTime.Now; } return (now - StartTime).TotalSeconds; } }

        public Stats(Hole hole)
        {
            Hole = hole;
            StartTime = DateTime.Now;

            ShotsForThisHole = 0;
        }
    }




    public struct RigidPreset
    {
        public float Drag;
        public float AngularDrag;

        public RigidPreset(float drag, float angularDrag)
        {
            Drag = drag;
            AngularDrag = angularDrag;
        }
    }

    public enum PlayState
    {
        Shooting,
        Flying,
        Rolling,
    }


}
