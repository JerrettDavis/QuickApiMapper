using System.Xml.Linq;
using Newtonsoft.Json.Linq;

namespace QuickApiMapper.Application.Writers;

public interface IDestinationWriter
{
    bool CanWrite(string? destPath);
    void Write(string? destPath, string? value, JObject? json, XDocument? xml);
}