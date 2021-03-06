﻿using Jusfr.Caching;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jusfr.Caching.Tests {
    [TestClass]
    public class RegionTest {
        [TestMethod]
        public void Duplicate() {
            var key = Guid.NewGuid().ToString();
            var val1 = Guid.NewGuid();

            //region a 创建 key->val1
            HttpRuntimeCacheProvider cacheProvider1 = new HttpRuntimeCacheProvider("a");
            var result = cacheProvider1.GetOrCreate<Guid>(key, k => val1);
            Assert.AreEqual(result, val1);
            Assert.IsTrue(((HttpRuntimeCacheProvider)cacheProvider1).Count() == 1);

            //reigon b 确认不存在键 key
            HttpRuntimeCacheProvider cacheProvider2 = new HttpRuntimeCacheProvider("b");
            Assert.IsTrue(cacheProvider2.Count() == 0);
            Guid val2;
            var exist = cacheProvider2.TryGet<Guid>(key, out val2);
            Assert.IsFalse(exist);
            Assert.AreEqual(val2, Guid.Empty);
        }

        [TestMethod]
        public void Duplicate2() {
            var key = Guid.NewGuid().ToString();
            //region a 创建 key->val1
            IHttpRuntimeCacheProvider cacheProvider1 = CacheProviderFactory.GetHttpRuntimeCache("c");
            cacheProvider1.GetOrCreate<Guid>(key, k => Guid.NewGuid());
            Assert.IsTrue(((HttpRuntimeCacheProvider)cacheProvider1).Count() == 1);

            //reigon b 创建 key-val2
            IHttpRuntimeCacheProvider cacheProvider2 = CacheProviderFactory.GetHttpRuntimeCache("d");
            cacheProvider2.GetOrCreate<Guid>(key, k => Guid.NewGuid());
            Assert.IsTrue(((HttpRuntimeCacheProvider)cacheProvider2).Count() == 1);


            //确认reigon a 键 key 与 region b 键 key 对应值不同
            Guid val1;
            var exist1 = cacheProvider1.TryGet<Guid>(key, out val1);
            Assert.IsTrue(exist1);

            Guid val2;
            var exist2 = cacheProvider2.TryGet<Guid>(key, out val2);
            Assert.IsTrue(exist2);

            Assert.AreNotEqual(val1, val2);
        }

        [TestMethod]
        public void ExpireAll() {
            var key = Guid.NewGuid().ToString();
            //region a 创建 key->val1
            IHttpRuntimeCacheProvider cacheProvider1 = CacheProviderFactory.GetHttpRuntimeCache("e");
            cacheProvider1.GetOrCreate<Guid>(key, k => Guid.NewGuid());

            //reigon b 创建 key-val2
            IHttpRuntimeCacheProvider cacheProvider2 = CacheProviderFactory.GetHttpRuntimeCache("f");
            cacheProvider2.GetOrCreate<Guid>(key, k => Guid.NewGuid());

            //region a 过期全部, 确认 region b 未被过期
            ((HttpRuntimeCacheProvider)cacheProvider1).ExpireAll();
            Guid val1;
            var exist = cacheProvider1.TryGet<Guid>(key, out val1);
            Assert.IsFalse(exist);
            Assert.AreEqual(val1, Guid.Empty);

            Guid val2;
            var exist2 = cacheProvider2.TryGet<Guid>(key, out val2);
            Assert.IsTrue(exist2);
        }

        [TestMethod]
        public void Concurrency() {
            HttpRuntimeCacheProvider cacheProvider1 = (HttpRuntimeCacheProvider)CacheProviderFactory.GetHttpRuntimeCache("g");
            HttpRuntimeCacheProvider cacheProvider2 = (HttpRuntimeCacheProvider)CacheProviderFactory.GetHttpRuntimeCache("h");

            var tasks = new List<Task>();
            for (int i = 0; i < 100; i++) {
                if ((Guid.NewGuid().GetHashCode() & 1) == 0) {
                    tasks.Add(new Task(action: x => cacheProvider1.GetOrCreate(x.ToString(), k => x), state: i));
                }
                else {
                    tasks.Add(new Task(action: x => cacheProvider2.GetOrCreate(x.ToString(), k => x), state: i));
                }
            }
            foreach (var task in tasks) {
                task.Start();
            }
            Task.WaitAll(tasks.ToArray(), CancellationToken.None);

            var count1 = cacheProvider1.Count();
            var count2 = cacheProvider2.Count();

            cacheProvider1.ExpireAll();
            Assert.IsTrue(cacheProvider1.Count() == 0);
            Assert.IsTrue(cacheProvider2.Count() == count2);

            foreach (var entry in System.Web.HttpRuntime.Cache.OfType<DictionaryEntry>()) {
                Console.WriteLine("{0}: {1}", entry.Key, entry.Value);
            }
        }
    }
}
