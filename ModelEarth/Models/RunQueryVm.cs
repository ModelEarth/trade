namespace ModelEarth.Models
{
    public class RunQueryVm
    {
        public string? ConnName { get; set; }
        public string? Query { get; set; }
        public System.Data.DataTable? Result { get; set; }
    }

}
