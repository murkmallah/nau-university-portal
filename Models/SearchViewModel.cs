namespace NAU_Web.Models
{
    public class SearchItemViewModel
    {
        public string Category { get; set; } = string.Empty; // "Courses", "Faculties", "Facilities", "Labs", "News", "Links"
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Badge { get; set; } = string.Empty;
        public string IconClass { get; set; } = "fas fa-search";
    }

    public class SearchResultViewModel
    {
        public string Query { get; set; } = string.Empty;
        public int TotalCount => Results.Count;
        public List<SearchItemViewModel> Results { get; set; } = new List<SearchItemViewModel>();
    }
}
