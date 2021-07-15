using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteShadow : MonoBehaviour
{
    public Vector2 offset = new Vector2(-0.1f, -0.1f);

    private SpriteRenderer sprRndCaster;
    private SpriteRenderer sprRndShadow;

    public Transform transCaster;
    public Transform transShadow;

    public Material shadowMaterial;
    public Color shadowColor;

    void Start()
    {
        transCaster = this.transform;
        transShadow = new GameObject().transform;
        transShadow.parent = transCaster;
        transShadow.gameObject.name = "shadow";
        transShadow.localRotation = Quaternion.identity;

        sprRndCaster = GetComponent<SpriteRenderer>();
        sprRndShadow = transShadow.gameObject.AddComponent<SpriteRenderer>();

        sprRndShadow.material = shadowMaterial;
        sprRndShadow.color = shadowColor;
        sprRndShadow.sortingLayerName = "Shadow";
        sprRndShadow.sortingOrder = sprRndCaster.sortingOrder - 1;
    }

    void LateUpdate()
    {
        transShadow.position = new Vector2(transCaster.position.x + offset.x,
            transCaster.position.y + offset.y);

        sprRndShadow.sprite = sprRndCaster.sprite;
    }
}
