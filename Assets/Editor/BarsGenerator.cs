using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class BarsGenerator : EditorWindow
{
    public GameObject holder;

    private static BarsGenerator window;
    private Image blackBarPrefab;
    //private List<RectTransform> rectsList = new List<RectTransform>();
    private List<RectTransform> verticalRectsList = new List<RectTransform>();
    private List<RectTransform> horizontalRectsList = new List<RectTransform>();

    [MenuItem("My Tools/Show Bars Generator")]
    public static void ShowWindow()
    {
        //window = GetWindow<BarsGenerator>(false, "Bar generator window");
        window = GetWindow<BarsGenerator>("Bar generator window");
        window.Show();
    }

    private void OnGUI()
    {
        blackBarPrefab = AssetDatabase.LoadAssetAtPath<Image>("Assets/Prefabs/BlackBar.prefab") as Image;

        EditorGUILayout.BeginVertical();

        EditorGUILayout.Space(20f);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Bars holder");
        holder = EditorGUILayout.ObjectField(holder, typeof(GameObject), true) as GameObject;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(12f);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.Space(6f);
        if (GUILayout.Button("Get rects"))
        {
            foreach(Image image in holder.GetComponentsInChildren<Image>())
            {
                
                //rectsList.Add(image.GetComponent<RectTransform>());
                RectTransform rectTransform = image.GetComponent<RectTransform>();
                if(rectTransform.rect.width > rectTransform.rect.height)
                {
                    Debug.Log("HOR: " + image.name);
                    horizontalRectsList.Add(rectTransform);
                }
                else
                {
                    Debug.Log("VER: " + image.name);
                    verticalRectsList.Add(rectTransform);
                }
            }
        }
        EditorGUILayout.Space(6f);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(12f);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.Space(6f);
        if (GUILayout.Button("Create bars"))
        {
            if(holder != null)
            {
                Image testImage = Instantiate(blackBarPrefab, holder.transform);
                // X and Y
                testImage.rectTransform.anchoredPosition = new Vector2(10, 800);
                // Width and Height
                testImage.rectTransform.sizeDelta = new Vector2(400, 12);
            }
        }
        EditorGUILayout.Space(6f);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }
}
