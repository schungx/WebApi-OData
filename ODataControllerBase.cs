using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.OData;

namespace WebApi.OData.Controllers
{
	// Standard OData controller with generic implementations

	public abstract class StandardODataControllerBase<C> : ODataController where C : DbContext, new()
	{
		private C m_DB = null;

		protected C db
		{
			get
			{
				if (m_DB == null) m_DB = new C();
				m_DB.Configuration.LazyLoadingEnabled = false;
				m_DB.Configuration.ProxyCreationEnabled = false;
				return m_DB;
			}
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing && m_DB != null) m_DB.Dispose();
			base.Dispose(disposing);
		}

		protected virtual async Task<T> GetEntity<T, K> (DbSet<T> entityset, K key) where T : class
		{
			var val = await entityset.FindAsync(key);
			if (val == null) throw new HttpResponseException(HttpStatusCode.NotFound);
			return val;
		}

		private static void HandleExceptions (Exception ex)
		{
			if (ex is NotSupportedException) {
				throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotImplemented)
				{
					Content = new StringContent(ex.Message),
					ReasonPhrase = "This entity set does not support POST."
				});
			} else if (ex is DbUpdateException) {
				throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.Conflict)
				{
					Content = new StringContent("Cannot save entity due to key conflicts or constraint violations."),
					ReasonPhrase = "Cannot save entity due to conflict."
				});
			} else if (ex is DbEntityValidationException) {
				var msg = new StringBuilder("Entity is not valid.\n");
				(ex as DbEntityValidationException).EntityValidationErrors.SelectMany(err => err.ValidationErrors).Select(err => err.PropertyName + ": " + err.ErrorMessage).ForEach(err => msg.AppendLine(err));

				throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.InternalServerError)
				{
					Content = new StringContent(msg.ToString()),
					ReasonPhrase = "Validation of entity property values failed."
				});
			} else {
				throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.InternalServerError)
				{
					Content = new StringContent(ex.Message)
				});
			}
		}

		protected virtual async Task<HttpResponseMessage> PostEntity<T> (DbSet<T> entityset, T entity) where T : class
		{
			entityset.Add(entity);
			try { await db.SaveChangesAsync(); } catch (Exception ex) { HandleExceptions(ex); }
			return Request.CreateResponse(HttpStatusCode.Created, entity);
		}

		protected virtual async Task<T> UpdateEntity<T> (DbSet<T> entityset, T existing, T entity) where T : class
		{
			typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(prop => prop.CanRead && prop.CanWrite).ForEach(prop => {
				var val = prop.GetValue(entity);
				prop.SetValue(existing, val);
			});
			try { await db.SaveChangesAsync(); } catch (Exception ex) { HandleExceptions(ex); }
			return existing;
		}

		protected virtual async Task<T> PatchEntity<T> (DbSet<T> entityset, T existing, Delta<T> patch) where T : class
		{
			patch.Patch(existing);
			try { await db.SaveChangesAsync(); } catch (Exception ex) { HandleExceptions(ex); }
			return existing;
		}

		protected virtual async Task DeleteEntity<T> (DbSet<T> entityset, T entity) where T : class
		{
			entityset.Remove(entity);
			try { await db.SaveChangesAsync(); } catch (Exception ex) { HandleExceptions(ex); }
		}
	}

	public abstract class StandardODataController<C, T, K> : StandardODataControllerBase<C>
		where C : DbContext, new()
		where T : class
	{
		private DbSet<T> m_Entities = null;

		public StandardODataController ()
			: base()
		{
			var entitysetprop = typeof(C).GetProperties(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(prop => prop.PropertyType == typeof(DbSet<T>));
			if (entitysetprop == null) throw new HttpResponseException(HttpStatusCode.InternalServerError);

			var dbset = entitysetprop.GetValue(db) as DbSet<T>;
			if (dbset == null) throw new HttpResponseException(HttpStatusCode.InternalServerError);

			m_Entities = dbset as DbSet<T>;
		}

		protected virtual DbSet<T> Entities { get { return m_Entities; } }

		public virtual async Task<bool> EntityExists ([FromODataUri] K key)
		{
			return await Entities.FindAsync(key) != null;
		}

		[EnableQuery]
		public virtual IEnumerable<T> Get () { return Entities; }

		[EnableQuery]
		public virtual async Task<T> Get ([FromODataUri] K key)
		{
			var val = await Entities.FindAsync(key);
			if (val == null) throw new HttpResponseException(HttpStatusCode.NotFound);
			return val;
		}

		private static ConcurrentDictionary<string, MethodInfo> OfTypeMethods = new ConcurrentDictionary<string, MethodInfo>(StringComparer.InvariantCultureIgnoreCase);

		[EnableQuery]
		public virtual IEnumerable<T> GetType ([FromUri] string type)
		{
			// NOTE - Marking the parameter "type" with [FromODataUri] does not work -- type will be null

			// Cache the generic OfType method
			var ofType = OfTypeMethods.GetOrAdd(type, x => {
				// Search for the type in all loaded assemblies
				var t = Enumerable.Repeat(type, 1).Concat(AppDomain.CurrentDomain.GetAssemblies().Select(asm => type + ", " + asm.FullName)).Select(tx => {
					try { return Type.GetType(tx, true, true); } catch { return null; }
				}).FirstOrDefault(tx => tx != null);

				// Not found?
				if (t == null) {
					throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.BadRequest)
					{
						Content = new StringContent("Invalid entity type: " + type),
						ReasonPhrase = "Invalid entity type."
					});
				}

				// Type not subtype of T?
				if (!typeof(T).IsAssignableFrom(t)) throw new HttpResponseException(HttpStatusCode.NoContent);

				// Make a call to OfType with the correct generic type parameter
				return typeof(Queryable).GetMethod("OfType").MakeGenericMethod(t);
			});

			// Invoke the OfType call on the IQueryable
			return ofType.Invoke(null, new[] { Entities.AsQueryable() }) as IQueryable<T>;
		}

		// Use POST for both INSERT and UPDATE
		public virtual async Task<T> Post ([FromODataUri] K key, [FromBody] T entity) { return await UpdateEntity(Entities, await GetEntity(Entities, key), entity); }

		public virtual async Task<HttpResponseMessage> Post ([FromBody] T entity) { return await PostEntity(Entities, entity); }

		// PUT is used for incremental UPDATE
		//public virtual async Task<T> Put ([FromODataUri] K key, [FromBody] T entity) { return await UpdateEntity(Entities, await GetEntity(Entities, key), entity); }
		public virtual async Task<T> Put ([FromODataUri] K key, [FromBody] Delta<T> patch) { return await PatchEntity(Entities, await GetEntity(Entities, key), patch); }

		[AcceptVerbs("PATCH", "MERGE")]
		public virtual async Task<T> Patch ([FromODataUri] K key, [FromBody] Delta<T> patch) { return await PatchEntity(Entities, await GetEntity(Entities, key), patch); }

		public virtual async Task Delete ([FromODataUri] K key) { await DeleteEntity(Entities, await GetEntity(Entities, key)); }
	}
}
