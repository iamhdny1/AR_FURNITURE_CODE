using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARFurnitureController : MonoBehaviour
{
    [Header("AR Components")]
    public ARRaycastManager raycastManager;
    public Camera arCamera;

    [Header("Danh sách 5 Prefab Nội Thất")]
    public GameObject[] furniturePrefabs;
    private int currentPrefabIndex = 0;

    [Header("Giao diện Hiển thị Thông số (UI)")]
    public Text infoTitleText;
    public Text infoDetailText;

    private readonly string[] itemNames = {
        "Kệ Tivi Phòng Khách",
        "Tủ Gỗ Đa Năng",
        "Ghế Treo Thư Giãn",
        "Bàn Trà Hiện Đại",
        "Ghế Sofa Băng"
    };

    private readonly string[] itemDetails = {
        "Kích thước: 180 x 40 x 140 cm | Chất liệu: Gỗ MDF | Giá: 4.500.000đ",
        "Kích thước: 120 x 45 x 90 cm | Chất liệu: Gỗ sồi | Giá: 3.800.000đ",
        "Kích thước: 100 x 100 x 180 cm | Chất liệu: Khung thép | Giá: 3.200.000đ",
        "Kích thước: 100 x 60 x 45 cm | Chất liệu: Gỗ tự nhiên | Giá: 2.200.000đ",
        "Kích thước: 200 x 85 x 80 cm | Chất liệu: Vải nỉ cao cấp | Giá: 8.900.000đ"
    };

    private GameObject selectedObject;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private float initialPinchDistance;
    private Vector3 initialScale;

    void Start()
    {
        SelectFurniture(0);
    }

    void Update()
    {
        if (Input.touchCount == 0) return;
        if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) return;

        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                Ray ray = arCamera.ScreenPointToRay(touch.position);
                RaycastHit hitObject;

                if (Physics.Raycast(ray, out hitObject))
                {
                    Transform rootFurniture = GetFurnitureRoot(hitObject.transform);
                    if (rootFurniture != null)
                    {
                        selectedObject = rootFurniture.gameObject;
                        return;
                    }
                }

                if (raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
                {
                    Pose hitPose = hits[0].pose;
                    if (furniturePrefabs.Length > currentPrefabIndex && furniturePrefabs[currentPrefabIndex] != null)
                    {
                        selectedObject = Instantiate(furniturePrefabs[currentPrefabIndex], hitPose.position, hitPose.rotation);
                    }
                }
            }
            else if (touch.phase == TouchPhase.Moved && selectedObject != null)
            {
                float rotationSpeed = 0.4f;
                selectedObject.transform.Rotate(0f, -touch.deltaPosition.x * rotationSpeed, 0f);
            }
        }
        else if (Input.touchCount == 2 && selectedObject != null)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
            {
                initialPinchDistance = Vector2.Distance(touch0.position, touch1.position);
                initialScale = selectedObject.transform.localScale;
            }
            else if (touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved)
            {
                float currentDistance = Vector2.Distance(touch0.position, touch1.position);
                if (Mathf.Approximately(initialPinchDistance, 0)) return;

                float scaleFactor = currentDistance / initialPinchDistance;
                Vector3 targetScale = initialScale * scaleFactor;
                float clamped = Mathf.Clamp(targetScale.x, 0.3f, 2.5f);
                selectedObject.transform.localScale = new Vector3(clamped, clamped, clamped);
            }
        }
    }

    private Transform GetFurnitureRoot(Transform hitTransform)
    {
        Transform current = hitTransform;
        while (current != null)
        {
            if (current.CompareTag("Furniture")) return current;
            current = current.parent;
        }
        return null;
    }

    public void SelectFurniture(int index)
    {
        if (index < 0 || index >= furniturePrefabs.Length) return;
        currentPrefabIndex = index;

        if (infoTitleText != null && index < itemNames.Length)
            infoTitleText.text = itemNames[index];
        if (infoDetailText != null && index < itemDetails.Length)
            infoDetailText.text = itemDetails[index];
    }

    public void ChangeColor(string hexColor)
    {
        if (selectedObject == null) return;
        if (ColorUtility.TryParseHtmlString(hexColor, out Color newColor))
        {
            Renderer[] renderers = selectedObject.GetComponentsInChildren<Renderer>();
            foreach (Renderer rend in renderers)
            {
                foreach (Material mat in rend.materials)
                {
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", newColor);
                    else mat.color = newColor;
                }
            }
        }
    }

    public void DeleteSelectedFurniture()
    {
        if (selectedObject != null)
        {
            Destroy(selectedObject);
            selectedObject = null;
        }
    }

    public void CaptureScreenshot()
    {
        StartCoroutine(TakeScreenshotCoroutine());
    }

    private IEnumerator TakeScreenshotCoroutine()
    {
        yield return new WaitForEndOfFrame();
        string fileName = "AR_Furniture_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
        string path = Path.Combine(Application.persistentDataPath, fileName);
        ScreenCapture.CaptureScreenshot(fileName);
        Debug.Log("Đã lưu ảnh tại: " + path);
    }
}