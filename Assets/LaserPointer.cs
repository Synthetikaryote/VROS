using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class LaserPointer : MonoBehaviour {
    public GameObject laserPrefab;
    public Transform mosaic;
    public Transform cameraRigTransform;
    public GameObject teleportReticlePrefab;
    public Transform headTransform;
    public Vector3 teleportReticleOffset;
    public LayerMask teleportMask, portalMask;
    public Main main;
    private SteamVR_TrackedObject trackedObj;
    private GameObject laser;
    private Transform laserTransform;
    private Vector3 teleportPoint;
    private GameObject reticle;
    private Transform teleportReticleTransform;
    private float startPitchDelta, startYawDelta, startRigYaw;
    private Vector3 startMosaicPosition;
    private Vector3 startRigEulerAngles;
    private Portal currentPortal = null;

    private SteamVR_Controller.Device Controller
    {
        get { return SteamVR_Controller.Input((int)trackedObj.index); }
    }

    void Awake()
    {
        trackedObj = GetComponent<SteamVR_TrackedObject>();
    }

    void Start () {
        laser = Instantiate(laserPrefab);
        laserTransform = laser.transform;
        reticle = Instantiate(teleportReticlePrefab);
        teleportReticleTransform = reticle.transform;
    }
	
	void Update () {
        bool shouldTeleport = false;
        if (!RaycastPortal())
        {
            shouldTeleport = RaycastFloor();
        }

        if (Controller.GetPressDown(SteamVR_Controller.ButtonMask.Trigger) && shouldTeleport)
        {
            Teleport();
        }

        if (Controller.GetPressDown(SteamVR_Controller.ButtonMask.Trigger) && currentPortal != null)
        {
            main.LoadDirectory(currentPortal.FilePath);
        }

        if (Controller.GetPressDown(SteamVR_Controller.ButtonMask.ApplicationMenu))
        {
            main.ResetPosition();
        }

        if (Controller.GetPressDown(SteamVR_Controller.ButtonMask.Grip))
        {
            startPitchDelta = Mathf.Tan(-transform.eulerAngles.x * Mathf.Deg2Rad);
            startYawDelta = Mathf.Tan((transform.eulerAngles.y - 90f) * Mathf.Deg2Rad);
            startMosaicPosition = mosaic.localPosition;
        }

        if (Controller.GetPress(SteamVR_Controller.ButtonMask.Grip))
        {
            mosaic.position = startMosaicPosition
                + Vector3.up * (Mathf.Tan(-transform.eulerAngles.x * Mathf.Deg2Rad) - startPitchDelta)
                + Vector3.forward * (Mathf.Tan((transform.eulerAngles.y - 90f) * Mathf.Deg2Rad) - startYawDelta);
        }

        if (Controller.GetPressDown(SteamVR_Controller.ButtonMask.Touchpad))
        {
            startRigYaw = transform.localEulerAngles.y;
            startRigEulerAngles = cameraRigTransform.localEulerAngles;
        }
        if (Controller.GetPress(SteamVR_Controller.ButtonMask.Touchpad))
        {
            cameraRigTransform.localEulerAngles = startRigEulerAngles + Vector3.up * -(transform.localEulerAngles.y - startRigYaw);
        }
    }

    private bool RaycastPortal()
    {
        RaycastHit hit;
        Portal portal = null;
        if (Physics.Raycast(trackedObj.transform.position, transform.forward, out hit, 100f, portalMask))
        {
            portal = hit.collider.gameObject.GetComponent<Portal>();
            ShowLaser(hit);
        }
        else
        {
            if (currentPortal != null)
                currentPortal.Highlight = false;
            currentPortal = null;
        }
        if (portal != null && portal != currentPortal)
        {
            if (currentPortal != null)
                currentPortal.Highlight = false;
            currentPortal = portal;
            currentPortal.Highlight = true;
            reticle.SetActive(false);
            portal.Highlight = true;
        }
        return currentPortal != null;
    }

    private bool RaycastFloor()
    {
        RaycastHit hit;
        if (Physics.Raycast(trackedObj.transform.position, transform.forward, out hit, 100, teleportMask))
        {
            teleportPoint = hit.point;
            ShowLaser(hit);
            reticle.SetActive(true);
            teleportReticleTransform.position = teleportPoint + teleportReticleOffset;
            return true;
        }
        else
        {
            laser.SetActive(false);
            reticle.SetActive(false);
            return false;
        }
    }

    private void ShowLaser(RaycastHit hit)
    {
        laser.SetActive(true);
        laserTransform.position = Vector3.Lerp(trackedObj.transform.position, hit.point, .5f);
        laserTransform.LookAt(hit.point);
        laserTransform.localScale = new Vector3(laserTransform.localScale.x, laserTransform.localScale.y,
            hit.distance);
    }

    private void Teleport()
    {
        reticle.SetActive(false);
        Vector3 difference = cameraRigTransform.position - headTransform.position;
        difference.y = 0;
        cameraRigTransform.position = teleportPoint + difference;
    }
}
