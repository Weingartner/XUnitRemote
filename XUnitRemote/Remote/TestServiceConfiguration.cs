﻿using System;
using System.Collections.Generic;

namespace XUnitRemote.Remote
{
    public class TestServiceConfiguration
    {
        /// <summary>
        /// The unique id to assign to this service.
        /// </summary>
        public string Id { get; }
        
        /// <summary>
        /// A dictionary of data that will be assigned to XunitService.Data in the child app domains.
        /// All objects must be serializable.
        /// </summary>
        public IReadOnlyDictionary<string, object> Data { get; }

        /// <summary>
        /// Called before executing tests.
        /// </summary>
        public Action GlobalTestSetup { get; }

        public TestServiceConfiguration(string id, IReadOnlyDictionary<string, object> data)
            : this(id, data, () => { })
        {
        }

        public TestServiceConfiguration(string id, IReadOnlyDictionary<string, object> data, Action globalTestSetup)
        {
            Id = id;
            Data = data;
            GlobalTestSetup = globalTestSetup;
        }
    }
}