using System;

namespace npg.tomatoecs.Editor
{
	internal sealed class Entities : IDisposable
	{
		private Entity[] _entities;
		private uint _count;

		private uint[] _removedEntities;
		private int _removedEntitiesCount;

		internal int Capacity { get; private set; }

		internal Entity[] Raw => _entities;

		internal Entities(int capacity)
		{
			Capacity = capacity;
			_entities = new Entity[Capacity];
			_removedEntities = new uint[Capacity];
		}

		internal Entity CreateEntity(Context context)
		{
			var entityIndex = _count;
			if (_removedEntitiesCount > 0)
			{
				_removedEntitiesCount--;
				entityIndex = _removedEntities[_removedEntitiesCount];
			}

			if (_count == _entities.Length)
			{
				Resize();
			}

			_count++;

			ref var entity = ref _entities[entityIndex];
			entity.Context = context;
			entity.Id = entityIndex;
			return entity;
		}

		internal Entity GetEntity(uint entityId)
		{
			return _entities[entityId];
		}

		internal void RemoveEntity(uint entityId)
		{
			_count--;
			if (_removedEntitiesCount == _removedEntities.Length)
			{
				Array.Resize(ref _removedEntities, _removedEntitiesCount << 1);
			}

			_removedEntities[_removedEntitiesCount] = entityId;
			_removedEntitiesCount++;
			_entities[entityId] = default;
		}

		public void Dispose()
		{
			_entities.Clear();
			_count = 0;
			_removedEntities.Clear();
			_removedEntitiesCount = 0;
		}

		private void Resize()
		{
			Capacity <<= 1;
			Array.Resize(ref _entities, Capacity);
		}
	}
}