WebApi-OData
============

These are some useful classes for when you need to auto-expose (with minimal fuss) a number of entity sets from an Entity Framework context in OData format using Web API.

It uses Web API 2.2 and exposes OData v4.

Since WCF Data Service is no longer being actively developed, I was searching for an easy-to-use replacement based on Web API, but couldn't find any that didn't require creating a godzillion custom controllers.  As a result, I spun my own.

Reflection is used in the base class to locate the entity set (``DbSet``) property within the ``DbContext`` based on the entity's type.

The process can actually be automated further by using reflection to register all ``DbSet`` properties in the ``DbContext`` with the EDM builder, as well as to auto-generate the controller classes.  Then it will be almost as easy as WCF Data Services.  I haven't done it since I don't mind a few more lines of code to define the controllers, but it shouldn't be difficult.


How To Use
----------

Exposing an entity set from a DbContext is as simple as adding a line of code:

	builder.EntitySet<Entity1>("Entities1");

and building a controller (yes, you still need to do this):

	public class Entities1Controller : StandardODataController<DB, Entity1, KeyType> { }

The standard controller base class implements the standard set of CRUD operations with POST/GET/PUT/PATCH/DELETE verbs.

However, many web servers actually have problem (or require additional configuration) supporting the PATCH or MERGE verbs, so in this implementation PUT is used for incremental update while POST is used for creation as well as full-object replace:

* GET = SELECT
* POST (no URL parameters) = INSERT
* POST (with URL key parameters) = full-object replace
* PUT = UPDATE (incremental)
* DELETE = DELETE


Composite Primary Keys
----------------------

There is support (via a custom routing convention) for composite primary keys.  For example:

	http://example.com/odata/Entities1(Key1=..., Key2=...)

gets the particular entity with the corresponding composite key values.

To use this custom routing convention, you need to add ``CompositeKeyRoutingConvention`` during configuration:

	var routingConventions = ODataRoutingConventions.CreateDefault();
	routingConventions.Insert(0, new OData.CompositeKeyRoutingConvention());
	config.MapODataServiceRoute("ODataRoute", "odata", model, new DefaultODataPathHandler(), routingConventions, new DefaultODataBatchHandler(GlobalConfiguration.DefaultServer));

See the sample for details.


Type Casting Support for TPH
----------------------------

There is also built-in support for type casting (again via a custom routing convention) -- which is Web API-speak for simulating OData's ``isof`` function.

For example, to get only a certain type of objects from a TPH inheritance table, you can do:

	http://example.com/odata/Entities1/Namespace.Type
	
to get a list of all items in ``Entities1`` that is of type ``Namespace.Type``.

To use it, add ``CastRoutingConvention`` during configuration:

	var routingConventions = ODataRoutingConventions.CreateDefault();
	routingConventions.Insert(0, new OData.CastRoutingConvention());
	config.MapODataServiceRoute("ODataRoute", "odata", model, new DefaultODataPathHandler(), routingConventions, new DefaultODataBatchHandler(GlobalConfiguration.DefaultServer));
