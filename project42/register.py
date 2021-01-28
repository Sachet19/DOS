#!/usr/bin/env python

# WS client example

import asyncio
import websockets
import json 

#Cleanup

def switchMenu(choice):
    if choice == '1':
        urlSanitise()
        
async def hello():
    uri = "ws://localhost:8080"
    async with websockets.connect(uri) as websocket:
        flag1 = True
        username = ""
        password = ""
        while(flag1):
            print("\n---------------------------------")
            print("\n        Twitter Simulator        ")
            print("\n---------------------------------\n")
            print("Hello, What would you like to do?\n")
            print("To Register: Press 1")
            print("If Existing User: Press 2")
            regOption = input()
            #print(f"regOption{regOption}" )
            if int(regOption)==1:
                print("\nEnter desired username")
                username = input()
                print("\nEnter desired password")
                password = input()

                message = { "Command" : "Register",
                            "Param1" : username,
                            "Param2" : password }

                outputMessage = json.dumps(message) 

                #await websocket.send("Register "+ username + " "+ password)
                await websocket.send(outputMessage)
                response = await websocket.recv()

                json_response = json.loads(response)
                print("\n")
                print(json_response["Response"])
                flag1 = False  
            else:
                print("\nEnter username")
                username = input()
                print("\nEnter password")
                password = input()

                message = { "Command" : "Login",
                            "Param1" : username,
                            "Param2" : password }

                outputMessage = json.dumps(message) 

                #await websocket.send("Register "+ username + " "+ password)
                await websocket.send(outputMessage)
                response = await websocket.recv()

                json_response = json.loads(response)

                LoginResponse = json_response["Response"]

                #await websocket.send("Login "+ username + " "+ password)
                #LoginResponse = await websocket.recv()
                #print(LoginResponse)
                if LoginResponse=="NoUser":
                    print("\nUser does not exist.. Please try again\n")
                elif LoginResponse=="WrongPassword":
                    print("\nPassword incorrect.. Please try again\n") 
                else:
                    print("\nLogin Successfull!\n")
                    response = await websocket.recv()
                    json_response = json.loads(response)
                    
                    offlineTweetCount = json_response["Response"]

                    # print(f"< {hastagResponseCount}")
                    i=0
                    if int(offlineTweetCount)==0:
                        print("No new tweets for you while you were away\n")
                    else:
                        print(f"You missed these while you were offline:-\n")
                        while(i<int(offlineTweetCount)):
                            response = await websocket.recv()
                            json_response = json.loads(response)
                            
                            offlineTweetResponse = json_response["Response"]

                            #offlineTweetResponse = await websocket.recv()
                            print(f"{offlineTweetResponse}")
                            #print("\n")
                            i = i + 1
                    flag1 = False
        flag2 = True
        while(flag2):
            print(f"\nWelcome {username}, What would you like to do?\n")
            print("Tweet: Press 1")
            print("Retweet: Press 2 ")
            print("Search for Hashtag: Press 3 ")
            print("Look at your mentions: Press 4 ")
            print("Look at your feed: Press 5")
            print("Follow a user: Press 6")
            print("Logout: Press 7")
            imenuOption = input()
            menuOption = int(imenuOption)
            if menuOption==1:
                print("\nWhat would you like to tweet?")
                tweet = input()

                message = { "Command" : "Tweet",
                            "Param1" : username,
                            "Param2" : tweet }

                outputMessage = json.dumps(message) 

                #await websocket.send("Register "+ username + " "+ password)
                await websocket.send(outputMessage)
                response = await websocket.recv()

                json_response = json.loads(response)


                print("\n")
                print(json_response["Response"])
                #print("\n") 
            elif menuOption==2:

                message = { "Command" : "GetFeed",
                            "Param1" : username,
                            "Param2" : "dummy" }

                outputMessage = json.dumps(message) 

                #await websocket.send("Register "+ username + " "+ password)
                await websocket.send(outputMessage)
                response = await websocket.recv()

                json_response = json.loads(response)
                retweetQueryResponseCount = json_response["Response"]

                #First we get the tweets that can be retweeted by us            
                    # print(f"< {hastagResponseCount}")
                i=0
                if int(retweetQueryResponseCount)==0:
                    print("\nNo tweets in your feed to retweet")
                else:
                    print("\nYour feed is:-\n")
                    tweetDictionary = {}
                    while(i<int(retweetQueryResponseCount)):
                        #reTweetQueryResponse = await websocket.recv()

                        response = await websocket.recv()

                        json_response = json.loads(response)
                        reTweetQueryResponse = json_response["Response"]

                        i = i + 1
                        print(f"{i}) - {reTweetQueryResponse}")#TODO Modify print to display in the format: 1 - @SACHET: <Tweet Content>
                        tweetDictionary[i] = reTweetQueryResponse
                        #TODO Insert each tweet with corresponding i value into a map

                    print("\nEnter the number corresponding to the tweet you would like to re-tweet")
                    tweetIndex = input()
                    intTweetIndex = int(tweetIndex)

                    #TODO Fetch the tweet content of the tweet you want to re-tweet from the map using the index
                    reTweetContent = "RT: " + tweetDictionary[intTweetIndex] #TODO append the tweet fetched with RT into this variable

                    #await websocket.send("Tweet "+ username + " "+ reTweetContent)

                    message = { "Command" : "Tweet",
                                "Param1" : username,
                                "Param2" : reTweetContent }

                    outputMessage = json.dumps(message) 

                    #await websocket.send("Register "+ username + " "+ password)
                    await websocket.send(outputMessage)
                    response = await websocket.recv()

                    json_response = json.loads(response)


                    #print("\n")
                    print(json_response["Response"])



                    # reTweetResponse = await websocket.recv()
                    # print(reTweetResponse)
                        #print("\n")
                #Retweet
            elif menuOption==3:    
                print("\nEnter hashtag you want to search?")
                hashtag = input()

                message = { "Command" : "HashtagQuery",
                            "Param1" : username,
                            "Param2" : hashtag }

                outputMessage = json.dumps(message) 

                #await websocket.send("Register "+ username + " "+ password)
                await websocket.send(outputMessage)
                response = await websocket.recv()

                json_response = json.loads(response)

                hashtagResponseCount = json_response["Response"]

                #await websocket.send("HashtagQuery "+ username + " "+ hashtag)
                #hashtagResponseCount = await websocket.recv()
                #print(f"< {hashtagResponseCount}")
                i=0
                if int(hashtagResponseCount)==0:
                    print("\nNo tweets found with this hashtag")
                else:
                    print(f"\nTweets containing {hashtag} are:-\n")
                    while(i<int(hashtagResponseCount)):
                        #hashtagResponse = await websocket.recv()

                        response = await websocket.recv()

                        json_response = json.loads(response)
                        hashtagResponse = json_response["Response"]

                        print(f"{hashtagResponse}")
                        #print("\n")
                        i = i + 1

            elif menuOption==4:
                message = { "Command" : "MentionQuery",
                            "Param1" : username,
                            "Param2" : "dummy" }

                outputMessage = json.dumps(message) 

                #await websocket.send("Register "+ username + " "+ password)
                await websocket.send(outputMessage)
                response = await websocket.recv()

                json_response = json.loads(response)

                mentionResponseCount = json_response["Response"]                
                # await websocket.send("MentionQuery "+ username)
                # mentionResponseCount = await websocket.recv()
                    # print(f"< {hastagResponseCount}")
                i=0
                if int(mentionResponseCount)==0:
                    print("\nNo menitons found")
                else:
                    print("\nYour mentions are:-\n")
                    while(i<int(mentionResponseCount)):
                        response = await websocket.recv()

                        json_response = json.loads(response)
                        mentionResponse = json_response["Response"]                        
                        # mentionResponse = await websocket.recv()
                        print(f"{mentionResponse}")
                        #print("\n")
                        i = i + 1
            elif menuOption==5:
                message = { "Command" : "GetFeed",
                            "Param1" : username,
                            "Param2" : "dummy" }

                outputMessage = json.dumps(message) 

                #await websocket.send("Register "+ username + " "+ password)
                await websocket.send(outputMessage)
                response = await websocket.recv()

                json_response = json.loads(response)

                feedResponseCount = json_response["Response"]      
                print("\nfeedResponseCount = ")
                print(feedResponseCount)

                # await websocket.send("GetFeed "+ username)
                # feedResponseCount = await websocket.recv()
                    # print(f"< {hastagResponseCount}")
                i=0
                if int(feedResponseCount)==0:
                    print("\nNo tweets found")
                else:
                    print("\nYour feed is:-\n")
                    while(i<int(feedResponseCount)):
                        response = await websocket.recv()

                        json_response = json.loads(response)
                        feedResponse = json_response["Response"] 

                        # feedResponse = await websocket.recv()
                        print(f"{feedResponse}")
                        #print("\n")
                        i = i + 1
            elif menuOption==6:
                message = { "Command" : "FollowQuery",
                            "Param1" : username,
                            "Param2" : "dummy" }

                outputMessage = json.dumps(message) 

                #await websocket.send("Register "+ username + " "+ password)
                await websocket.send(outputMessage)
                response = await websocket.recv()

                json_response = json.loads(response)

                followQueryResponseCount = json_response["Response"] 

                # await websocket.send("FollowQuery "+ username)
                # followQueryResponseCount = await websocket.recv()
                #print(f"< {followQueryResponseCount}")
                i=0
                if int(followQueryResponseCount)==0:
                    print("\nNo users available to follow")
                else:
                    print("\nList of users you can follow:-\n")
                    candidateDictionary = {}
                    while(i<int(followQueryResponseCount)):
                        response = await websocket.recv()

                        json_response = json.loads(response)
                        followQueryResponse = json_response["Response"] 

                        # followQueryResponse = await websocket.recv()
                        i = i + 1                        
                        print(f"{i}) - @{followQueryResponse}") #TODO Modify print to display in the format: 1 - @SACHET
                        candidateDictionary[i] = followQueryResponse
                        #TODO Insert each user with corresponding i value into a map

                    print("\nEnter the number corresponding to the user you would like to follow")
                    followIndex = input()
                    intFollowIndex = int(followIndex)

                    #TODO Fetch the userID of person you want to follow from the map using the index
                    followId = candidateDictionary[intFollowIndex] #TODO set the value fetched into this variable

                    message = { "Command" : "FollowRequest",
                                "Param1" : username,
                                "Param2" : followId }

                    outputMessage = json.dumps(message) 

                    #await websocket.send("Register "+ username + " "+ password)
                    await websocket.send(outputMessage)
                    response = await websocket.recv()

                    json_response = json.loads(response)

                    followResponse = json_response["Response"]   


                    # await websocket.send("FollowRequest "+ username + " " + followId)
                    # followResponse = await websocket.recv()
                    print("You are now following @"+followId+"!")
                        #print("\n")
            elif menuOption==7:
                message = { "Command" : "Logout",
                            "Param1" : username,
                            "Param2" : "dummy" }

                outputMessage = json.dumps(message) 

                #await websocket.send("Register "+ username + " "+ password)
                await websocket.send(outputMessage)
                response = await websocket.recv()

                json_response = json.loads(response)

                logoutResponse = json_response["Response"]  

                # await websocket.send("Logout "+ username)
                # logoutResponse = await websocket.recv()
                    # print(f"< {hastagResponseCount}")       
                print("\nSuccessfully logged out!\n")
                #await websocket.close()

                flag2 = False
            else:
                print("\nInvalid command Please retry\n")

asyncio.get_event_loop().run_until_complete(hello())