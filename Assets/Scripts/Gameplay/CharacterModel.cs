using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class CharacterModel : MonoBehaviour
{
    [SerializeField]
    private Material outlineMaterial;
    [SerializeField]
    private Color selectedOutlineColor;

    private List<Renderer> modelParts;
    private Animator animator;
    private float prevSpeed = 1;
    private int numberOfPauses;
    private Material outlineMaterialInstance;
    private Color initialOutlineColor;
    private List<Material> originalMaterials = new List<Material>();

    public void Initialise()
    {
        animator = GetComponent<Animator>();
        animator.SetFloat("Offset", Random.Range(0.0f, 1.0f));

        //Look inward
        SetLookTowards(Vector3.zero);

        modelParts = GetComponentsInChildren<Renderer>().ToList();
        foreach(var p in modelParts)
        {
            originalMaterials.Add(p.material);
        }
        outlineMaterialInstance = new Material(outlineMaterial);
        initialOutlineColor = outlineMaterialInstance.GetColor("_OutlineColor");
    }

    public void SetLookTowards(Vector3 targetPosition)
    {
        Vector3 relPos = targetPosition - transform.position;
        //transform.rotation = Quaternion.LookRotation(relPos, Vector3.up);
        Quaternion newRotation = Quaternion.LookRotation(relPos, Vector3.up);
        transform.rotation = Quaternion.Euler(0f, newRotation.eulerAngles.y, 0f);
    }

    public void Cast(Card card)
    {
        animator.SetBool("IsCasting", true);
    }

    public void EndCast()
    {
        animator.SetBool("IsCasting", false);
    }

    public void Throw(bool selfTarget)
    {

        if (selfTarget)
        {
            animator.SetTrigger("SelfThrow");
        }
        else
        {
            animator.SetTrigger("OtherThrow");
        }
    }

    public void Damage()
    {
        animator.SetTrigger("Hit");
    }

    public void Heal()
    {
        animator.SetTrigger("Heal");
    }

    public void Death()
    {
        animator.SetTrigger("Death");
    }
    
    public void Downed()
    {
        animator.SetTrigger("Downed");
    }
    
    public void Revive()
    {
        animator.SetTrigger("Revive");
    }

    public void Pause(bool pause)
    {
        if (pause)
        {
            numberOfPauses++;
        }
        else
        {
            numberOfPauses--;
        }

        if(numberOfPauses == 1)
        {
            prevSpeed = animator.speed;
            animator.speed = 0;
        }

        if(numberOfPauses == 0)
        {
            animator.speed = prevSpeed;
        }
    }

    public void ShowOutline()
    {
        foreach(var r in modelParts)
        {
            List<Material> materials = new List<Material>(r.sharedMaterials);
            materials.Add(outlineMaterialInstance);
            r.SetMaterials(materials);
        }
    }

    public void HideOutline()
    {
        foreach (var r in modelParts)
        {
            List<Material> materials = new List<Material>(r.sharedMaterials);
            materials.Remove(outlineMaterialInstance);
            r.SetMaterials(materials);
        }
    }

    public void ShowSelected()
    {
        outlineMaterialInstance.SetColor("_OutlineColor", selectedOutlineColor);
    }

    public void HideSelected()
    {
        outlineMaterialInstance.SetColor("_OutlineColor", initialOutlineColor);
    }

    public void ChangeMaterial(Material material)
    {
        if(modelParts.Count > 0)
        {
            foreach (var modelPart in modelParts)
            {
                modelPart.material = material;
            }
        }
    }

    public void ChangeMaterialToOriginal()
    {
        for(int i = 0; i < modelParts.Count; i++)
        {
            modelParts[i].material = originalMaterials[i];
        }
    }

    private static void ApplyTransparencyInternal(bool isTransparent, Renderer renderer, float transparentValue)
    {
        var material = renderer.material;
        var color = material.color;
        color.a = transparentValue;
        material.color = color;
        
        if (isTransparent)
        {
            material.SetFloat("_Mode", 3);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
        }
        else
        {
            material.SetFloat("_Mode", 0);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = -1;
        }
    }

}
