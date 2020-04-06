using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using UnityEngine;

namespace Spellplague.Saving
{
    public enum FileType
    {
        Binary,
        JSON,
        XML
    }

    #pragma warning disable IDE0063 // Use simple 'using' statement - breaks code.
    /// <summary>
    /// A generic save system, able save any data to different formats and load them.
    /// </summary>
    public static class SaveSystem
    {
        private enum FileExtension
        {
            binary,
            json,
            xml
        }

        #region Autosaving
        private static readonly List<ISaveable> saveableObjects = new List<ISaveable>();

        /// <summary>
        /// Add an object that implements ISaveable to a list that can be iterated over.
        /// </summary>
        /// <param name="saveable"></param>
        public static void AddSaveable(ISaveable saveable) 
            => saveableObjects.Add(saveable);

        /// <summary>
        /// Call save on all the objects in the saveable list.
        /// </summary>
        public static void SaveSaveables()
        {
            foreach (ISaveable saveable in saveableObjects)
            {
                saveable.Save();
            }
        }

        /// <summary>
        /// Call load on all the objects in the saveable list.
        /// </summary>
        public static void LoadSaveables()
        {
            foreach (ISaveable saveable in saveableObjects)
            {
                saveable.Load();
            }
        }
        #endregion

        #region Save
        /// <summary>
        /// Save data to a file. Please note: use public fields for when serializing, properties are not serializable. 
        /// XML saving requires the object to have a public parameterless constructor.
        /// </summary>
        /// <param name="toFile"></param>
        /// <param name="dataToSave"></param>
        public static void Save<T>(FileType fileType, string toFile, T saveData)
        {
            switch (fileType)
            {
                case FileType.Binary:
                    SaveBinary(toFile, saveData);
                    break;
                case FileType.JSON:
                    SaveJSON(toFile, saveData);
                    break;
                case FileType.XML:
                    SaveXML(toFile, saveData);
                    break;
            }
        }

        private static void SaveBinary<T>(string toFile, T saveData)
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using (FileStream fileStream = new FileStream(GetFilePath(toFile, FileExtension.binary),
                FileMode.Create))
            {
                binaryFormatter.Serialize(fileStream, saveData);
            }
        }

        private static void SaveJSON<T>(string toFile, T saveData)
        {
            string jsonData = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(GetFilePath(toFile, FileExtension.json), jsonData);
        }

        private static void SaveXML<T>(string toFile, T saveData)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
            using (FileStream fileStream = new FileStream(GetFilePath(toFile, FileExtension.xml),
                FileMode.Create))
            {
                xmlSerializer.Serialize(fileStream, saveData);
            }
        }
        #endregion

        #region Load
        /// <summary>
        /// Load data from a file.
        /// </summary>
        /// <param name="fromFile"></param>
        /// <returns></returns>
        public static T Load<T>(FileType fileType, string fromFile)
        {
            switch (fileType)
            {
                case FileType.Binary:
                    return LoadBinary<T>(fromFile);
                case FileType.JSON:
                    return LoadJSON<T>(fromFile);
                case FileType.XML:
                    return LoadXML<T>(fromFile);
                default:
                    return default;
            }
        }

        private static T LoadBinary<T>(string fromFile)
        {
            string file = GetFilePath(fromFile, FileExtension.binary);
            if (File.Exists(file))
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                using (FileStream fileStream = new FileStream(file, FileMode.Open))
                {
                    return (T)binaryFormatter.Deserialize(fileStream);
                }
            }

            Log(file);
            return default;
        }

        private static T LoadJSON<T>(string fromFile)
        {
            string file = GetFilePath(fromFile, FileExtension.json);
            if (File.Exists(file))
            {
                string jsonData = File.ReadAllText(file);
                return JsonUtility.FromJson<T>(jsonData);
            }

            Log(file);
            return default;
        }

        private static T LoadXML<T>(string fromFile)
        {
            string file = GetFilePath(fromFile, FileExtension.xml);
            if (File.Exists(file))
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
                using (FileStream fileStream = new FileStream(file, FileMode.Open))
                {
                    return (T)xmlSerializer.Deserialize(fileStream);
                }
            }

            Log(file);
            return default;
        }
        #endregion

        #region Exists
        /// <summary>
        /// Check if a certain file exists.
        /// </summary>
        /// <param name="fileToCheck"></param>
        /// <returns></returns>
        public static bool Exists(FileType fileType, string fileToCheck)
        {
            switch (fileType)
            {
                case FileType.Binary:
                    return ExistsCheck(fileToCheck, FileExtension.binary);
                case FileType.JSON:
                    return ExistsCheck(fileToCheck, FileExtension.json);
                default:
                    return false;
            }
        }

        private static bool ExistsCheck(string fileToCheck, FileExtension extension) 
            => File.Exists(GetFilePath(fileToCheck, extension));
        #endregion

        #region Delete
        /// <summary>
        /// Delete a single file from the disk.
        /// </summary>
        /// <param name="fileToDelete"></param>
        public static void Delete(FileType fileType, string fileToDelete)
        {
            switch (fileType)
            {
                case FileType.Binary:
                    DeleteFile(fileToDelete, FileExtension.binary);
                    break;
                case FileType.JSON:
                    DeleteFile(fileToDelete, FileExtension.json);
                    break;
            }
        }

        private static void DeleteFile(string fileToDelete, FileExtension extension)
        {
            string file = GetFilePath(fileToDelete, extension);
            if (File.Exists(file))
            {
                File.Delete(file);
                return;
            }

            Log(file);
        }
        #endregion

        #region Clear
        /// <summary>
        /// Delete all saved files from the disk.
        /// </summary>
        public static void Clear()
        {
            foreach (string file in Directory.GetFiles(GetDirectory()))
            {
                if (file.Contains(ExtensionString(FileExtension.binary))
                    || file.Contains(ExtensionString(FileExtension.json))
                    || file.Contains(ExtensionString(FileExtension.xml)))
                {
                    File.Delete(file);
                }
            }
        }
        #endregion

        #region Get Files
        /// <summary>
        /// Return all currently saved files.
        /// </summary>
        /// <returns></returns>
        public static string[] GetFiles()
        {
            List<string> files = new List<string>();
            foreach (string file in Directory.GetFiles(GetDirectory()))
            {
                if (file.Contains(ExtensionString(FileExtension.binary))
                    || file.Contains(ExtensionString(FileExtension.json))
                    || file.Contains(ExtensionString(FileExtension.xml)))
                {
                    files.Add(file);
                }
            }

            return files.ToArray();
        }
        #endregion

        #region Amount
        /// <summary>
        /// Get the amount of files currently saved.
        /// </summary>
        /// <returns></returns>
        public static int Amount()
        {
            int amount = 0;
            foreach (string file in Directory.GetFiles(GetDirectory()))
            {
                if (file.Contains(ExtensionString(FileExtension.binary))
                    || file.Contains(ExtensionString(FileExtension.json))
                    || file.Contains(ExtensionString(FileExtension.xml)))
                {
                    amount++;
                }
            }

            return amount;
        }
        #endregion

        #region Helpers
        private static string GetFilePath(string file, FileExtension extension)
            => $"{Application.persistentDataPath}/{file}.{extension.ToString()}";

        private static string GetDirectory() 
            => Application.persistentDataPath;

        private static string ExtensionString(FileExtension extension) 
            => extension.ToString();

        private static void Log(string file)
        {
            #if UNITY_EDITOR
            Debug.LogWarning($"File {file} does not exist!");
            #endif
        }
        #endregion
    }
}