# azure-service-fabric-actor-fsm
Example to show how to use Finite State Machine (FSM) in [Azure Service Fabric](https://azure.microsoft.com/en-us/services/service-fabric/) Actor Programming model.  Learn more about the example on my blog at http://www.jamessturtevant.com/posts/Creating-a-Finite-State-Machine-Distributed-Workflow-with-Service-Fabric-Reliable-Actors/.

> Note: This sample has modified `ActorGarbageCollectionSettings` in the start up `program.cs` file of the actor service to test the deactivation and rehydration of the actors state for the FSM.  You most likely will not want to modify to modify the default settings in your solution so do not copy them.

## Get started

1. Make sure you have the [Service Fabric SDK](https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-get-started#install-the-sdk-and-tools) and [Visual Studio](https://www.visualstudio.com/downloads/) installed
2. Clone this repository: `git clone https://github.com/jsturtevant/azure-service-fabric-actor-fsm.git` 
3. Open the solution file in Visual Studio 
4. Press `F5` to build and run the solution on your local Service Fabric cluster (the application is set up for [refresh on single node local cluster](https://blogs.msdn.microsoft.com/azureservicefabric/2017/04/17/speed-up-service-fabric-development-with-the-new-refresh-application-debug-mode-2/))
5. That deployed the FSM Actor service, now deploy the sample consumer by right clicking on the `ActorExpirement` project and selecting `Debug->Start new instance` 

You will see output from a console application as it spins through creating a couple actors, pushing them through the FSM states, waiting for them to be deactivated and then push them through the FSM after they have been re-hydrated.
