using System.Collections.Generic;

namespace RuriLib.Interfaces;

public interface IRepository<TEntity, in TId> where TEntity : class
{
	IEnumerable<TEntity> Get();

	TEntity Get(TId id);

	void Add(TEntity entity);

	void Add(IEnumerable<TEntity> entities);

	void Remove(TEntity entity);

	void Remove(IEnumerable<TEntity> entities);

	void RemoveAll();

	void Update(TEntity entity);

	void Update(IEnumerable<TEntity> entities);
}
