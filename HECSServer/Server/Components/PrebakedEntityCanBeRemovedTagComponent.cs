using HECSFramework.Core;

namespace Components
{
    [Documentation("Tag", "World", "ServerLogic", "это компонент которым мы помечаем запеченные ентити, которые могут быть удалены с сервера, и их потом также нужно зачистить с клиентов, в первую очередь при новом подключении")]
    public class PrebakedEntityCanBeRemovedTagComponent : BaseComponent
    {
    }
}
