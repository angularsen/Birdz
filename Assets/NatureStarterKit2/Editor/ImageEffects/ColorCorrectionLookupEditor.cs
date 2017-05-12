using UnityEditor;
using UnityEngine;

// ReSharper disable InconsistentNaming
// ReSharper disable ArrangeTypeMemberModifiers, ArrangeTypeModifiers, FieldCanBeMadeReadOnly.Global, ConvertToConstant.Global, CheckNamespace, MemberCanBePrivate.Global, UnassignedField.Global, UnusedMember.Local, UnusedMember.Global

namespace UnityStandardAssets.ImageEffects
{
    [CustomEditor (typeof(ColorCorrectionLookup))]
    class ColorCorrectionLookupEditor : Editor
    {
        SerializedObject serObj;

        void OnEnable () {
            serObj = new SerializedObject (target);
        }

        private Texture2D tempClutTex2D;


        public override void OnInspectorGUI () {
            serObj.Update ();

            var colorCorrectionLookup = (ColorCorrectionLookup)target;

            EditorGUILayout.LabelField("Converts textures into color lookup volumes (for grading)", EditorStyles.miniLabel);

            //EditorGUILayout.LabelField("Change Lookup Texture (LUT):");
            //EditorGUILayout.BeginHorizontal ();
            //Rect r = GUILayoutUtility.GetAspectRect(1.0ff);

            Texture2D t;

            //EditorGUILayout.Space();
            tempClutTex2D = EditorGUILayout.ObjectField (" Based on", tempClutTex2D, typeof(Texture2D), false) as Texture2D;
            if (tempClutTex2D == null) {
                t = AssetDatabase.LoadMainAssetAtPath(colorCorrectionLookup.basedOnTempTex) as Texture2D;
                if (t) tempClutTex2D = t;
            }

            Texture2D tex = tempClutTex2D;

            if (tex && colorCorrectionLookup.basedOnTempTex != AssetDatabase.GetAssetPath(tex))
            {
                EditorGUILayout.Separator();
                if (!colorCorrectionLookup.ValidDimensions(tex))
                {
                    EditorGUILayout.HelpBox ("Invalid texture dimensions!\nPick another texture or adjust dimension to e.g. 256x16.", MessageType.Warning);
                }
                else if (GUILayout.Button ("Convert and Apply"))
                {
                    string path = AssetDatabase.GetAssetPath (tex);
                    TextureImporter textureImporter = (TextureImporter) AssetImporter.GetAtPath(path);
                    bool doImport = textureImporter.isReadable == false || textureImporter.mipmapEnabled || textureImporter.textureCompression == TextureImporterCompression.Uncompressed;
                    if (doImport)
                    {
                        textureImporter.isReadable = true;
                        textureImporter.mipmapEnabled = false;
                        textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
                        AssetDatabase.ImportAsset (path, ImportAssetOptions.ForceUpdate);
                        //tex = AssetDatabase.LoadMainAssetAtPath(path);
                    }

                    colorCorrectionLookup.Convert(tex, path);
                }
            }

            if (colorCorrectionLookup.basedOnTempTex != "")
            {
                EditorGUILayout.HelpBox("Using " + colorCorrectionLookup.basedOnTempTex, MessageType.Info);
                t = AssetDatabase.LoadMainAssetAtPath(colorCorrectionLookup.basedOnTempTex) as Texture2D;
                if (t) {
                    Rect r = GUILayoutUtility.GetLastRect();
                    r = GUILayoutUtility.GetRect(r.width, 20);
                    r.x += r.width * 0.05f/2.0f;
                    r.width *= 0.95f;
                    GUI.DrawTexture (r, t);
                    GUILayoutUtility.GetRect(r.width, 4);
                }
            }

            //EditorGUILayout.EndHorizontal ();

            serObj.ApplyModifiedProperties();
        }
    }
}
