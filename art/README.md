# Robin draft

This is a Blender art review draft. It is not imported into the Unity scene.

- `Robin_Draft.blend`: editable Blender source with separate wing pivots and flap keyframes.
- `Robin_Draft.fbx`: test export for a later Unity import.
- `previews/`: rendered rear, side, front, and three-quarter views.
- `build_robin.py`: regenerates the model, renders, and FBX from Blender 5.2.

From the repository root on Windows:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' -b --factory-startup --python art\build_robin.py
```

Review the model's silhouette and colors before using it as the game's bird.
