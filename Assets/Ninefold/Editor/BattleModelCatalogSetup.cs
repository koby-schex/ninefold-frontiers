using UnityEditor;
using UnityEngine;
using Ninefold.Presentation;

namespace Ninefold.Editor
{
    /// <summary>Creates a stable Resources-based handoff without changing
    /// model team importers, review scenes or asset GUIDs.</summary>
    internal static class BattleModelCatalogSetup
    {
        private const string Directory="Assets/Ninefold/Resources/Battle";
        private const string File=Directory+"/BattleUnitVisuals.asset";

        [MenuItem("Ninefold/Art/Create or Open Playable Battle Visual Catalog")]
        private static void Open()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Ninefold battle visuals",
                    "Stop Play mode before editing battle prefab registrations.","OK");
                return;
            }
            var catalog=AssetDatabase.LoadAssetAtPath<FrontiersBattleVisualCatalog>(File);
            if(catalog==null)
            {
                if(!AssetDatabase.IsValidFolder("Assets/Ninefold/Resources"))
                    AssetDatabase.CreateFolder("Assets/Ninefold","Resources");
                if(!AssetDatabase.IsValidFolder(Directory))
                    AssetDatabase.CreateFolder("Assets/Ninefold/Resources","Battle");
                catalog=ScriptableObject.CreateInstance<FrontiersBattleVisualCatalog>();
                AssetDatabase.CreateAsset(catalog,File);
                AssetDatabase.SaveAssets();
            }
            Selection.activeObject=catalog;
            EditorGUIUtility.PingObject(catalog);
            EditorUtility.FocusProjectWindow();
            Debug.Log("Battle visuals catalog: "+File+
                ". Match exact gameplay UnitIds and enable Approved For Battle only after art review.");
        }
    }
}
