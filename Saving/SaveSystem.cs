using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using UnityEngine;
using System.IO.Compression;
using CompressionLevel = System.IO.Compression.CompressionLevel;
using System.Threading.Tasks;

namespace Saving
{
    public enum FileType
    {
        Binary,
        JSON,
        XML
    }

    #pragma warning disable IDE0063, IDE0066 // Use simple 'using' statement - breaks code, Switch expression not supported in C# 7.
    /// <summary>
    /// A generic save system, able save any data to different formats and load them.
    /// </summary>
    public static class SaveSystem
    {
        private enum FileExtension
        {
            bin,
            json,
            xml
        }

        #region Autosaving
        private static readonly List<ISaveable> saveablesList = new List<ISaveable>();

        /// <summary>
        /// Add an object that implements ISaveable to a list that can be iterated over.
        /// </summary>
        /// <param name="saveable"></param>
        public static void AddSaveable(ISaveable saveable)
            => saveablesList.Add(saveable);

        /// <summary>
        /// Call save on all the objects in the saveable list.
        /// </summary>
        public static void SaveSaveables()
        {
            foreach (ISaveable saveable in saveablesList)
            {
                saveable.Save();
            }
        }

        /// <summary>
        /// Call load on all the objects in the saveable list.
        /// </summary>
        public static void LoadSaveables()
        {
            foreach (ISaveable saveable in saveablesList)
            {
                saveable.Load();
            }
        }
        #endregion

        #region Compression
        /// <summary>
        /// Compress the save folder in to a .zip file.
        /// </summary>
        public static void Compress(CompressionLevel compressionLevel)
        {
            if (Directory.Exists(GetSaveDirectoryPath()))
            {
                ZipFile.CreateFromDirectory(GetSaveDirectoryPath(), GetZipFilePath(), compressionLevel, false);
                ClearSaves(); // Remove remaining files because they are now saves in the .zip file.
                return;
            }

            LogWarning(GetSaveDirectoryPath());
        }

        /// <summary>
        /// Decompress the .zip file in to a new save folder.
        /// </summary>
        public static void Decompress()
        {
            if (File.Exists(GetZipFilePath()))
            {
                ZipFile.ExtractToDirectory(GetZipFilePath(), GetSaveDirectoryPath());
                File.Delete(GetZipFilePath());
                return;
            }

            LogWarning(GetZipFilePath());
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
            CheckDirectory();

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

                default:
                    break;
            }
        }

        private static void CheckDirectory()
        {
            if (!Directory.Exists(GetSaveDirectoryPath()))
            {
                Directory.CreateDirectory(GetSaveDirectoryPath());
            }
        }

        private static void SaveBinary<T>(string toFile, T saveData)
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using (FileStream fileStream = new FileStream(GetFilePath(toFile, FileExtension.bin),
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
            string file = GetFilePath(fromFile, FileExtension.bin);
            if (File.Exists(file))
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                using (FileStream fileStream = new FileStream(file, FileMode.Open))
                {
                    return (T)binaryFormatter.Deserialize(fileStream);
                }
            }

            LogWarning(file);
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

            LogWarning(file);
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

            LogWarning(file);
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
                    return ExistsCheck(fileToCheck, FileExtension.bin);

                case FileType.JSON:
                    return ExistsCheck(fileToCheck, FileExtension.json);

                case FileType.XML:
                    return ExistsCheck(fileToCheck, FileExtension.xml);

                default:
                    return default;
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
                    DeleteFile(fileToDelete, FileExtension.bin);
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

            LogWarning(file);
        }
        #endregion

        #region Clear
        /// <summary>
        /// Delete all saved files from the disk.
        /// </summary>
        public static void ClearSaves()
        {
            string[] files = Directory.GetFiles(GetSaveDirectoryPath());
            foreach (string file in files)
            {
                if (file.Contains(GetExtensionString(FileExtension.bin))
                    || file.Contains(GetExtensionString(FileExtension.json))
                    || file.Contains(GetExtensionString(FileExtension.xml)))
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
            List<string> filesToReturn = new List<string>();
            string[] files = Directory.GetFiles(GetSaveDirectoryPath());
            foreach (string file in files)
            {
                if (file.Contains(GetExtensionString(FileExtension.bin))
                    || file.Contains(GetExtensionString(FileExtension.json))
                    || file.Contains(GetExtensionString(FileExtension.xml)))
                {
                    filesToReturn.Add(file);
                }
            }

            return filesToReturn.ToArray();
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
            string[] files = Directory.GetFiles(GetSaveDirectoryPath());
            foreach (string file in files)
            {
                if (file.Contains(GetExtensionString(FileExtension.bin))
                    || file.Contains(GetExtensionString(FileExtension.json))
                    || file.Contains(GetExtensionString(FileExtension.xml)))
                {
                    amount++;
                }
            }

            return amount;
        }
        #endregion

        #region Helpers
        private static string GetFilePath(string file, FileExtension extension)
            => $"{Application.persistentDataPath}/saves/{file}.{extension}";

        private static string GetSaveDirectoryPath()
            => $"{Application.persistentDataPath}/saves/";

        private static string GetZipDirectoryPath()
            => Application.persistentDataPath;

        private static string GetZipFilePath()
            => $"{GetZipDirectoryPath()}/saves.zip";

        private static string GetExtensionString(FileExtension extension)
            => extension.ToString();

        private static void LogWarning(string path)
        {
            #if UNITY_EDITOR
            Debug.LogWarning($"The file or directory {path} does not exist! Ignore if first time saving or loading.");
            #endif
        }
        #endregion
    }
}