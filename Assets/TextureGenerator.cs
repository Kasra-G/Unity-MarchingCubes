using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureGenerator : MonoBehaviour
{
    public ComputeShader shader;
    public NoiseParams noiseParameters;
    private RenderTexture render;

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (render == null)
        {
            render = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            render.enableRandomWrite = true;
            render.Create();
        }
        int kernel = shader.FindKernel("CSMain");

        shader.SetTexture(kernel, "Result", render);
        shader.SetInt("octaves", this.noiseParameters.octaves);
        shader.SetFloat("amplitude", this.noiseParameters.amplitude);
        shader.SetVector("offset", this.noiseParameters.offset);
        shader.SetFloat("frequency", this.noiseParameters.frequency);
        shader.SetFloat("persistence", this.noiseParameters.persistence);
        shader.SetFloat("lacunarity", this.noiseParameters.lacunarity);
        shader.SetFloat("deltatime", Time.timeSinceLevelLoad * 1000f / this.noiseParameters.frequency);

        int workgroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        int workgroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
        shader.Dispatch(kernel, workgroupsX, workgroupsY, 1);
        Graphics.Blit(render, destination);
    }
}
