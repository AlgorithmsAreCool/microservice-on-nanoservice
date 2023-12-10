# microservice-on-nanoservice
A quick hack to build a microservice system on top of Microsoft Orleans 

# Intro

I was writing some documentation and I had a bad idea! I often describe Orleans as a "nanoservice" framework because it's unit of abstraction is single c# interface.
Then I thought, what if I could build a serverless system on top of Orleans? So I did!

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