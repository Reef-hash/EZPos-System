using System.Collections.Generic;
using System.Data.SQLite;

namespace EZPos.DataAccess.Repositories
{
    public class CategoryRepository
    {
        /// <summary>Returns all category names sorted alphabetically.</summary>
        public List<string> GetAll()
        {
            var list = new List<string>();
            using var conn = Database.GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Name FROM Categories ORDER BY Name ASC";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(reader.GetString(0));
            return list;
        }

        /// <summary>
        /// Adds a new category. Returns false if the name already exists (case-insensitive).
        /// </summary>
        public bool Add(string name)
        {
            using var conn = Database.GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT OR IGNORE INTO Categories (Name) VALUES (@name)";
            cmd.Parameters.AddWithValue("@name", name.Trim());
            return cmd.ExecuteNonQuery() > 0;
        }

        /// <summary>
        /// Renames a category and updates all products that reference the old name.
        /// Returns false if the old name does not exist or new name already exists.
        /// </summary>
        public bool Rename(string oldName, string newName)
        {
            newName = newName.Trim();
            oldName = oldName.Trim();
            if (oldName == newName) return true;

            using var conn = Database.GetConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                var renameCmd = conn.CreateCommand();
                renameCmd.Transaction = tx;
                renameCmd.CommandText = "UPDATE Categories SET Name = @new WHERE Name = @old";
                renameCmd.Parameters.AddWithValue("@new", newName);
                renameCmd.Parameters.AddWithValue("@old", oldName);
                int rows = renameCmd.ExecuteNonQuery();
                if (rows == 0) { tx.Rollback(); return false; }

                // Keep Products in sync
                var updateProducts = conn.CreateCommand();
                updateProducts.Transaction = tx;
                updateProducts.CommandText = "UPDATE Products SET Category = @new WHERE Category = @old";
                updateProducts.Parameters.AddWithValue("@new", newName);
                updateProducts.Parameters.AddWithValue("@old", oldName);
                updateProducts.ExecuteNonQuery();

                tx.Commit();
                return true;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Deletes a category and reassigns all products in that category to "General".
        /// Refuses to delete "General" itself.
        /// </summary>
        public bool Delete(string name)
        {
            name = name.Trim();
            if (name == "General") return false;

            using var conn = Database.GetConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                // Reassign affected products to General
                var reassign = conn.CreateCommand();
                reassign.Transaction = tx;
                reassign.CommandText = "UPDATE Products SET Category = 'General' WHERE Category = @name";
                reassign.Parameters.AddWithValue("@name", name);
                reassign.ExecuteNonQuery();

                // Remove the category
                var delete = conn.CreateCommand();
                delete.Transaction = tx;
                delete.CommandText = "DELETE FROM Categories WHERE Name = @name";
                delete.Parameters.AddWithValue("@name", name);
                int rows = delete.ExecuteNonQuery();

                tx.Commit();
                return rows > 0;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        /// <summary>Returns how many products belong to this category.</summary>
        public int GetProductCount(string name)
        {
            using var conn = Database.GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Products WHERE Category = @name";
            cmd.Parameters.AddWithValue("@name", name);
            return (int)(long)cmd.ExecuteScalar()!;
        }
    }
}
