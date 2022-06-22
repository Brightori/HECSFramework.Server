using HECSFramework.Core;

namespace Commands
{
    [Documentation(Doc.Abilities, Doc.Equipment, Doc.Character, "Оправляется в систему эквипа, чтобы она надела на персонажа временную абилку")]
    public struct EquipmentUpdateOverrideCommand : ICommand
    {
        public IEntity EquipmentOwner { get; set; }
        public bool IsAdded { get; set; }
    }
}