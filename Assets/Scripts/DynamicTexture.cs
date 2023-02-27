using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class DynamicTexture : MonoBehaviour
{
    SpriteRenderer spriteRenderer;
    Texture2D tex;
    int width;
    int height;
    int totalLength;
    Color32[] pixels;
    float[] oldR;
    float[] newR;
    float[] oldG;
    float[] newG;
    float[] oldB;
    float[] newB;
    int[,] neighborList;
    float[,] RFlux;
    float[,] GFlux;
    float[,] BFlux;
    
    int framesToSkip = 0;

    public float FluxInertia = 0.5f;
    public float Temperature = 1.0f;
    public float RandomFactor = 0.0f;
    public float framesPerFrame = 0.1f;
    public float R2R = 0.0f;
    public float R2G = 0.0f;
    public float R2B = 0.0f;
    public float G2R = 0.0f;
    public float G2G = 0.0f;
    public float G2B = 0.0f;
    public float B2R = 0.0f;
    public float B2G = 0.0f;
    public float B2B = 0.0f;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) { Debug.LogError("No SpriteRenderer assigned!"); Destroy(this); return; }
        var texture = spriteRenderer.sprite.texture;
        if (texture == null) { Debug.LogError("No Texture assigned!"); Destroy(this); return; }
        SetTexture(texture);
    }

    // Start is called before the first frame update
    void Start()
    {
        SetRandomImage();
    }

    // Update is called once per frame
    void Update()
    {
        if(framesPerFrame < 1.0f)
        {
            int maxFramesToSkip = (int)(1.0f / framesPerFrame);
            if (framesToSkip > maxFramesToSkip) { framesToSkip = maxFramesToSkip; }
            else if(--framesToSkip > 0) { return; }
            else if(framesToSkip <= 0) { framesToSkip = maxFramesToSkip; }
        }
        // else
        int frameCount = framesPerFrame > 1.0f ? (int)framesPerFrame : 1;
        for (int i = 0; i < frameCount; ++i)
        {
            ComputeNewPixelValues();
        }
        UpdateImage();
    }

    void ComputeNewPixelValues()
    {
        newR.CopyTo(oldR, 0);
        newG.CopyTo(oldG, 0);
        newB.CopyTo(oldB, 0);

        // Calculate fluxes from old state
        for (int i = 0; i < totalLength; ++i)
        {
            for (int j = 0; j < 4; ++j)
            {
                RFlux[i, j] *= FluxInertia;
                GFlux[i, j] *= FluxInertia;
                BFlux[i, j] *= FluxInertia;

                int nei = neighborList[i, j];
                RFlux[i, j] += Temperature * R2R * Mathf.Sign(oldR[nei] - oldR[i]);
                RFlux[i, j] += Temperature * R2G * Mathf.Sign(oldG[nei] - oldG[i]);
                RFlux[i, j] += Temperature * R2B * Mathf.Sign(oldB[nei] - oldB[i]);
                RFlux[i, j] += Temperature * RandomFactor * Random.Range(-1.0f, 1.0f);
                newR[i] -= RFlux[i, j];
                newR[nei] += RFlux[i, j];
                GFlux[i, j] += Temperature * G2R * Mathf.Sign(oldR[nei] - oldR[i]);
                GFlux[i, j] += Temperature * G2G * Mathf.Sign(oldG[nei] - oldG[i]);
                GFlux[i, j] += Temperature * G2B * Mathf.Sign(oldB[nei] - oldB[i]);
                GFlux[i, j] += Temperature * RandomFactor * Random.Range(-1.0f, 1.0f);
                newG[i] -= GFlux[i, j];
                newG[nei] += GFlux[i, j];
                BFlux[i, j] += Temperature * B2R * Mathf.Sign(oldR[nei] - oldR[i]);
                BFlux[i, j] += Temperature * B2G * Mathf.Sign(oldG[nei] - oldG[i]);
                BFlux[i, j] += Temperature * B2B * Mathf.Sign(oldB[nei] - oldB[i]);
                BFlux[i, j] += Temperature * RandomFactor * Random.Range(-1.0f, 1.0f);
                newB[i] -= BFlux[i, j];
                newB[nei] += BFlux[i, j];
            }
        }

        // Populate new state based on fluxes
    }

    void SetRandomImage()
    {
        for (int i = 0; i < totalLength; ++i)
        {
            newR[i] = (byte)Random.Range(0, 256);
            newG[i] = (byte)Random.Range(0, 256);
            newB[i] = (byte)Random.Range(0, 256);
        }
    }

    void UpdateImage()
    {
        for (int i = 0; i < totalLength; ++i)
        {
            pixels[i].r = (byte)(newR[i] < 0.0f ? 0 : (newR[i] > 255.0f) ? 255.0f : newR[i]);
            pixels[i].g = (byte)(newG[i] < 0.0f ? 0 : (newG[i] > 255.0f) ? 255.0f : newG[i]); ;
            pixels[i].b = (byte)(newB[i] < 0.0f ? 0 : (newB[i] > 255.0f) ? 255.0f : newB[i]); ;
        }
        tex.SetPixels32(pixels);
        tex.Apply();
    }

    void SetTexture(Texture2D texture)
    {
        tex = new Texture2D(texture.width, texture.height, texture.format, texture.mipmapCount, false);
        Graphics.CopyTexture(texture, tex);
        Configure();
    }

    void Configure()
    {
        width = tex.width;
        height = tex.height;
        if (width <= 0 || height <= 0) { Debug.LogError("Texture has 0 width or height!"); Destroy(this); return; }
        totalLength = width * height;
        ConstructNeighborList();
        var sprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), spriteRenderer.sprite.pixelsPerUnit);
        spriteRenderer.sprite = sprite;
        newR = new float[totalLength];
        oldR = new float[totalLength];
        newG = new float[totalLength];
        oldG = new float[totalLength];
        newB = new float[totalLength];
        oldB = new float[totalLength];
        RFlux = new float[totalLength, 4];
        GFlux = new float[totalLength, 4];
        BFlux = new float[totalLength, 4];
        pixels = tex.GetPixels32();
        for (int i = 0; i < totalLength; ++i)
        {
            Color32 c = pixels[i];
            newR[i] = c.r;
            newG[i] = c.g;
            newB[i] = c.b;
        }
    }

    void ConstructNeighborList()
    {
        neighborList = new int[totalLength, 4];
        for (int i = 0; i < totalLength; ++i)
        {
            // Left Neighbor
            neighborList[i,0] = i % width != 0 ? i - 1 : i + width - 1;

            // Right Neighbor
            neighborList[i, 1] = i % width != width - 1 ? i + 1 : i - width + 1;

            // Top Neighbor
            neighborList[i, 2] = i > width - 1 ? i - width : i + totalLength - width;

            // Bottom Neighbor
            neighborList[i, 3] = i < totalLength - width ? i + width : i - totalLength + width;
        }
    }
}
