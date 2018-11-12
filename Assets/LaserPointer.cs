using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserPointer : MonoBehaviour {
    public GameObject laserPrefab;
    public Transform mosaic;
    public Transform cameraRigTransform;
    public GameObject teleportReticlePrefab;
    public Transform headTransform;
    public Vector3 teleportReticleOffset;
    public LayerMask teleportMask;
    private SteamVR_TrackedObject trackedObj;
    private GameObject laser;
    private Transform laserTransform;
    private Vector3 hitPoint;
    private GameObject reticle;
    private Transform teleportReticleTransform;
    private bool shouldTeleport;
    private float startPitchDelta, startYawDelta;
    private Vector3 startMosaicPosition;

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
        RaycastHit hit;
        if (Physics.Raycast(trackedObj.transform.position, transform.forward, out hit, 100, teleportMask))
        {
            hitPoint = hit.point;
            ShowLaser(hit);
            reticle.SetActive(true);
            teleportReticleTransform.position = hitPoint + teleportReticleOffset;
            shouldTeleport = true;
        }
        else
        {
            laser.SetActive(false);
            reticle.SetActive(false);
        }

        if (Controller.GetPressDown(SteamVR_Controller.ButtonMask.Trigger) && shouldTeleport)
        {
            Teleport();
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
    }

    private void ShowLaser(RaycastHit hit)
    {
        laser.SetActive(true);
        laserTransform.position = Vector3.Lerp(trackedObj.transform.position, hitPoint, .5f);
        laserTransform.LookAt(hitPoint);
        laserTransform.localScale = new Vector3(laserTransform.localScale.x, laserTransform.localScale.y,
            hit.distance);
    }

    private void Teleport()
    {
        shouldTeleport = false;
        reticle.SetActive(false);
        Vector3 difference = cameraRigTransform.position - headTransform.position;
        difference.y = 0;
        cameraRigTransform.position = hitPoint + difference;
    }
}
