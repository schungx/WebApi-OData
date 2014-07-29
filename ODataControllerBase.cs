using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
using System.Web.Http.OData;

namespace WebApi.OData.Controllers
{
	// Standard OData controller with generic implementations

	public abstract class StandardODataControllerBase<Context> : ODataController where Context : DbContext, new()
	{
		private Context m_DB = null;

		protected Context db { get { if (m_DB == null) m_DB = new Context(); return m_DB; } }

		protected override void Dispose (bool disposing) { if (disposing && m_DB != null) m_DB.Dispose(); base.Dispose(disposing); }

		protected virtual async Task<EntityType> GetEntity<EntityType, KeyType> (DbSet<EntityType> entityset, KeyType key) where EntityType : class
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

		protected virtual async Task<HttpResponseMessage> PostEntity<EntityType> (DbSet<EntityType> entityset, EntityType entity) where EntityType : class
		{
			entityset.Add(entity);
			try { await db.SaveChangesAsync(); } catch (Exception ex) { HandleExceptions(ex); }
			return Request.CreateResponse(HttpStatusCode.Created, entity);
		}

		protected virtual async Task<EntityType> UpdateEntity<EntityType> (DbSet<EntityType> entityset, EntityType existing, EntityType entity) where EntityType : class
		{
			typeof(EntityType).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(prop => prop.CanRead && prop.CanWrite).ForEach(prop => {
				var val = prop.GetValue(entity);
				prop.SetValue(existing, val);
			});
			try { await db.SaveChangesAsync(); } catch (Exception ex) { HandleExceptions(ex); }
			return existing;
		}

		protected virtual async Task<EntityType> PatchEntity<EntityType> (DbSet<EntityType> entityset, EntityType existing, Delta<EntityType> patch) where EntityType : class
		{
			patch.Patch(existing);
			try { await db.SaveChangesAsync(); } catch (Exception ex) { HandleExceptions(ex); }
			return existing;
		}

		protected virtual async Task DeleteEntity<EntityType> (DbSet<EntityType> entityset, EntityType entity) where EntityType : class
		{
			entityset.Remove(entity);
			try { await db.SaveChangesAsync(); } catch (Exception ex) { HandleExceptions(ex); }
		}
	}

	public abstract class StandardODataController<Context, EntityType, KeyType> : StandardODataControllerBase<Context>
		where Context : DbContext, new()
		where EntityType : class
	{
		protected virtual DbSet<EntityType> Entities
		{
			get
			{
				var entitysetprop = typeof(Context).GetProperties(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(prop => prop.PropertyType == typeof(DbSet<EntityType>));
				if (entitysetprop == null) throw new HttpResponseException(HttpStatusCode.InternalServerError);

				var dbset = entitysetprop.GetValue(db) as DbSet<EntityType>;
				if (dbset == null) throw new HttpResponseException(HttpStatusCode.InternalServerError);

				return dbset as DbSet<EntityType>;
			}
		}

		public virtual async Task<bool> EntityExists ([FromODataUri] KeyType key)
		{
			return await Entities.FindAsync(key) != null;
		}

		[EnableQuery]
		public virtual IEnumerable<EntityType> Get () { return Entities; }

		[EnableQuery]
		public virtual async Task<EntityType> Get ([FromODataUri] KeyType key)
		{
			var val = await Entities.FindAsync(key);
			if (val == null) throw new HttpResponseException(HttpStatusCode.NotFound);
			return val;
		}

		public virtual async Task<EntityType> Post ([FromODataUri] KeyType key, [FromBody] EntityType entity) { return await UpdateEntity(Entities, await GetEntity(Entities, key), entity); }

		public virtual async Task<T> Put ([FromODataUri] K key, [FromBody] T entity) { return await UpdateEntity(Entities, await GetEntity(Entities, key), entity); }

		[AcceptVerbs("PATCH", "MERGE")]
		public virtual async Task<EntityType> Patch ([FromODataUri] KeyType key, [FromBody] Delta<EntityType> patch) { return await PatchEntity(Entities, await GetEntity(Entities, key), patch); }

		public virtual async Task Delete ([FromODataUri] KeyType key) { await DeleteEntity(Entities, await GetEntity(Entities, key)); }
	}
}
