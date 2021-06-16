using System.Collections.Concurrent;
using MessagePack;
using System.Collections.Generic;

namespace HECSServer.HECSNetwork
{
    [MessagePackObject]
    public class SyncData
    {
        [Key(0)]
        public ConcurrentDictionary<int, object> data = new ConcurrentDictionary<int, object>();

        [Key(1)]
        public long DateTime;

        public SyncData()
        {
        }

        public SyncData(int countOfFields)
        {
            Init(countOfFields);
        }

        public void Init(int numbers)
        {
            for (int i = 0; i < numbers; i++)
                data.TryAdd(i, null);
        }
    }
}
