using UnityEngine;

namespace Portfolio.Data
{
    public enum ProjectStatus
    {
        Concept,
        Prototype,
        InDevelopment,
        Published,
        Archived
    }

    [CreateAssetMenu(fileName = "ProjectEntry", menuName = "Portfolio/Project Entry")]
    public sealed class ProjectEntry : ScriptableObject
    {
        [SerializeField] private string projectTitle = "Untitled Project";
        [SerializeField, TextArea(2, 4)] private string shortDescription = "";
        [SerializeField, TextArea(4, 10)] private string longDescription = "";
        [SerializeField] private string videoUrl = "";
        [SerializeField] private string playableUrl = "";
        [SerializeField] private string githubUrl = "";
        [SerializeField] private string itchUrl = "";
        [SerializeField] private string downloadUrl = "";
        [SerializeField] private ProjectStatus status = ProjectStatus.Prototype;

        public string ProjectTitle => projectTitle;
        public string ShortDescription => shortDescription;
        public string LongDescription => longDescription;
        public string VideoUrl => videoUrl;
        public string PlayableUrl => playableUrl;
        public string GithubUrl => githubUrl;
        public string ItchUrl => itchUrl;
        public string DownloadUrl => downloadUrl;
        public ProjectStatus Status => status;
    }
}
