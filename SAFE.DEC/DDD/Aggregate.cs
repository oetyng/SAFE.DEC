﻿using SAFE.SystemUtils;
using SAFE.SystemUtils.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SAFE.CQRS
{
    public abstract class Aggregate
    {
        List<RaisedEvent> _uncommittedEvents = new List<RaisedEvent>();
        Dictionary<Type, Action<Event>> _handlers = new Dictionary<Type, Action<Event>>();

        protected long? _id;

        public int Version = -1;
        public string StreamName { get; private set; }
        public long StreamId { get { return _id.HasValue ? _id.Value : 0; } }
        public string StreamKey { get { return $"{StreamName}@{StreamId}"; } }
        //public string StreamKey { get { return $"{char.ToLower(StreamName[0])}{StreamName.Substring(1)}-{StreamId.ToString("N")}"; } }

        public Aggregate()
        {
            var t = this.GetType();
            StreamName = t.Name;
            var methods = GetAllMethods(t)
                .Where(m => m.Name == "Apply");
            foreach (var m in methods)
            {
                _handlers[m.GetParameters().First().ParameterType] = new Action<Event>((e) => m.Invoke(this, new object[] { e }));
            }
        }

        internal void RaiseEvents(IEnumerable<Event> events, bool isHistoric = false)
        {
            foreach (var e in events)
                RaiseEvent(e, isHistoric);
        }

        internal void BuildFromHistory(EventSourcing.Models.EventData @event)
        {
            if (@event.MetaData.SequenceNumber != Version + 1)
                throw new InvalidOperationException();

            var evt = @event.GetDeserialized((b, t) => (Event)b.Parse(t));
            RaiseEvent(evt, true);
        }

        internal List<RaisedEvent> GetUncommittedEvents()
        {
            return _uncommittedEvents.ToList();
        }

        internal void ClearUncommittedEvents()
        {
            _uncommittedEvents.Clear();
        }

        protected void RaiseEvent(Event @event, bool isHistoric = false)
        {
            // ANTI DATA CORRUPTION MEASURE // For bulk applying and exporting events, since we cannot guarantee they wont contain any objects that are stored to AR state but not deepcloned 
            var typeName = @event.GetType().AssemblyQualifiedName;
            var safeEvent = (Event)@event.AsBytes().Parse(typeName);
            // ANTI DATA CORRUPTION MEASURE

            Apply(safeEvent);
            Version++;

            if (!isHistoric)
            {
                var raised = new RaisedEvent(@event)
                {
                     SequenceNumber = Version
                };
                _uncommittedEvents.Add(raised);
            }
        }

        void Apply(Event @event)
        {
            _handlers[@event.GetType()](@event);
        }

        // If have yet another base class with Apply methods too..
        IEnumerable<MethodInfo> GetAllMethods(Type t) // recursive
        {
            if (t == null)
                return Enumerable.Empty<MethodInfo>();

            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            return t.GetMethods(flags).Concat(GetAllMethods(t.BaseType));
        }
    }
}