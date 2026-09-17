using System;
using System.Collections.Generic;
using System.Data.SQLite;
using EZPos.Models.Domain;

namespace EZPos.DataAccess.Repositories
{
    /// <summary>Print history CRUD — logs every label print/export job for later review.</summary>
    public class BarcodeLabelRepository
    {
        private static BarcodeLabelRecord MapRow(SQLiteDataReader reader)
        {
            var formatStr = reader.IsDBNull(reader.GetOrdinal("BarcodeFormat"))
                ? "Code128"
                : reader.GetString(reader.GetOrdinal("BarcodeFormat"));
            var format = Enum.TryParse<BarcodeFormat>(formatStr, out var f) ? f : BarcodeFormat.Code128;

            return new BarcodeLabelRecord
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
                PrintedAt = DateTime.TryParse(reader.GetString(reader.GetOrdinal("PrintedAt")), out var dt) ? dt : DateTime.Now,
                Quantity = reader.GetInt32(reader.GetOrdinal("Quantity")),
                TemplateName = reader.GetString(reader.GetOrdinal("TemplateName")),
                BarcodeFormat = format
            };
        }

        /// <summary>Inserts one print history record.</summary>
        public void Insert(BarcodeLabelRecord record)
        {
            using var conn = Database.GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO BarcodeLabels (ProductId, PrintedAt, Quantity, TemplateName, BarcodeFormat)
                VALUES (@productId, @printedAt, @quantity, @templateName, @barcodeFormat)";
            cmd.Parameters.AddWithValue("@productId", record.ProductId);
            cmd.Parameters.AddWithValue("@printedAt", record.PrintedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@quantity", record.Quantity);
            cmd.Parameters.AddWithValue("@templateName", record.TemplateName);
            cmd.Parameters.AddWithValue("@barcodeFormat", record.BarcodeFormat.ToString());
            cmd.ExecuteNonQuery();
        }

        /// <summary>Most recent print history records, newest first.</summary>
        public List<BarcodeLabelRecord> GetRecent(int limit = 100)
        {
            var list = new List<BarcodeLabelRecord>();
            using var conn = Database.GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM BarcodeLabels ORDER BY Id DESC LIMIT @limit";
            cmd.Parameters.AddWithValue("@limit", limit);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(MapRow(reader));
            return list;
        }

        /// <summary>Print history for a single product, newest first.</summary>
        public List<BarcodeLabelRecord> GetByProduct(int productId)
        {
            var list = new List<BarcodeLabelRecord>();
            using var conn = Database.GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM BarcodeLabels WHERE ProductId = @productId ORDER BY Id DESC";
            cmd.Parameters.AddWithValue("@productId", productId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(MapRow(reader));
            return list;
        }
    }
}
