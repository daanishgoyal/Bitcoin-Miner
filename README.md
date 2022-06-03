# Project 1 - DOSP COP 5615
  

Made By:

        Daanish Goyal 
        UF:ID - 1767 3302
        daanishgoyal@ufl.edu
        

To Run:


    - Server:
                
            dotnet fsi server.fsx N 
                                    // N is the number of zeroes

    
    
    - Client:


            dotnet fsi client.fsx ip_address
                                            // ip_address like 192.160.0.1




## Implementation

Actors are created as per the number of cores in the environment. Distrubtion of work/task amongst the actors and the generation of unique random strings was implemented using the paradigm of Cryptographic Nonce. Each Actor, on being provided with a prefix "daanishgoyal" and a min-max Nonce value was assigned a task of a particular size/load according to the nonces that is, to hash it and check for the required number of zeros in the generated hash and print upon success. The number of actors were usually double the number of processors.

Distributed Implementation:
Server has the job of mining and assigning work to actors or the remote actors. On activation, the client create workers/Actors(remote actors) and sends the list along with other identifiers to the server. Server irrespective of an ongoing task , assigns some work/task (mining operation) to the remote actors on the client. These remote workers/actors upon finishing the work/task (mining operation), reports back to the server(supervisor actor) if it finds a coin (success).

## Notes

1. The size of the work unit depended on the nonces but was kept above 10000000. The program was run on a    2.6GHz dual-core Intel Core i5. Upto 50000000 the workers were able to do the tasks with high concurrency.

2. The result of running your program for input 4 (Machiene : Mac with 2.6GHz dual-core Intel Core i5).

![](https://i.imgur.com/Pt1GbQv.png)



3. The running time: 
                                

                CPU Time: 20487ms
                Absolute/Real Time: 9205ms
                Ratio of (CPU Time/Real Time): 2.23
4. The coin with the most 0s was of 6. However finding a coin of 7 zero's was taking too much time, which could be because of the processor.
 
![](https://i.imgur.com/oDAmXLH.png)


5. The largest number of working machines I was able to run this was on 2, but this can be easily scaled over more machienes.





