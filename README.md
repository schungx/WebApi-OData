WebApi-OData
============

These are some useful classes for when you need to auto-expose (with minimal fuss) a number of entity sets from an Entity Framework context in OData format using Web API.

Currently, it uses Web API 2.2 and exposes OData v1-3.  OData v4 should require only minor method call changes.  I am currently not using OData v4 due to its lack of DateTime support which breaks legacy code.  I'm happy for anyone to test it on v4 though.

Since WCF Data Service is no longer being actively developed, I was searching for an easy-to-use replacement based on Web API, but couldn't find any that didn't require creating a godzillion custom controllers.  As a result, I spun my own.

Exposing an entity set from a DbContext is as simple as adding a line of code:

	builder.EntitySet<Entity1>("Entities1");

and building a controller (yes, you still need to do this):

	public class EntitiesController : StandardODataController<DB, Entity1, KeyType> { }

The standard controller base class implements the standard set of CRUD operations with POST/GET/PUT/PATCH/DELETE verbs.

Reflection is used in the base class to locate the entity set (``DbSet``) property within the ``DbContext`` based on the entity's type.

The process can actually be automated further by using reflection to register all ``DbSet`` properties in the ``DbContext`` with the EDM builder, as well as to auto-generate the controller classes.  Then it will be almost as easy as WCF Data Services.  I haven't done it since I don't mind a few more lines of code to define the controllers, but it shouldn't be difficult.
