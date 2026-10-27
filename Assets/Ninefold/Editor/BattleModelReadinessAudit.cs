using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Ninefold.Presentation;

namespace Ninefold.Editor
{
    /// <summary>
    /// Read-only Unity audit of the model-to-gameplay-ID integration. It does
    /// not edit the model team's art, save a prefab or toggle approvals.
    /// </summary>
    internal static class BattleModelReadinessAudit
    {
        private const string CatalogPath="Assets/Ninefold/Resources/Battle/BattleUnitVisuals.asset";

        [MenuItem("Ninefold/Art/Audit Playable Battle Models")]
        private static void Audit()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Battle model audit",
                    "Stop Play mode to audit prefab assets.","OK");
                return;
            }
            var catalog=AssetDatabase.LoadAssetAtPath<FrontiersBattleVisualCatalog>(CatalogPath);
            if(catalog==null)
            {
                EditorUtility.DisplayDialog("Battle model audit",
                    "No runtime visual catalog exists yet. Use Ninefold > Art > Create or Open Playable Battle Visual Catalog first.","OK");
                return;
            }

            var serialized=new SerializedObject(catalog);
            var entries=serialized.FindProperty("entries");
            if(entries==null)
            {
                Debug.LogError("Battle model catalog entries are unreadable.");
                return;
            }

            int errors=0,warnings=0,ready=0;
            var seen=new HashSet<string>(StringComparer.Ordinal);
            var report=new StringBuilder();
            report.AppendLine("NINEFOLD — BATTLE MODEL READINESS AUDIT");
            report.AppendLine("Read-only inspection; no approval has been changed.");
            if(entries.arraySize==0)
                report.AppendLine("No entries configured. All battles continue using the safe placeholder models.");

            void Error(string message) { errors++; report.AppendLine("ERROR: "+message); }
            void Warning(string message) { warnings++; report.AppendLine("REVIEW: "+message); }

            for(int i=0;i<entries.arraySize;i++)
            {
                int errorsBefore=errors;
                var e=entries.GetArrayElementAtIndex(i);
                string id=e.FindPropertyRelative("UnitId").stringValue;
                var prefab=e.FindPropertyRelative("Prefab").objectReferenceValue as GameObject;
                bool approved=e.FindPropertyRelative("ApprovedForBattle").boolValue;
                string name=string.IsNullOrEmpty(id)?"(blank UnitId)":id;
                report.AppendLine("");
                report.AppendLine("ENTRY "+(i+1)+": "+name+
                    (approved?" [approved for battle]":" [not approved]"));

                if(string.IsNullOrWhiteSpace(id))
                {
                    Error("Every catalog entry needs an exact gameplay UnitId.");
                    continue;
                }
                if(!seen.Add(id))
                {
                    Error("Duplicate gameplay UnitId "+id+"; runtime intentionally falls back.");
                    continue;
                }
                if(prefab==null)
                {
                    if(approved)Error(name+" is marked approved but has no prefab.");
                    else Warning(name+" has no assigned prefab; placeholders remain.");
                    continue;
                }

                string path=AssetDatabase.GetAssetPath(prefab);
                if(!path.EndsWith(".prefab",StringComparison.OrdinalIgnoreCase))
                {
                    Error(name+": "+path+" is not a stable Unity prefab asset.");
                    continue;
                }
                if(path.StartsWith("Assets/Ninefold/ArtReview/",StringComparison.OrdinalIgnoreCase))
                    Error(name+": editor-generated review prefab is not a supported production runtime asset.");

                var dependencies=AssetDatabase.GetDependencies(path,true);
                var reviewDependencies=dependencies.Where(d=>
                    d.StartsWith("Assets/Ninefold/ArtReview/",StringComparison.OrdinalIgnoreCase))
                    .Take(5).ToArray();
                if(reviewDependencies.Length>0)
                    Error(name+": prefab has transient ArtReview dependencies: "+
                        string.Join(", ",reviewDependencies));

                int missingScripts=0;
                foreach(var t in prefab.GetComponentsInChildren<Transform>(true))
                    missingScripts+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                if(missingScripts>0)Error(name+": "+missingScripts+" missing component script(s).");
                int liveScripts=prefab.GetComponentsInChildren<MonoBehaviour>(true).Length;
                if(liveScripts>0)
                    Error(name+": "+liveScripts+" MonoBehaviour script(s); visual-only battle prefabs cannot execute scripts.");

                var renderers=prefab.GetComponentsInChildren<Renderer>(true)
                    .Where(renderer=>renderer.enabled).ToArray();
                if(renderers.Length==0)Error(name+": no enabled visual renderers.");
                long triangleCount=0;
                int materials=0;
                var textures=new HashSet<Texture>();
                foreach(var renderer in renderers)
                {
                    Mesh mesh=null;
                    if(renderer is SkinnedMeshRenderer skin)mesh=skin.sharedMesh;
                    else
                    {
                        var filter=renderer.GetComponent<MeshFilter>();
                        if(filter!=null)mesh=filter.sharedMesh;
                    }
                    if(mesh!=null)
                        for(int sub=0;sub<mesh.subMeshCount;sub++)
                            triangleCount+=(long)mesh.GetIndexCount(sub)/3L;
                    foreach(var material in renderer.sharedMaterials)
                    {
                        if(material==null) { Error(name+": renderer has an empty material slot."); continue; }
                        materials++;
                        if(material.shader==null || material.shader.name.Contains("InternalError"))
                            Error(name+": missing shader on "+material.name);
                        foreach(var property in material.GetTexturePropertyNames())
                        {
                            var texture=material.GetTexture(property);
                            if(texture!=null)textures.Add(texture);
                        }
                    }
                }
                report.AppendLine("Geometry: "+renderers.Length+" renderers; approx. "+
                    triangleCount+" rendered triangles; "+materials+
                    " material slots; "+textures.Count+" distinct textures.");
                if(triangleCount>100000)
                    Warning(name+": high triangle estimate. Profile on iPhone before battle approval.");
                if(materials>24)
                    Warning(name+": many material slots may cause excess draw calls.");
                foreach(var texture in textures)
                    if(texture.width>2048 || texture.height>2048)
                        Warning(name+": texture "+texture.name+" exceeds 2048 px on one axis.");

                int cameras=prefab.GetComponentsInChildren<Camera>(true).Length;
                int lights=prefab.GetComponentsInChildren<Light>(true).Length;
                int colliders=prefab.GetComponentsInChildren<Collider>(true).Length;
                int rigidbodies=prefab.GetComponentsInChildren<Rigidbody>(true).Length;
                if(cameras+lights+colliders+rigidbodies>0)
                    Warning(name+": contains "+cameras+" cameras, "+lights+" lights, "+
                        colliders+" colliders and "+rigidbodies+
                        " rigidbodies (disabled by the battle renderer).");

                var animators=prefab.GetComponentsInChildren<Animator>(true);
                if(animators.Length==0)
                    Warning(name+": no Animator; static rendering is supported, but authored locomotion is unavailable.");
                else
                {
                    bool moving=false,attack=false,hit=false;
                    foreach(var animator in animators)
                    {
                        if(animator.runtimeAnimatorController==null)
                            Warning(name+": Animator lacks a runtime controller.");
                        foreach(var parameter in animator.parameters)
                        {
                            if(parameter.name=="Moving" &&
                                parameter.type==AnimatorControllerParameterType.Bool)moving=true;
                            if(parameter.name=="Attack" &&
                                parameter.type==AnimatorControllerParameterType.Trigger)attack=true;
                            if(parameter.name=="Hit" &&
                                parameter.type==AnimatorControllerParameterType.Trigger)hit=true;
                        }
                    }
                    if(!moving || !attack || !hit)
                        Warning(name+": Animator lacks one or more optional Moving(bool), Attack/Hit(trigger) cues.");
                }
                if(approved && errors==errorsBefore && triangleCount>0 &&
                    liveScripts==0 && renderers.Length>0 &&
                    reviewDependencies.Length==0)
                    ready++;
            }

            report.AppendLine("");
            report.AppendLine("RESULT: "+errors+" errors, "+warnings+
                " review items, "+ready+" approved entries with valid basic presentation wiring.");
            report.AppendLine("This audit does NOT certify canon, iPhone GPU performance, transparency or animation aesthetics.");
            var output=report.ToString();
            if(errors>0)Debug.LogError(output);
            else if(warnings>0)Debug.LogWarning(output);
            else Debug.Log(output);
            EditorUtility.DisplayDialog("Battle model audit",
                errors+" errors · "+warnings+" review items · "+ready+
                " approved candidates. See Console for the complete per-prefab report.","OK");
        }
    }
}
