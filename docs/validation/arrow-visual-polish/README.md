# Procedural arrow visual polish

Validated on 2026-11-07 with the running Unity 6000.2.14f1 Editor using the existing Unity CLI / Pipeline C# workflow. All geometry is original procedural C#; no imported models or textures.

## Changes

- Shared tapered warm wood/bamboo shaft with two shallow nodes.
- Ridged, beveled steel point and narrow socket in one new ArrowSteel material.
- Three thin solid deep red feathers with ivory quills and brown root and socket bindings. The feather color was changed from cream to the existing Red palette tone at the user’s request.
- Small split ivory nock, with an open string slot at local Z = 0.
- One 656-triangle mesh (1,968 vertices) reused by the nocked preview and the existing flying arrow pool. One renderer / five material slots per arrow; no colliders or per-frame visual work. The existing legacy indoor arrow uses the same mesh with a slightly slimmer cross-section.

Only the two arrow construction sections in JemparinganGame changed. Projectile code, input, launch origin, aim, hit testing, scoring and trail settings were left intact. The flying visual is offset inside its existing parent so the metal point ends at the original swept-tip local Z = 0.36 m. Its parent transform is not adjusted. Both outdoor visuals have the same 0.94 m length and proportions.

## Completed checks

The Editor was already playing when work started. Its Play Mode session, camera, scene and settings were left running. To check the changed source without reloading that session, all 17 current runtime C# source files were compiled together in memory under an isolated namespace using `unity command script run`. The temporary harness lived in `/tmp`; it was not added to the project.

Compilation passed with no errors and two existing unassigned Inspector-field warnings (`preferQuestHandsInEditor` and `outdoorTargetAnchor`). The harness built nocked, outdoor flying and indoor flying visual instances in a Unity PreviewRenderUtility scene, then checked:

- Mesh and material instances are shared.
- Parent position and rotation are preserved.
- The visible flying point ends at Z = 0.360 m.
- Mesh length is 0.940 m, with 656 nondegenerate triangles.
- Each arrow has one renderer and five material slots, using red feathers and steel.
- Visual construction adds no collider, rigidbody or projectile component.

The following images were rendered in that isolated Unity preview and visually inspected. They are **asset previews, not gameplay or headset captures**. The top and bottom arrows in the comparison are the nocked and flying mesh, arranged together for comparison.

- [Nocked / flying comparison](shared-arrows.png)
- [Fletching and split nock](fletching-and-nock.png)
- [Steel point close-up](steel-point.png)

## Remaining gameplay inspection

Start a fresh Play Mode session after script import; existing procedurally built objects do not rebuild themselves during the already-running session. In Traditional and Modern, inspect idle, half draw, full draw, release, arrow camera and embedded arrows. Confirm no clipping at the string, red feathers from both sides, and a steel point aligned with impacts. Fire repeated hits and misses, then restart to check visibility and reuse. The existing Target Hitbox Checks and Gameplay QA menus remain available.

Full scene Play Mode verification of the new art, APK builds, hands-only Quest testing and device performance measurements were not performed in this pass.
