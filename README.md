# MDBee-dfs

This project has been developed for the **Liberate Data Week** event, an [hackathon](https://medium.com/ethereum-swarm/liberate-data-hackathon-guidelines-rules-and-programme-554c65a05cdb) organized by [Fair Data Society](https://fairdatasociety.org/).

We want to implement a tool inspired to [MDBee](https://github.com/Etherna/mdbee) using the framework [fairOS-dfs](https://github.com/fairDataSociety/fairOS-dfs). There are some differences from the original paper, due to use of this different framework.  
All the code is original and developed into the hackathon time frame.

This tool synchronizes an existing instance of a MongoDB Replica Set over Swarm, publishing the data to the net with public pods and permitting users to access, monitor and consume it in real-time.

Different reasons exist for have to maintain a centralized control over data. For example because we have to maintain a moderated environment for a public service (totally uncensored web3 can have GREAT pros, but also some cons), or because we want to make full transparent the management of an existing application.  
After all, decentralization can be achieved in many ways where there are **transparency**, **full awareness** and **free consensus**.

In these cases, and if the scope is to make completely transparent our data maintained over MongoDB, this is the tool!

## Dev env initialization

Environment has been built with `docker-compose`. It's started with these commands:

```
cd devenv
docker-compose up -d
```

Before start to use the environment, we need a bit of configuration.

### Create Mongo replica set

Modify the local system `hosts` file adding these lines:

```
# Mongo replica
127.0.10.1	mongo1
127.0.10.2	mongo2
127.0.10.3	mongo3
```

Open a new terminal and connect a mongo client to `mongo1` node:

```
mongo 127.0.10.1
```

Initialize replica set:

```
rs.initiate()
rs.add("mongo2")
rs.add("mongo3")
```

### Fund Bee node using the Swarm Goerli Faucet

Follow this guide: https://docs.ethswarm.org/docs/installation/docker#docker-compose

## Project Build

Install .Net5 SDK: [instructions](https://docs.microsoft.com/en-us/dotnet/core/install/)

```
dotnet build .\MDBee-dfs.sln
```

## Usage

There are two main projects into this solution:

+ `HackathonDemo` is the interactive demo for presentation
+ `MDBee-dfs` is the synchronization tool

Both can be executed with `-h` parameter for print the help.

## How it works

This tool maps one mongo database with one dfs pod, and all internal collections of mongo with as many related documentDbs of dfs:

```
MongoDB database   <->  Dfs pod
+ collection A     <->  + documentDB A
+ collection B     <->  + documentDB B
+ collection C     <->  + documentDB C
                   ...  
```

`HackathonDemo` application can connect to an instance of FairOS-dfs and an instance of MongoDB. For demo scope, at first it tries to login with provided credentials on dfs. If the user doesn't exist, creates it.  
It also tries to open, or create, a pod with the given database name to sync.  

The demo app can:
+ Print status of the observed dfs pod and of the observed mongo database
+ Insert new random documents into any collection of mongo database
+ Remove random documents from any collection of mongo database

`MDBee-dfs` is the sync tool. Like the demo application, it takes dfs and mongo's connecting params and credentials. If a dfs user doesn't exist, it creates one.  
It starts to look at present dfs situation. The sync status is stored into a `kv` archive. If these information are not present, it starts a new sync from scratch, otherwise it tries to resume from last processed instructions.

The synchronization protocol is similar to the one implemented by MongoDB itself for keep synced nodes of a replica set. It uses `oplogs` from nodes, and implements also a similar initial sync protocol ([Mongo's official docs](https://github.com/mongodb/mongo/blob/master/src/mongo/db/repl/README.md#initial-sync)). A sync from scratch starts listening and buffering any occurring new `oplog`. So it starts a full copy of all documents from all collections of observed db. When the copy is completed, all buffered oplogs are applied. When the buffer is empty, the initial sync is completed.

In any moment, any new operation on mongo's db is listen by the tool and buffered with its oplog. Any buffered oplog will be replicated when possibile on dfs' pod.