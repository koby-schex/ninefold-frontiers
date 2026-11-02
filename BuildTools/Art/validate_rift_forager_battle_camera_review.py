#!/usr/bin/env python3
"""PR acceptance gate for factual, battle-scale Rift Forager review captures.

This is source-structure validation, not a native Unity compilation or a
visual/lore/performance claim. Only pinned Unity can render the PNG evidence.
"""
from pathlib import Path
import re

root=Path(__file__).resolve().parents[2]
def read(p): return (root/p).read_text(encoding="utf-8")
tool=read("Assets/Ninefold/Editor/RiftForagerBattleCameraReview.cs")
game=read("Assets/Ninefold/Runtime/Presentation/PlaytestBattlefield.cs")
rig=read("Assets/Ninefold/Editor/AshboundRigReview.cs")
motion=read("Assets/Ninefold/Runtime/Presentation/FrontiersBattleModelMotion.cs")
guide=read("Docs/Review/RiftForagerBattleCameraGate01/README.md")

def expect(ok,msg):
    if not ok:
        raise AssertionError(msg)

expect('Review Rift Forager at Battle Camera Scale' in tool and
       'RiftForagerRigStudy01.prefab' in tool and
       'Build Rift Forager Skinned Benchmark' in rig,
       "Tool must be available in Unity and consume existing actual skinned prefab")
expect('ReviewRoot="Assets/Ninefold/ArtReview/"' in tool and
       'StartsWith(ReviewRoot+"AshboundRiftForagerRig01_"' in tool and
       'GetComponentsInChildren<SkinnedMeshRenderer>' in tool and
       'renderer.bones.Length!=AshboundSkeleton.Count' in tool and
       'distinctMaterials!=8' in tool,
       "Rig must be validated, not an arbitrary possibly unsafe prefab")
expect('GetComponentsInChildren<MonoBehaviour>' in tool and
       'GetComponentsInChildren<Rigidbody>' in tool and
       'GetComponentsInChildren<Collider>' in tool,
       "Candidate must be presentation only")
expect('EditorSceneManager.NewPreviewScene()' in tool and
       'EditorSceneManager.ClosePreviewScene(scene)' in tool and
       'PrefabUtility.InstantiatePrefab(prefab,scene)' in tool and
       'SaveCurrentModifiedScenesIfUserWantsTo' not in tool,
       "Do not open/overwrite the user's real gameplay or scene in the comparison tool")
expect('Library","NinefoldReviews","RiftForager"' in tool and
       'File.WriteAllBytes(path,image.EncodeToPNG())' in tool and
       'review-report.txt' in tool and
       'EditorUtility.RevealInFinder' in tool,
       "Export actual local images and geometric metrics for review")
expect('camera.transform.rotation = Quaternion.Euler(58, 0, 0);' in game and
       'float needed = 4;' in game and
       'new View("front",180f)' in tool and
       'new View("three-quarter",140f)' in tool and
       'new View("side",90f)' in tool and
       'new View("rear",0f)' in tool and
       'Capture(camera,390,844,58f,view.Yaw,4f,' in tool and
       'Capture(camera,320,568,58f,view.Yaw,4f,' in tool,
       "Tactical views must use the actual game's tilt/minimum framing and portrait resolutions")
expect('new[]{"Idle","Attack","Afterecho"}' in tool and
       'new[]{"Idle","Attack"}' in tool and
       'clips[pose].SampleAnimation(actor,clips[pose].length*.5f)' in tool,
       "Capture different actual skeletal poses rather than fake static textures")
expect('ContactSheet(export,tactical,4,3,390,844,tacticalSheet)' in tool and
       'ContactSheet(export,narrowFrames,2,2,320,568,narrowSheet)' in tool and
       'contact-sheet-tactical-390x844.png' in tool and
       'contact-sheet-narrow-320x568.png' in tool and
       'tile.GetPixels32()' in tool and
       'Array.Copy(pixels,y*tileWidth,surface,' in tool,
       "Actual captured PNGs must be assembled into mobile-scale review grids")
expect('controller.AddParameter("Moving",AnimatorControllerParameterType.Bool);' in rig and
       'startWalking=idle.AddTransition(advance)' in rig and
       'stopWalking=advance.AddTransition(idle)' in rig and
       'AnimatorConditionMode.If,0,"Moving"' in rig and
       'AnimatorConditionMode.IfNot,0,"Moving"' in rig and
       'private static readonly int Moving=Animator.StringToHash("Moving");' in motion and
       'public void BeginMovement() => SetMoving(true)' in motion and
       'public void EndMovement() => SetMoving(false)' in motion,
       "Actual authored locomotion must be wired to battle renderer Moving bool")
expect('new MaterialPropertyBlock()' in tool and
       'flat.SetFloat("_BumpScale",0)' in tool and
       'control-390x844-idle-no-normal-detail.png' in tool,
       "Provide an explicit actual-material control capture for normal map quality")
expect('RenderTexture.GetTemporary(width,height,24,' in tool and
       'RenderTexture.ReleaseTemporary(rt)' in tool and
       'RenderTexture.active=previous;' in tool and
       'UnityEngine.Object.DestroyImmediate(image)' in tool,
       "Capture must restore temporary render target and texture resources")
expect('No performance / GPU / FPS claims' in tool and
       'No gameplay catalog, saved progression or scene modified.' in tool,
       "Captures must not claim to certify mobile performance or change approval")
for forbidden in ("BattleModelCatalogSetup", "BattleUnitVisuals.asset",
                  "PlayerPrefs", "FrontiersPlaytest", "WorldFiveBattleOrganics",
                  "EditorApplication.isPlaying = true", "Application.persistentDataPath"):
    expect(forbidden not in tool, "Review cannot register live visuals or touch user progression: "+forbidden)
expect('58°' in guide and '390×844' in guide and '320×568' in guide and
       'ArtReview' in guide and 'iPhone 14' in guide and 'rejection' in guide.lower(),
       "Acceptance must include specific mobile-scale constraints and visual rejection")
expect(len(re.findall(r'\bCapture\(camera,',tool))>=4,
       "Multiple real capture scenarios, not a single staged glamour image")
print("PASS: real Rift Forager skinned prefab -> 58° orthographic portrait pose captures")
print("PASS: isolated preview scene, albedo/normal A/B, candidate-only file export")
print("PASS: no live battle catalog, saves or canon changes")
print("NOTE: Unity Editor render, art master comparison and iPhone profiling still required")
