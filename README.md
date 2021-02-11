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

TO-DO

## Usage

TO-DO

## How it works

TO-DO