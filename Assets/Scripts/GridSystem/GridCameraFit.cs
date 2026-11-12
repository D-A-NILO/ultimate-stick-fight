using UnityEngine;

namespace Wreckyard.Core
{
    /// <summary>
    /// Fits this orthographic camera to the whole grid plus a margin. Refits when the grid raises
    /// Resized and when the camera's aspect ratio changes (checked in LateUpdate).
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public class GridCameraFit : MonoBehaviour
    {
        [SerializeField] private GridBehaviour grid;
        [SerializeField, Min(0f)] private float margin = 1f;

        private Camera targetCamera;
        private GridBehaviour subscribedGrid;
        private float lastAspect;

        /// <summary>
        /// Centers the camera on the grid (keeping its z) and sets the orthographic size so the
        /// whole grid plus margin is visible.
        /// </summary>
        public void Fit()
        {
            if (grid == null || grid.Model == null)
            {
                return;
            }

            Camera cam = GetCamera();
            float aspect = cam.aspect;
            if (aspect <= 0f)
            {
                return;
            }

            lastAspect = aspect;

            if (!cam.orthographic)
            {
                Debug.LogWarning($"{nameof(GridCameraFit)} on '{name}' needs an orthographic camera.", this);
                return;
            }

            GridModel model = grid.Model;
            Vector2 size = model.WorldSize;
            Vector3 center = model.Origin + (Vector3)(size * 0.5f);

            Vector3 position = new Vector3(center.x, center.y, transform.position.z);
            if (transform.position != position)
            {
                transform.position = position;
            }

            float halfHeight = size.y * 0.5f + margin;
            float halfWidth = size.x * 0.5f + margin;
            float orthographicSize = Mathf.Max(halfHeight, halfWidth / aspect);
            if (!Mathf.Approximately(cam.orthographicSize, orthographicSize))
            {
                cam.orthographicSize = orthographicSize;
            }
        }

        private void OnEnable()
        {
            SubscribeTo(grid);
            Fit();
        }

        private void OnDisable()
        {
            SubscribeTo(null);
        }

        private void OnValidate()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (grid != subscribedGrid)
            {
                SubscribeTo(grid);
            }

            Fit();
        }

        private void LateUpdate()
        {
            if (!Mathf.Approximately(GetCamera().aspect, lastAspect))
            {
                Fit();
            }
        }

        private void HandleGridResized(GridModel model)
        {
            Fit();
        }

        private void SubscribeTo(GridBehaviour target)
        {
            if (subscribedGrid is not null)
            {
                subscribedGrid.Resized -= HandleGridResized;
            }

            subscribedGrid = target;

            if (subscribedGrid is not null)
            {
                subscribedGrid.Resized += HandleGridResized;
            }
        }

        private Camera GetCamera()
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
            }

            return targetCamera;
        }
    }
}
