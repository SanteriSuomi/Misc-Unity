using MessagePack;
using System;
using System.Xml.Serialization;

namespace Essentials.Saving
{
    /// <summary>
    /// Base save class for all save data objects.
    /// </summary>
    [Serializable, MessagePackObject,
                   XmlInclude(typeof(SAMI_PlayerHealth.PlayerHealthData)), XmlInclude(typeof(StaminaBar.PlayerStaminaData)),
                   XmlInclude(typeof(SAMI_PlayerStats.PlayerStatsData)), XmlInclude(typeof(PositionRotationSave.PositionRotationData)),
                   XmlInclude(typeof(StaminaBar.PlayerStaminaData)), XmlInclude(typeof(EnemyBase.EnemyData)),
                   XmlInclude(typeof(SAMI_EnemyDamage.EnemyDamageData)), XmlInclude(typeof(QuestContext.QuestSaveData)),
                   XmlInclude(typeof(QuestManager.QuestManagerData)), XmlInclude(typeof(SAMI_KeyChecker.KeyCheckerData)),
                   XmlInclude(typeof(SAMI_BossDoor.BossDoorData)), XmlInclude(typeof(Tutorial.TutorialData)),
                   XmlInclude(typeof(InventoryItemBaseManager.InventoryItemBaseData)), XmlInclude(typeof(PlayerAttack.PlayerAttackData)),
                   XmlInclude(typeof(Aleksi_Inventory.InventoryData)), XmlInclude(typeof(QuestDisplayController.QuestDisplayData)),
                   XmlInclude(typeof(ObjectSaveManager.ObjectSaveManagerData))]
    public class SaveData
    {
        [Key("ObjectName")]
        public string objName;

        public SaveData()
        {
            objName = GetType().Name;
        }
    }
}