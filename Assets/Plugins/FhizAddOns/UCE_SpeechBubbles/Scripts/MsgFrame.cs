﻿// =======================================================================================
// Created and maintained by Fhiz
// Usable for both personal and commercial projects, but no sharing or re-sale
// * Discord Support Server.............: https://discord.gg/YkMbDHs
// * Public downloads website...........: https://www.indie-mmo.net
// * Pledge on Patreon for VIP AddOns...: https://www.patreon.com/IndieMMO
// =======================================================================================
using System.Collections;
using UnityEngine;

// =======================================================================================
//
// =======================================================================================
public partial class MsgFrame : MonoBehaviour
{
    private Animator animator;
    private TextMesh frameText;
    private SpriteRenderer bubble;
    public SpriteRenderer bubbleShadow;
    private float additionalWitdh;

    // -----------------------------------------------------------------------------------
    // Start
    // -----------------------------------------------------------------------------------
    private void Awake()
    {
        animator = GetComponent<Animator>();
        frameText = GetComponentInChildren<TextMesh>();
        bubble = GetComponentInChildren<SpriteRenderer>();

        additionalWitdh = bubble.size.x;
    }

    // -----------------------------------------------------------------------------------
    //
    // -----------------------------------------------------------------------------------
    private void LateUpdate()
    {
        transform.position = transform.parent.position;
        transform.forward = Camera.main.transform.forward;
    }

    // -----------------------------------------------------------------------------------
    //
    // -----------------------------------------------------------------------------------
    public void ShowMessage(string msg)
    {
        if (msg != "")
        {
            msg = msg.Replace(System.Environment.NewLine, " ");

            frameText.text = msg;
            bubble.size = new Vector2(GetTextMeshWidth(frameText, msg) + additionalWitdh, bubble.size.y);
            bubble.gameObject.SetActive(true);

            bubbleShadow.size = new Vector2(GetTextMeshWidth(frameText, msg) + additionalWitdh, bubble.size.y);
            bubbleShadow.gameObject.SetActive(true);

            StopCoroutine("ShowMsgFrameSequence");
            StartCoroutine("ShowMsgFrameSequence");
            animator.SetBool("SHOW_MSG", true);
        }
    }

    // -----------------------------------------------------------------------------------
    //
    // -----------------------------------------------------------------------------------
    private IEnumerator ShowMsgFrameSequence()
    {
        yield return new WaitForSeconds((frameText.text.Length / 10) + 2.5f);
        animator.SetBool("SHOW_MSG", false);
        yield return new WaitForSeconds(0.3f);
        frameText.text = "";
    }

    // -----------------------------------------------------------------------------------
    //
    // -----------------------------------------------------------------------------------
    public float GetTextMeshWidth(TextMesh mesh, string txt)
    {
        float width = 0;
        foreach (char symbol in mesh.text)
        {
            CharacterInfo info;
            if (mesh.font.GetCharacterInfo(symbol, out info, mesh.fontSize, mesh.fontStyle))
            {
                width += info.advance;
            }
        }
        return width * mesh.characterSize * 0.1f;
    }

    // -----------------------------------------------------------------------------------
}