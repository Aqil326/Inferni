using UnityEngine;
using UnityEngine.UI;

public class CharacterModelDisplay : MonoBehaviour
{
    [SerializeField]
    private Transform characterParent;

    [SerializeField]
    private Camera characterCamera;

    [SerializeField]
    private RenderTexture texture;

    private GameObject model;

    public void Init(RawImage image)
    {
        image.texture = texture;
    }

    public void ShowCharacter(CharacterData characterData)
    {
        if(model != null)
        {
            Destroy(model);
        }
        model = Instantiate(characterData.CharacterModel, characterParent);
        SetGameLayerRecursive(model, LayerMask.NameToLayer("CharacterDisplay"));
    }

    private void SetGameLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
        {
            child.gameObject.layer = layer;

            Transform _HasChildren = child.GetComponentInChildren<Transform>();
            if (_HasChildren != null)
            {
                SetGameLayerRecursive(child.gameObject, layer);
            }
        }
    }
}



