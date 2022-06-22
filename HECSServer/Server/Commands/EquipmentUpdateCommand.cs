using HECSFramework.Core;

namespace Commands
{
    [Documentation(Doc.Abilities, Doc.Equipment, Doc.Character, "Оправляется в систему эквипа, чтобы она надела на персонажа новую абилку")]
    public struct EquipmentUpdateCommand : ICommand
    {
        public IEntity EquipmentOwner { get; set; }
    }
}