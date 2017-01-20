﻿using FocusTreeManager.CodeStructures;
using System.Runtime.Serialization;
using System.Collections.Generic;

namespace FocusTreeManager.DataContract
{
    [KnownType(typeof(Script))]
    [DataContract(Name = "event_option")]
    public class EventOption
    {
        [DataMember(Name = "name", Order = 0)]
        public string Name { get; set; }

        [DataMember(Name = "script", Order = 1)]
        public Script InternalScript { get; set; }

        public EventOption()
        {
        }

        public EventOption(Model.LegacySerialization.EventOption legacyItem)
        {
            Name = legacyItem.Name;
            InternalScript = new Script();
            InternalScript.Analyse(legacyItem.InternalScript.Parse());
        }

        public void setDefaults()
        {
            Name = "namespace.count.a";
            InternalScript = new Script();
        }

        internal static List<EventOption> PopulateFromLegacy(List<Model.LegacySerialization.EventOption> options)
        {
            List<EventOption> list = new List<EventOption>();
            foreach (Model.LegacySerialization.EventOption legacyItem in options)
            {
                list.Add(new EventOption(legacyItem));
            }
            return list;
        }
    }
}