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

```
dotnet new gRPC
```
and then some fixups to define the control-plane interface

## 2. Bring in Orleans

Now lets bring in Orleans and get the silo running. I'm going to use the in-memory storage provider for demo purposes.
