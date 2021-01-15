# DOS
Projects created as part of the Distributed Operating Systems coursework

Project 1 (SquareSumDos) 

The goal of this project is to use F# and the actor model to build a good solution to the problem of finding perfect squares that are sums of consecutive squares that runs well on multi-core machines.

Project 2 (Gossip)

Implement a Gossip type algorithm and determine the convergence of such algorithms through a simulator based on actors written in F#. Since actors in F# are fully asynchronous, the particular type of Gossip implemented is the so called Asynchronous Gossip.

Gossip Algorithm for information propagation The Gossip algorithm involves the following:
Starting: A participant(actor) it told/sent a roumor(fact) by the mainprocess
Step: Each actor selects a random neighbor and tells it the rumor
Termination: Each actor keeps track of rumors and how many times it has heard the rumor. It stops transmitting once it has heard the rumor 10 times (10 is arbitrary, you can select other values).

Push-Sum algorithm for sum computation
State: Each actor Ai maintains two quantities: s and w. Initially, s = x(i) = i (that is actor number i has value i, play with other distribution if you so desire) and w = 1
Starting: Ask one of the actors to start from the main process.
Receive: Messages sent and received are pairs of the form (s,w). Uponreceive, an actor should add received pair to its own corresponding val-ues. Upon receive, each actor selects a random neighboor and sends it amessage.
Send: When sending a message to another actor, half of s and w is keptby the sending actor and half is placed in the message.
Sum estimate: At any given moment of time, the sum estimate is s/w where s and w are the current values of an actor.
Termination: If an actors ratio s/w did not change more than 10^(-10) in 3 consecutive rounds the actor terminates. WARNING: the values s and w independently never converge, only the ratio does.

Topologies The actual network topology plays a critical role in the dissemination speed of Gossip protocols. As part of this project you have to experiment with various topologies. The topology determines who is considered a neighboor in the above algorithms.
Full Network Every actor is a neighbor of all other actors. That is, every actor can talk directly to any other actor.
2D Grid: Actors form a 2D grid. The actors can only talk to the grid neighbors.
Line: Actors are arranged in a line. Each actor has only 2 neighbors (oneleft and one right, unless you are the first or last actor).
Imperfect 2D Grid: Grid arrangement but one random other neighbor is selected from the list of all actors (4+1 neighbors).

Project 3 (Pastry)

The goal of this project is to implement in F# using the actor model the Pastry protocol and a simple object access service to prove its usefulness. The specification of the Pastry protocol can be found in the paper Pastry: Scalable, decentralized object location and routing for large-scale peer-to-peer systems.by A. Rowstron and P. Druschel. You can find the paper at http://rowstron.azurewebsites.net/PAST/pastry.pdf. The paper above, in Section 2.3 contains a specification of the Pastry API and of the API to be implemented by the application.

Project 4 (Twitter Simulator)

Utilizing the actor model in F# and learnings from the 3 projects above build a application that simulates the functions that twitter performs. For e.g users should be able to follow other users, send tweets, re-tweet stuff, search for hashtags etc.
