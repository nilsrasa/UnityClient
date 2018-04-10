using System;
using System.Collections;
using System.Runtime.InteropServices;
//using sl;
using UnityEngine;
//using Resolution = sl.Resolution;

public class StereolabsZedCamera : MonoBehaviour
{
    [SerializeField] private MeshRenderer _mesh;
    [SerializeField] private ComputeShader _compute;
    [SerializeField] private Texture2D _text;
    [SerializeField] private float _gizmoSize = 0.1f;
    [SerializeField] private float _pictureWidth = 5;
    [SerializeField] private float _pictureHeight = 5;
    [SerializeField] private float _maxDepth = 2;

    //private const int WIDTH = 672;
    //private const int HEIGHT = 376;
    private const int WIDTH = 384;
    private const int HEIGHT = 192;
    //private ZEDCamera _zedCamera;
    private bool _running;
    private Color32[] _depthColors;
    private Resolution _resolution;
    private bool YES;
    private float[,] _depths = new float[WIDTH,HEIGHT];

	// Use this for initialization
	void Start () {
		//_zedCamera = ZEDCamera.GetInstance();
	    //_resolution = new Resolution(WIDTH, HEIGHT);
	}
	
	// Update is called once per frame
	void Update ()
	{
        /*
	    if (_zedCamera.IsCameraReady && !_running)
	    {
	        _running = true;
	        StartCoroutine(RenderDepthTexture());
	    }
        */
    }

    private IEnumerator RenderDepthTexture()
    {
        yield return new WaitForSeconds(5);
        while (true)
        {
            /*
            Vector3[] depthValueBuffer = new Vector3[WIDTH * HEIGHT];
            ComputeBuffer cBuffer = new ComputeBuffer(depthValueBuffer.Length, 12, ComputeBufferType.Default);
            cBuffer.SetData(depthValueBuffer);
            Texture2D _depthTexture = _zedCamera.CreateTextureMeasureType(sl.MEASURE.DEPTH, _resolution);

            RenderTexture rt = new RenderTexture(WIDTH, HEIGHT, 1);
            rt.enableRandomWrite = true;
            rt.Create();
            
            int kernel = _compute.FindKernel("CSMain");
            _compute.SetBuffer(kernel, "depth", cBuffer);
            _compute.SetInt("width", WIDTH);
            _compute.SetTexture(kernel, "inputTexture", _depthTexture);
            _compute.Dispatch(kernel, WIDTH/32, HEIGHT/8, 1);

            _mesh.material.mainTexture = rt;
            cBuffer.GetData(depthValueBuffer);
            cBuffer.Release();
            foreach (Vector3 v in depthValueBuffer)
            {
                _depths[(int) v.x, (int) v.y] = v.z;
            }
            YES = true;
            */
        
        
            yield return new WaitForSeconds(0.2f);
        }
    }

    void OnDrawGizmos()
    {
        if (!YES) return;
        for (int x = 0; x < _depths.GetLength(0); x++)
        {
            for (int y = 0; y < _depths.GetLength(1); y++)
            {
                float depth = _depths[x, y];
                if (depth > _maxDepth) depth = _maxDepth;
                float width = ((float)x / WIDTH) * _pictureWidth;
                float height = ((float)y / HEIGHT) * _pictureWidth;
                
                
                Gizmos.color = new Color(depth / _maxDepth, 0, 0, 1);
                Gizmos.DrawCube(new Vector3(width, height, depth), new Vector3(_gizmoSize, _gizmoSize, _gizmoSize) );
                
            }
        }
    }

}
