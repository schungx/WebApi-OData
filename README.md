WebApi-OData
============

These are some useful classes for when you need to auto-expose (with minimal fuss) a number of entity sets from an Entity Framework context in OData format using Web API.

It uses Web API 2.x and exposes OData v4.

Since WCF Data Service is no longer being actively developed, I was searching for an easy-to-use replacement based on Web API, but couldn't find any that didn't require creating a godzillion custom controllers.  As a result, I spun my own.

Exposing an entity set from a DbContext is as simple as adding a line of code:

	builder.EntitySet<Entity1>("Entities1");

and building a controller (yes, you still need to do this):

	public class EntitiesController : StandardODataController<DB, Entity1, KeyType> { }

The standard controller base class implements the standard set of CRUD operations with POST/GET/PUT/PATCH/DELETE verbs.

There is support (via a custom route) for composite primary keys (see the sample), as well as built-in support for type casting (again via a custom route) -- which is Web API-speak for simulating OData's ``oftype`` function.  For example, you can do:

	http://example.com/odata/Entities1/Namespace.Type/
	
to get a list of all items in ``Entities1`` that is of type ``Namespace.Type``.

Reflection is used in the base class to locate the entity set (``DbSet``) property within the ``DbContext`` based on the entity's type.

The process can actually be automated further by using reflection to register all ``DbSet`` properties in the ``DbContext`` with the EDM builder, as well as to auto-generate the controller classes.  Then it will be almost as easy as WCF Data Services.  I haven't done it since I don't mind a few more lines of code to define the controllers, but it shouldn't be difficult.
