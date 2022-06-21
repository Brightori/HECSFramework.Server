using System.IO;
using HECSFramework.Core;
using HECSFramework.Network;

namespace Components
{
    [Documentation(Doc.Location, "Компонент хранит информацию из какого файла были загржуены данные для локации")]
    public class LocationFileInfoComponent : BaseComponent,INotReplicable
    {
        public FileInfo LocationFileInfo;
    }
}