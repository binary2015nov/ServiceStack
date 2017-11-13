#if NETSTANDARD2_0

using System;
using System.Collections;
using System.Collections.Specialized;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace ServiceStack.NetCore
{
    public class NetCoreHeadersCollection : NameValueCollection
    {
        readonly IHeaderDictionary original;
        public NetCoreHeadersCollection(IHeaderDictionary original) => this.original = original;

        public override int Count => original.Count;
        public bool IsSynchronized => false;
        public object SyncRoot => original;    

        public object Original => original;

        public override string[] AllKeys => original.Keys.ToArray();
        public override string Get(int index) => Get(GetKey(index));
        public override string Get(string name) => name != null ? (string)original[name] : null;
        public override string GetKey(int index) => AllKeys[index];
        public override string[] GetValues(string name) => original[name];
        public new bool HasKeys() => original.Count > 0;

        public override void Add(string name, string value) => throw new NotSupportedException();
        public override void Clear() => throw new NotSupportedException();
        public override void Remove(string name) => throw new NotSupportedException();
        public override void Set(string key, string value) => throw new NotSupportedException();

        public override IEnumerator GetEnumerator()
        {
            foreach (var item in original)
            {
                yield return new KeyValuePair<string, string>(item.Key, item.Value.ToString());
            }
        }
    }
}

#endif
