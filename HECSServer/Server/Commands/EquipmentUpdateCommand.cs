using HECSFramework.Core;

namespace Commands
{
    [Documentation(Doc.Ability, Doc.Equipment, Doc.Character, "Оправляется в систему эквипа, чтобы она надела на персонажа новую абилку")]
    public struct EquipmentUpdateCommand : ICommand
    {
        public IEntity EquipmentOwner { get; set; }
    }
}