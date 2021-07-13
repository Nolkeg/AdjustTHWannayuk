using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;

public class AdjustWannayuk : EditorWindow
{
    static bool overrideAll = true;
    static float wannayukHeight = 30f;
    static float aumXPlacementAfter = -35f;
    static float aumXPlacementBefore = 10f;
    
    Object _fontAsset;
    void OnGUI () 
    {
        _fontAsset = EditorGUILayout.ObjectField("FontAsset",_fontAsset, typeof(TMP_FontAsset), false);
        overrideAll = EditorGUILayout.Toggle("Override", overrideAll);
        wannayukHeight = EditorGUILayout.FloatField("Height offset", wannayukHeight);
        aumXPlacementAfter = EditorGUILayout.FloatField("สระอำ x offset after", aumXPlacementAfter);
        aumXPlacementBefore = EditorGUILayout.FloatField("สระอำ x offset before", aumXPlacementBefore);
        
        if (GUILayout.Button("Adjust"))
        {
            TMP_FontAsset fontAsset = _fontAsset as TMP_FontAsset;
            _Adjust(fontAsset);
        }
        if (GUILayout.Button("Clear"))
        {
            TMP_FontAsset fontAsset = _fontAsset as TMP_FontAsset;
            _Clear(fontAsset);
        }
    }
    
    [MenuItem ("Window/TextMeshPro/AdjustWannayuk")]
    public static void  AdjustThaiSara () {
        EditorWindow.GetWindow(typeof(AdjustWannayuk));
    }
    
    /// <summary>
    /// Quick adjust by ContextMenu
    /// </summary>
    [MenuItem("Assets/AdjustWannayuk")]
    static void Adjust()
    {
        TMP_FontAsset fontAsset = Selection.activeObject as TMP_FontAsset;
        _Adjust(fontAsset);
    }
    
    [MenuItem("Assets/AdjustWannayuk",true)]
    static bool ValidateLogSelection()
    {
        return Selection.activeObject is TMP_FontAsset;
    }

