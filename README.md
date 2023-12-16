# microservice-on-nanoservice
A quick hack to build a microservice system on top of Microsoft Orleans 

# What is this thing?

This is a proof of concept of a serverless system built on top of Orleans. It does a few things:
- Allows you to deploy javascript functions to respond to HTTP requests
- Transparently loads handler scripts from storage on demand and caches their parsed AST in memory 
- Automatically unloads cold scripts from memory
- Captures basic consumption metrics for each script
- Exposes a gRPC control-plane to deploy javascript functions and specify the routes they handle
- Exposes a data-plane to dispatch requests to the correct javascript functions

# But why?
I was writing some documentation at a cafe one day, and I  described Orleans as a "nanoservice" framework as i 
often do to newcomers because it's unit of abstraction is single C# interface. Then I thought, since a nanoservice
might be a primitive building block of a microservice, could i build a microservice/serverless system on top of it? 
The answer is yes. Yes I can.

# Live blog of development

## 0. Get organized
I want to be able to take some javascript, deploy it to some magical hosting solution and have it handle a request
from the web, much like a Azure Function or a AWS lambda.

So that will take a few things:

1. A javascript runtime/interpreter
2. A storage backend for the javascript and metadata
3. A "control-plane" to manage the deployment of the javascript
4. A "data-plane" to handle the requests 
5. Some management logic prevent routing conflicts


I'm going to pretend that this is a multi-tenant system, so each deployment will be associated with a family of routes.
I'm arbitrarily treating the top segment of the path as the "family" of routes. So for example, if I deploy a javascript
function to the "foo" family, then it will handle all requests to /foo/*.

I'm skipping authentication and authorization for because move fast and break things.

Chat GPT suggests the name "CosmoCompute" for this project. I like it.

## 1. Setup the project

I'm going to use gRPC for the control-plane because it is easier than REST and I'm lazy.
The data-plane of course is defined by the custom routing, so it will only need a single hook and then
i'll implement custom dispatching from there.

I'm going to lead the gRPC server and then add the data plane hooking later

```powershell
dotnet new gRPC
```

and then some fixups to define the control-plane interface

## 2. Bring in Orleans

Now lets bring in Orleans and get the silo running. I'm going to use the in-memory storage provider for demo purposes.

## 3. Scaffold the control-plane implementation

Ok, so now i need to figure out how to register a route and deploy  the javascript to the silo. I

So in Orleans, we start be defining a grain interface that will store the javascript and metadata.
Since we said that the first route segment would be the "family" of routes, we'll use that as the grain key.
For now, we will not support storing multiple versions of the route handler. But maybe we'll add that later.

I've also left a method to validate the javascript before import, because why let your users push broken code?

## 4. Implement the control-plane and bring in the javascript runtime

Alright lets get to the meat. We have 2 basic choices Jint or Clearscript.
I'm going to go with Jint because it is pure C# and gives me some nice features i want for later.

But before we get to that, we are using the underlying Esprima parser to validate the javascript. 

This is also a good time to add some tests!
Testing is a strong point of the Orleans framework, but more on that later. For now, lets just make sure we can
deploy some javascript to the grain;

We run `dotnet new xunit` to get the test project and then add a reference to the core project with
`dotnet add reference ../CosmoCompute/CosmoCompute.csproj`

Fun fact, CoPilot inferred the entire test class just from the name! But it had a small bug that i fixed up and now they are green!😀

## 4.5 MORE TESTS!

I'm going to bring in some more testing infrastructure just for fun. Orleans provides a testing framework
that lets you run an entire cluster (multiple silos!) in memory. This is great for testing because it lets you
get some deep realism in your tests. 

## 5. Bring in the data-plane

Now we need to implement the data-plane. This is the part that will handle the requests from the web.
There are a couple of parts to this:

1. Add plumbing to the JavascriptGrain to handle the requests
2. dynamically configure the routing to point to the grain


So, we are going to create an abstraction to link up the route to the grain. 
Then we add some smarts to the grain to execute the javascript and return the result.
For the moment, we are only going to support GET requests and only allow the script to return plain strings.

Now, one of the downsides to ASP.NET minimal APIs is that it impedes unit testing :/, so maybe we'll deal with that later.

## 6. Adding a fetch API

Being able to run javascript is cool, but it's not very useful without any APIs to help you do stuff. So lets add a fetch API.

Now, i'm not an expert on how to work with Jint, so i might be doing this wrong, but lets give it a shot.

The plan is to inject a fetch function into the javascript runtime. We'll use the `System.Net.Http.HttpClient` to do the actual work.
Unbelievably, this works! I was expecting to have to do some crazy stuff, but it just works! Jint is really cool!

## 7. Adding a Consumption API

Continuing on the theme of cloning AWS Lambda, lets add a consumption API. This will let us charge the user for the compute time.
We will be adding this to the control-plane, so we can track the usage of each route handler.

Now to keep things simple, we will only store the aggregate usage for each route handler. Storing high cardinality data requires
specialized storage solutions, so we'll leave that for later.

Whats neat about this is that the memory consumption can be lower than 1MB for simple scripts and the execution time can be less than 1ms
So consumption time is to the microsecond and the memory is measured accurate to the byte! Just for fun. Future work could be to add
a sampling profiler to the runtime to get a better idea of the memory usage over the time of the request.

## 8. Building a Test App and measuring performance

So at this point the proof of concept is done. It's time to build a test app to see how it performs.
To do this, I need to move out the protobuf definitions into a shared project so that the test app can use them.

It also turns out that i had a couple little missing bits needed to run the server correctly 😅.
The test app is a simple "hello world" app that just returns a string. It's not very realistic, but it's a start.

So how did we do?

```
====================================
Thread Count: 1 Requests Per Thread: 10000
Total: 10000 Errors: 0 Subjective Time: 2.575 Real Time: 2.576
Subjective Average: 3,884.03
Mean Latency: 0.257
Real Average: 3,881.79
...
====================================
Thread Count: 8 Requests Per Thread: 10000
Total: 80000 Errors: 0 Subjective Time: 16.519 Real Time: 2.100
Subjective Average: 4,842.97
Mean Latency: 0.206
Real Average: 38,095.11
```

Not bad! End to End Sub-millisecond latency and 38k requests per second on my laptop! Memory usage topped out at 40MB
I'm sure we can do better, but this is a good start.

## 9. Where we ended up

So we have a working prototype of a serverless system built on top of Orleans. It's is far from production ready
but it can to quite a bit already and it can do it pretty fast! After adding some things like telemetry, auth, 
execution quotas, etc this could actually be deployed as an internal facing system! I wouldn't put it on the 
public internet until i better understand Jint's Security model however 😅.

Orleans is a beautiful thing to work with. It is so easy to build a prototype like this and take it all the way to production.
With the code as-is exact same code with no modifications could be deployed to a cluster of machines and it would just work! 
I didn't have to worry about how to scale out to multiple machines, or how to handle concurrency, or how to load data from storage,
or how to evict cold data from memory. All of that is handled by Orleans.


This has been a very fun weekend project! I hope you enjoyed reading about it as much as I enjoyed building it!

## 10. So, I said I was done...

I'm hitting my performance goals, I have some confidence in scaling up to a pretty high level of load.
But now that I'm here, I can see father over the hill towards a real platform

So next i'm going to add some basic history tracking to the script uploads. A minor interesting thing is that
we use canonical CBOR to generate a sable hash of the script and metadata. This is because I think CBOR is cool.

I also did some renaming and refactoring to make the code a little more readable.

