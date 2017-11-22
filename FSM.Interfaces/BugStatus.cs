using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace FSM.Interfaces
{

    [DataContract]
    public class BugStatus
    {
        public BugStatus(State machineState)
        {
            this.CurrentStatus = machineState.ToString();
        }

        [DataMember]
        public string CurrentStatus { get; set; }

        [DataMember]
        public StatusHistory History { get; set; }

        [DataMember]
        public List<string> Avaliable => Enum.GetNames((typeof(State))).ToList();

        [DataMember]
        public string Assignee { get; set; }
    }

    public enum State { Open, Assigned, Deferred, Resolved, Closed }

    [DataContract]
    public class RecordedStatus
    {
        public RecordedStatus(State state)
        {
            this.State = state.ToString();
            this.Time = DateTimeOffset.Now.ToUnixTimeSeconds();
        }

        [DataMember]
        public long Time { get; set; }

        [DataMember]
        public string State { get; set; }
    }

    public class StatusHistory : SortedList<Guid, RecordedStatus>
    {
        // needed for serilization
        private StatusHistory() : base() { }

        //used for making new copy
        private StatusHistory(IDictionary<Guid, RecordedStatus> dictionary) : base(dictionary)
        {
        }

        public StatusHistory(State newState) : base()
        {
            this.AddState(newState);
        }

        public static StatusHistory AddNewStatus(State newState, StatusHistory history)
        {
            var newHistory = new StatusHistory(history);
            newHistory.AddState(newState);
            return newHistory;
        }

        private void AddState(State newState)
        {
            this.Add(Guid.NewGuid(), new RecordedStatus(newState));
        }
    }

}