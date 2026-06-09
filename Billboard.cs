using UnityEngine;
using System.IO;
using System.Collections.Generic;

namespace GreenDemon;

public class Billboard : MonoBehaviour
{
    private Vector2 spriteFrameScale;
    private Camera _camera;
    private int currentFrame = 0;
    public Dictionary<string, int[]> animations = new Dictionary<string, int[]>();
    public bool mirrorCamera = false;
    public bool levelY = true;
    
    private static Texture2D LoadTexture(string path)
    {
        if (!File.Exists(path))
        {
            GreenDemonPlugin.Logger.LogWarning($"Texture file not found: {path}");
            return null;
        }
        
        byte[] imageData = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.LoadImage(imageData);
        
        if (texture.LoadImage(imageData))
        {
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }
        
        return null;
    }

    public static GameObject CreateBillboard(string texturePath, int spriteFrameScaleX = 0, int spriteFrameScaleY = 0)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "Billboard";

        Shader cutoutShader = Shader.Find("Transparent/Cutout/Diffuse");
        
        Material runtimeMaterial = new Material(cutoutShader);
        
        runtimeMaterial.mainTexture = LoadTexture(texturePath);

        Renderer renderer = quad.GetComponent<Renderer>();
        renderer.material = runtimeMaterial;

        if (spriteFrameScaleX <= 0) spriteFrameScaleX = runtimeMaterial.mainTexture.width;
        if (spriteFrameScaleY <= 0) spriteFrameScaleY = runtimeMaterial.mainTexture.height;

        float widthFit = (float)spriteFrameScaleX / runtimeMaterial.mainTexture.width;
        float heightFit = (float)spriteFrameScaleY / runtimeMaterial.mainTexture.height;
        renderer.material.mainTextureScale = new Vector2(widthFit, heightFit);

        Billboard billboard = quad.AddComponent<Billboard>();
        billboard.spriteFrameScale = new Vector2(spriteFrameScaleX, spriteFrameScaleY);

        return quad;
    }

    public void SetFrame(int frame, string animationName = null)
    {
        if (animationName == null)
        {
            Renderer renderer = GetComponent<Renderer>();
            
            // 1. Get our scales back from the material (or use cached variables)
            float widthFit = renderer.material.mainTextureScale.x;
            float heightFit = renderer.material.mainTextureScale.y;

            // 2. Calculate columns and rows dynamically based on the scale
            int columns = Mathf.RoundToInt(1f / widthFit);
            
            // 3. Find our grid positions
            int frameX = frame % columns;
            int frameY = frame / columns;

            // 4. Translate grid to UV space (incorporating top-down inversion)
            float uOffset = frameX * widthFit;
            float vOffset = 1.0f - (frameY * heightFit) - heightFit;

            // 5. Shift the window
            renderer.material.mainTextureOffset = new Vector2(uOffset, vOffset);
            currentFrame = frame;
        }
        else
        {
            // Handle animationName
            if (animations.ContainsKey(animationName))
            {
                int[] frames = animations[animationName];
                if (frame >= 0 && frame < frames.Length)
                {
                    SetFrame(frames[frame]);
                }
                else
                {
                    GreenDemonPlugin.Logger.LogWarning($"Frame {frame} out of bounds for animation '{animationName}' (max: {frames.Length - 1})");
                }
            }
            else
            {
                GreenDemonPlugin.Logger.LogWarning($"Animation '{animationName}' not found");
            }
        }
    }

    private void FaceCamera()
    {
        if (_camera == null)
        {
            _camera = Camera.main;
            return;
        }

        Vector3 direction;

        if (mirrorCamera)
        {
            direction = _camera.transform.forward;
            transform.Rotate(0, 180, 0);
        }
        else
        {
            direction = transform.position - _camera.transform.position;
        }

        if (levelY)
        {
            direction.y = 0;
        }

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    void LateUpdate()
    {
        FaceCamera();
    }
}
