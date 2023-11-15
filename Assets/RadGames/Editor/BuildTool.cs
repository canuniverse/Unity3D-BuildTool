using System.Collections.Generic;
using System.Linq;
using RadGames.Scripts;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class BuildTool : EditorWindow
{
    [MenuItem("Tools/BuildTool")]
    public static void OpenCanon() => GetWindow<BuildTool>();

    public float Radius = 2f;
    public int SpawnCount = 8;
    public KeyCode SpawnKey = KeyCode.P;

    private bool _isActive;

    private SerializedObject _serializedObject;
    private SerializedProperty _propRadius;
    private SerializedProperty _propSpawnCount;
    private SerializedProperty _spawnKey;

    private SpawnData[] _spawnDataPoints;
    private GameObject[] _prefabs;
    private List<GameObject> _spawnPrefabs = new List<GameObject>();

    private Material _materialInvalid;

    [SerializeField] private bool[] _prefabSelectionStates;

    const float TAU = 6.28318530718f;

    private void OnEnable()
    {
        _serializedObject = new SerializedObject(this);
        _propRadius = _serializedObject.FindProperty("Radius");
        _propSpawnCount = _serializedObject.FindProperty("SpawnCount");
        _spawnKey = _serializedObject.FindProperty("SpawnKey");

        GenerateRandomPoints();
        SceneView.duringSceneGui += DuringSceneGUI;

        var shader = Shader.Find("Unlit/InvalidSpawn");
        _materialInvalid = new Material(shader);

        var guids = AssetDatabase.FindAssets("t:prefab", new[] { "Assets/RadGames/Prefabs" });
        var paths = guids.Select(AssetDatabase.GUIDToAssetPath);

        _prefabs = paths.Select(AssetDatabase.LoadAssetAtPath<GameObject>).ToArray();
        if (_prefabSelectionStates == null || _prefabSelectionStates.Length != _prefabs.Length)
        {
            _prefabSelectionStates = new bool[_prefabs.Length];
        }
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= DuringSceneGUI;
        DestroyImmediate(_materialInvalid);
    }

    void GenerateRandomPoints()
    {
        _spawnDataPoints = new SpawnData[SpawnCount];
        for (int i = 0; i < SpawnCount; i++)
        {
            _spawnDataPoints[i].SetRandomValues(_spawnPrefabs);
        }
    }

    private void OnGUI()
    {
        _serializedObject.Update();
        EditorGUILayout.PropertyField(_propRadius);
        _propRadius.floatValue = _propRadius.floatValue.AtLeast(1);
        EditorGUILayout.PropertyField(_propSpawnCount);
        _propSpawnCount.intValue = _propSpawnCount.intValue.AtLeast(1);
        EditorGUILayout.PropertyField(_spawnKey);
        
        if (GUILayout.Button("Activate Tool"))
        {
            _isActive = !_isActive;
        }

        if (_serializedObject.ApplyModifiedProperties())
        {
            GenerateRandomPoints();
            SceneView.RepaintAll();
        }

        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            GUI.FocusControl(null);
            Repaint();
        }
    }

    private void DrawSphere(Vector3 pos)
    {
        Handles.SphereHandleCap(-1, pos, Quaternion.identity, 0.1f, EventType.Repaint);
    }

    private void TrySpawnObjects(List<SpawnPoint> spawnPoints)
    {
        if (spawnPoints.Count == 0)
        {
            return;
        }

        foreach (var spawnPoint in spawnPoints)
        {
            if (spawnPoint.IsValid == false)
            {
                continue;
            }

            var instantiatedPrefab = (GameObject)PrefabUtility.InstantiatePrefab(spawnPoint.SpawnData.Prefab);
            Undo.RegisterCreatedObjectUndo(instantiatedPrefab, "Spawned Objects");
            instantiatedPrefab.transform.position = spawnPoint.Position;
            instantiatedPrefab.transform.rotation = spawnPoint.Rotation;
        }

        GenerateRandomPoints();
    }

    private static bool TryRaycastFromCamera(Vector2 cameraUp, out Matrix4x4 tangentToWorldMtx)
    {
        var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            var hitNormal = hit.normal;
            var hitTangent = Vector3.Cross(hitNormal, cameraUp).normalized;
            var hitBitangent = Vector3.Cross(hitNormal, hitTangent);
            tangentToWorldMtx = Matrix4x4.TRS(hit.point, Quaternion.LookRotation(hitNormal, hitBitangent),
                Vector3.one);
            return true;
        }

        tangentToWorldMtx = default;
        return false;
    }

    private void DuringSceneGUI(SceneView sceneView)
    {
        if (_isActive == false)
        {
            return;
        }

        ShowIcons();

        Handles.zTest = CompareFunction.LessEqual;
        var cameraTransform = sceneView.camera.transform;

        if (Event.current.type == EventType.MouseMove)
        {
            sceneView.Repaint();
        }

        var isHoldingAlt = (Event.current.modifiers & EventModifiers.Alt) != 0;
        if (Event.current.type == EventType.ScrollWheel && isHoldingAlt == false)
        {
            float scrollDir = Mathf.Sign(Event.current.delta.y);
            _serializedObject.Update();
            _propRadius.floatValue *= 1 + scrollDir * 0.05f;
            _serializedObject.ApplyModifiedProperties();
            Repaint();
            Event.current.Use();
        }

        if (TryRaycastFromCamera(cameraTransform.up, out var tangentToWorldMtx))
        {
            var spawnPoints = GetSpawnPoints(tangentToWorldMtx);

            if (Event.current.type == EventType.Repaint)
            {
                DrawCircleRegion(tangentToWorldMtx);
                DrawSpawnPreviews(spawnPoints, sceneView.camera);
            }

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == SpawnKey)
            {
                TrySpawnObjects(spawnPoints);
                Event.current.Use();
            }
        }
    }

    private void ShowIcons()
    {
        Handles.BeginGUI();

        var rect = new Rect(8, 8, 64, 64);
        for (var i = 0; i < _prefabs.Length; i++)
        {
            var prefab = _prefabs[i];
            Texture icon = AssetPreview.GetAssetPreview(prefab);
            EditorGUI.BeginChangeCheck();
            _prefabSelectionStates[i] = GUI.Toggle(rect, _prefabSelectionStates[i], new GUIContent(icon));
            if (EditorGUI.EndChangeCheck())
            {
                _spawnPrefabs.Clear();
                for (var j = 0; j < _prefabs.Length; j++)
                {
                    if (_prefabSelectionStates[j])
                        _spawnPrefabs.Add(_prefabs[j]);
                }

                GenerateRandomPoints();
            }

            rect.y += rect.height + 2;
        }

        Handles.EndGUI();
    }

    private void DrawSpawnPreviews(List<SpawnPoint> spawnPoints, Camera cam)
    {
        foreach (var spawnPoint in spawnPoints)
        {
            if (spawnPoint.SpawnData.Prefab != null)
            {
                Matrix4x4 poseToWorld = Matrix4x4.TRS(spawnPoint.Position, spawnPoint.Rotation, Vector3.one);
                DrawPrefab(spawnPoint.SpawnData.Prefab, poseToWorld, cam, spawnPoint.IsValid);
            }
            else
            {
                Handles.SphereHandleCap(-1, spawnPoint.Position, Quaternion.identity, 0.1f, EventType.Repaint);
                Handles.DrawAAPolyLine(spawnPoint.Position, spawnPoint.Position + spawnPoint.Up);
            }
        }
    }

    private void DrawPrefab(GameObject prefab, Matrix4x4 poseToWorld, Camera cam, bool valid)
    {
        var meshFilters = prefab.GetComponentsInChildren<MeshFilter>();
        foreach (var filter in meshFilters)
        {
            var childToPoseMtx = filter.transform.localToWorldMatrix;
            var childToWorldMtx = poseToWorld * childToPoseMtx;
            var mesh = filter.sharedMesh;
            var material = valid ? filter.GetComponent<MeshRenderer>().sharedMaterial : _materialInvalid;
            Graphics.DrawMesh(mesh, childToWorldMtx, material, 0, cam);
        }
    }

    private List<SpawnPoint> GetSpawnPoints(Matrix4x4 tangentToWorld)
    {
        var hitSpawnPoints = new List<SpawnPoint>();
        foreach (var rndDataPoint in _spawnDataPoints)
        {
            var circleRay = GetCircleRay(tangentToWorld, rndDataPoint.PointInDisc);
            if (Physics.Raycast(circleRay, out var raycastHit))
            {
                var randomRotation = Quaternion.Euler(0f, 0f, rndDataPoint.RandAngleDeg);
                var rotation = Quaternion.LookRotation(raycastHit.normal) *
                               (randomRotation * Quaternion.Euler(90f, 0f, 0f));
                SpawnPoint spawnPoint;
                if (SpawnCount < 2)
                {
                    var origin = tangentToWorld.MultiplyPoint3x4(Vector3.zero);
                    spawnPoint = new SpawnPoint(origin, rotation, rndDataPoint);
                }
                else
                {
                    spawnPoint = new SpawnPoint(raycastHit.point, rotation, rndDataPoint);
                }

                hitSpawnPoints.Add(spawnPoint);
            }
        }

        return hitSpawnPoints;
    }

    private Ray GetCircleRay(Matrix4x4 tangentToWorld, Vector2 pointInCircle)
    {
        var origin = tangentToWorld.MultiplyPoint3x4(Vector3.zero);
        var normal = tangentToWorld.MultiplyVector(Vector3.forward);
        var tangent = tangentToWorld.MultiplyVector(Vector3.right);
        var bitangent = tangentToWorld.MultiplyVector(Vector3.up);

        var rayOrigin = origin + (tangent * pointInCircle.x + bitangent * pointInCircle.y) * Radius;
        rayOrigin += normal * 1.5f;
        var rayDirection = -normal;
        return new Ray(rayOrigin, rayDirection);
    }

    private void DrawCircleRegion(Matrix4x4 tangentToWorld)
    {
        var origin = tangentToWorld.MultiplyPoint3x4(Vector3.zero);
        var normal = tangentToWorld.MultiplyVector(Vector3.forward);
        var tangent = tangentToWorld.MultiplyVector(Vector3.right);
        var bitangent = tangentToWorld.MultiplyVector(Vector3.up);

        Handles.color = Color.red;
        Handles.DrawAAPolyLine(6, origin, origin + tangent);
        Handles.color = Color.green;
        Handles.DrawAAPolyLine(6, origin, origin + bitangent);
        Handles.color = Color.blue;
        Handles.DrawAAPolyLine(6, origin, origin + normal);
        Handles.color = Color.white;

        const int circleDetail = 128;
        var circlePoints = new Vector3[circleDetail];
        for (var i = 0; i < circleDetail; i++)
        {
            var t = i / (float)circleDetail - 1;
            var angRad = t * TAU;
            var direction = new Vector2(Mathf.Cos(angRad), Mathf.Sin(angRad));
            var ray = GetCircleRay(tangentToWorld, direction);
            if (Physics.Raycast(ray, out var cHit))
            {
                circlePoints[i] = cHit.point + cHit.normal * 0.02f;
            }
            else
            {
                circlePoints[i] = ray.origin;
            }
        }

        Handles.DrawAAPolyLine(circlePoints);
    }
}