namespace Saving
{
    public interface ISaveable
    {
        /// <summary>
        /// The file name that is used in the saving and loading.
        /// </summary>
        /// <returns>string</returns>
        string GetFileName();

        /// <summary>
        /// SaveSystem uses load to load the object state from the file.
        /// </summary>
        void Load();

        /// <summary>
        /// SaveSystem uses save to save the object state to the file.
        /// </summary>
        void Save();
    }
}