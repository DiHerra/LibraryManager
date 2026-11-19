using System;
using System.IO;
using LibraryManager.Application.Saves;
using UnityEngine;

namespace LibraryManager.Infrastructure.Persistence
{
    /// <summary>
    /// Guarda la partida en JSON. Escribe primero en un archivo temporal y lo sustituye,
    /// conservando la versión anterior como copia (.bak), para no perder la partida si
    /// el juego se cierra a mitad de guardado.
    /// </summary>
    public sealed class JsonSaveGameRepository : ISaveGameRepository
    {
        public const string DefaultFileName = "library-save.json";

        public JsonSaveGameRepository(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("La ruta del guardado no puede estar vacía.", nameof(filePath));
            }

            FilePath = filePath;
        }

        public string FilePath { get; }

        public string BackupPath => FilePath + ".bak";

        public static JsonSaveGameRepository CreateDefault()
        {
            return new JsonSaveGameRepository(Path.Combine(UnityEngine.Application.persistentDataPath, DefaultFileName));
        }

        public bool Exists()
        {
            return File.Exists(FilePath);
        }

        public void Save(SaveGameData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            string directory = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonUtility.ToJson(data, true);
            string temporaryPath = FilePath + ".tmp";
            File.WriteAllText(temporaryPath, json);

            if (File.Exists(FilePath))
            {
                File.Replace(temporaryPath, FilePath, BackupPath);
            }
            else
            {
                File.Move(temporaryPath, FilePath);
            }
        }

        public SaveGameData Load()
        {
            if (!Exists())
            {
                throw new SaveGameException("No hay ninguna partida guardada.");
            }

            string json;
            try
            {
                json = File.ReadAllText(FilePath);
            }
            catch (IOException exception)
            {
                throw new SaveGameException("No se pudo leer la partida.", exception);
            }

            SaveGameData data;
            try
            {
                data = JsonUtility.FromJson<SaveGameData>(json);
            }
            catch (ArgumentException exception)
            {
                throw new SaveGameException("El archivo de partida no es JSON válido.", exception);
            }

            if (data == null || data.schemaVersion == 0)
            {
                throw new SaveGameException("El archivo no contiene una partida de Library Manager.");
            }

            return data;
        }
    }
}
