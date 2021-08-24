namespace ElsaWinVHD.Model
{
    public class ElsaWin
    {
        public ElsaWin(int id, string name, string selectName, string rImage, string path = "")
        {
            Id = id;
            Name = name;
            SelectName = selectName;
            RImage = rImage;
            Path = path;
        }

        public int Id { get; }
        public string Name { get; }
        public string SelectName { get; }
        public string RImage { get; }
        public string Path { get; set; }
    }
}
