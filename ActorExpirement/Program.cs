using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Query;
using FSM.Interfaces;

namespace ActorExpirement
{
    class Program
    {
        static async Task Main(string[] args)
        {
            ActorId actorId = new ActorId(Guid.NewGuid().ToString());
            // This only creates a proxy object, it does not activate an actor or invoke any methods yet.
            IFSM actor = ActorProxy.Create<IFSM>(actorId, new Uri("fabric:/ActorFSM/FSMActorService"));

            Console.WriteLine($"Running actor 1 [{actorId}] through FSM states.");
            var token = new CancellationToken();
            await actor.Assign("Joe", token);
            await actor.Defer(token);
            await actor.Assign("Harry", token);
            await actor.Assign("Fred", token);
            await actor.Close(token);

            ActorId actorId2 = new ActorId(Guid.NewGuid().ToString());
            IFSM actor2 = ActorProxy.Create<IFSM>(actorId2, new Uri("fabric:/ActorFSM/FSMActorService"));

            Console.WriteLine($"Running actor 2 [{actorId2}] through FSM states");
            await actor2.Assign("Sally", token);
            await actor2.Defer(token);
            await actor2.Assign("Sue", token);

            Console.WriteLine("Waiting for actors to deactivate for 20s (using modified Actor Garbage Collection settings in actor service program.cs)");
            await Task.Delay(TimeSpan.FromSeconds(20));

            Console.WriteLine("Queury Service Fabric to make sure Actors have been Garbage Collected");
            var actorService = ActorServiceProxy.Create(new Uri("fabric:/ActorFSM/FSMActorService"), actorId);
            ContinuationToken continuationToken = null;
            IEnumerable<ActorInformation> inactiveActors;
            do
            {
                var queryResult = await actorService.GetActorsAsync(continuationToken, token);
                inactiveActors = queryResult.Items.Where(x => !x.IsActive);
                continuationToken = queryResult.ContinuationToken;
            } while (continuationToken != null);

            Console.WriteLine("Found the following Inactive actors in the system (should include actors just created):");
            foreach (var actorInformation in inactiveActors)
            {
                Console.WriteLine($"\t {actorInformation.ActorId}");
            }

            try
            {
                //should blow up because joe is in closed state 
                await actor.Assign("Joe", token);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Actor 1 [{actorId}] should throw exception becuase it is in closed state in the FSM");
            }

            Console.WriteLine($"Actor 2 [{actorId2}] should be able to close in the FSM");
            await actor2.Close(token);

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}   
