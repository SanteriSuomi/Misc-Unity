namespace Spellplague.Saving
{
    public interface ISaveable
    {
        string SaveFileName { get; set; }
        void Awake();
        void Load();
        void Save();
    }
}