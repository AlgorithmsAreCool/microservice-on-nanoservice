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