    static void _Adjust(TMP_FontAsset fontAsset)
    {
        if (fontAsset == null)
        {
            Debug.Log("No FontAsset selected");
            return;
        }
        
        var glyphPairAdjustmentRecords = new List<TMP_GlyphPairAdjustmentRecord>(fontAsset.fontFeatureTable.glyphPairAdjustmentRecords);
        var lookupTable = fontAsset.characterLookupTable;
        
        var glyphPairAdjustmentRecordLookupDictionary = 
            (Dictionary<uint, TMP_GlyphPairAdjustmentRecord>) fontAsset.fontFeatureTable
            .GetType()
            .GetField("m_GlyphPairAdjustmentRecordLookupDictionary", BindingFlags.NonPublic | BindingFlags.Instance)?
            .GetValue(fontAsset.fontFeatureTable);

        int[] saras = new int[7];
        int[] wannayuks = new int[4];
        
        //get sara 
        saras[0] = (int) lookupTable[GetUnicodeCharacter("ิ")].glyphIndex;  // อิ
        saras[1] = (int) lookupTable[GetUnicodeCharacter("ี")].glyphIndex;  // อี
        saras[2] = (int) lookupTable[GetUnicodeCharacter("ึ")].glyphIndex;  // อึ
        saras[3] = (int) lookupTable[GetUnicodeCharacter("ื")].glyphIndex;  // อื
        saras[4] = (int) lookupTable[GetUnicodeCharacter("ำ")].glyphIndex; // ำ
        saras[5] = (int) lookupTable[GetUnicodeCharacter("ั")].glyphIndex;  // ั
        saras[6] = (int) lookupTable[GetUnicodeCharacter("ํ")].glyphIndex;  // ํ
        //get wanna yuk
        wannayuks[0] = (int) lookupTable[GetUnicodeCharacter("่")].glyphIndex; //เอก
        wannayuks[1] = (int) lookupTable[GetUnicodeCharacter("้")].glyphIndex; //โท
        wannayuks[2] = (int) lookupTable[GetUnicodeCharacter("๊")].glyphIndex; //ตรี
        wannayuks[3] = (int) lookupTable[GetUnicodeCharacter("๋")].glyphIndex; //จัตวา
        int recordAdd = 0;
        foreach (var sara in saras)
        {
            foreach (var wannayuk in wannayuks)
            {
                float xPlacement = sara == saras[4] || sara == saras[6] ? aumXPlacementAfter : 0;
                
                TMP_GlyphValueRecord saraPosition = new TMP_GlyphValueRecord(0, 0, 0, 0);
                TMP_GlyphAdjustmentRecord saraGlyph = new TMP_GlyphAdjustmentRecord((uint) sara, saraPosition);
                
                TMP_GlyphValueRecord wannayukPosition = new TMP_GlyphValueRecord(xPlacement, wannayukHeight, 0, 0);
                TMP_GlyphAdjustmentRecord wannayukGlyph = new TMP_GlyphAdjustmentRecord((uint) wannayuk, wannayukPosition);

                var saraThenWannayukGlyphPair = new TMP_GlyphPairAdjustmentRecord(saraGlyph, wannayukGlyph);

                if (sara == saras[4] || sara == saras[6])
                {
                    xPlacement = aumXPlacementBefore;
                    wannayukPosition = new TMP_GlyphValueRecord(xPlacement, wannayukHeight, 0, 0);
                    wannayukGlyph = new TMP_GlyphAdjustmentRecord((uint) wannayuk, wannayukPosition);
                }
                
                var wannayukThenSaraGlyphPair = new TMP_GlyphPairAdjustmentRecord(wannayukGlyph, saraGlyph); 
                
                uint firstPairKey = saraThenWannayukGlyphPair.firstAdjustmentRecord.glyphIndex << 16 | saraThenWannayukGlyphPair.secondAdjustmentRecord.glyphIndex;
                uint secondPairKey = wannayukThenSaraGlyphPair.firstAdjustmentRecord.glyphIndex << 16 | wannayukThenSaraGlyphPair.secondAdjustmentRecord.glyphIndex;
                
                if (overrideAll)
                {
                    glyphPairAdjustmentRecords.RemoveAll(record => IsGlyphPairEqual(record, saraThenWannayukGlyphPair) || 
                                                                   IsGlyphPairEqual(record, wannayukThenSaraGlyphPair));
                    
                    glyphPairAdjustmentRecords.Add(saraThenWannayukGlyphPair);
                    glyphPairAdjustmentRecords.Add(wannayukThenSaraGlyphPair);
                    
                    if (glyphPairAdjustmentRecordLookupDictionary != null && !glyphPairAdjustmentRecordLookupDictionary.ContainsKey(firstPairKey))
                    {
                        glyphPairAdjustmentRecordLookupDictionary.Add(firstPairKey, saraThenWannayukGlyphPair);
                    }

                    recordAdd += 2;
                }
                else if (glyphPairAdjustmentRecordLookupDictionary != null)
                {
                    if (!glyphPairAdjustmentRecordLookupDictionary.ContainsKey(firstPairKey))
                    {
                        glyphPairAdjustmentRecords.Add(saraThenWannayukGlyphPair);
                        recordAdd++;
                    }
                    
                    if (!glyphPairAdjustmentRecordLookupDictionary.ContainsKey(secondPairKey))
                    {
                        glyphPairAdjustmentRecords.Add(wannayukThenSaraGlyphPair);
                        recordAdd++;
                    }
                }
            }
        }

        if (recordAdd > 0)
        {
            fontAsset.fontFeatureTable.glyphPairAdjustmentRecords = glyphPairAdjustmentRecords;
            fontAsset.fontFeatureTable.SortGlyphPairAdjustmentRecords();
            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            Canvas.ForceUpdateCanvases();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
        
        Debug.Log("Adjust font : <color=#2bcaff>" + fontAsset.name + "</color>" +
                  " Height offset : <color=#d8ff2b>" + wannayukHeight + "</color>" +
                  " Number of adjustment add : <color=#5dfa41>" + recordAdd + "</color>");
    }

    static void _Clear(TMP_FontAsset fontAsset)
    {
        fontAsset.fontFeatureTable.glyphPairAdjustmentRecords = new List<TMP_GlyphPairAdjustmentRecord>();
        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        Canvas.ForceUpdateCanvases();
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
    }
    
    static uint GetUnicodeCharacter (string source)
    {
        uint unicode;

        if (source.Length == 1)
            unicode = source[0];
        else if (source.Length == 6)
            unicode = (uint)TMP_TextUtilities.StringHexToInt(source.Replace("\\u", ""));
        else
            unicode = (uint)TMP_TextUtilities.StringHexToInt(source.Replace("\\U", ""));

        return unicode;
    }

    static bool IsGlyphPairEqual(TMP_GlyphPairAdjustmentRecord a, TMP_GlyphPairAdjustmentRecord b)
    {
        return a.firstAdjustmentRecord.glyphIndex == b.firstAdjustmentRecord.glyphIndex &&
               a.secondAdjustmentRecord.glyphIndex == b.secondAdjustmentRecord.glyphIndex;
    }
}
