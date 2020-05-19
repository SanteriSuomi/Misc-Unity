using Steamworks;
using System;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Runtime;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace Essentials.Saving
{
    public class SaveManager : Singleton<SaveManager>
    {
        private CallResult<RemoteStorageFileWriteAsyncComplete_t> OnFileWriteCallback;
        private CallResult<RemoteStorageFileReadAsyncComplete_t> OnFileReadCallback;

        [Header("Autosave")]
        [SerializeField]
        private float autosaveInterval = 60;
        private float timer;

        [Header("Save/Load Popup Text")]
        [SerializeField]
        private float savingTextMinShowTime = 1;
        [SerializeField]
        private string saveText = "Saving...";
        //[SerializeField]
        //private string loadText = "Loading...";
        private TextMeshProUGUI savingText;

        [Header("Save Names/Extensions")]
        [SerializeField]
        private string localSaveName = "save";
        [SerializeField]
        private FileExtension localSaveExtension = FileExtension.json;
        [SerializeField]
        private string cloudSaveName = "save.dat";
        [SerializeField]
        private string cloudScreenshotSaveName = "savescreenshot.dat";

        private WaitForSeconds savingTextWFS;
        private WaitForEndOfFrame screenshotWFEOF;
        private bool isShowingPopup;

        protected override void Awake()
        {
            base.Awake();
            savingTextWFS = new WaitForSeconds(savingTextMinShowTime);
            savingText = GameObject.Find("SavingText").GetComponent<TextMeshProUGUI>();
            savingText.gameObject.SetActive(false);
            screenshotWFEOF = new WaitForEndOfFrame();
        }

        private void Start()
        {
            if (SteamManager.Instance.Initialized)
            {
                OnFileWriteCallback = CallResult<RemoteStorageFileWriteAsyncComplete_t>.Create(OnFileWriteComplete);
                OnFileReadCallback = CallResult<RemoteStorageFileReadAsyncComplete_t>.Create(OnFileReadComplete);
            }

            if (LoadNotifier.CanLoad)
            {
                Load();
            }

            LoadNotifier.CanLoad = false;
        }

        private void Update()
        {
            if (!SaveSystem.IsSaving 
                && (Keyboard.current?.f10Key.wasPressedThisFrame == true))
            {
                Save();
            }
            //else if (!SaveSystem.IsLoading && Input.GetKeyDown(KeyCode.F11))
            //{
            //    SaveSystemTextPopup(loadText);
            //    Load();
            //}

            timer += Time.deltaTime;
            if (timer >= autosaveInterval)
            {
                timer = 0;
                Save();
            }
        }

        public void Save()
        {
            ActivateTextPopup(saveText);
            StartCoroutine(UploadScreenShot());
            SaveSystem.SaveAll(saveName: localSaveName, FileType.JSON, CompressionLevel.NoCompression);
            UploadSave();
            Cleanup();
        }

        private void ActivateTextPopup(string text)
        {
            if (!isShowingPopup)
            {
                isShowingPopup = true;
                StartCoroutine(ShowPopup(text));
            }
        }

        private IEnumerator ShowPopup(string text)
        {
            savingText.text = text;
            savingText.gameObject.SetActive(true);
            while (SaveSystem.IsSaving)
            {
                yield return null;
            }

            yield return savingTextWFS;

            savingText.gameObject.SetActive(false);
            isShowingPopup = false;
        }

        private IEnumerator UploadScreenShot()
        {
            if (!SteamManager.Instance.Initialized)
            {
                yield break;
            }

            yield return screenshotWFEOF;
            Texture2D screenshot = new Texture2D(Screen.width, Screen.height);
            screenshot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            screenshot.Apply();
            byte[] screenshotAsBytes = screenshot.EncodeToPNG();
            Destroy(screenshot);

            WriteToCloud(cloudScreenshotSaveName, screenshotAsBytes);
        }

        private void UploadSave()
        {
            if (SteamManager.Instance.Initialized)
            {
                string jsonData = File.ReadAllText(SaveSystem.GetFilePath(localSaveName, localSaveExtension));
                byte[] byteData = Encoding.UTF8.GetBytes(jsonData);
                WriteToCloud(localSaveName, byteData);
            }
        }

        private void WriteToCloud(string toFile, byte[] byteData)
        {
            SteamAPICall_t fileWriteHandle = SteamRemoteStorage.FileWriteAsync(toFile, byteData, (uint)byteData.Length);
            OnFileWriteCallback.Set(fileWriteHandle);
        }

        private void OnFileWriteComplete(RemoteStorageFileWriteAsyncComplete_t resultData, bool result)
        {
            // Not implemented
        }

        public void Load()
        {
            if (!DownloadCloudSave()) // If cloud save exists, load after download the file from the cloud.
            {
                SaveSystem.LoadAll(saveName: localSaveName, FileType.JSON, decompress: false);
            }
            
            Cleanup();
        }

        private bool DownloadCloudSave()
        {
            if (SteamManager.Instance.Initialized
                && SteamRemoteStorage.FileExists(cloudSaveName))
            {
                SteamAPICall_t fileReadHandle = SteamRemoteStorage.FileReadAsync(cloudSaveName, 0, GetSaveSize());
                OnFileReadCallback.Set(fileReadHandle);
                return true;
            }

            return false;
        }

        private void OnFileReadComplete(RemoteStorageFileReadAsyncComplete_t resultData, bool result)
        {
            byte[] byteData = new byte[GetSaveSize()];
            SteamRemoteStorage.FileReadAsyncComplete(resultData.m_hFileReadAsync, byteData, resultData.m_cubRead);
            string jsonData = Encoding.UTF8.GetString(byteData);
            File.WriteAllText(SaveSystem.GetFilePath(localSaveName, localSaveExtension), jsonData);

            SaveSystem.LoadAll(saveName: localSaveName, FileType.JSON, decompress: false);
        }

        private uint GetSaveSize()
        {
            return (uint)SteamRemoteStorage.GetFileSize(cloudSaveName);
        }

        private static void Cleanup()
        {
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, false, true);
            AsyncOperationManager.CreateOperation(Resources.UnloadUnusedAssets());
        }
    }
}