using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseCursorHover : MonoBehaviour
{
    public Texture2D cursorTexture;
    public CursorMode cursorMode = CursorMode.Auto;
    public Vector2 hotSpot = Vector2.zero;
    public void OnMouseOver()
    {

        //        Cursor.SetCursor(cursorTexture, hotSpot, cursorMode);
#if UNITY_WEBGL
                float xspot = cursorTexture.width / 2;
                float yspot = cursorTexture.height / 2;
                Vector2 hotSpot = new Vector2(xspot, yspot);
                Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.ForceSoftware);
#else
        Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.Auto);
#endif
    }

    public void OnMouseExit()
    {

        Cursor.SetCursor(null, Vector2.zero, cursorMode);
    }
}
