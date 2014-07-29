WebApi-OData
============

These are some classes useful when you want to auto-expose a number of entity sets from an Entity Framework as OData using Web API.

Currently, it uses Web API 2.2 and exposes OData v1-3.  OData v4 should require only minor method call changes.  I am not using OData v4 due to its lack of DateTime support which breaks much legacy code.

Since WCF Data Services is no longer being actively developed, I was searching for an easy-to-use replacement based on Web API, but couldn't find any.  As a result, I spun my own.

Exposing an entity set is as simple as adding a line of code:

	builder.EntitySet<Entity1>("Entities1");

and building a controller:

	public class EntitiesController : StandardODataController<DB, Entity1, KeyType> { }

The standard controller base class supports all the standard CRUD operations with POST/GET/PUT/PATCH/DELETE verbs.

Reflection is used in the base class to locate the entity set within the DbContext.

The process can be automated further by using reflection to register all DbSet's in the DbContext with the EDM builder, as well as to auto-generate the controller classes.  Then it will be almost as easy as WCF Data Services.  I haven't done it since I don't think a few more lines of code matter, but it shouldn't be difficult.
