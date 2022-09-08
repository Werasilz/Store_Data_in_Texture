using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureData : MonoBehaviour
{
    [Header("Compute")]
    public ComputeShader textureCompute;
    private ComputeShader compute;
    private int kernelWriteData;
    private Vector2Int threadGroupSize;

    [Header("Texture")]
    public TextureSize textureSize = TextureSize.x32;
    public enum TextureSize { x32, x64, x128, x256 }
    public Texture2D[] textures;
    private Texture2D texture;

    [Header("Output")]
    public RenderTexture outputTexture;
    public List<float> outputDatas;

    private void Start()
    {
        // Get the texture from array
        texture = textures[(int)textureSize];

        // Create compute
        compute = Instantiate(textureCompute);

        // Kernel setup
        kernelWriteData = compute.FindKernel("WriteData");
        threadGroupSize = GetThreadGroupSize(kernelWriteData);

        // Pass data
        CreateTexture(ref outputTexture);
        compute.SetTexture(kernelWriteData, "_Texture", outputTexture);
        compute.SetInt("_Size", texture.width);

        // Execute
        compute.Dispatch(kernelWriteData, threadGroupSize.x, threadGroupSize.y, 1);

        ReadData();
    }

    public void ReadData()
    {
        Texture2D tex = ConvertRenderTexture(outputTexture);
        outputDatas = new List<float>();
        for (int i = 0; i < tex.width; i++)
        {
            for (int j = 0; j < tex.height; j++)
            {
                outputDatas.Add(Mathf.Round(tex.GetPixel(j, i).r * 256));
            }
        }
    }

    Texture2D ConvertRenderTexture(RenderTexture rt)
    {
        Texture2D tex = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        return tex;
    }

    Vector2Int GetThreadGroupSize(int kernelIndex)
    {
        uint x, y, z;
        compute.GetKernelThreadGroupSizes(kernelIndex, out x, out y, out z);
        Vector2Int size = Vector2Int.zero;
        size.x = Mathf.CeilToInt((float)texture.width / (float)x);
        size.y = Mathf.CeilToInt((float)texture.height / (float)y);
        return size;
    }

    void CreateTexture(ref RenderTexture rt)
    {
        rt = new RenderTexture(texture.width, texture.height, 0);
        rt.enableRandomWrite = true;
        rt.Create();
    }
}
