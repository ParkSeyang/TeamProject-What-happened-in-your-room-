using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARSpaceManager : MonoBehaviour
{
    [Header("AR Foundation Components")]
    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private ARSession arSession;

    [Header("Occlusion Setting")]
    [SerializeField] private Material occlusionMaterial;

    [Header("Generation Settings")]
    [SerializeField] private float scanDuration = 2.0f;
    [SerializeField] private float groupingDistance = 0.5f;
    [SerializeField] private float minBoxThickness = 0.05f;

    [Header("Visualization Colors")]
    [SerializeField] private Color horizontalColor = new Color(0f, 0.8f, 1f, 0.6f);
    [SerializeField] private Color verticalColor = new Color(1f, 0.5f, 0f, 0.6f);
    [SerializeField] private Color mixedColor = new Color(0.5f, 0.9f, 0.2f, 0.6f);

    private List<GameObject> allGeneratedCubes = new List<GameObject>();
    private Dictionary<GameObject, Material> cubeMaterialCache = new Dictionary<GameObject, Material>();
    private Dictionary<ARPlane, Material> planeMaterialCache = new Dictionary<ARPlane, Material>();

    private bool isHorizontalDetectionEnabled = false;
    private bool isVerticalDetectionEnabled = false;
    private bool isScanning = false;
    private bool areCubesVisible = true;
    private bool arePlanesVisible = true;
    private bool isOcclusionActive = false;

    #region Properties (for View)
    public bool IsHorizontalDetectionEnabled => isHorizontalDetectionEnabled;
    public bool IsVerticalDetectionEnabled => isVerticalDetectionEnabled;
    public bool IsScanning => isScanning;
    public bool AreCubesVisible => areCubesVisible;
    public bool ArePlanesVisible => arePlanesVisible;
    #endregion

    public List<GameObject> GetGeneratedCubes() => allGeneratedCubes;
    
    private void Awake()
    {
        planeManager = FindFirstObjectByType<ARPlaneManager>();
        arSession = FindFirstObjectByType<ARSession>();
        occlusionMaterial.renderQueue = 1999; 
    }

    private void OnEnable()
    {
        planeManager.planesChanged += OnPlanesChanged;
    }

    private void OnDisable()
    {
        planeManager.planesChanged -= OnPlanesChanged;
    }

    private void Start() => UpdateDetectionMode();

    private void OnPlanesChanged(ARPlanesChangedEventArgs args)
    {
        if (isOcclusionActive == false) return;
        foreach (var plane in args.added) ApplyOcclusionToPlane(plane);
        foreach (var plane in args.updated) ApplyOcclusionToPlane(plane);
    }

    private void Update()
    {
        var pointer = Pointer.current;
        if (pointer.press.wasPressedThisFrame)
        {
            Vector2 screenPosition = pointer.position.ReadValue();
            if (UnityEngine.EventSystems.EventSystem.current != null && 
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
            HandleTouchDeletion(screenPosition);
        }
    }

    private void HandleTouchDeletion(Vector2 screenPosition)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject hitObj = hit.collider.gameObject;
            if (allGeneratedCubes.Contains(hitObj))
            {
                allGeneratedCubes.Remove(hitObj);
                if (cubeMaterialCache.ContainsKey(hitObj)) { Destroy(cubeMaterialCache[hitObj]); cubeMaterialCache.Remove(hitObj); }
                Destroy(hitObj);
            }
        }
    }

    #region Public Interface

    public void ToggleHorizontal() { isHorizontalDetectionEnabled = !isHorizontalDetectionEnabled; UpdateDetectionMode(); }
    public void ToggleVertical() { isVerticalDetectionEnabled = !isVerticalDetectionEnabled; UpdateDetectionMode(); }
    public void StartBoxGeneration() { if (!isScanning) StartCoroutine(GenerateBoxIncremental()); }
    public void ClearAllBoxes() { ClearPreviousCubes(); }
    public void ResetARSession() { if (arSession != null) arSession.Reset(); }

    public void ToggleCubeVisibility()
    {
        areCubesVisible = !areCubesVisible;
        foreach (var cube in allGeneratedCubes)
        {
            if (cube != null)
            {
                var r = cube.GetComponent<Renderer>();
                if (r != null) r.enabled = areCubesVisible && !isOcclusionActive;
            }
        }
    }

    public void TogglePlaneVisibility()
    {
        arePlanesVisible = !arePlanesVisible;
        
        foreach (var plane in planeManager.trackables)
        {
            var renderers = plane.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers) r.enabled = arePlanesVisible;
        }
    }

    public void SetOcclusion(bool enable)
    {
        isOcclusionActive = enable;

        foreach (var cube in allGeneratedCubes)
        {
            if (cube == null) continue;
            var renderer = cube.GetComponent<MeshRenderer>();
            if (renderer == null) continue;

            if (enable && occlusionMaterial != null)
            {
                renderer.enabled = true; 
                renderer.sharedMaterial = occlusionMaterial;
            }
            else
            {
                renderer.enabled = areCubesVisible;
                if (cubeMaterialCache.TryGetValue(cube, out Material originalMat))
                    renderer.sharedMaterial = originalMat;
            }
        }

        foreach (var plane in planeManager.trackables)
        {
            if (enable) ApplyOcclusionToPlane(plane);
            else RestorePlaneMaterial(plane);
        }
    }

    #endregion

    #region Internal Logic

    private void ApplyOcclusionToPlane(ARPlane plane)
    {
        var renderer = plane.GetComponentInChildren<MeshRenderer>();
        if (renderer == null || occlusionMaterial == null) return;

        if (!planeMaterialCache.ContainsKey(plane))
            planeMaterialCache[plane] = renderer.sharedMaterial;

        renderer.enabled = true; 
        renderer.sharedMaterial = occlusionMaterial;
    }

    private void RestorePlaneMaterial(ARPlane plane)
    {
        var renderer = plane.GetComponentInChildren<MeshRenderer>();
        if (renderer == null) return;

        if (planeMaterialCache.TryGetValue(plane, out Material originalMat))
        {
            renderer.sharedMaterial = originalMat;
            renderer.enabled = arePlanesVisible;
        }
    }

    private void UpdateDetectionMode()
    {
        if (planeManager == null) return;
        PlaneDetectionMode mode = PlaneDetectionMode.None;
        if (isHorizontalDetectionEnabled) mode |= PlaneDetectionMode.Horizontal;
        if (isVerticalDetectionEnabled) mode |= PlaneDetectionMode.Vertical;
        planeManager.requestedDetectionMode = mode;
        planeManager.enabled = (mode != PlaneDetectionMode.None);

        foreach (var plane in planeManager.trackables)
        {
            bool isHor = IsHorizontal(plane);
            bool isVer = IsVertical(plane);
            if (isHor) plane.gameObject.SetActive(isHorizontalDetectionEnabled);
            else if (isVer) plane.gameObject.SetActive(isVerticalDetectionEnabled);
        }
    }

    private IEnumerator GenerateBoxIncremental()
    {
        isScanning = true;
        ClearPreviousCubes();
        yield return new WaitForSeconds(scanDuration);

        List<ARPlane> activePlanes = new List<ARPlane>();
        foreach (var plane in planeManager.trackables)
        {
            if (plane.trackingState != TrackingState.None) activePlanes.Add(plane);
        }

        var planeGroups = GroupNearbyPlanes(activePlanes);
        foreach (var group in planeGroups)
        {
            if (group.Count > 0 && group.Any(IsHorizontal))
            {
                Bounds groupBounds = CalculateAABB(group);
                CreateCubeFromGroup(groupBounds, group);
            }
        }
        
        isScanning = false;
    }

    // BFS 로 탐색을 합니다
    private List<List<ARPlane>> GroupNearbyPlanes(List<ARPlane> planes)
    {
        Dictionary<TrackableId, Bounds> planeBoundsMap = new Dictionary<TrackableId, Bounds>();
        foreach (var p in planes) planeBoundsMap[p.trackableId] = CalculateSinglePlaneBounds(p);
        List<List<ARPlane>> groups = new List<List<ARPlane>>();
        HashSet<TrackableId> visited = new HashSet<TrackableId>();

        foreach (var startPlane in planes)
        {
            if (visited.Contains(startPlane.trackableId)) continue;
            List<ARPlane> currentGroup = new List<ARPlane>();
            Queue<ARPlane> queue = new Queue<ARPlane>();
            queue.Enqueue(startPlane);
            visited.Add(startPlane.trackableId);

            while (queue.Count > 0)
            {
                ARPlane p = queue.Dequeue();
                currentGroup.Add(p);
                Bounds expBounds = planeBoundsMap[p.trackableId];
                expBounds.Expand(groupingDistance);
                foreach (var other in planes)
                {
                    if (visited.Contains(other.trackableId)) continue;
                    if (IsVertical(p) && IsVertical(other)) continue;
                    if (expBounds.Intersects(planeBoundsMap[other.trackableId]))
                    {
                        visited.Add(other.trackableId);
                        queue.Enqueue(other);
                    }
                }
            }
            groups.Add(currentGroup);
        }
        return groups;
    }

    private void CreateCubeFromGroup(Bounds bounds, List<ARPlane> group)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = $"IntegratedBox_Group_{allGeneratedCubes.Count}";
        cube.transform.SetPositionAndRotation(bounds.center, Quaternion.identity);
        cube.transform.localScale = bounds.size;

        bool hasVer = group.Any(IsVertical);
        bool hasHor = group.Any(IsHorizontal);

        var cubeRenderer = cube.GetComponent<Renderer>();
        if (cubeRenderer != null)
        {
            Color finalColor = (hasVer && hasHor) ? mixedColor : (hasVer ? verticalColor : horizontalColor);
            Material visualMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            visualMat.color = finalColor;
            cubeMaterialCache[cube] = visualMat;
            
            if (isOcclusionActive && occlusionMaterial != null)
                cubeRenderer.sharedMaterial = occlusionMaterial;
            else
                cubeRenderer.sharedMaterial = visualMat;

            cubeRenderer.enabled = areCubesVisible;
        }
        allGeneratedCubes.Add(cube);
    }

    private void ClearPreviousCubes()
    {
        foreach (var cube in allGeneratedCubes)
        {
            if (cube != null)
            {
                if (cubeMaterialCache.TryGetValue(cube, out Material mat)) Destroy(mat);
                Destroy(cube);
            }
        }
        allGeneratedCubes.Clear();
        cubeMaterialCache.Clear();
    }

    private Bounds CalculateAABB(List<ARPlane> group)
    {
        Bounds bounds = new Bounds();
        bool initialized = false;
        foreach (var plane in group) EncapsulatePlaneBoundary(ref bounds, ref initialized, plane);
        Vector3 size = bounds.size;
        size.x = Mathf.Max(size.x, minBoxThickness);
        size.y = Mathf.Max(size.y, minBoxThickness);
        size.z = Mathf.Max(size.z, minBoxThickness);
        bounds.size = size;
        return bounds;
    }

    private Bounds CalculateSinglePlaneBounds(ARPlane plane)
    {
        Bounds bounds = new Bounds();
        bool initialized = false;
        EncapsulatePlaneBoundary(ref bounds, ref initialized, plane);
        return bounds;
    }

    private void EncapsulatePlaneBoundary(ref Bounds bounds, ref bool initialized, ARPlane plane)
    {
        foreach (var localPoint in plane.boundary)
        {
            Vector3 worldPoint = plane.transform.TransformPoint(new Vector3(localPoint.x, 0, localPoint.y));
            if (!initialized) { bounds = new Bounds(worldPoint, Vector3.zero); initialized = true; }
            else bounds.Encapsulate(worldPoint);
        }
    }

    private bool IsHorizontal(ARPlane p) => p.alignment == PlaneAlignment.HorizontalUp || p.alignment == PlaneAlignment.HorizontalDown;
    private bool IsVertical(ARPlane p) => p.alignment == PlaneAlignment.Vertical;

    #endregion
}
