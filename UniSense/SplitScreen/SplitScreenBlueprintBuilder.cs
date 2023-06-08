using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UI;
using UnityEditor;

public class SplitScreenLayout
{
    public RectTransform Parent;
    public RectTransform[] RectTransforms;
    public Rect[] CameraRect;
    

    public SplitScreenLayout(RectTransform[] rects, RectTransform parent)
    {
        RectTransforms = rects;
        Parent = parent;
    }

    public void CalculateCameraRects(Vector2 ScreenSize)
    {
        CameraRect = new Rect[RectTransforms.Length];
        for (int i = 0; i < CameraRect.Length; i++)
        {
           CameraRect[i] = new Rect(
                                     x:( RectTransforms[i].localPosition.x / ScreenSize.x), 
                                     y:( RectTransforms[i].localPosition.y / ScreenSize.y), 
                                     width: RectTransforms[i].sizeDelta.x / ScreenSize.x, 
                                     height: RectTransforms[i].sizeDelta.y / ScreenSize.y
                                     );
          
        }
    }

    public Rect TranslateToUV(Rect rect, Vector2 screenSize)
    {
        rect.position = new Vector2((screenSize.x / 2) + rect.position.x - (rect.size.x / 2), (screenSize.y / 2) + rect.position.y - (rect.size.y / 2)) ;   
        return rect;
    }
    public Rect TranslateFromUV(Rect rect, Vector2 screenSize)
    {
        rect.position = new Vector2(rect.x + (rect.size.x / 2) - (screenSize.x / 2), rect.y + (rect.size.y / 2) - (screenSize.y / 2));
        return rect;
    }

    public void CalculateRectTransforms(Vector2 ScreenSize)
    {
        for (int i = 0; i < CameraRect.Length; i++)
        {
            RectTransforms[i].sizeDelta = new Vector2(CameraRect[i].width * ScreenSize.x, CameraRect[i].height * ScreenSize.y);
            RectTransforms[i].localPosition = new Vector3((CameraRect[i].x * ScreenSize.x), (CameraRect[i].y * ScreenSize.y));
        }
    }
}


[ExecuteInEditMode]
public class SplitScreenBlueprintBuilder : MonoBehaviour
{
    public SplitScreenBlueprint blueprint;
    public int MaxPlayers = 4;

    public RectTransform[] GetFirstLevelChildren(RectTransform rectTransform)
    {
        List<RectTransform> children = new List<RectTransform>();
        int count = rectTransform.childCount;
        for (int i = 0; i < count; i++)
        {
           if(rectTransform.GetChild(i).parent == rectTransform)
            {
                children.Add((RectTransform)rectTransform.GetChild(i));
            }
        }
        return children.ToArray();
    }

    public void Update()
    {
        Vector2 rectSize = GetComponent<RectTransform>().sizeDelta;
        RectTransform[] parents = GetFirstLevelChildren(gameObject.GetComponent<RectTransform>());
        foreach(RectTransform parent in parents)
        {
            parent.sizeDelta = rectSize;
        }
        
    }

    public void Execute()
    {
        
        if (blueprint == null)
        {
            Debug.Log("No Blueprint attached");
            return;
        }
        Vector2 rectSize = GetComponent<RectTransform>().sizeDelta;
        RectTransform[] parents = GetFirstLevelChildren(gameObject.GetComponent<RectTransform>());
        SplitScreenLayout[] layouts = new SplitScreenLayout[parents.Length];
        Rect[][] finalRects = new Rect[parents.Length][];
        for (int i = 0; i < parents.Length; i++)
        {
            
            RectTransform[] rects = GetFirstLevelChildren(parents[i].gameObject.GetComponent<RectTransform>());
            layouts[i] = new SplitScreenLayout(rects, parents[i]);
            layouts[i].CalculateCameraRects(rectSize);
            finalRects[i] = layouts[i].CameraRect;
            
        }
        blueprint.rects = finalRects;
        blueprint.Save();
       
    }

    public void Recover()
    {
        if (blueprint == null) 
        {
            Debug.Log("No Blueprint attached");
            return;
        }
        blueprint.Recover();
        Vector2 rectSize = GetComponent<RectTransform>().sizeDelta;
        RectTransform[] parents = GetFirstLevelChildren(gameObject.GetComponent<RectTransform>());
        SplitScreenLayout[] layouts = new SplitScreenLayout[parents.Length];
        for (int i = 0; i < parents.Length; i++)
        { 
            RectTransform[] rects = GetFirstLevelChildren(parents[i].gameObject.GetComponent<RectTransform>());
            layouts[i] = new SplitScreenLayout(rects, parents[i]);
            if (i >= blueprint.rects.Length) return;
            layouts[i].CameraRect = blueprint.rects[i];
            layouts[i].CalculateRectTransforms(rectSize);
        }

      

    }

    public void ResetDefaults()
    {
        RectTransform[] rectTransforms = GetFirstLevelChildren(gameObject.GetComponent<RectTransform>());
        for(int i = 0; i < rectTransforms.Length; i++)
        {
            DestroyImmediate(rectTransforms[i].gameObject);
        }
        rectTransforms = null;
        Rect[][] defualtRects = blueprint.RetriveDefualt(MaxPlayers);
        if (defualtRects == null) { Debug.LogError("Index Out Of Range"); return; }
        for (int i = 0; i < MaxPlayers; i++)
        {
            GameObject parent = new GameObject((i + 1) + " Players");
            parent.transform.SetParent(gameObject.transform, false);
            parent.AddComponent<RectTransform>();
            parent.AddComponent<RawImage>().color = new Color(0, 0, 255);
            parent.AddComponent<CanvasGroup>().alpha = 0;
            for(int j = 0; j < i + 1; j++)
            {
                float color = (((float)j + 1) / ((float)i + 1));
                GameObject child = new GameObject("Player " + (j + 1));
                child.AddComponent<RectTransform>();
                child.transform.SetParent(parent.transform, false);
                child.AddComponent<RawImage>().color = new Color(color, color, color);
                GameObject label = new GameObject("Player Label");
                label.transform.SetParent(child.transform, false);
                label.AddComponent<RectTransform>();
                label.AddComponent<Text>().color = new Color(1, 0, 0);
                label.GetComponent<Text>().text = "Player " + (j + 1);
                label.GetComponent <Text>().alignment = TextAnchor.MiddleCenter;
                label.GetComponent<Text>().fontSize = 20;
            }
           

        }
      
        Vector2 rectSize = GetComponent<RectTransform>().sizeDelta;
        RectTransform[] parents = GetFirstLevelChildren(gameObject.GetComponent<RectTransform>());
        SplitScreenLayout[] layouts = new SplitScreenLayout[parents.Length];
        for (int i = 0; i < parents.Length; i++)
        {
            RectTransform[] rects = GetFirstLevelChildren(parents[i].gameObject.GetComponent<RectTransform>());
            layouts[i] = new SplitScreenLayout(rects, parents[i]);
            if (i >= defualtRects.Length) return;
            layouts[i].CameraRect = defualtRects[i];
            layouts[i].CalculateRectTransforms(rectSize);
        }


    }
}
