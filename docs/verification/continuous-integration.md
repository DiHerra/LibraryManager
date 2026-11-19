# Integración continua

El workflow `.github/workflows/unity-editmode.yml` se ejecuta en cada push a `main` y a mano. Usa un runner propio con las etiquetas `self-hosted`, `Windows`, `X64` y `unity`, el mismo que Volley Manager.

Llama a `scripts/Verify.ps1 -SkipDotNetBuild`, que:

1. Lee la versión de Unity en `LibraryManager/ProjectSettings/ProjectVersion.txt`.
2. Comprueba que no haya otro editor abierto sobre el proyecto.
3. Ejecuta la suite EditMode en batch y guarda resultados y log en `TestResults/Verification/<fecha>`.
4. Falla si alguna prueba falla.

Para usar otra instalación de Unity, define `UNITY_EDITOR_PATH` en el runner o pasa `-UnityPath`.

En local: `./scripts/Verify.ps1` con Unity cerrado.
