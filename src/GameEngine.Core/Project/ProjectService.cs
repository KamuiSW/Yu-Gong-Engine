using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace GameEngine.Core.Project
{
    public class ProjectService
    {
        private const string RECENT_PROJECTS_FILE = "recent_projects.json";
        private const string PROJECT_FILE = "project.json";
        private static readonly string AppDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GameEngine"
        );

        public ProjectService()
        {
            Directory.CreateDirectory(AppDataPath);
        }

        public async Task<ProjectMetadata> CreateProjectAsync(ProjectMetadata metadata)
        {
            // Create project directory
            Directory.CreateDirectory(metadata.ProjectPath);

            // Create basic project structure
            var directories = new[]
            {
                Path.Combine(metadata.ProjectPath, "Assets"),
                Path.Combine(metadata.ProjectPath, "Assets", "Models"),
                Path.Combine(metadata.ProjectPath, "Assets", "Textures"),
                Path.Combine(metadata.ProjectPath, "Assets", "Materials"),
                Path.Combine(metadata.ProjectPath, "Assets", "Scripts"),
                Path.Combine(metadata.ProjectPath, "Scenes"),
                Path.Combine(metadata.ProjectPath, "Settings")
            };

            foreach (var dir in directories)
            {
                Directory.CreateDirectory(dir);
            }

            // Create default project settings
            var settingsPath = Path.Combine(metadata.ProjectPath, "Settings");
            var editorSettings = new Dictionary<string, object>
            {
                { "AutoSave", true },
                { "AutoRefresh", true }
            };

            await File.WriteAllTextAsync(
                Path.Combine(settingsPath, "EditorSettings.json"),
                JsonConvert.SerializeObject(editorSettings, Formatting.Indented)
            );

            // Create empty scene
            var scenesPath = Path.Combine(metadata.ProjectPath, "Scenes");
            var defaultScene = new Dictionary<string, object>
            {
                { "Name", "Default Scene" },
                { "Objects", new List<object>() }
            };

            await File.WriteAllTextAsync(
                Path.Combine(scenesPath, "DefaultScene.scene"),
                JsonConvert.SerializeObject(defaultScene, Formatting.Indented)
            );

            // Save project metadata
            string projectFile = Path.Combine(metadata.ProjectPath, PROJECT_FILE);
            await File.WriteAllTextAsync(projectFile, JsonConvert.SerializeObject(metadata, Formatting.Indented));

            // Add to recent projects
            await AddToRecentProjectsAsync(metadata);

            return metadata;
        }

        public async Task<List<ProjectMetadata>> LoadRecentProjectsAsync()
        {
            string recentProjectsFile = Path.Combine(AppDataPath, RECENT_PROJECTS_FILE);
            if (!File.Exists(recentProjectsFile))
            {
                return new List<ProjectMetadata>();
            }

            try
            {
                string json = await File.ReadAllTextAsync(recentProjectsFile);
                return JsonConvert.DeserializeObject<List<ProjectMetadata>>(json) ?? new List<ProjectMetadata>();
            }
            catch (Exception)
            {
                // If the file is corrupted, return empty list
                return new List<ProjectMetadata>();
            }
        }

        private async Task AddToRecentProjectsAsync(ProjectMetadata metadata)
        {
            var recentProjects = await LoadRecentProjectsAsync();
            
            // Remove if already exists
            recentProjects.RemoveAll(p => p.ProjectPath == metadata.ProjectPath);
            
            // Add to beginning of list
            recentProjects.Insert(0, metadata);
            
            // Keep only last 10 projects
            if (recentProjects.Count > 10)
            {
                recentProjects.RemoveRange(10, recentProjects.Count - 10);
            }

            string recentProjectsFile = Path.Combine(AppDataPath, RECENT_PROJECTS_FILE);
            await File.WriteAllTextAsync(recentProjectsFile, JsonConvert.SerializeObject(recentProjects, Formatting.Indented));
        }

        public async Task<ProjectMetadata?> LoadProjectAsync(string projectPath)
        {
            string projectFile = Path.Combine(projectPath, PROJECT_FILE);
            if (!File.Exists(projectFile))
            {
                return null;
            }

            try
            {
                string json = await File.ReadAllTextAsync(projectFile);
                var metadata = JsonConvert.DeserializeObject<ProjectMetadata>(json);
                
                if (metadata != null)
                {
                    await AddToRecentProjectsAsync(metadata);
                }

                return metadata;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
} 