using System;
using System.Collections.Generic;
using LiteDB;
using RuriLib.Interfaces;
using RuriLib.Models;

namespace OpenBullet.Repositories;

public class LiteDBRepository<T> : IRepository<T, Guid> where T : Persistable<Guid>
{
	private LiteDatabase _db;

	private ILiteCollection<T> _coll;

	public string ConnectionString { get; set; }

	public string Collection { get; set; }

	public LiteDBRepository(string connectionString, string collection)
	{
		ConnectionString = connectionString;
		Collection = collection;
	}

	private LiteDatabase Connect()
	{
		_db = new LiteDatabase("filename=" + ConnectionString + "; Connection=Shared;", (BsonMapper)null);
		_coll = _db.GetCollection<T>(Collection, (BsonAutoId)10);
		return _db;
	}

	public void Disconnect(bool dispose = false)
	{
		if (dispose)
		{
			_db.Dispose();
		}
		_coll = null;
	}

	public void Add(T entity)
	{
		LiteDatabase val = Connect();
		try
		{
			_coll.Insert(entity);
			Disconnect();
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	public void Add(IEnumerable<T> entities)
	{
		LiteDatabase val = Connect();
		try
		{
			_coll.InsertBulk(entities, 5000);
			Disconnect();
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	public IEnumerable<T> Get()
	{
		LiteDatabase val = Connect();
		try
		{
			return _coll.FindAll();
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	public T Get(Guid id)
	{
		LiteDatabase val = Connect();
		try
		{
			T result = _coll.FindById((BsonValue)(id));
			Disconnect();
			return result;
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	public void Remove(T entity)
	{
		LiteDatabase val = Connect();
		try
		{
			_coll.Delete((BsonValue)(((Persistable<Guid>)entity).Id));
			Disconnect();
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	public void Remove(IEnumerable<T> entities)
	{
		LiteDatabase val = Connect();
		try
		{
			foreach (T entity in entities)
			{
				_coll.Delete((BsonValue)(((Persistable<Guid>)entity).Id));
			}
			Disconnect();
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	public void RemoveAll()
	{
		LiteDatabase val = Connect();
		try
		{
			val.DropCollection(Collection);
			Disconnect();
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	public void Update(T entity)
	{
		LiteDatabase val = Connect();
		try
		{
			_coll.Update(entity);
			Disconnect();
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	public void Update(IEnumerable<T> entities)
	{
		LiteDatabase val = Connect();
		try
		{
			_coll.Update(entities);
			Disconnect();
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}
}
