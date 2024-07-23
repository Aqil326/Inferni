using UnityEngine;
using UnityEngine.UI;

public class IconWithMaskController : MonoBehaviour
{
    [SerializeField] private Image icon; 

    /// <summary>
    /// Adjusting the image in the mask, stretches the image relative to the aspect ratio
    /// </summary>
    /// <param name="sprite">sprite icon</param>
    public void InitIcon(Sprite sprite)
    {
        if (sprite == null)
        {
            icon.sprite = null;
            icon.transform.localScale = Vector3.one;
            return;
        }

        icon.sprite = sprite;
        Rect rtSptite = sprite.textureRect;

        float w = rtSptite.width;
        float h = rtSptite.height;
        float k = w / h;

        icon.transform.localScale = Vector3.one;
        icon.transform.localScale = Vector3.one * (k > 1 ? k : 1 / k);
    }
    
    public void SetAlpha(float val)
    {
        val = Mathf.Clamp(val, 0f, 1f);
        var color = icon.color;
        color.a = val;
        icon.color = color;
    }
}

