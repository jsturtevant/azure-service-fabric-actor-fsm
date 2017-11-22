//example modified to work in Azure Service Fabric from https://github.com/dotnet-state-machine/stateless/tree/dev/example/BugTrackerExample 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Actors.Client;
using FSM.Interfaces;
using Stateless;

namespace FSM
{
    /// <remarks>
    /// This class represents an actor.
    /// Every ActorID maps to an instance of this class.
    /// The StatePersistence attribute determines persistence and replication of actor state:
    ///  - Persisted: State is written to disk and replicated.
    ///  - Volatile: State is kept in memory only and replicated.
    ///  - None: State is kept in memory only and not replicated.
    /// </remarks>
    [StatePersistence(StatePersistence.Persisted)]
    internal class FSM : Actor, IFSM
    {
        private StateMachine<State, Trigger> machine;
        private StateMachine<State, Trigger>.TriggerWithParameters<string> assignTrigger;
        private State state;

      
        enum Trigger { Assign, Defer, Resolve, Close }

        /// <summary>
        /// Initializes a new instance of FSM
        /// </summary>
        /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
        public FSM(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
            ActorEventSource.Current.ActorMessage(this, $"Contructored called: {this.GetActorId()}");
        }

        protected override Task OnDeactivateAsync()
        {
            ActorEventSource.Current.ActorMessage(this, $"Actor deactivated: {this.GetActorId()}");
            ActorEventSource.Current.ActorMessage(this, $"Actor State deactivated: {this.GetActorId()}, {this.state}");

            return base.OnDeactivateAsync();
        }

        //protected override async Task OnPostActorMethodAsync(ActorMethodContext actorMethodContext)
        //{
        //    await base.OnPostActorMethodAsync(actorMethodContext);
            
        //    //could call the statemachine save here to make sure every method gets called.
        //    await this.StateManager.SetStateAsync("state", machine.State);
        //}

        /// <summary>
        /// This method is called whenever an actor is activated.
        /// An actor is activated the first time any of its methods are invoked.
        /// </summary>
        protected override async Task OnActivateAsync()
        {
            ActorEventSource.Current.ActorMessage(this, $"Actor activated: {this.GetActorId()}");

            var savedState = await this.StateManager.TryGetStateAsync<State>("state");
            if (savedState.HasValue)
            {
                // has started processing
                this.state = savedState.Value;
            }
            else
            {
                // first load ever initalize
                this.state = State.Open;
                await this.StateManager.SetStateAsync<State>("state", this.state);
            }

            ActorEventSource.Current.ActorMessage(this, $"Actor state at activation: {this.GetActorId()}, {this.state}");

            machine = new StateMachine<State, Trigger>(() => this.state, s => this.state = s);
            machine.OnTransitionedAsync(LogTransitionAsync);

            assignTrigger = machine.SetTriggerParameters<string>(Trigger.Assign);

            machine.Configure(State.Open)
                .Permit(Trigger.Assign, State.Assigned);

            machine.Configure(State.Assigned)
                .SubstateOf(State.Open)
                .OnEntryFromAsync(assignTrigger, assignee => OnAssigned(assignee))
                .PermitReentry(Trigger.Assign)
                .Permit(Trigger.Close, State.Closed)
                .Permit(Trigger.Defer, State.Deferred)
                .OnExitAsync(() => OnDeassigned());



            machine.Configure(State.Deferred)
                .OnEntryAsync(() => this.StateManager.SetStateAsync<string>("assignee", null))
                .Permit(Trigger.Assign, State.Assigned);
        }


        public async Task Close(CancellationToken cancellationToken)
        {
            await machine.FireAsync(Trigger.Close);

            Debug.Assert(state == machine.State);
            await this.StateManager.SetStateAsync("state", machine.State);
        }

        public async Task Assign(string assignee, CancellationToken cancellationToken)
        {
            await machine.FireAsync(assignTrigger, assignee);

            Debug.Assert(state == machine.State);
            await this.StateManager.SetStateAsync("state", machine.State);
        }

        public async Task<bool> CanAssign(CancellationToken cancellationToken)
        {
            Debug.Assert(state == machine.State);
            return machine.CanFire(Trigger.Assign);
        }

        public async Task Defer(CancellationToken cancellationToken)
        {
            await machine.FireAsync(Trigger.Defer);

            Debug.Assert(state == machine.State);
            await this.StateManager.SetStateAsync("state", machine.State, cancellationToken);
        }

        private async Task LogTransitionAsync(StateMachine<State, Trigger>.Transition arg)
        {
            var conditionalValue = await this.StateManager.TryGetStateAsync<StatusHistory>("statusHistory");

            StatusHistory history;
            if (conditionalValue.HasValue)
            {
                history = StatusHistory.AddNewStatus(arg.Destination, conditionalValue.Value);
            }
            else
            {
                history = new StatusHistory(arg.Destination);
            }

            await this.StateManager.SetStateAsync<StatusHistory>("statusHistory", history);
        }

        public async Task<BugStatus> GetStatus()
        {
            var statusHistory = await this.StateManager.TryGetStateAsync<StatusHistory>("statusHistory");
            var assignee = await this.StateManager.TryGetStateAsync<string>("assignee");

            var status = new BugStatus(this.machine.State);
            status.History = statusHistory.HasValue ? statusHistory.Value : new StatusHistory(machine.State);
            status.Assignee = assignee.HasValue ? assignee.Value : string.Empty;

            return status;
        }

        async Task OnAssigned(string assignee)
        {
            var previousAssignee = await this.StateManager.TryGetStateAsync<string>("assignee");
            if (previousAssignee.HasValue && assignee != previousAssignee.Value)
                await SendEmailToAssignee("Don't forget to help the new employee!");

            await this.StateManager.SetStateAsync("assignee", assignee);
            await SendEmailToAssignee("You own it.");
        }

        async Task OnDeassigned()
        {
            await SendEmailToAssignee("You're off the hook.");
        }

        async Task SendEmailToAssignee(string message)
        {
            var assignee = await this.StateManager.GetStateAsync<string>("assignee");
            Console.WriteLine("{0}, RE {1}", assignee, message);
        }
    }
}
