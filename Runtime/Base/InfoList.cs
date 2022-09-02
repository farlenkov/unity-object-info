
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace UnityObjectInfo
{
    public abstract class InfoList
    {
        public virtual Type InfoType => typeof(ObjectInfo);

        [JsonIgnore]
        public virtual int Count => 0;

        public virtual void Init()
        {

        }

        internal virtual void AddToObjectInfoCache()
        {

        }
    
        internal virtual void AddArray(object[] objects)
        {

        }

        internal virtual void Add(ObjectInfo item)
        {

        }
    }

    public class InfoList<INFO> : InfoList where INFO : ObjectInfo
    {
        static System.Random random = new System.Random();

        [JsonIgnore]
        public override Type InfoType => typeof(INFO);

        // IN MEMORY CACHE

        [JsonIgnore]
        public List<INFO> Enabled { get; private set; }

        [JsonIgnore]
        public Dictionary<int, INFO> ByID { get; private set; }

        [JsonIgnore]
        public override int Count => all.Count;

        [JsonIgnore]
        public INFO this[int index] => all[index];

        // SERIALIZABLE data

        List<INFO> all = new List<INFO>();

        public List<INFO> All
        {
            get 
            { 
                return all; 
            }
            set
            {
                all = value;
                Init();
            }
        }

        // PROXY METHODS

        internal override void Add(ObjectInfo item)
        {
            all.Add((INFO)item);
        }

        public void AddRange(IEnumerable<INFO> collection)
        {
            all.AddRange(collection);
        }

        internal override void AddArray(object[] array)
        {
            for (var i = 0; i < array.Length; i++)
            {
                var obj = (INFO)array[i];
                all.Add(obj);
            }
        }

        public void Clear()
        {
            all?.Clear();
            Enabled?.Clear();
            ByID?.Clear();
        }

        public bool Contains(INFO item)
        {
            return all.Contains(item);
        }

        public INFO GetRandomEnabled()
        {
            var count = Enabled?.Count;

            if (count == 0)
            {
                return default;
            }
            else if (count == 1)
            {
                return Enabled[0];
            }
            else
            {
                // TODO: check offset is in range 0..list.Count
                var index = random.Next(0, Enabled.Count);
                return Enabled[index];
            }
        }

        // INIT METHODS

        public override void Init()
        {
            Enabled = new List<INFO>();
            ByID = new Dictionary<int, INFO>();

            for (var i = 0; i < all.Count; i++)
            {
                var item = all[i];
                ByID.Add(item.ID, item);

                if (item.Enabled)
                    Enabled.Add(item);
            }
        }

        internal override void AddToObjectInfoCache()
        {
            foreach (var asset in All)
            {
                ObjectInfo.All.Add(asset);
                ObjectInfo.ByID.Add(asset.ID, asset);
            }
        }
    }

    public abstract class InfoListNamed<T> : InfoList<T> where T : ObjectInfo
    {
        [NonSerialized] public Dictionary<string, T> ByName;
        
        public override void Init()
        {
            base.Init();

            var count = All.Count;
            ByName = new Dictionary<string, T>();

            for (var i = 0; i < count; i++)
            {
                var item = All[i];
                ByName.Add(item.name, item);
            }
        }
    }
}