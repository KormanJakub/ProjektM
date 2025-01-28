using System.Xml.Serialization;

namespace ProjektM.Models;

public class ProductXML
{
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string TagName { get; set; }
}