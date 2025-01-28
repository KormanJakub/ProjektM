using System.Xml.Serialization;

namespace ProjektM.Models;

[XmlRoot("Products")]
public class ProductsXML
{
    [XmlElement("Product")]
    public List<ProductXML> Items { get; set; }
}