using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using EZPos.Models.Domain;

namespace EZPos.DataAccess.Repositories
{
    /// <summary>
    /// JSON-backed persistence for label templates. Stored in %ProgramData%\EZPos\label-templates.json
    /// (not SQLite — templates are user configuration, not transactional data).
    /// </summary>
    public class LabelTemplateRepository
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        private static string GetFilePath()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "EZPos");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return Path.Combine(dir, "label-templates.json");
        }

        /// <summary>Loads all templates. Seeds the four default templates on first run.</summary>
        public List<LabelTemplate> GetAll()
        {
            var path = GetFilePath();
            if (!File.Exists(path))
            {
                var seeded = CreateDefaultTemplates();
                SaveAll(seeded);
                return seeded;
            }

            try
            {
                var json = File.ReadAllText(path);
                var templates = JsonSerializer.Deserialize<List<LabelTemplate>>(json, JsonOptions);
                if (templates == null || templates.Count == 0)
                {
                    var seeded = CreateDefaultTemplates();
                    SaveAll(seeded);
                    return seeded;
                }
                return templates;
            }
            catch (JsonException)
            {
                // Corrupt file — reseed rather than crash the Barcodes page
                var seeded = CreateDefaultTemplates();
                SaveAll(seeded);
                return seeded;
            }
        }

        /// <summary>Returns the template marked IsDefault, or the first template if none is marked.</summary>
        public LabelTemplate GetDefault()
        {
            var all = GetAll();
            return all.FirstOrDefault(t => t.IsDefault) ?? all.First();
        }

        /// <summary>Upserts a template by Id and persists the full list.</summary>
        public void Save(LabelTemplate template)
        {
            var all = GetAll();
            var index = all.FindIndex(t => t.Id == template.Id);
            if (index >= 0)
                all[index] = template;
            else
                all.Add(template);

            SaveAll(all);
        }

        /// <summary>Removes a template by Id. Refuses to delete the last remaining template.</summary>
        public void Delete(string id)
        {
            var all = GetAll();
            if (all.Count <= 1)
                return;

            all.RemoveAll(t => t.Id == id);
            SaveAll(all);
        }

        private void SaveAll(List<LabelTemplate> templates)
        {
            var json = JsonSerializer.Serialize(templates, JsonOptions);
            File.WriteAllText(GetFilePath(), json);
        }

        private static List<LabelTemplate> CreateDefaultTemplates() => new()
        {
            new LabelTemplate
            {
                Name = "Standard 40x30", LabelWidthMm = 40, LabelHeightMm = 30,
                LabelsPerRow = 1, LabelsPerColumn = 1, IsDefault = true
            },
            new LabelTemplate
            {
                Name = "Shelf Label 100x50", LabelWidthMm = 100, LabelHeightMm = 50,
                LabelsPerRow = 1, LabelsPerColumn = 1
            },
            new LabelTemplate
            {
                Name = "Price Tag 58x40", LabelWidthMm = 58, LabelHeightMm = 40,
                LabelsPerRow = 1, LabelsPerColumn = 1
            },
            new LabelTemplate
            {
                Name = "A4 Sheet (24 labels)", LabelWidthMm = 70, LabelHeightMm = 37,
                LabelsPerRow = 4, LabelsPerColumn = 6
            }
        };
    }
}
