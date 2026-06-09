namespace Threadelle.ViewModels.Admin
{
    public class ColorListItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? HexCode { get; set; }
        public bool IsActive { get; set; }
        public int ProductCount { get; set; }
    }

    public class ColorUpsertViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? HexCode { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
