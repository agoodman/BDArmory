using System;
using System.Collections.Generic;
using UnityEngine;

namespace BahaTurret
{
    public abstract class MissileBase : PartModule, IBDWeapon
    {
        protected WeaponClasses weaponClass;

        [KSPField]
        public string missileType = "missile";

        [KSPField(isPersistant = true)]
        public string shortName = string.Empty;

        [KSPField]
        public float maxStaticLaunchRange = 5000;

        [KSPField]
        public float minStaticLaunchRange = 10;

        [KSPField]
        public float minLaunchSpeed = 0;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Max Off Boresight"), UI_FloatRange(minValue = 0f, maxValue = 180f, stepIncrement = 5f, scene = UI_Scene.Editor, affectSymCounterparts = UI_Scene.All)]
        public float maxOffBoresight = 50;

        [KSPField]
        public bool guidanceActive = true;

        [KSPField]
        public float lockedSensorFOV = 2.5f;

        [KSPField]
        public float heatThreshold = 150;

        [KSPField]
        public bool allAspect = false;

        [KSPField]
        public bool isTimed = false;

        [KSPField]
        public bool radarLOAL = false;

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Drop Time"),
            UI_FloatRange(minValue = 0f, maxValue = 2f, stepIncrement = 0.1f, scene = UI_Scene.Editor)]
        public float dropTime = 0.4f;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "In Cargo Bay: "),
        UI_Toggle(disabledText = "False", enabledText = "True", affectSymCounterparts = UI_Scene.All)]
        public bool inCargoBay = false;

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Detonation Time"),
    UI_FloatRange(minValue = 2f, maxValue = 30f, stepIncrement = 0.5f, scene = UI_Scene.Editor)]
        public float detonationTime = 2;

        [KSPField]
        public float activeRadarRange = 6000;

        [KSPField]
        public float activeRadarMinThresh = 140;

        public enum MissileStates { Idle, Drop, Boost, Cruise, PostThrust }

        public enum TargetingModes { None, Radar, Heat, Laser, Gps, AntiRad }

        public MissileStates MissileState { get; set; } = MissileStates.Idle;

        public enum GuidanceModes { None, AAMLead, AAMPure, AGM, AGMBallistic, Cruise, STS, Bomb, RCS, BeamRiding }

        public GuidanceModes GuidanceMode;

        public bool HasFired { get; set; } = false;

        public bool Team { get; set; }

        public bool HasMissed { get; set; } = false;

        public Vector3 TargetPosition { get; set; } = Vector3.zero;

        public Vector3 TargetVelocity { get; set; } = Vector3.zero;


        public Vector3 TargetAcceleration { get; set; } = Vector3.zero;

        public float TimeIndex { get; set; } = 0;

        public TargetingModes TargetingMode { get; set; }

        public float TimeToImpact { get; set; }

        public bool TargetAcquired { get; set; } = false;

        public bool ActiveRadar { get; set; } = false;

        public Vessel SourceVessel { get; set; } = null;

        public bool HasExploded { get; set; } = false;

        public float TimeFired = -1;

        protected float lockFailTimer = -1;

        public Vessel legacyTargetVessel;

        public Transform MissileReferenceTransform;

        protected ModuleTargetingCamera targetingPod;

        //laser stuff
        public ModuleTargetingCamera lockedCamera = null;
        protected Vector3 lastLaserPoint;
        protected Vector3 laserStartPosition;
        protected Vector3 startDirection;

        //GPS stuff
        public Vector3d targetGPSCoords;

        //heat stuff
        public TargetSignatureData heatTarget;

        //radar stuff
        //public ModuleRadar radar;
        public VesselRadarData vrd;
        public TargetSignatureData radarTarget;
        private int snapshotTicker;
        private int locksCount = 0;
        private TargetSignatureData[] scannedTargets;
        private float _radarFailTimer = 0;
        private float maxRadarFailTime = 1;
        private float lastRWRPing = 0;
        private bool radarLOALSearching = false;

        public MissileFire TargetMf = null;

        protected bool checkMiss = false;

        private LineRenderer LR;
        protected string debugString = "";

        public WeaponClasses GetWeaponClass()
        {
            return weaponClass;
        }

        public string GetShortName()
        {
            return shortName;
        }

        public string GetSubLabel()
        {
            if (Enum.GetName(typeof(TargetingModes), TargetingMode) == "None")
            {
                return string.Empty;
            }
            return Enum.GetName(typeof(TargetingModes), TargetingMode);
        }

        public Part GetPart()
        {
            return part;
        }

        public abstract void FireMissile();


        public abstract void Jettison();

        public abstract float GetBlastRadius();


        protected abstract void PartDie(Part p);

        public abstract void Detonate();

        public abstract Vector3 GetForwardTransform();

        protected void  AddTargetInfoToVessel()
        {
                TargetInfo info = vessel.gameObject.AddComponent<TargetInfo>();
				info.team = BDATargetManager.BoolToTeam(Team);
				info.isMissile = true;
				info.MissileBaseModule = this;
        }

        public override void OnFixedUpdate()
        {
            if (HasFired && !HasExploded && part != null)
            {
                TimeIndex = Time.time - TimeFired;
            }
        }

        protected void UpdateGPSTarget()
        {
            if (TargetAcquired)
            {
                TargetPosition = VectorUtils.GetWorldSurfacePostion(targetGPSCoords, vessel.mainBody);
                TargetVelocity = Vector3.zero;
                TargetAcceleration = Vector3.zero;
            }
            else
            {

                guidanceActive = false;
            }
        }

        protected void UpdateHeatTarget()
        {
            if (lockFailTimer > 1)
            {
                legacyTargetVessel = null;
                TargetAcquired = false;
                return;
            }

            if (heatTarget.exists && lockFailTimer < 0)
            {
                lockFailTimer = 0;
            }
            if (lockFailTimer >= 0)
            {
                Ray lookRay = new Ray(transform.position, heatTarget.position + (heatTarget.velocity * Time.fixedDeltaTime) - transform.position);
                heatTarget = BDATargetManager.GetHeatTarget(lookRay, lockedSensorFOV / 2, heatThreshold, allAspect);

                if (heatTarget.exists)
                {
                    TargetAcquired = true;
                    TargetPosition = heatTarget.position + (2 * heatTarget.velocity * Time.fixedDeltaTime);
                    TargetVelocity = heatTarget.velocity;
                    TargetAcceleration = heatTarget.acceleration;
                    lockFailTimer = 0;
                }
                else
                {
                    TargetAcquired = false;
                    if (FlightGlobals.ready)
                    {
                        lockFailTimer += Time.fixedDeltaTime;
                    }
                }
            }
        }

        protected void SetLaserTargeting()
        {
            if (TargetingMode == TargetingModes.Laser)
            {
                laserStartPosition = MissileReferenceTransform.position;
                if (lockedCamera)
                {
                    TargetAcquired = true;
                    TargetPosition = lastLaserPoint = lockedCamera.groundTargetPosition;
                    targetingPod = lockedCamera;
                }
            }
        }
        protected void UpdateLaserTarget()
        {
            if (TargetAcquired)
            {
                if (lockedCamera && lockedCamera.groundStabilized && !lockedCamera.gimbalLimitReached && lockedCamera.surfaceDetected) //active laser target
                {
                    TargetPosition = lockedCamera.groundTargetPosition;
                    TargetVelocity = (TargetPosition - lastLaserPoint) / Time.fixedDeltaTime;
                    TargetAcceleration = Vector3.zero;
                    lastLaserPoint = TargetPosition;

                    if (GuidanceMode == GuidanceModes.BeamRiding && TimeIndex > 0.25f && Vector3.Dot(GetForwardTransform(), part.transform.position - lockedCamera.transform.position) < 0)
                    {
                        TargetAcquired = false;
                        lockedCamera = null;
                    }
                }
                else //lost active laser target, home on last known position
                {
                    if (CMSmoke.RaycastSmoke(new Ray(transform.position, lastLaserPoint - transform.position)))
                    {
                        //Debug.Log("Laser missileBase affected by smoke countermeasure");
                        float angle = VectorUtils.FullRangePerlinNoise(0.75f * Time.time, 10) * BDArmorySettings.SMOKE_DEFLECTION_FACTOR;
                        TargetPosition = VectorUtils.RotatePointAround(lastLaserPoint, transform.position, VectorUtils.GetUpDirection(transform.position), angle);
                        TargetVelocity = Vector3.zero;
                        TargetAcceleration = Vector3.zero;
                        lastLaserPoint = TargetPosition;
                    }
                    else
                    {
                        TargetPosition = lastLaserPoint;
                    }
                }
            }
            else
            {
                ModuleTargetingCamera foundCam = null;
                bool parentOnly = (GuidanceMode == GuidanceModes.BeamRiding);
                foundCam = BDATargetManager.GetLaserTarget(this, parentOnly);
                if (foundCam != null && foundCam.cameraEnabled && foundCam.groundStabilized && BDATargetManager.CanSeePosition(foundCam.groundTargetPosition, vessel.transform.position, MissileReferenceTransform.position))
                {
                    Debug.Log("[BDArmory]: Laser guided missileBase actively found laser point. Enabling guidance.");
                    lockedCamera = foundCam;
                    TargetAcquired = true;
                }
            }
        }

        protected void UpdateRadarTarget()
        {
            TargetAcquired = false;

            float angleToTarget = Vector3.Angle(radarTarget.predictedPosition - transform.position, GetForwardTransform());
            if (radarTarget.exists)
            {
                if (!ActiveRadar && ((radarTarget.predictedPosition - transform.position).sqrMagnitude > Mathf.Pow(activeRadarRange, 2) || angleToTarget > maxOffBoresight * 0.75f))
                {
                    if (vrd)
                    {
                        TargetSignatureData t = TargetSignatureData.noTarget;
                        List<TargetSignatureData> possibleTargets = vrd.GetLockedTargets();
                        for (int i = 0; i < possibleTargets.Count; i++)
                        {
                            if (possibleTargets[i].vessel == radarTarget.vessel)
                            {
                                t = possibleTargets[i];
                            }
                        }


                        if (t.exists)
                        {
                            TargetAcquired = true;
                            radarTarget = t;
                            TargetPosition = radarTarget.predictedPosition;
                            TargetVelocity = radarTarget.velocity;
                            TargetAcceleration = radarTarget.acceleration;
                            _radarFailTimer = 0;
                            return;
                        }
                        else
                        {
                            if (_radarFailTimer > maxRadarFailTime)
                            {
                                Debug.Log("[BDArmory]: Semi-Active Radar guidance failed. Parent radar lost target.");
                                radarTarget = TargetSignatureData.noTarget;
                                legacyTargetVessel = null;
                                return;
                            }
                            else
                            {
                                if (_radarFailTimer == 0)
                                {
                                    Debug.Log("[BDArmory]: Semi-Active Radar guidance failed - waiting for data");
                                }
                                _radarFailTimer += Time.fixedDeltaTime;
                                radarTarget.timeAcquired = Time.time;
                                radarTarget.position = radarTarget.predictedPosition;
                                TargetPosition = radarTarget.predictedPosition;
                                TargetVelocity = radarTarget.velocity;
                                TargetAcceleration = Vector3.zero;
                                TargetAcquired = true;
                            }
                        }
                    }
                    else
                    {
                        Debug.Log("[BDArmory]: Semi-Active Radar guidance failed. Out of range and no data feed.");
                        radarTarget = TargetSignatureData.noTarget;
                        legacyTargetVessel = null;
                        return;
                    }
                }
                else
                {
                    vrd = null;

                    if (angleToTarget > maxOffBoresight)
                    {
                        Debug.Log("[BDArmory]: Radar guidance failed.  Target is out of active seeker gimbal limits.");
                        radarTarget = TargetSignatureData.noTarget;
                        legacyTargetVessel = null;
                        return;
                    }
                    else
                    {
                        if (scannedTargets == null) scannedTargets = new TargetSignatureData[5];
                        TargetSignatureData.ResetTSDArray(ref scannedTargets);
                        Ray ray = new Ray(transform.position, radarTarget.predictedPosition - transform.position);
                        bool pingRWR = Time.time - lastRWRPing > 0.4f;
                        if (pingRWR) lastRWRPing = Time.time;
                        bool radarSnapshot = (snapshotTicker > 20);
                        if (radarSnapshot)
                        {
                            snapshotTicker = 0;
                        }
                        else
                        {
                            snapshotTicker++;
                        }
                        RadarUtils.UpdateRadarLock(ray, lockedSensorFOV, activeRadarMinThresh, ref scannedTargets, 0.4f, pingRWR, RadarWarningReceiver.RWRThreatTypes.MissileLock, radarSnapshot);
                        float sqrThresh = radarLOALSearching ? Mathf.Pow(500, 2) : Mathf.Pow(40, 2);

                        if (radarLOAL && radarLOALSearching && !radarSnapshot)
                        {
                            //only scan on snapshot interval
                        }
                        else
                        {
                            for (int i = 0; i < scannedTargets.Length; i++)
                            {
                                if (scannedTargets[i].exists && (scannedTargets[i].predictedPosition - radarTarget.predictedPosition).sqrMagnitude < sqrThresh)
                                {
                                    radarTarget = scannedTargets[i];
                                    TargetAcquired = true;
                                    radarLOALSearching = false;
                                    TargetPosition = radarTarget.predictedPosition + (radarTarget.velocity * Time.fixedDeltaTime);
                                    TargetVelocity = radarTarget.velocity;
                                    TargetAcceleration = radarTarget.acceleration;
                                    _radarFailTimer = 0;
                                    if (!ActiveRadar && Time.time - TimeFired > 1)
                                    {
                                        if (locksCount == 0)
                                        {
                                            RadarWarningReceiver.PingRWR(ray, lockedSensorFOV, RadarWarningReceiver.RWRThreatTypes.MissileLaunch, 2f);
                                            Debug.Log("[BDArmory]: Pitbull! Radar missileBase has gone active.  Radar sig strength: " + radarTarget.signalStrength.ToString("0.0"));

                                        }
                                        else if (locksCount > 2)
                                        {
                                            guidanceActive = false;
                                            checkMiss = true;
                                            if (BDArmorySettings.DRAW_DEBUG_LABELS)
                                            {
                                                Debug.Log("[BDArmory]: Radar missileBase reached max re-lock attempts.");
                                            }
                                        }
                                        locksCount++;
                                    }
                                    ActiveRadar = true;
                                    return;
                                }
                            }
                        }

                        if (radarLOAL)
                        {
                            radarLOALSearching = true;
                            TargetAcquired = true;
                            TargetPosition = radarTarget.predictedPosition + (radarTarget.velocity * Time.fixedDeltaTime);
                            TargetVelocity = radarTarget.velocity;
                            TargetAcceleration = Vector3.zero;
                            ActiveRadar = false;
                        }
                        else
                        {
                            radarTarget = TargetSignatureData.noTarget;
                        }

                    }
                }
            }
            else if (radarLOAL && radarLOALSearching)
            {
                if (scannedTargets == null) scannedTargets = new TargetSignatureData[5];
                TargetSignatureData.ResetTSDArray(ref scannedTargets);
                Ray ray = new Ray(transform.position, GetForwardTransform());
                bool pingRWR = Time.time - lastRWRPing > 0.4f;
                if (pingRWR) lastRWRPing = Time.time;
                bool radarSnapshot = (snapshotTicker > 6);
                if (radarSnapshot)
                {
                    snapshotTicker = 0;
                }
                else
                {
                    snapshotTicker++;
                }
                RadarUtils.UpdateRadarLock(ray, lockedSensorFOV * 3, activeRadarMinThresh * 2, ref scannedTargets, 0.4f, pingRWR, RadarWarningReceiver.RWRThreatTypes.MissileLock, radarSnapshot);
                float sqrThresh = Mathf.Pow(300, 2);

                float smallestAngle = 360;
                TargetSignatureData lockedTarget = TargetSignatureData.noTarget;

                for (int i = 0; i < scannedTargets.Length; i++)
                {
                    if (scannedTargets[i].exists && (scannedTargets[i].predictedPosition - radarTarget.predictedPosition).sqrMagnitude < sqrThresh)
                    {
                        float angle = Vector3.Angle(scannedTargets[i].predictedPosition - transform.position, GetForwardTransform());
                        if (angle < smallestAngle)
                        {
                            lockedTarget = scannedTargets[i];
                            smallestAngle = angle;
                        }


                        ActiveRadar = true;
                        return;
                    }
                }

                if (lockedTarget.exists)
                {
                    radarTarget = lockedTarget;
                    TargetAcquired = true;
                    radarLOALSearching = false;
                    TargetPosition = radarTarget.predictedPosition + (radarTarget.velocity * Time.fixedDeltaTime);
                    TargetVelocity = radarTarget.velocity;
                    TargetAcceleration = radarTarget.acceleration;

                    if (!ActiveRadar && Time.time - TimeFired > 1)
                    {
                        RadarWarningReceiver.PingRWR(new Ray(transform.position, radarTarget.predictedPosition - transform.position), lockedSensorFOV, RadarWarningReceiver.RWRThreatTypes.MissileLaunch, 2f);
                        Debug.Log("[BDArmory]: Pitbull! Radar missileBase has gone active.  Radar sig strength: " + radarTarget.signalStrength.ToString("0.0"));
                    }
                    return;
                }
                else
                {
                    TargetAcquired = true;
                    TargetPosition = transform.position + (startDirection * 500);
                    TargetVelocity = Vector3.zero;
                    TargetAcceleration = Vector3.zero;
                    radarLOALSearching = true;
                    return;
                }
            }

            if (!radarTarget.exists)
            {
                legacyTargetVessel = null;
            }
        }

        protected void DrawDebugLine(Vector3 start, Vector3 end)
        {
            if (BDArmorySettings.DRAW_DEBUG_LINES)
            {
                if (!gameObject.GetComponent<LineRenderer>())
                {
                    LR = gameObject.AddComponent<LineRenderer>();
                    LR.material = new Material(Shader.Find("KSP/Emissive/Diffuse"));
                    LR.material.SetColor("_EmissiveColor", Color.red);
                }
                else
                {
                    LR = gameObject.GetComponent<LineRenderer>();
                }
                LR.SetVertexCount(2);
                LR.SetPosition(0, start);
                LR.SetPosition(1, end);
            }
        }
    }
}