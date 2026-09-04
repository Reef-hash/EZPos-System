using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using EZPos.DataAccess.Repositories;
using EZPos.Models.Domain;

namespace EZPos.UI.ViewModels
{
    /// <summary>State + commands for LabelTemplateEditorDialog — create/edit/delete label templates.</summary>
    public sealed class LabelTemplateEditorViewModel : INotifyPropertyChanged
    {
        private readonly LabelTemplateRepository _repo;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action<string>? StatusMessage;

        public ObservableCollection<LabelTemplate> Templates { get; } = new();

        private LabelTemplate? selectedTemplate;
        public LabelTemplate? SelectedTemplate
        {
            get => selectedTemplate;
            set
            {
                if (SetProperty(ref selectedTemplate, value))
                {
                    DeleteCommand.RaiseCanExecuteChanged();
                    SaveCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public RelayCommand NewCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand DeleteCommand { get; }

        public LabelTemplateEditorViewModel(LabelTemplateRepository repo)
        {
            _repo = repo;

            foreach (var t in repo.GetAll())
                Templates.Add(t);
            selectedTemplate = Templates.FirstOrDefault();

            NewCommand = new RelayCommand(_ => NewTemplate());
            SaveCommand = new RelayCommand(_ => Save(), _ => SelectedTemplate != null);
            DeleteCommand = new RelayCommand(_ => Delete(), _ => SelectedTemplate != null && Templates.Count > 1);
        }

        private void NewTemplate()
        {
            var template = new LabelTemplate
            {
                Name = "New Template",
                LabelWidthMm = 40,
                LabelHeightMm = 30,
                LabelsPerRow = 1,
                LabelsPerColumn = 1,
                ShowBarcode = true,
                ShowName = true,
                ShowPrice = true
            };

            try
            {
                _repo.Save(template);
                Templates.Add(template);
                SelectedTemplate = template;
                StatusMessage?.Invoke("New template created — edit and save its details.");
            }
            catch (Exception ex)
            {
                StatusMessage?.Invoke($"Could not create template: {ex.Message}");
            }
        }

        private void Save()
        {
            var template = SelectedTemplate;
            if (template == null)
                return;

            if (string.IsNullOrWhiteSpace(template.Name))
            {
                StatusMessage?.Invoke("Template name is required.");
                return;
            }

            if (template.LabelWidthMm <= 0 || template.LabelHeightMm <= 0)
            {
                StatusMessage?.Invoke("Label width and height must be greater than zero.");
                return;
            }

            if (template.LabelsPerRow <= 0) template.LabelsPerRow = 1;
            if (template.LabelsPerColumn <= 0) template.LabelsPerColumn = 1;

            if (template.IsDefault)
            {
                foreach (var t in Templates)
                    if (!ReferenceEquals(t, template))
                        t.IsDefault = false;
            }

            try
            {
                foreach (var t in Templates)
                    _repo.Save(t);

                StatusMessage?.Invoke($"Template '{template.Name}' saved.");
            }
            catch (Exception ex)
            {
                StatusMessage?.Invoke($"Save failed: {ex.Message}");
            }
        }

        private void Delete()
        {
            var template = SelectedTemplate;
            if (template == null || Templates.Count <= 1)
                return;

            try
            {
                _repo.Delete(template.Id);
                Templates.Remove(template);
                SelectedTemplate = Templates.FirstOrDefault();
                StatusMessage?.Invoke("Template deleted.");
            }
            catch (Exception ex)
            {
                StatusMessage?.Invoke($"Delete failed: {ex.Message}");
            }
        }

        private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(storage, value))
                return false;

            storage = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
