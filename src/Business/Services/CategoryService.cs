using System.Collections.Generic;
using EZPos.DataAccess.Repositories;

namespace EZPos.Business.Services
{
    /// <summary>
    /// Manages user-defined product categories.
    /// Wraps CategoryRepository and keeps an in-memory cache for the current session.
    /// </summary>
    public class CategoryService
    {
        private readonly CategoryRepository _repo;
        private List<string> _cache;

        public CategoryService()
        {
            _repo  = new CategoryRepository();
            _cache = _repo.GetAll();
        }

        /// <summary>Returns the current list of category names (from cache).</summary>
        public IReadOnlyList<string> GetAll() => _cache;

        /// <summary>Reloads the cache from the database.</summary>
        public void Reload() => _cache = _repo.GetAll();

        /// <summary>
        /// Adds a new category.
        /// Returns false if the name is blank or already exists (case-insensitive).
        /// </summary>
        public bool Add(string name)
        {
            name = name.Trim();
            if (string.IsNullOrWhiteSpace(name)) return false;
            bool ok = _repo.Add(name);
            if (ok) Reload();
            return ok;
        }

        /// <summary>
        /// Renames an existing category and updates all products that reference it.
        /// Returns false if the old name does not exist or new name already exists.
        /// </summary>
        public bool Rename(string oldName, string newName)
        {
            newName = newName.Trim();
            if (string.IsNullOrWhiteSpace(newName)) return false;
            bool ok = _repo.Rename(oldName, newName);
            if (ok) Reload();
            return ok;
        }

        /// <summary>
        /// Deletes a category and reassigns its products to "General".
        /// "General" cannot be deleted.
        /// </summary>
        public bool Delete(string name)
        {
            bool ok = _repo.Delete(name);
            if (ok) Reload();
            return ok;
        }

        /// <summary>Returns how many products belong to a given category.</summary>
        public int GetProductCount(string name) => _repo.GetProductCount(name);
    }
}
