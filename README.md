# DurableDungeon
A game designed to teach and learn [serverless durable functions](https://jlik.me/e9m) in C#

## Overview
The Durable Dungeon is a very simple game I wrote to illustrate a long-running serverless application architecture. It is entirely serverless and uses [Table Storage](https://jlik.me/fbd) as the database back end (or the preview [Durable Entities](https://jlik.me/gar)). The general game flow works like this:

1. New user assigned to game. 
2. A room is created, with a monster. A weapon is placed in the room and the monster holds the treasure.
3. The user must confirm their commitment in 2 minutes or the character is killed. 
4. The user must pick up the weapon, slay the monster, and collect the treasure to win.

Commands are issued via end point "posts". A walkthrough below describes in more detail.

## Get started
Clone the repository. 

> *Optional* :
>
> There is a console app that monitors the queue. By default, it uses the [Azure Storage Emulator](https://jlik.me/e9i). To use a real storage account, set `STORAGE_CONNECTION` to the connection string. Run this in a separate window to view "game play." Alternatively, you can monitor the queue directly. The app generates a lot of logging information so the console is useful for demoing steps in a clearer fashion. 

Run the functions app locally from Visual Studio 2017 or later, Visual Studio Code (tasks and settings are included) or by using the functions runtime directly. Create a file in the root of the `DurableDungeon` project named `local.settings.json` and populate it with this to use the storage emulator:

```json
{
    "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet"
  }
}
```

> There are two versions of code.`DurableDungeon` uses Durable Functions v1 and Table Storage. `DungeonEntities` surfaces the exact same API but uses the preview v2 and Durable Entities to track state. 

Change `AzureWebJobsStorage` to a connection string if you wish to use real storage. You can also [publish to Azure](https://jlik.me/e9k) or run a [command-line Zip deployment](https://jlik.me/e9l).

## Game walkthrough 

This is a simple walkthrough of the game. The application is designed to showcase all of the [durable patterns and concepts](https://jlik.me/e9n). To issue "post" and "get" I recommend using the [cross-platform HTTP REPL](https://www.hanselman.com/blog/ACommandlineREPLForRESTfulHTTPServices.aspx) tool, but you can use any client of your choice.

> If you want the **easy button** open `index.html` in the `TestHarness` folder. This is configured to connect with the local running instance (you can change the base URL as needed) and provides a rudimentary UI to interact with the game and monitor workflow status. If you experience Cross-Origin Resource Sharing (CORS) issues, consider adding this snippet as a peer to `Values` in your `local.settings.json` file:
``` json
    "Host": {
        "CORS": "*"
    }
```

### Create User

The first step is to create a user. Assume a user named "Pat" for the following steps. If you are using HTTP REPL and running locally, you can set your base URL to:

`set base http://localhost:7071/api` 

(Change the port if your functions run a different one). 

Issue a POST to the NewUser endpoint:

```json
{
   "name": "Pat" 
}
```

At this stage, you have 2 minutes to confirm. Tables named `Inventory`, `Monster`, `Room`, and `User` have been created and a monitor has started. You can view the tables with [Azure Storage Explorer](https://jlik.me/e9o).

### Check Monitor Status

You can check the confirmation status with: 

`GET CheckStatus/Pat/UserConfirmationWorkflow`. 

Check the monitor with: 

`GET CheckStatus/Pat/UserMonitorWorkflow`.

### Confirm 

If you wish to confirm game play, issue a `POST ConfirmUser` with:

```json
{
   "name": "Pat"
}
```

The console queue will load with the monster, inventory, room descriptions, etc. You can refresh the tables to see the updates. 

### Get the Weapon 

Assuming the weapon is a "Magic Mace" you can issue a `POST Action` with:

```json
{
   "name": "Pat",
   "action": "get",
   "target": "Magic Mace"
}
```

### Slay the Monster

Assuming the monster is a Gibbering Orange Minotaur, slay it by issuing `POST Action` with:

```json
{
   "name": "Pat",
   "action": "kill",
   "target": "Gibbering Orange Minotaur",
   "with": "Magic Mace"
}
```

### Grab the Treasure 

Once slayed, the monster will drop a treasure. Grabbing the treasure will win the game. Assuming it drops a Bloodied Zork trilogy set, get the treasure with `POST Action` and:

```json
{
   "name": "Pat",
   "action": "get",
   "target": "Bloodied Zork trilogy set"
}
```

This will indicate the treasure has been nabbed. The game workflow should end almost immediately after it is issued and the user monitor will conclude within 20 seconds.

## Patterns

`DungeonFunctions` - these are endpoints used to kick off orchestrations. `CheckStatus` shows how to monitor status of long-running workflows (or ones that have completed).

`NewUserParallelFunctions` - this demonstrates running multiple functions in parallel. It also starts a confirmation workflow as a sub-orchestration. That means this orchestratoin doesn't end until the user is confirmed or times out after 2 minutes because the orchestrations are tied to each other. The various activites create the user, room, monster and inventory.

`NewUserSequentialFunctions` - this demonstrates an asynchronous sequential workflow. The relationships between items, characters, and rooms are established sequentially to avoid concurrency issues. A new workflow is also kicked off (by wrapping the kick-off in an activity). 

`ConfirmationFunctions` - this features a user interaction workflow that waits for input and times out after 2 minutes. `KillUser` is an activity that marks the user as "dead" after the timout. `ConfirmUser` illustrates how an ordinary function can raise an event to a running workflow.

`ConsoleFunctions` - this is necessary to send information to the console. `async` is not allowed directly in orchestrations, so the collector is wrapped in an activity.

`MonitorFunctions` - this has two long-running workflows. The `UserMonitorWorkflow` runs every 20 seconds. It ends when the user dies, when the user obtains the treasure, or when the workflow times out after an hour. The `MonitorUser` activity performs the necessary checks. The `GameMonitorWorkflow` waits for two external events. It will run indefinitely, only "wakes up" when external events are sent to it and terminates when both events for killing the monster and obtaining the treasure have been received.

## Feedback

Contact me on Twitter: [@JeremyLikness](https://twitter.com/JeremyLikness)
