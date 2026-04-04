using UnityEngine;
using UnityEngine.Tilemaps;

public class CameraController : MonoBehaviour
{
    public float zoomSpeed = 2f;
    public float minZoom = 3f;
    public float maxZoom = 10f;
    public Tilemap backgroundTilemap;

    private Camera cam;
    private Bounds mapBounds;
    private Vector3 dragOrigin;

    void Start()
    {
        cam = Camera.main;
        if (backgroundTilemap == null)
            backgroundTilemap = Object.FindFirstObjectByType<Tilemap>();

        UpdateBounds();
    }

    public void UpdateBounds()
    {
        backgroundTilemap.CompressBounds();
        mapBounds = backgroundTilemap.localBounds;
        mapBounds.min = backgroundTilemap.transform.TransformPoint(mapBounds.min);
        mapBounds.max = backgroundTilemap.transform.TransformPoint(mapBounds.max);
    }

    void LateUpdate()
    {
        if (FarmingTutorialController.IsTutorialMode || TutorialManager.isTutorialRunning) return;
        HandleZoom();
        HandleMovement();
        ClampCameraPosition();
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

#if UNITY_ANDROID || UNITY_IOS
        if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);
            Vector2 t0Prev = t0.position - t0.deltaPosition;
            Vector2 t1Prev = t1.position - t1.deltaPosition;
            float prevMag = (t0Prev - t1Prev).magnitude;
            float currMag = (t0.position - t1.position).magnitude;
            scroll = (currMag - prevMag) * 0.01f;
        }
#endif
        if (Mathf.Abs(scroll) > 0.001f)
        {
            cam.orthographicSize -= scroll * zoomSpeed;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
    }

    void HandleMovement()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Moved)
        {
            Vector2 delta = Input.GetTouch(0).deltaPosition;
            float speed = cam.orthographicSize * 2 / Screen.height;
            cam.transform.Translate(-delta.x * speed, -delta.y * speed, 0);
        }
#else
        if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
        {
            dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
        }

        if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
        {
            Vector3 difference = dragOrigin - cam.ScreenToWorldPoint(Input.mousePosition);
            cam.transform.position += difference;
        }
#endif
    }

    void ClampCameraPosition()
    {
        float camHeight = cam.orthographicSize;
        float camWidth = cam.orthographicSize * cam.aspect;

        float minX = mapBounds.min.x + camWidth;
        float maxX = mapBounds.max.x - camWidth;
        float minY = mapBounds.min.y + camHeight;
        float maxY = mapBounds.max.y - camHeight;

        float newX = (maxX > minX) ? Mathf.Clamp(cam.transform.position.x, minX, maxX) : mapBounds.center.x;
        float newY = (maxY > minY) ? Mathf.Clamp(cam.transform.position.y, minY, maxY) : mapBounds.center.y;

        cam.transform.position = new Vector3(newX, newY, cam.transform.position.z);
    }

    void OnDrawGizmos()
    {
        if (backgroundTilemap != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(mapBounds.center, mapBounds.size);
        }
    }
}