using FullSerializer;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace Essentials.Saving
{
    public enum FileType
    {
        Binary,
        JSON,
        XML,
        MessagePack
    }

    public enum FileExtension
    {
        bin,
        json,
        xml,
        dat
    }

    [Serializable, MessagePackObject]
    public class AllSaveData<T>
    {
        [Key("Saves")]
        public T[] saves;
    }

    /// <summary>
    /// A generic save system, able save any data to different formats and load them.
    /// </summary>
    public static class SaveSystem
    {
        private static readonly BinaryFormatter binarySerializer = new BinaryFormatter();
        private static readonly fsSerializer jsonSerializer = new fsSerializer();

        /// <summary>
        /// Is save currently in progress?
        /// </summary>
        public static bool IsSaving { get; private set; }

        /// <summary>
        /// Is load currently in progress?
        /// </summary>
        public static bool IsLoading { get; private set; }

        static SaveSystem()
        {
            SurrogateSelector surrogateSelector = new SurrogateSelector();
            surrogateSelector.AddSurrogate(typeof(Vector2),
                new StreamingContext(StreamingContextStates.All),
                new Vector2Surrogate());
            surrogateSelector.AddSurrogate(typeof(Vector3),
                new StreamingContext(StreamingContextStates.All),
                new Vector3Surrogate());
            surrogateSelector.AddSurrogate(typeof(Quaternion),
                new StreamingContext(StreamingContextStates.All),
                new QuaternionSurrogate());
            binarySerializer.SurrogateSelector = surrogateSelector;
        }

        #region Autosaving
        private static readonly List<ISaveable> saveablesList = new List<ISaveable>();

        /// <summary>
        /// Add an object to the list of objects that will be saved.
        /// </summary>
        /// <param name="saveable">Object that implements the ISaveable interface.</param>
        public static void Register(ISaveable saveable)
            => saveablesList.Add(saveable);

        /// <summary>
        /// Removes an object from the list of objects to be saved.
        /// </summary>
        /// <param name="saveable"></param>
        public static bool Unregister(ISaveable saveable)
            => saveablesList.Remove(saveable);

        /// <summary>
        /// Save all objects that are registered.
        /// </summary>
        /// <param name="saveName">Name of the save file.</param>
        /// <param name="fileType">Type of the save.</param>
        /// <param name="compressionLevel">Compression applied after saving.</param>
        public static void SaveAll(string saveName, FileType fileType, CompressionLevel compressionLevel)
        {
            IsSaving = true;

            try
            {
                SaveData[] allSaves = new SaveData[saveablesList.Count];
                for (int i = 0; i < saveablesList.Count; i++)
                {
                    if (i <= allSaves.GetUpperBound(0))
                    {
                        allSaves[i] = saveablesList[i].GetSave();
                    }
                    else
                    {
                        Debug.LogWarning("There is more objects marked as saveables than there are objects saved on the disk.");
                        break;
                    }
                }

                Save(fileType, saveName, new AllSaveData<SaveData> { saves = allSaves });
                if (compressionLevel != CompressionLevel.NoCompression)
                {
                    Compress(compressionLevel);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }

            IsSaving = false;
        }

        /// <summary>
        /// Load all objects that are registered.
        /// </summary>
        /// <param name="saveName">Name of the save file.</param>
        /// <param name="fileType">Type of the save.</param>
        /// <param name="decompress">Must be selected if the file was saved with compression.</param>
        public static void LoadAll(string saveName, FileType fileType, bool decompress)
        {
            IsLoading = true;

            try
            {
                if (decompress)
                {
                    Decompress();
                }

                AllSaveData<SaveData> allSaveData = Load<AllSaveData<SaveData>>(fileType, saveName);
                for (int i = 0; i < saveablesList.Count; i++)
                {
                    if (i <= allSaveData.saves.GetUpperBound(0)
                        && saveablesList[i] != null)
                    {
                        saveablesList[i].Load(allSaveData.saves[i]);
                    }
                    else
                    {
                        Debug.LogWarning("There is more objects marked as saveables than there are objects saved on the disk.");
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }

            IsLoading = false;
        }
        #endregion

        #region Compression
        /// <summary>
        /// Compress the save folder in to a .zip file.
        /// </summary>
        private static void Compress(CompressionLevel compressionLevel)
        {
            if (Directory.Exists(GetSaveDirectoryPath()))
            {
                if (File.Exists(GetZipFilePath()))
                {
                    File.Delete(GetZipFilePath());
                }

                ZipFile.CreateFromDirectory(GetSaveDirectoryPath(), GetZipFilePath(), compressionLevel, false);
                ClearSaves(); // Remove remaining files because they are now saves in the .zip file.
                return;
            }

            LogWarning(GetSaveDirectoryPath());
        }

        /// <summary>
        /// Decompress the .zip file in to a new save folder.
        /// </summary>
        private static void Decompress()
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
        /// XML saving requires all objects to have a public constructor, and include that class in the XmlInclude attribute in the base class (SaveData)
        /// </summary>
        /// <param name="toFile"></param>
        public static void Save<T>(FileType fileType, string toFile, T saveData)
        {
            ValidateDirectory();
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

                case FileType.MessagePack:
                    SaveMsgPack(toFile, saveData);
                    break;
            }
        }

        private static void SaveBinary<T>(string toFile, T saveData)
        {
            using (FileStream fileStream = new FileStream(GetFilePath(toFile, FileExtension.bin),
                FileMode.Create))
            {
                binarySerializer.Serialize(fileStream, saveData);
            }
        }

        private static void SaveJSON<T>(string toFile, T saveData)
        {
            jsonSerializer.TrySerialize(saveData, out fsData data);
            string jsonData = fsJsonPrinter.PrettyJson(data);
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

        private static void SaveMsgPack<T>(string toFile, T saveData)
        {
            var msgPackOptions = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray)
                .WithResolver(MessagePack.Resolvers.CompositeResolver.Create(MessagePack.Unity.UnityResolver.Instance,
                                                                             MessagePack.Unity.Extension.UnityBlitResolver.Instance,
                                                                             MessagePack.Resolvers.StandardResolver.Instance));
            byte[] msgPackData = MessagePackSerializer.Serialize(saveData, msgPackOptions);
            //var asd = MessagePackSerializer.Deserialize<AllSaveData<SaveData>>(msgPackData, msgPackOptions);
            //Debug.Log(asd.saves[0].objName);
            File.WriteAllBytes(GetFilePath(toFile, FileExtension.dat), msgPackData);
        }
        #endregion

        #region Load
        /// <summary>
        /// Load data from a file.
        /// </summary>
        /// <param name="fromFile"></param>
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

                case FileType.MessagePack:
                    return LoadMsgPack<T>(fromFile);

                default:
                    return default;
            }
        }

        private static T LoadBinary<T>(string fromFile)
        {
            string file = GetFilePath(fromFile, FileExtension.bin);
            if (File.Exists(file))
            {
                using (FileStream fileStream = new FileStream(file, FileMode.Open))
                {
                    return (T)binarySerializer.Deserialize(fileStream);
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
                fsJsonParser.Parse(jsonData, out fsData data);
                T instance = default;
                jsonSerializer.TryDeserialize(data, ref instance);
                return instance;
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

        private static T LoadMsgPack<T>(string fromFile)
        {
            string file = GetFilePath(fromFile, FileExtension.dat);
            if (File.Exists(file))
            {
                var msgPackOptions = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray)
                    .WithResolver(MessagePack.Resolvers.CompositeResolver.Create(MessagePack.Unity.UnityResolver.Instance,
                                                                                 MessagePack.Unity.Extension.UnityBlitResolver.Instance,
                                                                                 MessagePack.Resolvers.StandardResolver.Instance));
                byte[] msgPackData = File.ReadAllBytes(file);
                return MessagePackSerializer.Deserialize<T>(msgPackData, msgPackOptions);
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

                case FileType.MessagePack:
                    return ExistsCheck(fileToCheck, FileExtension.dat);

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

                case FileType.XML:
                    DeleteFile(fileToDelete, FileExtension.xml);
                    break;

                case FileType.MessagePack:
                    DeleteFile(fileToDelete, FileExtension.dat);
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
            foreach (string file in Directory.GetFiles(GetSaveDirectoryPath()))
            {
                if (file.Contains(GetExtensionString(FileExtension.bin))
                    || file.Contains(GetExtensionString(FileExtension.json))
                    || file.Contains(GetExtensionString(FileExtension.xml))
                    || file.Contains(GetExtensionString(FileExtension.dat)))
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
        public static string[] GetFiles()
        {
            List<string> filesToReturn = new List<string>();
            foreach (string file in Directory.GetFiles(GetSaveDirectoryPath()))
            {
                if (file.Contains(GetExtensionString(FileExtension.bin))
                    || file.Contains(GetExtensionString(FileExtension.json))
                    || file.Contains(GetExtensionString(FileExtension.xml))
                    || file.Contains(GetExtensionString(FileExtension.dat)))
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
        public static int Amount()
        {
            int amount = 0;
            foreach (string file in Directory.GetFiles(GetSaveDirectoryPath()))
            {
                if (file.Contains(GetExtensionString(FileExtension.bin))
                    || file.Contains(GetExtensionString(FileExtension.json))
                    || file.Contains(GetExtensionString(FileExtension.xml))
                    || file.Contains(GetExtensionString(FileExtension.dat)))
                {
                    amount++;
                }
            }

            return amount;
        }
        #endregion

        #region Other Helpers
        /// <summary>
        /// Check if the save directory exists, if it doesn't, create one.
        /// </summary>
        private static void ValidateDirectory()
        {
            if (!Directory.Exists(GetSaveDirectoryPath()))
            {
                Directory.CreateDirectory(GetSaveDirectoryPath());
            }
        }

        public static string GetFilePath(string file, FileExtension extension)
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
            Debug.LogWarning($"Warning occurred while saving or loading, the file or directory {path} might not exist. Ignore if first time saving or loading.");
            #endif
        }
        #endregion
    }
}